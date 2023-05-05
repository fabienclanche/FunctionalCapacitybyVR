using StudyStore;
using System;
using System.IO;
using UnityEngine;
using Utils;

namespace TestSuite.UI
{
	public class UploadUI : View
	{
		public MenuUI menuUI;
		public TestSuiteUI testSuiteUI;

		public override bool HidesCameraView => true;
        public override bool ShowBackgroundScreen => true;

        private float progress = 0;
		private string message = "";

		private string buttonLabel = null;
		private Action buttonAction = null;

		public override bool BackButtonEnabled => false;

		public override void OnMadeVisible()
		{
			progress = 0;
			message = "";
			buttonLabel = null;
			buttonAction = null;
		}

		private void TrackProgress(int currentFile, int totalFiles, string message)
		{
			this.progress = (currentFile / (float)totalFiles);
			this.message = Localization.Format(message) + " (" + currentFile + " / " + totalFiles + ")";
		}

		private void TrackError(int attempt, APIError error)
		{
			this.message = Localization.Format("$ui:uploadRetry (" + attempt + ")" + error.MessageToDisplay);
		}

		public void BeginUpload(ExperimentIndex experimentIndex, Stream log = null)
		{
			Debug.Log(API.Instance);
			API.Instance.UploadAll(experimentIndex,
				progress: TrackProgress,
				done: () => { message = Localization.Format("$ui:uploadDone"); Done(); },
				errors: TrackError,
				failure: () => { message = Localization.Format("<color='red'>$ui:uploadFailed (" + message + ")</color>"); Failure(); },
				deleteFilesOnSuccess: true,
				log: log
				);
		}

		public void Failure()
		{
			buttonLabel = "$ui:back";
			buttonAction = () => MainUI.ReturnToView(testSuiteUI);
		}

		public void Done()
		{
			buttonLabel = "$ui:continue";
			buttonAction = () => MainUI.ReturnToView(menuUI);
		}

		public override void OnViewGUI(Vector2 screen)
		{
			GUI.color = Color.white;
			GUI.Box(MainUI.ModalWindowRect, Localization.Format("<b>$ui:uploadHeader</b>"), "window");

			GUI.BeginGroup(MainUI.ModalWindowRect);

			GUI.Label(new Rect(20, 100, MainUI.ModalWindowRect.width - 40, 65), message);

			GUI.color = new Color(1, 1, 1, 0.5f);
			GUI.Box(new Rect(50, 175, MainUI.ModalWindowRect.width - 100, 10), "");
			GUI.color = new Color(3, 1, 1, 1);
			GUI.Box(new Rect(50, 175, (MainUI.ModalWindowRect.width - 110) * progress + 10, 10), "");

			if (buttonLabel != null && buttonAction != null)
			{
				Rect buttonRect = new Rect(MainUI.ModalWindowRect.width / 2 - 75, MainUI.ModalWindowRect.height - 40, 150, MainUI.ButtonSize.y);

				if (GUI.Button(buttonRect, Localization.Format(buttonLabel)))
				{
					buttonAction();
				}
			}

			GUI.EndGroup();
		}
	}
}