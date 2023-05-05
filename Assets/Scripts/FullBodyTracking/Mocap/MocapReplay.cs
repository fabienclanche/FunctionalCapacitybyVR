using RootMotion.FinalIK;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TestSuite;
using Interaction;

namespace FullBodyTracking.Mocap
{
	public class MocapReplay : MonoBehaviour
	{
		public TPoseCalibrator tPoseCalibrator;

		public Dictionary<BodyPart, TrackedObject> bodyTrackedObjects = new Dictionary<BodyPart, TrackedObject>();

		/// <summary>
		/// Stream reader for the mocap file
		/// </summary>
		private StreamReader reader;

		[Header("File")]
		public string fileURL = "";

		[Header("Replay options")]
		public float playTimeOffset = 0;

		public float playTime = 0;
		public bool playing = false;
		public float playSpeed = 1f;

		[Header("Replay objects")]
		public List<RecordableObject> replayPrefabs = new List<RecordableObject>();

		private bool initialized = false;

		/// <summary>
		/// Holds metadata such as captured scene, capture offset and calibration data
		/// </summary>
		[Header("Metadata")]
		public MocapMetadata metadata;

		private CalibratedIK avatarM, avatarF;
		private InteractiveHand rIHand, lIHand;

		private List<FullBodyTrackingData> frames;
		private int lastFrameIndex = -2, nextFrameIndex = -1;
		public FullBodyTrackingData lastFrame => lastFrameIndex >= 0 ? frames[lastFrameIndex] : null;
		public FullBodyTrackingData nextFrame => nextFrameIndex >= 0 ? frames[nextFrameIndex] : null;

		private Dictionary<int, RecordableObject> recObjects = new Dictionary<int, RecordableObject>();

		/// <summary>
		/// Creates a replay tracker for a given body part
		/// </summary>
		/// <param name="bodyPart">The body part to create a replay tracker for</param>
		/// <returns></returns>
		private TrackedObject CreateReplayTracker(BodyPart bodyPart)
		{
			var dummyTracker = new GameObject(bodyPart + " Tracker Replay");
			dummyTracker.transform.parent = this.transform;

			TrackedObject tObj = dummyTracker.AddComponent<TrackedObject>();

			this.bodyTrackedObjects[bodyPart] = tObj;
			tObj.SetReference(this.transform);

			return tObj;
		}

		/// <summary>
		/// Creates replay trackers when this behaviour is initialized
		/// </summary>
		public void Start()
		{
			CreateReplayTracker(BodyPart.Head);
			CreateReplayTracker(BodyPart.Rhand);
			CreateReplayTracker(BodyPart.Lhand);
			CreateReplayTracker(BodyPart.Back);
			CreateReplayTracker(BodyPart.Rfoot);
			CreateReplayTracker(BodyPart.Lfoot);
		}

        private CalibratedIK CalibrateAvatar(FullBodyBipedIK ikAvatar)
        {
            ikAvatar.gameObject.SetActive(true);

            var calIK = (ikAvatar.gameObject.GetComponent<CalibratedIK>()) ?? (ikAvatar.gameObject.AddComponent<CalibratedIK>());
            calIK.InitIK(ikAvatar);

            foreach (var part in bodyTrackedObjects.Keys)
            { 
                calIK.Track(part, bodyTrackedObjects[part], tPoseCalibrator.GetAnchor(part));
                bodyTrackedObjects[part].ROffset = Quaternion.identity;
                bodyTrackedObjects[part].YOffset = 0;
            }

            return calIK;
        }

		private void InitIK()
		{
			tPoseCalibrator.gameObject.SetActive(true);

            avatarM = CalibrateAvatar(tPoseCalibrator.ikAvatar);
            avatarF = CalibrateAvatar(tPoseCalibrator.ikAvatarF);
             
			tPoseCalibrator.enabled = false;
		}

		/// <summary>
		/// Reads a mocap file from its URL and initialize the replay
		/// </summary>
		/// <param name="fileURL">URL to a mocap file</param>
		private void ReadFile(string fileURL)
		{
			this.fileURL = fileURL;
			ReadStream(Utils.JSONSerializer.FileReader(fileURL));
		}

		public void ReadStream(StreamReader streamReader)
		{
			foreach (var recObj in this.recObjects.Values)
			{
				Destroy(recObj.gameObject);
			}
			this.recObjects.Clear();

			playTime = 0;
			playing = false;
			lastFrameIndex = -2;
			nextFrameIndex = -1;

			this.frames = new List<FullBodyTrackingData>();

			if (this.reader != null) this.reader.Close();
			this.reader = streamReader;

			this.metadata = this.reader.ReadLine();
            this.InitIK();
            this.metadata.ApplyTo(this.transform, avatarM, avatarF);

			// read the 2 first frames so we can begin to interpolate
			NextFrame();
			NextFrame();

			this.playTimeOffset = this.lastFrame.t; // last frame refers to the first frame of the file
            
            Play(0);

            if (rIHand) Destroy(rIHand);
            if (lIHand) Destroy(lIHand);
            if (bodyTrackedObjects[BodyPart.Rfoot].GetComponent<FootTracker>()) Destroy(bodyTrackedObjects[BodyPart.Rfoot].GetComponent<FootTracker>());
            if (bodyTrackedObjects[BodyPart.Lfoot].GetComponent<FootTracker>()) Destroy(bodyTrackedObjects[BodyPart.Lfoot].GetComponent<FootTracker>());

            rIHand = tPoseCalibrator.AddInteractiveHand(BodyPart.Rhand, bodyTrackedObjects[BodyPart.Rhand]);
            lIHand = tPoseCalibrator.AddInteractiveHand(BodyPart.Lhand, bodyTrackedObjects[BodyPart.Lhand]);

            tPoseCalibrator.AddFootTracker(BodyPart.Rfoot, bodyTrackedObjects[BodyPart.Rfoot]);
            tPoseCalibrator.AddFootTracker(BodyPart.Lfoot, bodyTrackedObjects[BodyPart.Lfoot]);
		}

