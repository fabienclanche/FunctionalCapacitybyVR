using FullBodyTracking;
using FullBodyTracking.Mocap;
using StudyStore;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Utils;

namespace TestSuite.UI
{
	public class ReplayUI : View
	{
		public MocapReplay mocapReplay;
		/// <summary>
		/// (optional) The VR IK rig to be moved at the same time than the replay one
		/// </summary>
		public Transform vrIkRig;
		public Camera replayCamera;

		public Texture2D playIcon, pauseIcon, fwdIcon, bwdIcon, stopIcon, nextIcon, prevIcon, cameraIcon;

		public delegate void ReplayStreamCallback(StreamReader reader);
		public delegate void ReplayStreamProvider(ReplayStreamCallback streamCallback, Action error);

		private List<string> filesLabels = new List<string>();
		private List<ReplayStreamProvider> streamProviders = new List<ReplayStreamProvider>();
		private List<float> durations = new List<float>();

		private Vector2 fileListScroll;

		private int currentFile = -1;

		private bool waitingForStream = false;
		private bool streamFailedToLoad = false;

		public bool ReplayFileOK => currentFile != -1 && !waitingForStream && !streamFailedToLoad;
		public override bool HidesCameraView => true;

		/// <summary>
		/// True if the replay has a speed different than 0. 
		/// Setting this to true when it was previously false will set the speed to 1. 
		/// Setting this to false will set the speed to 0.
		/// </summary>
		public bool Playing
		{
			get { return mocapReplay.playSpeed != 0; }
			set
			{
				if (Playing != value) mocapReplay.playSpeed = value ? 1 : 0;

				mocapReplay.playing = true;
			}
		}

		/// <summary>
		/// The replay speed factor. 0 when not playing.
		/// </summary>
		public float PlaySpeed
		{
			get { return mocapReplay.playSpeed; }
			set
			{
				mocapReplay.playSpeed = value;
				mocapReplay.playing = true;
			}
		}

		public void ClearFiles()
		{
			filesLabels.Clear();
			streamProviders.Clear();
			durations.Clear();
			currentFile = -1;
			waitingForStream = false;
			streamFailedToLoad = false;
		}

		public void AddFile(string label, float duration, ReplayStreamProvider streamProvider)
		{
			filesLabels.Add(label);
			durations.Add(duration);
			streamProviders.Add(streamProvider);
		}

		private static string ReplayTimestamp(float time)
		{
			int seconds = (int)time;
			int minutes = seconds / 60;
			seconds -= minutes * 60;
			return string.Format("{0:00}", minutes) + ":" + string.Format("{0:00}", seconds);
		}

		public void SelectFile(int i)
		{
			if (waitingForStream) return;

			if (currentFile != i && i >= 0 && i < streamProviders.Count)
			{
                FootTracker.ClearFootsteps();
                Playing = false;

				currentFile = i;

				waitingForStream = true;

				streamProviders[i](
						stream =>
						{
							mocapReplay.ReadStream(stream);
							waitingForStream = false;
							streamFailedToLoad = false;
							if(this.vrIkRig)
							{
								this.vrIkRig.position = mocapReplay.transform.position;
								this.vrIkRig.rotation = mocapReplay.transform.rotation;
							}
						},
						() =>
						{
							waitingForStream = false;
							streamFailedToLoad = true;
						}
					);
			}
		}

		public override void OnMadeVisible()
		{
			base.OnMadeVisible();

			this.MainUI.SelectExternalCamera(replayCamera);
			Playing = false;
            FootTracker.ClearFootsteps();
        }

		public override void OnClosed()
		{
			this.MainUI.DisableExternalCamera(replayCamera);
			Playing = false;
            FootTracker.ClearFootsteps();
		}

		/// <summary>
		/// Inits the replay screen, retrieving mocap data from the temporary local data
		/// </summary>
		/// <param name="index">Experiment index listing the experiment files</param>
		public void InitFromLocalFiles(ExperimentIndex index)
		{
			ClearFiles();

			foreach (var entry in index.contents)
			{
				if (entry == null) continue;
				if (entry.mocapFile == null || entry.mocapFile.Length == 0) continue;

				AddFile(Localization.LocalizeDefault(entry.name), entry.mocapLength, (streamCallback, errorCallback) =>
				 {
					 try
					 {
						 var fileReader = JSONSerializer.FileReader(index.RootDirectory + entry.mocapFile);
						 streamCallback(fileReader);
					 }
					 catch (IOException e)
					 {
						 errorCallback();
					 }
				 });
			}
		}

		/// <summary>
		/// Inits the replay screen, retrieving mocap data from the online API
		/// </summary>
		/// <param name="index">Experiment index listing the experiment files</param>
		public void InitRemote(ExperimentIndex index)
		{
			ClearFiles();

			foreach (var entry in index.contents)
			{
				if (entry == null || entry.mocapFile == null || entry.mocapFile.Length == 0) continue;

				AddFile(Localization.LocalizeDefault(entry.name), entry.mocapLength, (streamCallback, errorCallback) =>
				{
					API.Instance.ReadFile(index, entry.mocapFile, stream => streamCallback(stream), err => errorCallback());
				});
			}
		}

