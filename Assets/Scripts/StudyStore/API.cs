using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TestSuite;
using UnityEngine;
using Utils;

namespace StudyStore
{
	public abstract class API
	{
		static API()
		{
			Instance = new OnlineAPI();
		}

		public static API Instance { get; set; }

		public event Action onLoginExpired;

		protected void RaiseOnLoginExpired()
		{
			onLoginExpired?.Invoke();
		}

		public User? LoggedUser { get; protected set; }
		public Subject? CurrentSubject { get; protected set; }
		public Study? CurrentStudy { get; protected set; }

		/// <summary>
		/// Login to the API
		/// </summary>
		/// <param name="login">The account ID (email)</param>
		/// <param name="password">The password</param>
		/// <param name="success">Called upon successful login</param>
		/// <param name="error">Called if the login failed</param>
		public abstract void Login(string login, string password, Action<User> success, Action<APIError> error);

		/// <summary>
		/// Logs out from the API
		/// </summary>
		/// <param name="success">Called upon successful logout</param>
		/// <param name="error">Called if the logout failed</param>
		public abstract void Logout(Action success, Action<APIError> error);

		/// <summary>
		/// Creates a new subject in the current study from an anonymization ID
		/// </summary>
		/// <param name="anonymizationId">A unique anonymization ID for the new subject</param>
		/// <param name="success">Called upon successful creation of the subject</param>
		/// <param name="error">Called if the creation of the subject fails</param>
		public abstract void CreateSubject(string anonymizationId, Action<Subject> success, Action<APIError> error);

		/// <summary>
		/// Retrieves and selects a subject in the current study from its anonymization ID
		/// </summary>
		/// <param name="anonymizationId">The unique anonymization ID of the subject</param>
		/// <param name="success">Called upon successful retrieval of the subject</param>
		/// <param name="error">Called if the retrieval of the subject fails</param>
		public abstract void SelectSubject(string anonymizationId, Action<Subject> success, Action<APIError> error);

		/// <summary>
		/// Generates a random anonymization Id, verifying that it is currently unused. Does not create the subject
		/// </summary>
		/// <param name="success">Called upon successful generation of an ID</param>
		/// <param name="error">Called if the generation failed</param>
		public abstract void CreateRandomAnonymizationId(Action<string> success, Action<APIError> error);

		/// <summary>
		/// Lists the open studies in which the current user participates, of a study type that fits the criteria determined by the <code>typeFilter</code> function.
		/// </summary>
		/// <param name="typeFilter">A function that return trues if and only if the argument string is a desired study type</param>
		/// <param name="success">Called upon retrieval of the study list, the filtered study list will be passed to this callback function</param>
		/// <param name="error">Called if the retrieval failed</param>
		public abstract void ListStudies(Func<string, bool> typeFilter, Action<StudyList> success, Action<APIError> error);

		/// <summary>
		/// Lists the open studies in which the current user participates that are supported by this application
		/// </summary>
		/// <param name="success">Called upon retrieval of the study list, the filtered study list will be passed to this callback function</param>
		/// <param name="error">Called if the retrieval failed</param>
		public void ListStudies(Action<StudyList> success, Action<APIError> error)
		{
			ListStudies(Config.IsSupportedStudyType, success, error);
		}

		/// <summary>
		/// Sets the argument study as the active study after verifying that it exists and is open, allowing creating/selection patients in that study.
		/// </summary>
		/// <param name="study">The study to set as the active study</param>
		/// <param name="success">Called upon retrieval of the study, if it exists and is open</param>
		/// <param name="error">Called if the retrieval failed, the study is closed or does not exist anymore</param>
		public abstract void SelectStudy(Study study, Action<Study> success, Action<APIError> error);

		/// <summary>
		/// Retrieves an experiment in the currently selected study from its uuid
		/// </summary>
		/// <param name="uuid">The uuid of the experiment to retrieve</param>
		/// <param name="success">Called upon retrieval of the experiment</param>
		/// <param name="error">Called if the retrieval failed</param>
		public abstract void GetExperiment(string uuid, Action<Experiment> success, Action<APIError> error);

		/// <summary>
		/// Retrieves all experiments of a subject in the currently selected study
		/// </summary> 
		/// <param name="success">Called upon retrieval of the experiments</param>
		/// <param name="error">Called if the retrieval failed</param>
		public abstract void ListExperiments(Action<ExperimentList> success, Action<APIError> error);

		/// <summary>
		/// Creates an experiment in the currently selected study
		/// </summary> 
		/// <param name="success">Called upon creation of the experiment</param>
		/// <param name="error">Called if the creation failed</param>
		public abstract void CreateExperiment(Action<Experiment> success, Action<APIError> error);

