using UnityEngine;
using System.Runtime.Serialization;
using System;
using Utils;
using System.Collections.Generic;

namespace FullBodyTracking.Mocap
{
	/// <summary>
	/// Represents a single frame of full body motion capture data. Also records RecordableObject positions and events.
	/// </summary>
	[Serializable, DataContract]
	public class FullBodyTrackingData
	{
		[DataMember(Order = 0)] public float t = 0;
		[DataMember(Order = 1)] public SinglePointTrackingData head, rfoot, lfoot, rhand, lhand, back;
		[DataMember(Order = 2, EmitDefaultValue = false)] List<RecordableObjectEvent> events = null;

		/// <summary>
		/// Enumerates events of type UPDATE
		/// </summary>
		public IEnumerable<RecordableObjectEvent> UpdateEvents
		{
			get
			{
				if (events != null)
					foreach (var @event in this.events) if (@event.IsUpdate) yield return @event;
			}
		}

		/// <summary>
		/// An enumeration of all events
		/// </summary>
		public IEnumerable<RecordableObjectEvent> Events
		{
			get
			{
				if (events != null)
					foreach (var @event in this.events) yield return @event;
			}

			set
			{
				if (events != null) events.Clear();
				foreach (var @event in value) AddEvent(@event);
			}
		}

		/// <summary>
		/// Set all position, velocity and acceleration values for a given bodypart
		/// </summary>
		/// <param name="part">A body part</param>
		/// <param name="tobj">The tracked object corresponding to that body part</param>
		/// <param name="offset">the reference </param>
		public void Set(BodyPart part, TrackedObject tobj, Transform reference)
		{
			if (tobj == null) Debug.LogError("no " + part);
			var spt_data = new SinglePointTrackingData();
			spt_data.Position = reference.InverseTransformPoint(tobj.WorldPosition);
			spt_data.Rotation = Quaternion.Inverse(reference.rotation) * tobj.WorldRotation;
			spt_data.v = reference.InverseTransformVector(tobj.Velocity);
			spt_data.av = tobj.AngularVelocity;
			spt_data.a = reference.InverseTransformVector(tobj.Acceleration);
			spt_data.aa = tobj.AngularAcceleration;
			this[part] = spt_data;
		}

		/// <summary>
		/// Adds an event to the list of events, initializes the list if necessary
		/// </summary>
		/// <param name="event">An event to be added to this instance's event list</param>
		public void AddEvent(RecordableObjectEvent @event)
		{
			if (this.events == null) this.events = new List<RecordableObjectEvent>();
			this.events.Add(@event);
		}

		/// <summary>
		/// Retrieves the first event with matching object id and type
		/// </summary>
		/// <param name="id">the RecordableObject id of the event to retrieve</param>
		/// <param name="type">the type of the event to retrieve</param>
		/// <returns>the first event with matching object id and type</returns>
		public RecordableObjectEvent GetEvent(int id, RecordableObjectEventType type)
		{
			if (events != null)
				foreach (var @event in this.events)
				{
					if (@event.id == id && @event.EventType == type) return @event;
				}

			return null;
		}

		public SinglePointTrackingData this[BodyPart part]
		{
			get
			{
				switch (part)
				{
					case BodyPart.Back: return back;
					case BodyPart.Rfoot: return rfoot;
					case BodyPart.Lfoot: return lfoot;
					case BodyPart.Head: return head;
					case BodyPart.Rhand: return rhand;
					case BodyPart.Lhand: return lhand;
				}

				return null;
			}

			set
			{
				switch (part)
				{
					case BodyPart.Back: back = value; break;
					case BodyPart.Rfoot: rfoot = value; break;
					case BodyPart.Lfoot: lfoot = value; break;
					case BodyPart.Head: head = value; break;
					case BodyPart.Rhand: rhand = value; break;
					case BodyPart.Lhand: lhand = value; break;
				}
			}
		}

		public static implicit operator FullBodyTrackingData(string jsonData)
		{
			return JSONSerializer.FromJSON<FullBodyTrackingData>(jsonData);
		}

		public static implicit operator string(FullBodyTrackingData data)
		{
			return JSONSerializer.ToJSON(data);
		}
	}
}