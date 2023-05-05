using System;
using System.IO;
using System.Text;
using Utils;
using UnityEngine.Networking;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using System.Security.Cryptography.X509Certificates;

namespace StudyStore
{
	public class OnlineAPI : API
	{
		const string PRODUCTION_HOST = "studystore.telecomnancy.univ-lorraine.fr";
		private string API_HOST = PRODUCTION_HOST;

		private X509Certificate2 certificate;
		private string apiToken;

		public OnlineAPI()
		{

		}


#if UNITY_EDITOR
		public OnlineAPI(string hostOverride, string certificatePath)
		{
			certificate = new X509Certificate2(JSONSerializer.Path(certificatePath));
			API_HOST = hostOverride;
		}
#endif

		private UploadHandler GetJsonUpload<JSONObj>(JSONObj jsonObj)
		{
			string json = JSONSerializer.ToJSON<JSONObj>(jsonObj);
			byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

			UploadHandler uploadHandler = new UploadHandlerRaw(jsonBytes);
			uploadHandler.contentType = "application/json";

			return uploadHandler;
		}

		private JSONObj GetDownloadJson<JSONObj>(DownloadHandler downloadHandler)
		{
			string json = Encoding.UTF8.GetString(downloadHandler.data);
			return JSONSerializer.FromJSON<JSONObj>(json);
		}

		private void NewRequest(string method, string uri, UploadHandler uploadHandler, Action<DownloadHandler> success, Action<APIError> error)
		{
			UnityWebRequest req = new UnityWebRequest("https://" + API_HOST + "/api/v1" + uri, method, new DownloadHandlerBuffer(), uploadHandler);

			if (this.certificate != null) req.certificateHandler = new APICertificateHandler(this.certificate);
			req.SetRequestHeader("Authorization", "Bearer " + apiToken);

			req.SendWebRequest().completed += (asyncOperation) =>
			{
				if (req.isNetworkError)
				{
					APIError apiError = new APIError();
					apiError.status = (int)req.responseCode;
					apiError.message = req.error;
#if UNITY_EDITOR
					Debug.LogError(req.url + " " + req.method + " \n" + apiError.FullError);
#endif
					//	Debug.LogError(Encoding.UTF8.GetString(req.downloadHandler.data));
					error(apiError);
				}
				else if (req.isHttpError)
				{
					string response = Encoding.UTF8.GetString(req.downloadHandler.data);

					try
					{
						APIError apiError = JSONSerializer.FromJSON<APIError>(response);
						error(apiError);
#if UNITY_EDITOR
						Debug.LogError(req.url + " " + req.method + " \n" + apiError.FullError);
#endif

						if (apiError.status == 401)
						{
							RaiseOnLoginExpired();
						}
						Debug.LogError(apiError.FullError);
					}
					catch (Exception e)
					{
						APIError apiError = new APIError() { detail = uri, message = response, exception = e };
						error(apiError);
#if UNITY_EDITOR
						Debug.LogError(req.url + " " + req.method + " \n" + apiError.FullError);
#endif
					}
				}
				else
				{
#if UNITY_EDITOR
					Debug.Log("Success: " + req.url + " " + req.method);
#endif
					success(req.downloadHandler);
				}
			};
		}

		private string GetStudiesUrl() { return "/studies"; }

		private string GetSubjectsUrl()
		{
			return GetStudiesUrl() + "/" + ((Study)CurrentStudy).uuid + "/subjects";
		}

		private string GetExperimentsUrl()
		{
			return GetSubjectsUrl() + "/" + ((Subject)CurrentSubject).uuid + "/experiments";
		}

		private string GetFilesUrl(Experiment experiment) { return GetExperimentsUrl() + "/" + experiment.uuid + "/files"; }

		[DataContract]
		private struct JSONLogin
		{
			[DataMember] public string email;
			[DataMember] public string password;
		}

		public override void Login(string login, string password, Action<User> success, Action<APIError> error)
		{
			JSONLogin jsonLogin;
			jsonLogin.email = login;
			jsonLogin.password = password;

			NewRequest(UnityWebRequest.kHttpVerbPOST, "/login", GetJsonUpload<JSONLogin>(jsonLogin), (downloadHandler) =>
			{
				AuthToken authToken = GetDownloadJson<AuthToken>(downloadHandler);

				if (authToken.user != null)
				{
					LoggedUser = authToken.user;
					apiToken = authToken.token;

					success((User)authToken.user);
				}
				else
				{
					error(new APIError() { status = 2, message = "$api:badLogin" });
				}
			}, error);
		}

		public override void Logout(Action success, Action<APIError> error)
		{
			NewRequest(UnityWebRequest.kHttpVerbPOST, "/logout", GetJsonUpload(new object()), (downloadHandler) =>
			{
				success();
			}, error);
		}

		public override void ListStudies(Func<string, bool> typeFilter, Action<StudyList> success, Action<APIError> error)
		{
			NewRequest(UnityWebRequest.kHttpVerbGET, GetStudiesUrl() + "?own=true", null, (downloadHandler) =>
			{
				StudyList studyList = GetDownloadJson<StudyList>(downloadHandler);

				StudyList filteredStudyList = studyList.Where((study) => typeFilter(study.type) && study.closed == null);
				success(filteredStudyList);
			}, error);
		}

		public override void SelectStudy(Study study, Action<Study> success, Action<APIError> error)
		{
			NewRequest(UnityWebRequest.kHttpVerbGET, GetStudiesUrl() + "/" + study.uuid, null, (downloadHandler) =>
			{
				Study obtainedStudy = GetDownloadJson<Study>(downloadHandler);

				if (obtainedStudy.closed == null)
				{
					CurrentStudy = obtainedStudy;
					success(obtainedStudy);
				}
				else
				{
					CurrentStudy = null;
					error(new APIError() { status = 1, message = "$api:studyIsClosed" });
				}
			}, error);
		}