		private void Interpolate(BodyPart part, out Vector3 position, out Quaternion rotation, float interpolation)
		{
			var a = this.lastFrame[part];
			var b = this.nextFrame[part];

			position = Vector3.Slerp(a.Position, b.Position, interpolation);
			rotation = Quaternion.Slerp(a.Rotation, b.Rotation, interpolation);
		}

		/// <summary>
		/// Moves to the next frame from the current mocap file, reading from the file if the next frame hasn't been loaded before
		/// </summary>
		public bool NextFrame()
		{
			if (this.nextFrameIndex + 1 == frames.Count)
			{
				var readFrame = this.reader.ReadLine();
				if (readFrame == null)
				{
					this.playTime = this.nextFrame.t - this.playTimeOffset;
					return false;
				}
				this.frames.Add(readFrame);
			}

			this.nextFrameIndex++;
			this.lastFrameIndex++;

			if (this.lastFrame != null) ManageEvents(this.lastFrame, this.nextFrame, forward: true);

			return true;
		}

		public bool PrevFrame()
		{
			if (this.lastFrameIndex > 0)
			{
				this.lastFrameIndex--;
				this.nextFrameIndex--;

				ManageEvents(this.lastFrame, this.nextFrame, forward: false);
				return true;
			}
			else
			{
				this.playTime = this.lastFrame.t - this.playTimeOffset;
				return false;
			}
		}

		/// <summary>
		/// Plays all recordable object events from a frame, interpolating towards the next one
		/// </summary>
		/// <param name="lastFrame">A frame data</param>
		/// <param name="nextFrame">Data of the next frame, for interpolation</param>
		/// <param name="forward">Set to true to play events forward, otherwise events will be played backwards</param>
		private void ManageEvents(FullBodyTrackingData lastFrame, FullBodyTrackingData nextFrame, bool forward = true)
		{
			//	Debug.LogWarning("mngevt " + this.lastFrameIndex + " " + nextFrameIndex + " " + playTime);

			// swap frames if playing backwards (lastFrame events are resolved in forward mode, nextFrame events are resolved in backward mode)
			if (!forward)
			{
				var tmp = lastFrame;
				lastFrame = nextFrame;
				nextFrame = tmp;
			}

			// manage non-update events
			foreach (var ev in lastFrame.Events)
			{
				if (ev.IsUpdate) continue;

				RecordableObject robj;
				this.recObjects.TryGetValue(ev.id, out robj);

				var @event = forward ? ev : ev.ReverseEvent();

				if (@event.IsCreate && robj == null)
				{
					var prefab = this.replayPrefabs.Find(p => p.replayPrefabID == @event.arg);

					GameObject instance;

					if (prefab) instance = GameObject.Instantiate(prefab.gameObject);
					else
					{
						instance = new GameObject();
						robj = instance.AddComponent<RecordableObject>();
					}

					instance.transform.parent = this.transform;

					var rigidbody = instance.GetComponent<Rigidbody>();
					if (rigidbody) rigidbody.isKinematic = true;

					this.recObjects[@event.id] = robj = instance.GetComponent<RecordableObject>();
				}

				if (robj) robj.ReplayEvent(this.transform, @event, rIHand, lIHand);

				if (@event.IsDelete)
				{
					this.recObjects.Remove(@event.id);
				}
			}

			// re-swap frames, update events use interpolation to be played forwards and backwards
			if (!forward)
			{
				var tmp = lastFrame;
				lastFrame = nextFrame;
				nextFrame = tmp;
			}

			// manage update events
			foreach (var @event in lastFrame.UpdateEvents)
			{
				var nextEvent = nextFrame.GetEvent(@event.id, @event.EventType);

				// propagate update event to next frame if there isn't an update event for that object in the next frame
				if (forward && nextEvent == null)
				{
					nextEvent = @event;
					nextFrame.AddEvent(nextEvent);
				}

				RecordableObject robj;
				if (this.recObjects.TryGetValue(@event.id, out robj))
				{
					robj.ReplayEvent(this.transform, @event, rIHand, lIHand);
					robj.SetNextFrame(this.transform, nextEvent);
				}
			}
		}

		private void Play(float delta)
		{
			if (this.nextFrame == null || this.lastFrame == null) return;

			bool prevOk = true, nextOk = true;

			while (this.playTime + this.playTimeOffset > this.nextFrame.t) nextOk = this.NextFrame();
			while (this.playTime + this.playTimeOffset < this.lastFrame.t) prevOk = this.PrevFrame();

			if (prevOk && nextOk) this.playTime += delta;
			else playing = false;

			var t = this.playTime + this.playTimeOffset;
			var interpolation = (t - lastFrame.t) / (nextFrame.t - lastFrame.t);

			foreach (var part in this.bodyTrackedObjects.Keys)
			{
				var tr = this.bodyTrackedObjects[part].transform;

				Vector3 pos;
				Quaternion rot;

				Interpolate(part, out pos, out rot, interpolation);

				tr.localPosition = pos;
				tr.localRotation = rot;
			}

			foreach (var robj in this.recObjects.Values)
			{
				robj.Interpolate(this.transform, interpolation);
			}
		}

		public void Update()
		{
			if (!initialized)
			{
				this.initialized = true;
				if (fileURL != null && fileURL.Length > 0) this.ReadFile(fileURL);
			}

			if (playing)
			{
				Play(this.playSpeed * Time.deltaTime);
			}
		}
	}
}