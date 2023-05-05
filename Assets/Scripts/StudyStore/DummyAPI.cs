using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TestSuite;
using UnityEngine;
using Utils;

namespace StudyStore
{
	public class DummyAPI : API
	{ 
		public override void Login(string login, string password, Action<User> success, Action<APIError> error)
		{
			var user = new User();
			user.uuid = "local-user";
			user.email = "local-user";
			user.name = Localization.LocalizeDefault("$api:localUser");

			Delay(.3f, () =>
			{
				LoggedUser = user;
				success(user);
			});
		}

		public override void Logout(Action success, Action<APIError> error)
		{
			LoggedUser = null;

			success();
		}

		public override void CreateSubject(string anonymizationId, Action<Subject> success, Action<APIError> error)
		{
			foreach (char c in Path.GetInvalidFileNameChars())
			{
				if (anonymizationId.Contains(c))
				{
					error(new APIError() { message = Localization.Format("$api:error:invalidCharacter::1", c + "") });
					return;
				}
			}

			var subject = new Subject();
			subject.uuid = anonymizationId;
			subject.anonymizationId = anonymizationId;

			string dir = Config.OutputDirectory + "/OfflineData/" + anonymizationId + "/";

			if (Directory.Exists(dir))
			{
				error(new APIError() { status = 400, message = Localization.Format("$api:error:subjectAlreadyExists::1", anonymizationId) });
				return;
			}

			try
			{
				Directory.CreateDirectory(dir);

				CurrentSubject = subject;
				success(subject);
			}
			catch (Exception e)
			{
				error(new APIError() { exception = e });
			}
		}

		public override void SelectSubject(string anonymizationId, Action<Subject> success, Action<APIError> error)
		{
			foreach (char c in Path.GetInvalidFileNameChars())
			{
				if (anonymizationId.Contains(c))
				{
					error(new APIError() { message = Localization.Format("$api:error:invalidCharacter::1", c + "") });
					return;
				}
			}

			string dir = Config.OutputDirectory + "/OfflineData/" + anonymizationId + "/";

			if (Directory.Exists(dir))
			{
				var subject = new Subject();
				subject.uuid = anonymizationId;
				subject.anonymizationId = anonymizationId;

				CurrentSubject = subject;

				success(subject);
			}
			else
			{
				var apiError = new APIError();
				apiError.status = 404;
				apiError.message = Localization.Format("$api:error:subjectDoesNotExist::1", anonymizationId);

				error(apiError);
			}
		}

		public override void CreateRandomAnonymizationId(Action<string> success, Action<APIError> error)
		{
			string anonymizationId = "";

			int id_len = 10;

			do
			{
				for (int i = 0; i < id_len; i++)
				{
					int ran = UnityEngine.Random.Range(0, 10);
					char chr = (char)('0' + ran);

					anonymizationId += chr;
				}

				id_len++;

			} while (Directory.Exists(Config.OutputDirectory + "/OfflineData/" + anonymizationId + "/"));

			success(anonymizationId);
		}

		public static Study OfflineStudy => new Study()
		{
			uuid = "0000000",
			title = Localization.Format("$api:offlineStudy"),
			description = Localization.Format("$api:offlineStudyDescription"),
			configuration = Config.OfflineStudyConfig
		};

		public override void ListStudies(Func<string, bool> typeFilter, Action<StudyList> success, Action<APIError> error)
		{
			var studyList = new StudyList();
			studyList.studies = new Study[] { OfflineStudy };

			success(studyList);
		}

		public override void SelectStudy(Study study, Action<Study> success, Action<APIError> error)
		{
			if (study.uuid == OfflineStudy.uuid)
			{
				CurrentStudy = study;
				success(OfflineStudy);
			}
			else error(new APIError());
		}

