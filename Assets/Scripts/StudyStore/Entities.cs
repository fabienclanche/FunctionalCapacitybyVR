
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TestSuite;
using Utils;

/// A collection of structs to serialize/deserialize JSON objects exchanged with the API Server	
/// 
namespace StudyStore
{	
	[DataContract]
	public struct AuthToken
	{
		[DataMember(IsRequired = true)] public string token;
		[DataMember] public User? user;
	}

	[DataContract]
	public struct APIError
	{
		[DataMember(IsRequired = true)] public int status;
		[DataMember] public string title;
		[DataMember] public string detail;

		/// <summary>
		/// An error messsage that can be added by this application to provide additional detais about this error
		/// </summary>
		public string message;

		/// <summary>
		/// If applicable, the exception that prevented the API query to terminate
		/// </summary>
		public Exception exception;

		public string FullError => JSONSerializer.ToJSON(this) + " " + message + ((exception != null) ? ("\n exceptionMessage:" + exception.Message + "\n stackTrace:\n" + exception?.StackTrace) : "");

		public string MessageToDisplay => status != 0 ?
			Localization.Format("$api:error:statusAndMessage::2", status + "", message == null ? title + "\n" + detail : message)
			: exception == null ? message : message + " (" + exception.GetType().Name + ": " + exception.Message + ")";
	}

	[DataContract]
	public struct User
	{
		[DataMember(IsRequired = true)] public string uuid;
		[DataMember] public string name;
		[DataMember] public string email;
		[DataMember] bool admin, responsible, physician, experimenter, analyst;

		public string Role => admin ? "admin" : responsible ? "responsible" : physician ? "physician" : experimenter ? "experimenter" : analyst ? "analyst" : null;

		/// <summary>
		/// True iff this user has the role Responsible; Physician or Experimenter
		/// The online API prevents users without those roles to create or access patients & experiments in a study
		/// </summary>
		public bool IsExperimenter => responsible || physician || experimenter;
	}

	[DataContract]
	public struct Study
	{
		[DataMember(IsRequired = true)] public string uuid;
		[DataMember] public string title;
		[DataMember] public string description;
		[DataMember] public string type;
		[DataMember] public string opened, closed;
		[DataMember] public StudyConfig configuration;

		public override bool Equals(object obj)
		{
			if (obj != null && obj is Study) return this.uuid == ((Study)obj).uuid;
			else return false;
		}

		public override int GetHashCode()
		{
			return uuid.GetHashCode();
		}
	}

	[DataContract]
	public struct StudyList : IEnumerable<Study>
	{
		[DataMember(IsRequired = true)] internal Study[] studies;

		public Study this[int i] => studies[i];
		public int Length => studies.Length;

		public IEnumerator<Study> GetEnumerator()
		{
			foreach (var study in studies) yield return study;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public StudyList Where(Func<Study, bool> condition)
		{
			StudyList filtered = new StudyList();

			filtered.studies = studies.Where(condition).ToArray();

			return filtered;
		}
	}

	[DataContract]
	public struct Subject
	{
		[DataMember(IsRequired = true)] public string uuid;
		[DataMember] public string anonymizationId;
	}

	[DataContract]
	public struct Experiment
	{
		[DataMember(IsRequired = true)] public string uuid;
		[DataMember] public string timestamp;
	}

	[DataContract]
	public struct ExpFile
	{
		[DataMember(IsRequired = true)] public string filename;
		[DataMember] public string created, updated;
		internal Experiment origin;
	}

	[DataContract]
	public struct ExperimentList : IEnumerable<Experiment>
	{
		[DataMember(IsRequired = true)] internal Experiment[] experiments;

		public Experiment this[int i] => experiments[i];
		public int Length => experiments.Length;

		public IEnumerator<Experiment> GetEnumerator()
		{
			foreach (var study in experiments) yield return study;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	[DataContract]
	public struct ExpFileList : IEnumerable<ExpFile>
	{
		[DataMember(IsRequired = true)] internal ExpFile[] files;

		public ExpFile this[int i] => files[i];
		public int Length => files.Length;

		public IEnumerator<ExpFile> GetEnumerator()
		{
			foreach (var study in files) yield return study;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}