		/// <summary>
		/// Creates an experiment in the currently selected study
		/// </summary> 
		/// <param name="success">Called upon creation of the experiment, providing an ExperimentIndex object representing it</param>
		/// <param name="error">Called if the creation failed</param>
		public void CreateExperimentIndex(Action<Experiment, ExperimentIndex> success, Action<APIError> error)
		{
			var eIndex = new ExperimentIndex() { studyId = this.CurrentStudy?.uuid, subjectId = this.CurrentSubject?.uuid };
			CreateExperiment(
				exp =>
				{
					eIndex.experimentId = exp.uuid;
					success(exp, eIndex);
				},
				error);
		}

		/// <summary>
		/// Lists the uploaded files associated to the argument experiment
		/// </summary>
		/// <param name="experiment">The experiment for which to retrieve the list of uploaded files</param>
		/// <param name="success">Called upon retrieval of the list of files</param>
		/// <param name="error">Called if the retrieval failed</param>
		public abstract void ListFiles(Experiment experiment, Action<ExpFileList> success, Action<APIError> error);

		/// <summary>
		/// Uploads a file for the argument experiment
		/// </summary>
		/// <param name="experiment">The experiment to which the file should be associated</param>
		/// <param name="filename">A name identifying the file. If a file with the same name exists in that experiment, it will get overwritten</param>
		/// <param name="fileReader">A stream reader to read the file contents</param>
		/// <param name="success">Called upon successful upload</param>
		/// <param name="error">Called if the upload failed</param>
		public abstract void UploadFile(Experiment experiment, string filename, StreamReader fileReader, Action success, Action<APIError> error);

		/// <summary>
		/// Reads an experiment file
		/// </summary>
		/// <param name="file">Handle for the experiment file</param> 
		/// <param name="success">Called upon successful retrieval of the file</param>
		/// <param name="error">Called if the retrieval failed</param>
		public abstract void ReadFile(ExpFile file, Action<StreamReader> success, Action<APIError> error);

		/// <summary>
		/// Reads an experiment file
		/// </summary>
		/// <param name="eIndex">Experiment index with the id of the experiment</param>
		/// <param name="filename">Handle for the experiment file</param>
		/// <param name="success">Called upon successful retrieval of the file</param>
		/// <param name="error">Called if the retrieval failed</param>
		public void ReadFile(ExperimentIndex eIndex, string filename, Action<StreamReader> success, Action<APIError> error)
		{
			Experiment exp = new Experiment() { uuid = eIndex.experimentId };
			ExpFile file = new ExpFile() { origin = exp, filename = filename };

			ReadFile(file, success, error);
		}

		/// <summary>
		/// Delays execution of a callback
		/// </summary>
		/// <param name="time">Delay time, in seconds</param>
		/// <param name="callback">Callback to execute after the given delay</param>
		protected async void Delay(float time, Action callback)
		{
			await Task.Delay(TimeSpan.FromSeconds(time));
			callback();
		}

		/// <summary>
		/// Delete all the local files of an experiment, given an ExperimentIndex
		/// </summary>
		/// <param name="index">The experiment index, indexing all mocap/indicator files for a given experiment</param>
		public void DeleteLocalFiles(ExperimentIndex index)
		{
			var patientDirectory = Directory.GetParent(JSONSerializer.Path(index.RootDirectory));

			// delete experiment directory
			Directory.Delete(JSONSerializer.Path(index.RootDirectory), recursive: true);

			// delete patient directory if it has no more subfolders
			try
			{
				patientDirectory.Delete(recursive: false);
			}
			catch (IOException)
			{
				// do nothing, this exception means the patient folder couldn't be deleted as it wasn't empty, which is the desired behaviour
			}
		}

