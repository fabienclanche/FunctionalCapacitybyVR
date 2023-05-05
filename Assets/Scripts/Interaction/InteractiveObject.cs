using UnityEngine;
using SDUU.Hands;

namespace Interaction
{
	public enum GrabType
	{
		ATTACH_TRANSFORM,
		NONE,
	}

	public class InteractiveObject : MonoBehaviour
	{
		[SerializeField] GrabType grabType = GrabType.ATTACH_TRANSFORM;
		[SerializeField] bool releaseable = true;
		[SerializeField] Rigidbody body;
		[SerializeField] HandPose handPose;

		public GrabType GrabType { get { return this.grabType; } set { this.grabType = value; } }
		public HandPose HandPose => handPose;
		public bool Releasable { get { return this.releaseable; } set { this.releaseable = value; } }

		public Rigidbody Rigidbody => body;

		public Vector3 preferredOffset, preferredRotation;
		public Vector3 preferredOffsetL, preferredRotationL;
	}
}