		public override void GetExperiment(string uuid, Action<Experiment> success, Action<APIError> error)
		{
			string dir = Config.OutputDirectory + "/OfflineData/" + CurrentSubject?.anonymizationId + "/" + uuid + "/";

			if (Directory.Exists(dir))
			{
				var experiment = new Experiment()
				{
					uuid = uuid,
					timestamp = DateToString(Directory.GetCreationTime(dir))
				};
				success(experiment);
			}
			else
			{
				error(new APIError());
			}
		}

		private string DateToString(DateTime dateTime)
		{
			return Localization.Format("$unit:YMDdate", dateTime.Year + "", dateTime.Month + "", dateTime.Day + "");
		}

		public override void CreateExperiment(Action<Experiment> success, Action<APIError> error)
		{
			var now = DateTime.Now;
			var experiment = new Experiment()
			{
				uuid = "exp-" + now.Year + "-" + now.Month + "-" + now.Day + "-" + now.Hour + "-" + now.Minute,
				timestamp = DateToString(now)
			};
			
			string dir = Config.OutputDirectory + "/OfflineData/" + CurrentSubject?.anonymizationId + "/" + experiment.uuid + "/";

			if (Directory.Exists(dir))
			{
				error(new APIError());
			}
			else
			{
				Directory.CreateDirectory(dir);
				success(experiment);
			}
		}
		 
		public override void ListExperiments(Action<ExperimentList> success, Action<APIError> error)
		{
			string dir = Config.OutputDirectory + "/OfflineData/" + CurrentSubject?.anonymizationId + "/" ;

			var expList = new ExperimentList();

			if (Directory.Exists(dir))
			{
				expList.experiments = Directory.GetDirectories(dir).Select(filepath => new Experiment()
				{
					uuid = new DirectoryInfo(filepath).Name,
					timestamp = DateToString(new DirectoryInfo(filepath).CreationTime)
				}).ToArray();
			}
			else
			{
				expList.experiments = new Experiment[] { };
			}

			success(expList);
		}

		public override void ListFiles(Experiment experiment, Action<ExpFileList> success, Action<APIError> error)
		{
			string dir = Config.OutputDirectory + "/OfflineData/" + CurrentSubject?.anonymizationId + "/" + experiment.uuid + "/";

			var fileList = new ExpFileList();

			if (Directory.Exists(dir))
			{
				fileList.files = Directory.GetFiles(dir).Select(filepath => new ExpFile()
				{
					filename = Path.GetFileName(filepath),
					created = File.GetCreationTime(filepath).ToLongTimeString(),
					updated = File.GetCreationTime(filepath).ToLongTimeString(),
					origin = experiment
				}).ToArray();
			}
			else
			{
				fileList.files = new ExpFile[] { };
			}

			success(fileList);
		}

		public override void UploadFile(Experiment experiment, string filename, StreamReader fileReader, Action success, Action<APIError> error)
		{
			try
			{
				using (var writer = JSONSerializer.FileWriter(Config.OutputDirectory + "/OfflineData/" + CurrentSubject?.anonymizationId + "/" + experiment.uuid + "/" + filename))
				{
					string line = null;

					while ((line = fileReader.ReadLine()) != null)
					{
						writer.WriteLine(line);
					}

					fileReader.Close();
					success();
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e);

				error(new APIError()
				{
					exception = e
				});
			}
			finally
			{
				fileReader.Close();
			}
		}

		public override void ReadFile(ExpFile file, Action<StreamReader> success, Action<APIError> error)
		{
			try
			{
				string filepath = Config.OutputDirectory + "/OfflineData/" + (CurrentSubject?.anonymizationId) + "/" + file.origin.uuid + "/" + file.filename;
				Debug.Log(Config.OutputDirectory);
				Debug.Log(filepath);
				Debug.Log(CurrentSubject?.anonymizationId);
				Debug.Log(file.origin.uuid);
				var reader = JSONSerializer.FileReader(filepath);

				success(reader);
			}
			catch (Exception e)
			{
				Debug.LogError(e);

				error(new APIError()
				{
					exception = e					
				});
			}
		}
	}
}