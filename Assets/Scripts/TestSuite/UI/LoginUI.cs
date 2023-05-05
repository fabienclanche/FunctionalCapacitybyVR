using UnityEngine;
using StudyStore;
using Utils;
using FullBodyTracking;

namespace TestSuite.UI
{
	public class LoginUI : View
	{
		public MenuUI mainMenu;
		public TPoseCalibrator calibrator;

		// study list 
		private StudyList? studyList = null;
		private APIError? studyListError = null;
		private bool studyListRequested = false;
		private Vector2 studiesScroll;
		private bool study_waitForAPI = false;

		// subject selection
		private bool createNewSubject = true;
		public string anonID = "";
		private string subjectError = null;
		private bool subject_waitForAPI = false;

		private bool subjectValidated = false;

		public override bool HidesCameraView => true;
		public override bool ShowBackgroundScreen => true;
		public override bool AllowsApplicationExit => true;

		public override void OnViewGUI(Vector2 screenDimensions)
		{
			if (UserConfig.ForceOfflineMode)
			{
				if (API.Instance.LoggedUser == null)
				{
					if (API.Instance as DummyAPI == null)
					{
						API.Instance = new DummyAPI();
						MainUI.LoginPrompt(overrideExistingPrompt: false, defaultEmail: "local-user", defaultPassword: "********", forceConfirm: true);
					}
				}
				else if (!subjectValidated) StudyAndSubjectScreen(screenDimensions);
				else LoginCompleted();
			}
			else
			{
				if (API.Instance.LoggedUser == null) LoginScreen(screenDimensions);
				else if (!subjectValidated) StudyAndSubjectScreen(screenDimensions);
				else LoginCompleted();
			}
		}

