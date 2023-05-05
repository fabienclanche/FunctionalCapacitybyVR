using UnityEngine;
using Utils;
using System.Collections.Generic;
using System.Linq;
using FullBodyTracking.Mocap;

namespace TestSuite.Metrology
{
	public class ObjectCountingIndicator : TestIndicator
	{
		[Header("Object Prefabs and Transforms")]
		public GameObject objectAPrefab;
		public GameObject objectBPrefab;
		public Transform[] objectPositions;
		public int aCount { get; private set; }
		public int bCount { get; private set; }

		[Header("RNG Seed")]
		[Tooltip("The seed to randomize the object distribution. Leave blank to use a random seed every time the test is run.")]
		public string seed = "";

		[Header("LookAt")]
		[Range(0, 90)]
		public float lookAtAngleThreshold = 30;
		[Tooltip("Automatically managed")]
		public List<LookAtIndicator> lookAtIndicators;

		private List<GameObject> instantiatedObjects = new List<GameObject>();

		public override string Name => "$ind:objectCounting";

		[IndicatorValue, Metadata(importance = -1)] public string objectSetup { get; private set; }

		protected override void Begin()
		{
			objectSetup = "";
			ClearInstantiatedObjects();

			var random = seed.Length > 0 ? new System.Random(seed.GetHashCode()) : new System.Random();

			int maxCountPerClass = (int)Mathf.Ceil(objectPositions.Length / 2f);

			aCount = 0;
			bCount = 0;

			bool[] aOrB = new bool[objectPositions.Length];

			for (int i = 0; i < aOrB.Length; i++)
			{
				if (aCount >= maxCountPerClass) aOrB[i] = false;
				else if (bCount >= maxCountPerClass) aOrB[i] = true;
				else aOrB[i] = (random.Next(2) == 0);

				if (aOrB[i]) aCount++;
				else bCount++;

				objectSetup += aOrB[i] ? "A" : "B";
			}

			for (int i = 0; i < aOrB.Length; i++)
			{
				var obj = InstantiateObject(aOrB[i] ? objectAPrefab : objectBPrefab, objectPositions[i]);
				lookAtIndicators[i].@object = obj;
				lookAtIndicators[i].label = "$item:object " + i;

				RecordableObject recObj;
				if (recObj = obj.GetComponent<RecordableObject>()) Test.Mocap.AddRecordableObject(recObj);
			}
		}

		private GameObject InstantiateObject(GameObject prefab, Transform transform)
		{
			var instance = GameObject.Instantiate(prefab);

			instance.transform.parent = transform;
			instance.transform.localPosition = Vector3.zero;
			instance.transform.localRotation = Quaternion.identity;
			instance.transform.localScale = Vector3.one;

			this.instantiatedObjects.Add(instance);

			return instance;
		}

		protected override void End()
		{
			ClearInstantiatedObjects();
		}

		public void ClearInstantiatedObjects()
		{
			foreach (var o in instantiatedObjects)
			{
				Destroy(o);
			}

			foreach (var lookAtIndicator in this.lookAtIndicators)
			{
				lookAtIndicator.@object = null;
			}
		}

		public void OnValidate()
		{
			if (this.lookAtIndicators == null) this.lookAtIndicators = new List<LookAtIndicator>();
			this.lookAtIndicators = this.lookAtIndicators.Where(ind => ind != null).ToList();

			if (this.lookAtIndicators.Count < this.objectPositions.Length)
			{
				for (int i = this.lookAtIndicators.Count; i < this.objectPositions.Length; i++)
				{
					var indicator = this.gameObject.AddComponent<LookAtIndicator>();
					this.lookAtIndicators.Add(indicator);
				}
			}
			else if (this.lookAtIndicators.Count > this.objectPositions.Length)
			{
				for (int i = this.objectPositions.Length; i < this.lookAtIndicators.Count; i++)
				{
					this.lookAtIndicators.RemoveAt(i);
				}
			}

			foreach (var indicator in this.lookAtIndicators)
			{
				indicator.lookAtAngleThreshold = this.lookAtAngleThreshold;
			}
		}

		protected override void RecordFrame()
		{

		}
	}
}