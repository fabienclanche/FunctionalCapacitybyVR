using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

namespace FullBodyTracking.Mocap
{
	[RequireComponent(typeof(CalibratedIK))]
	public class MocapRecorder : MonoBehaviour
	{
		public int frequency = 18;

		private float lastRecordedTime;

		private List<FullBodyTrackingData> buffer = new List<FullBodyTrackingData>();

		private StreamWriter writer;

		private CalibratedIK calibratedIK, calibratedIK_F;

		private float startTime;

		private Transform @ref;

		private List<RecordableObject> recObjects = new List<RecordableObject>();

		public bool IsRecording
		{
			get { return writer != null; }
		}

		public float RecordedTime => Mathf.Max(0, this.lastRecordedTime - this.startTime);
 
		public void SetAvatars(CalibratedIK male, CalibratedIK female)
		{
			this.calibratedIK = male;
			this.calibratedIK_F = female;
		}

		public void StartRecording(string fileURL, Transform reference = null)
		{
			this.buffer.Clear();
			this.writer = JSONSerializer.FileWriter(fileURL, append: false);
			this.writer.WriteLine(this.GenerateMetadata(reference));
			Debug.Log("Started recording " + reference?.name);
			this.startTime = Time.time;
			this.lastRecordedTime = float.NegativeInfinity;
		}

		public void AddRecordableObject(RecordableObject robj)
		{
			robj.InitForRecording();
			recObjects.Add(robj);
			robj.id = recObjects.Count;
		}

		public MocapMetadata GenerateMetadata(Transform reference)
		{
			var metadata = new MocapMetadata();

			@ref = (reference == null) ? calibratedIK.TrackingReference : reference;

			metadata.position = @ref.position;
			metadata.rotation = @ref.rotation.eulerAngles;
			metadata.scale = @ref.lossyScale;

			metadata.armScale = calibratedIK.ArmScale;
			metadata.legScale = calibratedIK.LegScale;
			metadata.modelScale = calibratedIK.ModelScale;

			metadata.armScaleF = calibratedIK_F.ArmScale;
			metadata.legScaleF = calibratedIK_F.LegScale;
			metadata.modelScaleF = calibratedIK_F.ModelScale;

            metadata.accessibilityModeDevices = new List<string>();

            foreach(BodyPart part in Enum.GetValues(typeof(BodyPart)))
            {
                if(calibratedIK.GetEffectorDegradedMode(part))
                {
                    metadata.accessibilityModeDevices.Add(part.ToString());
                }
            }

			metadata.scene = SceneManager.GetActiveScene().name;

			return metadata;
		}

		public void StopRecording()
		{
			if (!IsRecording) return;

			try
			{
				this.RecordFrame();
			}
			catch (Exception e)
			{
				Debug.LogError(e);
			}

			this.Flush();

			this.writer.Close();
			this.writer = null;

			recObjects.Clear();

			Debug.Log("Stopped recording.");
		}

		public void Flush()
		{
			foreach (var fbt_data in this.buffer)
			{
				writer.WriteLine(fbt_data);
			}
			this.buffer.Clear();
		}

		void RecordFrame()
		{
			var fbt_data = new FullBodyTrackingData();
			fbt_data.t = Time.time - this.startTime;
			this.lastRecordedTime = Time.time;

			foreach (var part in (BodyPart[])Enum.GetValues(typeof(BodyPart)))
			{
				var tracker = calibratedIK[part];
				if (tracker)
				{
					fbt_data.Set(part, tracker, @ref);
				}
			}

			// rec. objects events
			var rObjEvents = new List<RecordableObjectEvent>();
			for (int i = 0; i < recObjects.Count; i++)
			{
				var robj = recObjects[i];
				robj.MakeEvents(@ref, rObjEvents.Add);

				if (!object.ReferenceEquals(robj, null) && false) //check if object has been destroyed (reference still exists, but Unity says it is == null)
				{
					recObjects[i] = null;
					rObjEvents.Add(RecordableObjectEvent.MakeUpdate(robj, @ref));
					rObjEvents.Add(RecordableObjectEvent.MakeGenericEvent(i + 1, RecordableObjectEventType.DELETE));
				}

			}
			if (rObjEvents.Count > 0) fbt_data.Events = rObjEvents;

			// add to buffer
			this.buffer.Add(fbt_data);
			if (this.buffer.Count > 5) this.Flush();
		}

		void LateUpdate()
		{
			if (IsRecording)
			{
				if (Time.time - this.lastRecordedTime < (1f / this.frequency)) return;
				this.RecordFrame();
			}
		}

		void OnDestroy()
		{
			if (writer != null) StopRecording();
		}
	}
}
