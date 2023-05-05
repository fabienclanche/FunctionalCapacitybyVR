using Utils;
using System.Runtime.Serialization;
using UnityEngine;
using System;
using TestSuite;

[DataContract]
public class Config
{

	// static members
	public static string OutputDirectory => JSONSerializer.Path(config.output_directory);
	public static StudyConfig OfflineStudyConfig => config.default_study;

	private static string[] StudyTypes = { "solution-3" };

	public static bool IsSupportedStudyType(string type)
	{
		for (int i = 0; i < StudyTypes.Length; i++) if (StudyTypes[i] == type) return true;

		return false;
	}

	private static Config config;

	// instance members
	[DataMember] string output_directory;
	[DataMember] StudyConfig default_study;

	static Config()
	{
		try
		{
			config = JSONSerializer.FromJSONFile<Config>("./config.json");
			Debug.Log("Read Config");
		}
		catch (Exception e)
		{
			config = new Config();
			config.output_directory = "~\\Sol3Data\\";

			Debug.LogException(e);
		}
	}

	private Config()
	{

	}
}