using System.Runtime.Serialization;
using UnityEngine;

namespace FullBodyTracking.Mocap
{
	public enum RecordableObjectEventType
	{
		TRANSFORM_UPDATE,
		CREATE,
		DELETE,
		GRAB_RIGHT,
		GRAB_LEFT,
		RELEASE_RIGHT,
		RELEASE_LEFT,
	}

	[DataContract]
	public class RecordableObjectEvent
	{
		[DataMember(Order = 0)] public int id;
		[DataMember(Order = 1, EmitDefaultValue = false)] int type = (int)RecordableObjectEventType.TRANSFORM_UPDATE;
		[DataMember(Order = 2, EmitDefaultValue = false)] public float[] t = null;
		[DataMember(Order = 3, EmitDefaultValue = false)] public string arg = null;

		private static float[] Transform(Component c, Transform reference)
		{
			Transform t = c.gameObject.transform;

			Vector3 pos = reference.InverseTransformPoint(t.position);
			Vector3 rot = (Quaternion.Inverse(reference.rotation) * t.rotation).eulerAngles;
			Vector3 scl = t.localScale;

			return new float[] { pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, scl.x, scl.y, scl.z };
		}

		public static RecordableObjectEvent MakeUpdate(RecordableObject robj, Transform reference)
		{
			var @event = new RecordableObjectEvent();

			@event.id = robj.id;
			@event.type = (int)RecordableObjectEventType.TRANSFORM_UPDATE;
			@event.t = Transform(robj, reference);

			return @event;
		}

		public static RecordableObjectEvent MakeCreateEvent(RecordableObject robj, Transform reference)
		{
			var @event = new RecordableObjectEvent();

			@event.id = robj.id;
			@event.type = (int)RecordableObjectEventType.CREATE;
			@event.arg = robj.replayPrefabID;

			return @event;
		}

		public static RecordableObjectEvent MakeGenericEvent(RecordableObject robj, RecordableObjectEventType eventType)
		{
			return MakeGenericEvent(robj.id, eventType);
		}

		public static RecordableObjectEvent MakeGenericEvent(int robjId, RecordableObjectEventType eventType)
		{
			var @event = new RecordableObjectEvent();

			@event.id = robjId;
			@event.type = (int)eventType;

			return @event;
		}

		public RecordableObjectEventType EventType => (RecordableObjectEventType)type;

		/// <summary>
		/// True only if this event is of type TRANSFORM_UPDATE
		/// </summary>
		public bool IsUpdate => ((RecordableObjectEventType)this.type) == RecordableObjectEventType.TRANSFORM_UPDATE;

		/// <summary>
		/// True only if this event is of type CREATE
		/// </summary>
		public bool IsCreate => ((RecordableObjectEventType)this.type) == RecordableObjectEventType.CREATE;

		/// <summary>
		/// True only if this event is of type DELETE
		/// </summary>
		public bool IsDelete => ((RecordableObjectEventType)this.type) == RecordableObjectEventType.DELETE;

		/// <summary>
		/// Create a reversed version of this event for backwards replay
		/// </summary>
		/// <returns>a reversed version of this event for backwards replay</returns>
		public RecordableObjectEvent ReverseEvent()
		{
			var @event = new RecordableObjectEvent();

			@event.id = this.id;
			@event.t = this.t;
			@event.arg = this.arg;

			switch ((RecordableObjectEventType)this.type)
			{
				case RecordableObjectEventType.TRANSFORM_UPDATE:
					@event.type = (int)RecordableObjectEventType.TRANSFORM_UPDATE;
					break;
				case RecordableObjectEventType.CREATE:
					@event.type = (int)RecordableObjectEventType.DELETE;
					break;
				case RecordableObjectEventType.DELETE:
					@event.type = (int)RecordableObjectEventType.CREATE; 
					break;
				case RecordableObjectEventType.GRAB_RIGHT:
					@event.type = (int)RecordableObjectEventType.RELEASE_RIGHT;
					break;
				case RecordableObjectEventType.GRAB_LEFT:
					@event.type = (int)RecordableObjectEventType.RELEASE_LEFT;
					break;
				case RecordableObjectEventType.RELEASE_RIGHT:
					@event.type = (int)RecordableObjectEventType.GRAB_RIGHT;
					break;
				case RecordableObjectEventType.RELEASE_LEFT:
					@event.type = (int)RecordableObjectEventType.GRAB_LEFT;
					break;
			}

			return @event;
		}
	}
}