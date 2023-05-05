using UnityEngine;
using TestSuite.Metrology;
using FullBodyTracking;
using System.Collections.Generic;
using Utils;

namespace TestSuite.Views
{
	public class EyeTrackingView : MonoBehaviour
	{
		public TestSuite suite;
		public int resolution = 4;
		public Material viewMaterial;
		public Gradient gradient;

		public Test runningTest = null;

		private Dictionary<Vector3, float> heatMap = new Dictionary<Vector3, float>();
		private Dictionary<Vector3, MeshRenderer> heatMapRenderers = new Dictionary<Vector3, MeshRenderer>();

		public void Update()
		{
			if(suite.RunningTest != runningTest && suite.RunningTest != null)
			{
				runningTest = suite.RunningTest;
				ResetAll();
			}

			if (suite.IKRig && suite.RunningTest != null && suite.InstructionsOK)
			{
				TrackedObject tobj = suite.IKRig[BodyPart.Head];
				Vector3 forward = tobj.WorldRotation * Vector3.forward;
				Vector3 origin = tobj.WorldPosition + forward * .2f;

				RaycastHit hit;
				if (Physics.Raycast(origin, forward, layerMask:~0, maxDistance:100, hitInfo: out hit, queryTriggerInteraction: QueryTriggerInteraction.Ignore)) AddAt(hit.point, Time.deltaTime);
			}
		}

		public Vector3 ToKey(Vector3 worldPos)
		{
			return worldPos.Map(x => Mathf.Round(x * resolution));
		}

		public Vector3 ToCoords(Vector3 key)
		{
			return key.Map(x => x / resolution);
		}

		public float GetAt(Vector3 worldPos)
		{
			float val;
			if (heatMap.TryGetValue(ToKey(worldPos), out val)) return val;
			else return 0;
		}

		public void AddAt(Vector3 worldPos, float delta)
		{
			SetAt(worldPos, GetAt(worldPos) + delta);
		}

		public Color GetColor(float value)
		{
			return gradient.Evaluate(value) * new Color(1, 1, 1, 0.25f);
		}

		public void ResetAll()
		{
			foreach(var key in new List<Vector3>(heatMap.Keys))
			{
				heatMap[key] = 0;
				heatMapRenderers[key].enabled = false;
			}
		}

		public void SetAt(Vector3 worldPos, float val)
		{
			var key = ToKey(worldPos);

			if (!heatMap.ContainsKey(key))
			{
				var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
				var renderer = cube.GetComponent<MeshRenderer>();
				renderer.sharedMaterial = viewMaterial;
				heatMapRenderers[key] = renderer;
				renderer.transform.position = ToCoords(key);
				renderer.transform.localScale *= (1f / resolution);

				foreach (Collider col in renderer.GetComponents<Collider>())
				{
					col.enabled = false;
				}
			}

			heatMap[key] = val;
			heatMapRenderers[key].material.SetColor("_Color", GetColor(val));
			heatMapRenderers[key].enabled = (val > 0); 
		}
	}
}