		[DataContract]
		private struct JSONSubjectCreation
		{
			[DataMember] public string anonymizationId;
		}

		public override void CreateSubject(string anonymizationId, Action<Subject> success, Action<APIError> error)
		{
			if(!(this.LoggedUser?.IsExperimenter ?? false))
			{				
				error(new APIError() { message = "$api:unauthorized"});
				return;
			}

			JSONSubjectCreation jsonSubjectCreation;
			jsonSubjectCreation.anonymizationId = anonymizationId;

			NewRequest(UnityWebRequest.kHttpVerbPOST, GetSubjectsUrl(), GetJsonUpload<JSONSubjectCreation>(jsonSubjectCreation), (downloadHandler) =>
			{
				Subject subject = GetDownloadJson<Subject>(downloadHandler);

				CurrentSubject = subject;
				success(subject);
			}, error);
		}

		[DataContract]
		private struct JSONSubjects
		{
			[DataMember] public Subject[] subjects;
		}

		public override void SelectSubject(string anonymizationId, Action<Subject> success, Action<APIError> error)
		{ 
			if (!(this.LoggedUser?.IsExperimenter ?? false))
			{
				error(new APIError() { message = "$api:unauthorized" });
				return;
			} 

			NewRequest(UnityWebRequest.kHttpVerbGET, GetSubjectsUrl() + "?anonymizationId=" + anonymizationId, null, (downloadHandler) =>
			{
				JSONSubjects jsonSubjects = GetDownloadJson<JSONSubjects>(downloadHandler);
				Subject subject = jsonSubjects.subjects[0]; // pas besoin de tester si le tableau n'est pas vide car il y a forcément un résultat de l'api, sinon elle aurait renvoyé une erreur 404 traitée directement par NewRequest

				CurrentSubject = subject;
				success(subject);
			}, error);
		}

		public override void CreateRandomAnonymizationId(Action<string> success, Action<APIError> error)
		{
			string anonymizationId = "";

			for (int i = 0; i < 10; i++)
			{
				int ran = UnityEngine.Random.Range(0, 10) % 10;
				char chr = (char)('0' + ran);

				anonymizationId += chr;
			}

			success(anonymizationId);
		}

		[DataContract]
		private struct JSONExperiments
		{
			[DataMember] public JSONExperiment[] experiments;
		}

		[DataContract]
		private struct JSONExperiment
		{
			[DataMember] public String uuid;
			[DataMember] public String timestamp;
		}

		public override void GetExperiment(string uuid, Action<Experiment> success, Action<APIError> error)
		{
			NewRequest(UnityWebRequest.kHttpVerbGET, GetExperimentsUrl() + "/" + uuid, null, (downloadHandler) =>
			{
				Experiment experiment = GetDownloadJson<Experiment>(downloadHandler);

				success(experiment);
			}, error);
		}

		[DataContract]
		private struct JSONExperimentCreation
		{
			[DataMember] public string type;
		}

		public override void CreateExperiment(Action<Experiment> success, Action<APIError> error)
		{
			JSONExperimentCreation jsonExperimentCreation;
			jsonExperimentCreation.type = "un-type";

			NewRequest(UnityWebRequest.kHttpVerbPOST, GetExperimentsUrl(), GetJsonUpload<JSONExperimentCreation>(jsonExperimentCreation), (downloadHandler) =>
			{
				Experiment experiment = GetDownloadJson<Experiment>(downloadHandler);

				success(experiment);
			}, error);
		}

		public override void ListFiles(Experiment experiment, Action<ExpFileList> success, Action<APIError> error)
		{
			NewRequest(UnityWebRequest.kHttpVerbGET, GetFilesUrl(experiment), null, (downloadHandler) =>
			{
				ExpFileList fileList = GetDownloadJson<ExpFileList>(downloadHandler);

				for (int i = 0; i < fileList.Length; i++)
				{
					fileList.files[i].origin = experiment;
				}

				success(fileList);
			}, error);
		}

		public override void ListExperiments(Action<ExperimentList> success, Action<APIError> error)
		{
			NewRequest(UnityWebRequest.kHttpVerbGET, GetExperimentsUrl(), null, (downloadHandler) =>
			{
				ExperimentList expList = GetDownloadJson<ExperimentList>(downloadHandler);
				success(expList);
			}, error);
		}

		public override async void UploadFile(Experiment experiment, string filename, StreamReader fileReader, Action success, Action<APIError> error)
		{
			Task<String> task = fileReader.ReadToEndAsync();
			await task;
			fileReader.Close();

			if (task.IsFaulted)
			{
				APIError apiError = new APIError();
				apiError.status = 1;
				apiError.message = Localization.Format("$api:cantReadFile::1", filename);
				apiError.exception = task.Exception;

				error(apiError);
			}
			else
			{
				byte[] fileBuffer = Encoding.UTF8.GetBytes(task.Result);
				NewRequest(UnityWebRequest.kHttpVerbPUT, GetFilesUrl(experiment) + "/" + filename, new UploadHandlerRaw(fileBuffer), (downloadHandler) => success(), error);
			}
		}

		public override void ReadFile(ExpFile file, Action<StreamReader> success, Action<APIError> error)
		{
			NewRequest(UnityWebRequest.kHttpVerbGET, GetFilesUrl(file.origin) + "/" + file.filename, null, (downloadHandler) =>
			{
				Stream stream = new MemoryStream(downloadHandler.data);
				success(new StreamReader(stream));
			}, error);
		}
	}
}