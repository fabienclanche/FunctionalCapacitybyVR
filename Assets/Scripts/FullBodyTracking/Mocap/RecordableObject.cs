using Interaction;
using System;
using UnityEngine;

namespace FullBodyTracking.Mocap
{
	public class RecordableObject : MonoBehaviour
	{
		public int id;
		public string replayPrefabID;

		private bool previouslyActive = false;

		// transform data for last recorded or replayed frame
		private Vector3 previousPosition;
		private Quaternion previousRotation;
		private Vector3 previousScale;
		private Transform previousParent;

		// values of next frame for replay interpolation
		private Vector3 nextPosition;
		private Quaternion nextRotation;
		private Vector3 nextScale;

		private Transform lastUsedReference = null;
		private RecordableObjectEvent lastRecordedUpdate = null;

		public void LateUpdate()
		{
			if (lastUsedReference != null)
				lastRecordedUpdate = RecordableObjectEvent.MakeUpdate(this, lastUsedReference);
		}

		public void InitForRecording()
		{
			previouslyActive = false;
			previousParent = null;
		}

		public bool HasMoved()
		{
			bool moved = false;

			moved = (previousPosition - transform.position).sqrMagnitude > 0.00001f || Quaternion.Angle(previousRotation, this.transform.rotation) > 0.1f || (previousScale != this.transform.lossyScale);

			return moved;
		}

		public override string ToString()
		{
			return this.gameObject.name + "[id=" + id + " replayPrefabId=" + replayPrefabID + "]";
		}

		public void SetNextFrame(Transform reference, RecordableObjectEvent @event)
		{
			float[] transform = @event.t;
			this.nextPosition = new Vector3(transform[0], transform[1], transform[2]);
			this.nextRotation = Quaternion.Euler(transform[3], transform[4], transform[5]);
			this.nextScale = new Vector3(transform[6], transform[7], transform[8]);
		}

		public void Interpolate(Transform reference, float interpolation)
		{
			this.transform.position = reference.TransformPoint(Vector3.Lerp(this.previousPosition, this.nextPosition, interpolation));
			this.transform.rotation = reference.rotation * Quaternion.Slerp(this.previousRotation, this.nextRotation, interpolation);
			this.transform.localScale = Vector3.Lerp(this.previousScale, this.nextScale, interpolation);
		}

		public void ReplayEvent(Transform reference, RecordableObjectEvent @event, InteractiveHand rhand, InteractiveHand lhand)
		{
			InteractiveObject intObj;

			switch (@event.EventType)
			{
				case RecordableObjectEventType.CREATE:
					break;

				case RecordableObjectEventType.TRANSFORM_UPDATE:
					float[] transform = @event.t;
					this.nextPosition = this.previousPosition = new Vector3(transform[0], transform[1], transform[2]);
					this.nextRotation = this.previousRotation = Quaternion.Euler(transform[3], transform[4], transform[5]);
					this.nextScale = this.previousScale = new Vector3(transform[6], transform[7], transform[8]);

					Interpolate(reference, 0);

					break;

				case RecordableObjectEventType.DELETE:
					// set prefab id so the event can be reversed
					@event.arg = this.replayPrefabID;
					Destroy(this.gameObject);
					break;

				case RecordableObjectEventType.GRAB_RIGHT:
					intObj = this.GetComponent<InteractiveObject>();
					if (!intObj) throw new InvalidOperationException(@event.EventType + ": " + this + " is not an interactive object");
					rhand.GrabObject(intObj);
					break;

				case RecordableObjectEventType.GRAB_LEFT:
					intObj = this.GetComponent<InteractiveObject>();
					if (!intObj) throw new InvalidOperationException(@event.EventType + ": " + this + " is not an interactive object");
					lhand.GrabObject(intObj);
					break;

				case RecordableObjectEventType.RELEASE_RIGHT:
					if (rhand.HeldObject?.gameObject == this.gameObject) rhand.ForceReleaseHeldObject();
					else throw new InvalidOperationException(@event.EventType + ": " + this + " is not held by Rhand");
					break;

				case RecordableObjectEventType.RELEASE_LEFT:
					if (lhand.HeldObject?.gameObject == this.gameObject) lhand.ForceReleaseHeldObject();
					else throw new InvalidOperationException(@event.EventType + ": " + this + " is not held by Lhand");
					break;
			}
		}

		public void MakeEvents(Transform reference, Action<RecordableObjectEvent> eventConsumer)
		{
			this.lastUsedReference = reference;
            bool justCreated = false;

			// check for creation/deletion/movement events
			if (!previouslyActive && this != null && this.isActiveAndEnabled)
			{
				eventConsumer(RecordableObjectEvent.MakeCreateEvent(this, reference));
				previouslyActive = true;
                justCreated = true;
			}
			else if (previouslyActive && (this == null || this.gameObject == null || !this.isActiveAndEnabled))
			{
				if (this.lastRecordedUpdate != null) eventConsumer(this.lastRecordedUpdate);
				eventConsumer(RecordableObjectEvent.MakeGenericEvent(this, RecordableObjectEventType.DELETE));
				previouslyActive = false;
			}

			if (this == null) return; // if this object has been destroyed, nothing more to do

			if (justCreated || (this.isActiveAndEnabled && HasMoved()))
			{
				eventConsumer(RecordableObjectEvent.MakeUpdate(this, reference));
				previousPosition = this.transform.position;
				previousRotation = this.transform.rotation;
				previousScale = this.transform.lossyScale;
			}

			// check for release event
			if (previousParent != this.transform.parent && previousParent != null)
			{
				var ihand = previousParent.GetComponent<InteractiveHand>();

				if (ihand)
				{
					eventConsumer(RecordableObjectEvent.MakeGenericEvent(this,
						ihand.Hand == BodyPart.Rhand ? RecordableObjectEventType.RELEASE_RIGHT : RecordableObjectEventType.RELEASE_LEFT));
				}
			}

			// check for grab event
			if (previousParent != this.transform.parent && this.transform.parent != null)
			{
				var ihand = this.transform.parent.GetComponent<InteractiveHand>();

				if (ihand)
				{
					eventConsumer(RecordableObjectEvent.MakeGenericEvent(this,
						ihand.Hand == BodyPart.Rhand ? RecordableObjectEventType.GRAB_RIGHT : RecordableObjectEventType.GRAB_LEFT));
				}
			}

			previousParent = this.transform.parent;
		}
	}
}