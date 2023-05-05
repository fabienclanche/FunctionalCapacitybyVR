
using System;
using UnityEngine;
using UnityEngine.XR;

namespace FullBodyTracking
{
	public class TPoseAnchor : MonoBehaviour
	{
		private const float ATTRACTION_STRENGTH = 0.2f,
			RESET_STRENGTH = 2f;

		public BodyPart bodypart;
		public bool forcePairWithHMD = false;
		[SerializeField] protected bool ignoreNodeTypeForPairing = false;
		[SerializeField] protected XRNode requiredNodeType = XRNode.HardwareTracker;

		public TrackedObject forcePairWith = null;

        /// <summary>
        /// Forces this anchor to pair with a Hardware Tracker instead of its preferred hardware type.
        /// Used to make calibration with a wrist-mounted tracker instead of a handheld controller. 
        /// <code>CalibratedIK</code> will relax IK constraints for a limb tracked in degraded mode (limb not considered for scaling, no rotational constraints).
        /// </summary>
		public bool degradedMode = false;

		[Header("IK Offset")]
		public Vector3 ikPosOffset;

		public Vector3 interactionPosOffset;

        public TrackedObject TrackedObject;

		public Vector3 LocalOrigin { get; private set; }
		public Vector3 WorldOrigin => this.transform.parent ? this.transform.parent.TransformPoint(LocalOrigin) : LocalOrigin;
		
		private void SetColor(Color color)
		{
			var renderer = GetComponent<MeshRenderer>();
			if (renderer) renderer.material.SetColor("_Color", color);
		}

		public bool AcceptsPairing(TrackedObject tobj)
		{
			if (tobj.IgnoreForBodyTracking) return false;

			if (this.forcePairWith != null) return this.forcePairWith == tobj;

			if (this.degradedMode) return tobj.nodeType == XRNode.HardwareTracker;

			if (this.ignoreNodeTypeForPairing) return true;

			if (requiredNodeType == XRNode.RightHand || requiredNodeType == XRNode.LeftHand) return (tobj.nodeType == XRNode.RightHand || tobj.nodeType == XRNode.LeftHand);

			return requiredNodeType == tobj.nodeType;
		}

		public void OnValidate()
		{
			if (forcePairWithHMD)
			{
				ignoreNodeTypeForPairing = false;
				requiredNodeType = XRNode.Head;
			}
		}

		void Start()
		{
			LocalOrigin = this.transform.localPosition;
		}

        [Obsolete]
		internal void ResetPosition()
		{
			this.transform.localPosition = LocalOrigin;
		}
	}
}