		/// <summary>
		/// Draws the timeline
		/// </summary>
		public void OnGUITimeline()
		{
			GUILayout.BeginVertical();

			if (this.ReplayFileOK)
			{
				float totalDuration = currentFile >= 0 ? durations[currentFile] : 0;
				string playStatus = "► " + mocapReplay.playSpeed.ToString("F1") + "x";

				GUILayout.Label(ReplayTimestamp(mocapReplay.playTime) + " / " + ReplayTimestamp(totalDuration) + "\t" + (mocapReplay.playing ? playStatus : ""));

				GUI.changed = false;
				mocapReplay.playTime = GUILayout.HorizontalSlider(mocapReplay.playTime, 0, totalDuration);
				if (GUI.changed)
				{
					if (mocapReplay.playing == false)
					{
						mocapReplay.playing = true;
						mocapReplay.playSpeed = 0;
					}
				}
			}
			else
			{
				if (this.streamFailedToLoad) GUILayout.Label(Localization.Format("<color=red>$ui:replayCantLoadStream</color>"));
				else GUILayout.Label(Localization.Format("$ui:replayWaitingForStream"));
				GUILayout.HorizontalSlider(0, 0, 0);
			}

			GUILayout.EndVertical();
		}

		/// <summary>
		/// Draws the play/pause controls buttons
		/// </summary>
		public void OnGUIControlsBar()
		{
			GUILayout.BeginHorizontal();

			bool endReached = ReplayFileOK && Mathf.Abs(mocapReplay.playTime - this.durations[currentFile]) < 0.01f;

			if (GUILayout.Button(bwdIcon) && ReplayFileOK)
			{
				Playing = true;
				if (PlaySpeed > 1) PlaySpeed /= 2;
				else if (PlaySpeed > -1) PlaySpeed = -1;
				else if (PlaySpeed > -8) PlaySpeed *= 2;
			}

			if (GUILayout.Button(prevIcon) && ReplayFileOK)
			{
				PlaySpeed = 0;
				mocapReplay.playTime -= 0.1f;
			}

			GUI.color = Playing && !endReached ? MainUI.BlinkColor(Color.yellow) : Color.green;
			if (GUILayout.Button(Playing && !endReached ? pauseIcon : playIcon) && ReplayFileOK)
			{
				Playing = !(Playing && !endReached);

				// loop if at the end
				if (Playing && endReached) mocapReplay.playTime = 0;
			}

			GUI.color = (Color.red * 2 + Color.white) / 3;
			if (GUILayout.Button(stopIcon) && ReplayFileOK)
			{
				PlaySpeed = 0;
				mocapReplay.playTime = 0;
                FootTracker.ClearFootsteps();
            }

			GUI.color = Color.white;
			if (GUILayout.Button(nextIcon) && ReplayFileOK)
			{
				mocapReplay.playing = true;
				mocapReplay.playSpeed = 0;
				mocapReplay.playTime += 0.1f;
			}

			if (GUILayout.Button(fwdIcon) && ReplayFileOK)
			{
				mocapReplay.playing = true;

				if (mocapReplay.playSpeed < -1) mocapReplay.playSpeed /= 2;
				else if (mocapReplay.playSpeed < 1) mocapReplay.playSpeed = 1;
				else if (mocapReplay.playSpeed < 8) mocapReplay.playSpeed *= 2;
			}

			GUILayout.EndHorizontal();
		}

		public void OnGUIFileSelection(Vector2 screen)
		{
			Vector2 viewportPos = new Vector2(5, 215);
			Vector2 viewportSize = Vector2.Scale(new Vector2(.2f, .45f), screen);

			// TEST LIST

			float contentHeight = 10 + 25 * this.streamProviders.Count;

			GUI.Box(new Rect(viewportPos, viewportSize), "");
			fileListScroll = GUI.BeginScrollView(new Rect(viewportPos, viewportSize), fileListScroll, new Rect(0, 0, .18f * screen.x, contentHeight));

			if (this.waitingForStream)
			{
				GUI.Label(new Rect(5, 5, viewportSize.x - 10, 25), Localization.Format("$ui:replayWaitingForStream"));
			}
			else
			{
				GUI.Label(new Rect(5, 5, viewportSize.x - 10, 25), Localization.Format("<b>$ui:availableReplays</b>"));

				for (int i = 0; i < this.streamProviders.Count; i++)
				{
					GUI.color = i == currentFile ? Color.cyan : Color.white;

					if (GUI.Toggle(new Rect(5, 30 + 25 * i, viewportSize.x - 10, 25), i == currentFile, this.filesLabels[i]))
					{
						SelectFile(i);
					}
				}
			}

			GUI.EndScrollView();
		}

		public override void OnViewGUI(Vector2 screenDimensions)
		{
			GUI.BeginGroup(new Rect(new Vector2(screenDimensions.x / 2 - 150, screenDimensions.y - 100), new Vector2(300, 100)));
			GUILayout.BeginVertical();
			OnGUITimeline();
			OnGUIControlsBar();
			GUILayout.EndVertical();
			GUI.EndGroup();

			OnGUIFileSelection(screenDimensions);

			CalibrationUI.AvatarSelectionGUI(screenDimensions, Vector2.zero, mocapReplay.tPoseCalibrator);
		}
	}
}