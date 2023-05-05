using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Utils;
using System.Runtime.Serialization;
using System;
using FullBodyTracking.Mocap;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TestSuite
{
	[DataContract, Serializable]
	public class TestMetadata
	{
		[DataMember(EmitDefaultValue = false, Order = 0)] public string label;
		[DataMember(EmitDefaultValue = false, Order = 1)] public string description;
		public string instructionsAudio;
	}

	[DisallowMultipleComponent]
	public class Test : MonoBehaviour
	{
		[DataContract, Serializable]
		public class TestData
		{
			[DataMember(Order = 0)] public string step;
			[DataMember(Order = 1)] public TestMetadata metadata;
			[DataMember(Order = 2, EmitDefaultValue = false)] public bool isForm;
			[DataMember(Order = 3, EmitDefaultValue = false)] public List<IndicatorField> fields;
			[DataMember(Order = 4, EmitDefaultValue = false)] public List<TestData> children;

			public IndicatorField Find(string indicatorName)
			{
				foreach (var field in fields) if (field.Name == indicatorName) return field;

				return null;
			}
		}

		[SerializeField] private bool onlyEnabledWhileRunning = true;
		[SerializeField] private bool isForm = false;

		[SerializeField] TestSuite testSuite;
		[SerializeField] TestMetadata metadata;
		[SerializeField] Transform origin;
		[SerializeField] List<Test> children = new List<Test>();
		[NonSerialized] TestData testData;

		[SerializeField] List<TestObjective> endConditions = new List<TestObjective>();
		[SerializeField] List<TestIndicator> indicators = new List<TestIndicator>();
				
		public Transform Origin => origin == null ? this.transform : origin;

		public TestMetadata Metadata => metadata;

		public Test this[int? i] => i == null ? null : children[i.Value];
		public int? RunningSubtestIndex { get; private set; }
		public Test RunningSubtest => this[RunningSubtestIndex];
		public int SubtestCount => children.Count;
		public bool Completed { get; internal set; }
		public bool IsRunning { get; private set; }

		public MocapRecorder Mocap => Suite.Mocap;

		public TestSuite Suite => testSuite;

		public bool IsForm => this.isForm;

		public bool OnlyEnabledWhileRunning => this.onlyEnabledWhileRunning;

		void Start()
		{

		}

		/// <summary>
		/// Coroutine managing the execution of this test and its children
		/// </summary>
		/// <returns></returns>
		public IEnumerator Run(string outputFileName = null, string mocapFileName = null)
		{
			Completed = false;

			this.StartTest();

			if (outputFileName != null)
			{
				JSONSerializer.MkDirParent(outputFileName);
				if (mocapFileName != null && !isForm) Suite.Mocap.StartRecording(mocapFileName, this.transform);
			}

			for (int i = 0; i < children.Count; i++)
			{
				var child = children[i];
				RunningSubtestIndex = i;
				yield return child.Run();
			}

			RunningSubtestIndex = null;

			if (!EndConditionVerified())
				yield return new WaitUntil(EndConditionVerified);

			this.EndTest();

			if (outputFileName != null)
			{
				JSONSerializer.ToJSONFile(outputFileName, this.testData);
				if (!isForm) Suite.Mocap.StopRecording();
			}

			Completed = true;
		}

		/// <summary>
		/// Initializes the test, and sub-tests, and their stored TestData
		/// Erases currently stored data
		/// </summary>
		public void InitTestData()
		{
			this.Completed = false;

			this.testData = new TestData();
			this.testData.step = this.gameObject.name;
			this.testData.isForm = this.isForm;
			this.testData.metadata = this.metadata;

			this.testData.fields = new List<IndicatorField>();

			children.ForEach(c => c.InitTestData());

			this.testData.children = children.Select(c => c.testData).ToList();
			if (this.testData.children.Count == 0) this.testData.children = null;
		}

		public void StartTest()
		{
			Debug.Log("Started Test " + this.gameObject.name);

			this.gameObject.SetActive(true);
			IsRunning = true;

			foreach (var ind in indicators)
			{
				ind.StartRecording(this);
			}
		}

		public void EndTest()
		{
			Debug.Log("Ended Test " + this.gameObject.name);

			if (OnlyEnabledWhileRunning) this.gameObject.SetActive(false);

			IsRunning = false;

			foreach (var test in GetComponentsInChildren<Test>())
			{
				if (test.OnlyEnabledWhileRunning) test.gameObject.SetActive(false);
			}

			foreach (var ind in indicators)
			{
				ind.StopRecording();
			}

			if (RunningSubtest) RunningSubtest.EndTest();
		}

		public bool EndConditionVerified()
		{
			return endConditions.All(ec => { try { return ec.ConditionVerified; } catch { return false; } });
		}

		public void AddIndicatorField(IndicatorField field)
		{
			this.testData.fields.Add(field);
		}

		public void OnValidate()
		{
			testSuite = transform.parent?.GetComponentInParent<TestSuite>();
			if (testSuite) testSuite.OnValidate();
			children = FetchDirectChildren<Test>(transform);

			indicators.Clear();
			indicators.AddRange(GetComponents<TestIndicator>());

			endConditions.Clear();
			endConditions.AddRange(GetComponents<TestObjective>());

			if (this.metadata != null && (this.metadata.label == null || this.metadata.label.Length == 0)) this.metadata.label = this.transform.name;
		}

		/// <summary>
		/// Retrieves components of type T in the direct children of a transform
		/// </summary>
		/// <typeparam name="T">the type of components to retrieve</typeparam>
		/// <param name="transform"></param>
		/// <returns></returns>
		public static List<T> FetchDirectChildren<T>(Transform transform) where T : Component
		{
			List<T> componentList = new List<T>();

			for (int i = 0; i < transform.childCount; i++)
			{
				var comp = transform.GetChild(i).GetComponent<T>();
				if (comp) componentList.Add(comp);
			}

			return componentList;
		}
	}


#if UNITY_EDITOR
	[CustomEditor(typeof(Test))]
	public class TestEditor : Editor
	{
		void OnSceneGUI()
		{
			// get the chosen game object
			Test node = target as Test;

			if (node == null) return;

			Action<float> drawLine = (z) =>
				Handles.DrawLine(node.transform.TransformPoint(new Vector3(-1, 0.01f, z)), node.transform.TransformPoint(new Vector3(1, 0.01f, z)));

			drawLine(-.375f);
			drawLine(+.375f);
			drawLine(+3.375f);
			drawLine(+4.125f);
		}
	}
#endif
}