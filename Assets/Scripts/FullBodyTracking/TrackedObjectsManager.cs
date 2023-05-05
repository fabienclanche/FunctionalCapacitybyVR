using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace FullBodyTracking
{
	public class TrackedObjectsManager : MonoBehaviour
	{
		private int lastUpdateFrameNumber = -1;
		private Dictionary<ulong, TrackedObject> trackedObjects = new Dictionary<ulong, TrackedObject>();
		private Dictionary<ulong, XRNodeState> states = new Dictionary<ulong, XRNodeState>();

		public GameObject trackerPrefab, hmdPrefab, controllerPrefab, lighthousePrefab;

		/// <summary>
		/// A reference to the tracked HMD
		/// </summary> 
		public TrackedObject HMD { get; private set; }

		/// <summary>
		/// An enumeration of all possible VR tracked objects (Devices from 1 to 16) excluding the HMD
		/// </summary>
		public IEnumerable<TrackedObject> TrackedObjects { get { return trackedObjects.Values.Where(tobj => tobj!=HMD); } }

		internal IEnumerator DummyTrackersCoroutine()
		{
			Vector3[] offsets = new Vector3[3];

			while (true)
			{
				float hmdHeight = HMD.TrackedPosition.y;

				offsets[0] = Vector3.right * 0.2f;
				offsets[1] = Vector3.left * 0.2f;
				offsets[2] = Vector3.zero;

				for (int i = 0; i < 3; i++)
				{
					XRNodeState state = default;
					state.uniqueID = (ulong)(i + 1);
					state.tracked = true;
					state.nodeType = XRNode.HardwareTracker;
					state.position = HMD.TrackedPosition + (HMD.TrackedRotation * offsets[i]) - ((i < 2) ? Vector3.up * hmdHeight: Vector3.up * hmdHeight / 2);
					state.rotation = HMD.TrackedRotation; 
					NodeUpdate(state);
				}

				yield return null;
			}
		}

		void NodeAdded(XRNodeState state)
		{
			if (!trackedObjects.ContainsKey(state.uniqueID))
			{
				var name = InputTracking.GetNodeName(state.uniqueID);

				// if (name == null || name.Length == 0) return;

				GameObject tobjRoot;
                string label = "???";

				switch (state.nodeType)
				{
					case XRNode.Head:
						tobjRoot = Instantiate(hmdPrefab);
                        label = "$device:hmd";
						break;
					case XRNode.GameController:
					case XRNode.LeftHand:
					case XRNode.RightHand:
						tobjRoot = Instantiate(controllerPrefab);
                        label = "$device:controller";
                        break;
					case XRNode.TrackingReference:
						tobjRoot = Instantiate(lighthousePrefab);
                        label = "$device:lighthouse";
                        break;
					case XRNode.HardwareTracker:
						tobjRoot = Instantiate(trackerPrefab);
                        label = "$device:tracker";
                        break;
					default:
						if (true) return;
						tobjRoot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
						tobjRoot.transform.localScale = 0.1f * Vector3.one;
						tobjRoot.layer = trackerPrefab.layer;
						tobjRoot.GetComponent<Collider>().enabled = false;
						break;
				}

				tobjRoot.transform.parent = this.transform;
				tobjRoot.name = state.nodeType + "(" + state.uniqueID + "): " + name;

				var tobj = tobjRoot.AddComponent<TrackedObject>();
                tobj.TypeLabel = label;
                tobj.SetReference(this.transform);
				tobj.SetState(state);                

				trackedObjects[state.uniqueID] = tobj;

				if (state.nodeType == XRNode.Head) HMD = tobj; 
			}
		}

		void NodeRemoved(XRNodeState state)
		{
			TrackedObject tobj;
			if (trackedObjects.TryGetValue(state.uniqueID, out tobj))
			{
				if (tobj && tobj.gameObject) Destroy(tobj.gameObject);
				trackedObjects.Remove(state.uniqueID);
			}
		}

		void NodeUpdate(XRNodeState state)
		{
			if (trackedObjects.ContainsKey(state.uniqueID))
			{
				trackedObjects[state.uniqueID].SetState(state);
			}
			else
			{
				NodeAdded(state);
			}
		}

		void Start()
		{
			InputTracking.nodeAdded += this.NodeAdded;
			InputTracking.nodeRemoved += this.NodeRemoved;

			var nodeStates = new List<XRNodeState>();
			InputTracking.GetNodeStates(nodeStates);
			nodeStates.ForEach(this.NodeAdded);
		}

		void UpdateTracking()
		{
			if (Time.frameCount > this.lastUpdateFrameNumber)
			{
				var nodeStates = new List<XRNodeState>();
				InputTracking.GetNodeStates(nodeStates);
				nodeStates.ForEach(this.NodeUpdate);
				this.lastUpdateFrameNumber = Time.frameCount;
			}
		}

		void Update()
		{
			UpdateTracking();
		}
	}
}