		/// <summary>
		/// Upload all of the files of an experiment, given an ExperimentIndex
		/// </summary>
		/// <param name="index">The experiment index, indexing all mocap/indicator files for a given experiment</param>
		/// <param name="progress">Callback to track progress (parameters: current file index, total files, progression message)</param>
		/// <param name="done">Called when all files are uploaded</param>
		/// <param name="errors">Called when an error occurs (parameters: consecutive upload failures, error)</param>
		/// <param name="failure">Called when the process failed</param>
		/// <param name="deleteFilesOnSuccess">If true, local files will be deleted when upload is done</param>
		public void UploadAll(ExperimentIndex index, Action<int, int, string> progress, Action done, Action<int, APIError> errors, Action failure, bool deleteFilesOnSuccess = false, Stream log = null)
		{
			var indexCopy = index.Clone();

			Experiment? experiment = null;
			APIError? error = null;
			int tries = 0;

			Action callback = null;

			int uploadIndex = -1;
			float lastQuery = -1000;

			callback = () =>
			{
				if (error != null) errors(tries, (APIError)error);
				error = null;

				if (experiment != null && uploadIndex >= indexCopy.contents.Count * 2) // end condition
				{
					if (log != null)
					{
						UploadFile((Experiment)experiment, "log.txt", new StreamReader(log), () => { log = null;  tries = 0; callback(); }, (err) => { error = err; tries++; callback(); });
					}
					else
					{
						done();
						if (deleteFilesOnSuccess) DeleteLocalFiles(indexCopy);
					}
				}
				else if (Time.time < lastQuery + .25f) // ensure that no more than 4 files are sent every second to avoid getting rejected by the server
				{
					Debug.Log("delay " + Time.time + " " + lastQuery);
					Delay(.25f, callback);
				}
				else if (tries >= 5) // failure condition (5 failed uploads in a row)
				{
					failure();
				}
				else if (experiment == null) // obtain experiment object from uuid
				{
					lastQuery = Time.time;

					progress(0, 1 + indexCopy.contents.Count * 2, "$api:preparingUpload");
					GetExperiment(indexCopy.experimentId, (e) => { experiment = e; tries = 0; callback(); }, (err) => { error = err; tries++; callback(); });
				}
				else if (uploadIndex < indexCopy.contents.Count * 2) // upload each file
				{
					lastQuery = Time.time;

					StreamReader stream = null;
					string filename;

					if (uploadIndex == -1)
					{
						filename = "index.json";
						stream = new StreamReader(JSONSerializer.ToJSONStream(indexCopy));
					}
					else
					{
						if (indexCopy.contents[uploadIndex / 2] == null)
						{
							uploadIndex++;
							Delay(.01f, callback);
							return;
						}

						filename = (uploadIndex % 2 == 0) ? indexCopy.contents[uploadIndex / 2].indicatorsFile : indexCopy.contents[uploadIndex / 2].mocapFile;

						if (filename != null) // sometimes there is no file to be read (for instance, no mocap data for forms)
						{
							if (!JSONSerializer.FileExists(indexCopy.RootDirectory + filename))
							{
								errors(1, new APIError() { status = 1, message = Localization.Format("$api:cantReadFile::1", filename) });
								failure();
								return;
							}

							stream = JSONSerializer.FileReader(indexCopy.RootDirectory + filename);
							filename = filename.Replace(" ", "_");
						}
					}

					progress(uploadIndex + 2, 1 + indexCopy.contents.Count * 2, "$api:uploadingFile");

					if (stream != null)
					{
						UploadFile((Experiment)experiment, filename, stream, () => { uploadIndex++; tries = 0; callback(); }, (err) => { error = err; tries++; callback(); });
					}
					else
					{
						uploadIndex++;
						callback();
					}
				}
			};

			callback();
		}

		/// <summary>
		/// Read an experiment json file and unserialize it into an object
		/// </summary>
		/// <typeparam name="T">Type of the object to unserialze into</typeparam> 
		/// <param name="experiment">Experiment </param>
		/// <param name="filename">Name of the experiment file</param>
		/// <param name="success">Called upon successful retrieval of the file</param>
		/// <param name="error">Called if the retrieval failed</param>
		public void UnserializeFile<T>(Experiment experiment, string filename, Action<T> success, Action<APIError> error)
		{ 
			ExpFile file = new ExpFile() { origin = experiment, filename = filename };

			UnserializeFile(file, success, error);
		}

		/// <summary>
		/// Read an experiment json file and unserialize it into an object
		/// </summary>
		/// <typeparam name="T">Type of the object to unserialze into</typeparam> 
		/// <param name="eIndex">Experiment index with the id of the experiment</param>
		/// <param name="filename">Name of the experiment file</param>
		/// <param name="success">Called upon successful retrieval of the file</param>
		/// <param name="error">Called if the retrieval failed</param>
		public void UnserializeFile<T>(ExperimentIndex eIndex, string filename, Action<T> success, Action<APIError> error)
		{
			Experiment exp = new Experiment() { uuid = eIndex.experimentId };
			ExpFile file = new ExpFile() { origin = exp, filename = filename };

			UnserializeFile(file, success, error);
		}

		/// <summary>
		/// Read an experiment json file and unserialize it into an object
		/// </summary>
		/// <typeparam name="T">Type of the object to unserialze into</typeparam> 
		/// <param name="file">Handle for the experiment file</param> 
		/// <param name="success">Called upon successful retrieval of the file</param>
		/// <param name="error">Called if the retrieval failed</param>
		public void UnserializeFile<T>(ExpFile file, Action<T> success, Action<APIError> error)
		{
			ReadFile(file,
				streamReader =>
				{
					try
					{
						T @object = JSONSerializer.FromJSON<T>(streamReader.BaseStream);
						success(@object);
					}
					catch (Exception e)
					{
						error(new APIError() { exception = e });
					}

					streamReader.Close();
				},
				err => error(err));
		}
	}
}