		public void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKey(KeyCode.Escape) && UserConfig.DevMode)
			{
				if (API.Instance.LoggedUser == null)
				{
					API.Instance = new DummyAPI();
					MainUI.LoginPrompt(overrideExistingPrompt: true, defaultEmail: "local-user", defaultPassword: "********", forceConfirm: true);
				}
				else
				{
					API.Instance.SelectStudy(DummyAPI.OfflineStudy,
							(study) => API.Instance.CreateSubject("Test",
								(subject) => subjectValidated = true,
								(er) => { API.Instance.SelectSubject("Test", (subject) => subjectValidated = true, err => { }); }),
							(er) => { });
				}
			}
#endif
        }

		private void LoginCompleted()
		{
			this.MainUI.PushView(mainMenu);
		}

		private void LoginScreen(Vector2 screenDimensions)
		{
			MainUI.LoginPrompt();

			if (API.Instance as DummyAPI == null)
			{
				GUI.color = Color.red;
				if (GUI.Button(new Rect(screenDimensions - new Vector2(125, 35), new Vector2(120, 30)), Localization.Format("$api:offlineMode")))
				{
					MainUI.ConfirmAction("$ui:goOfflineConfirmHeader", "$ui:goOfflineConfirmText",
						() =>
						{
							API.Instance = new DummyAPI();
							MainUI.LoginPrompt(overrideExistingPrompt: true, defaultEmail: "local-user", defaultPassword: "********", forceConfirm: true);
						},
						cancelAction: () => { },
						severity: 1);
				}
				GUI.color = Color.white;
			}

#if UNITY_EDITOR
			GUI.changed = false;
			if (UserConfig.AllowConnectionToTestServer)
			{
				GUI.color = Color.red;
				if (GUI.Button(new Rect(screenDimensions - new Vector2(245, 70), new Vector2(240, 30)), Localization.Format("TEST SERVER")))
				{
					MainUI.ConfirmAction("Connect to Test Server", "Confirm connection to unsecure server : " + UserConfig.TestServer,
						() =>
						{
							API.Instance = new OnlineAPI(UserConfig.TestServer, UserConfig.TestServerCert);
						},
						cancelAction: () => { },
						severity: 2);
				}
				GUI.color = Color.white;
			}
#endif
		}

		public override void OnMadeVisible()
		{
			studyList = null;
			studyListError = null;
			studyListRequested = false;
			createNewSubject = true;
			anonID = "";
			subject_waitForAPI = false;
			subjectError = null;
			subjectValidated = false;

			if (MainUI.PreviousView)
			{
				calibrator.UndoCalibration();
				calibrator.ResetTransform();
			}
		}

		public void OnSubjectSelected(Subject subject)
		{
			subjectValidated = true;
			subjectError = null;
			subject_waitForAPI = false;
		}

		public void HandleSubjectSelectionError(APIError error)
		{
			subject_waitForAPI = false;

			if (error.status == 400)
			{
				subjectError = Localization.Format("$api:error:subjectAlreadyExists::1", anonID);
			}
			else if (error.status == 404)
			{
				subjectError = Localization.Format("$api:error:subjectDoesNotExist::1", anonID);
			}
			else
			{
				subjectError = Localization.Format(error.MessageToDisplay);
			}
		}

		private void AnonIDFieldGUI()
		{
			GUILayout.Label(Localization.Format("\t$ui:subjectID"));

			if (subject_waitForAPI) GUILayout.Box(anonID);
			else anonID = GUILayout.TextField(anonID);

			if (subjectError != null) GUI.color = Color.red;
			GUILayout.Label(subjectError ?? "");
			GUI.color = Color.white;
		}

		private void StudyAndSubjectScreen(Vector2 screenDimensions)
		{
			if (studyList == null && !studyListRequested)
			{
				studyListRequested = true;
				API.Instance.ListStudies(
					list =>
					{
						studyList = list;
						studyListError = null;
						studyListRequested = false;
					},
					error =>
					{
						studyList = null;
						studyListError = error;
						studyListRequested = false;
					});
			}

			Rect mainRect = MainUI.CenteredRect(screenDimensions, 640, 480);
			GUI.Box(mainRect, Localization.Format("<b>$ui:studySelectWindow</b>"), "window");

			GUILayout.BeginArea(mainRect);
			{
				// STUDY 
				float w = (mainRect.width - 20);
				GUI.Label(new Rect(10, 25, w / 2, 240), Localization.Format("$ui:studySelect"));

				if (studyListRequested) GUILayout.Label(Localization.Format("$ui:waitingForAPI"));
				else if (studyListError != null) GUILayout.Label(Localization.Format("<color=red>" + studyListError?.MessageToDisplay + "</color>"));
				else if (studyList != null)
				{
					Study? selected = API.Instance.CurrentStudy;
					int length = ((StudyList)studyList).Length;

					bool changed = MainUI.SelectFromList(ref selected, i => studyList?[i], length, new Rect(10 + w / 2, 25, w / 2, 240), ref studiesScroll,
						study => study?.title, study => study?.description);

					if (selected != null && changed && !study_waitForAPI)
					{
						study_waitForAPI = true;
						API.Instance.SelectStudy((Study)selected,
							(study) => { study_waitForAPI = false; Localization.PatchFromConfig(study.configuration); },
							(err) => { studyListError = err; studyList = null; study_waitForAPI = false; });
					}
				}

				// SUBJECT
				if (API.Instance.CurrentStudy != null)
				{
					GUI.Label(new Rect(10, 300, w / 2, 200), Localization.Format("$ui:subjectSelect"));

					GUILayout.BeginArea(new Rect(10 + w / 2, 300, w / 2, 210));
					GUILayout.BeginVertical();
					{
						GUI.color = createNewSubject ? Color.cyan : Color.white;
						createNewSubject = GUILayout.Toggle(createNewSubject, Localization.Format("$ui:newSubject"));
						GUI.color = Color.white;
						if (createNewSubject) AnonIDFieldGUI();

						GUI.color = !createNewSubject ? Color.cyan : Color.white;
						createNewSubject = !GUILayout.Toggle(!createNewSubject, Localization.Format("$ui:existingSubject"));
						GUI.color = Color.white;
						if (!createNewSubject) AnonIDFieldGUI();

						if (anonID.Length > 0)
						{
							if (GUILayout.Button(Localization.Format("$ui:confirm")))
							{
								subject_waitForAPI = true;
								if (createNewSubject) API.Instance.CreateSubject(anonID, OnSubjectSelected, HandleSubjectSelectionError);
								else API.Instance.SelectSubject(anonID, OnSubjectSelected, HandleSubjectSelectionError);
							}
						}
						else
						{
							GUILayout.Box(Localization.Format("$ui:confirm"));
						}

					}
					GUILayout.EndVertical();
					GUILayout.EndArea();
				}
			}
			GUILayout.EndArea();
		}

	}
}