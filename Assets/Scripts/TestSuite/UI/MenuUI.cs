using UnityEngine;
using StudyStore;
using Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System;

namespace TestSuite.UI
{
    public class MenuUI : View
    {
        public TestSuite testSuite;
        public TestSuiteUI testSuiteUI;
        public DataViewUI dataView;

        private bool waitForNewExp = false;
        private List<ExperimentIndex> restorableSessions;
        private HashSet<string> restorableSessionsSet;

        private Study currentStudy;

        // selected experiment index
        private bool showRestoreWindow = false;
        private ExperimentIndex selectedExperimentToRestore = null;
        private Vector2 popupScroll;

        // saved data
        private bool showSavedWindow = false;
        private List<Experiment> savedExperiments = new List<Experiment>();
        private Experiment? selectedExperimentToView = null;

        public override bool HidesCameraView => true;
        public override bool ShowBackgroundScreen => true;
        public override bool AllowsApplicationExit => true;

        public override void OnMadeVisible()
        {
            waitForNewExp = false;
            currentStudy = API.Instance.CurrentStudy ?? default;
            restorableSessions = FindRestorableSessions();
            restorableSessionsSet = new HashSet<string>();
            foreach (var session in restorableSessions) restorableSessionsSet.Add(session.experimentId);

            showRestoreWindow = false;
            selectedExperimentToRestore = null;
            popupScroll = Vector2.zero;

            selectedExperimentToView = null;
            savedExperiments.Clear();

            API.Instance.ListExperiments(expList =>
            {
                savedExperiments.Clear();
                foreach (var exp in expList) if (!this.restorableSessionsSet.Contains(exp.uuid)) this.savedExperiments.Add(exp);

            }, err => { });
        }

        private void ExperimentGUI(Experiment experiment, Vector2 size)
        {
            GUI.Box(new Rect(Vector2.one, size - Vector2.one * 2), "");

            GUI.color = Color.white;

            if (!waitForNewExp && GUI.Button(new Rect(15, 15, 180, 30), Localization.Format("$ui:viewDataButton")))
            {
                this.selectedExperimentToView = experiment;
                ViewExperimentData();
            }

            GUI.color = Color.white;

            GUI.Label(new Rect(200, 8, 1300, 20), ExperimentIndex.ToString(experiment));
            GUI.Label(new Rect(200, 35, 1300, 20), Localization.Format("$ui:expStateSent"));
        }

        private void ExperimentIndexGUI(ExperimentIndex eIndex, Vector2 size)
        {
            GUI.Box(new Rect(Vector2.one, size - Vector2.one * 2), "");

            GUI.color = Color.yellow;

            if (!waitForNewExp && GUI.Button(new Rect(15, 15, 180, 30), Localization.Format(eIndex.readyForQuestionnaire ? "$ui:fillForm" : "$ui:continueExperiment")))
            {
                this.selectedExperimentToRestore = eIndex;
                LoadExperiment();
            }

            GUI.color = Color.white;

            GUI.Label(new Rect(200, 8, 1300, 20), eIndex.ToString());
            GUI.Label(new Rect(200, 35, 1300, 20), Localization.Format(eIndex.readyForQuestionnaire ? "$ui:expStateWaitForQ" : "$ui:expStateIncomplete"));
        }

        public override void OnViewGUI(Vector2 screen)
        {
            GUI.color = Color.white;

            string patient = API.Instance?.CurrentSubject?.anonymizationId ?? "";
            string study = API.Instance?.CurrentStudy?.title ?? "";

            Rect rect = new Rect(75, screen.y / 2 - 200, 240, 90);
            //MainUI.LightBox(rect);
            GUI.Label(rect, Localization.Format("$ui:menuStudyAndPatientStatus::2", study, patient));

            GUI.color = Color.green;
            
            if (MainUI.Button(new Rect(75, screen.y / 2 - 100, 240, 65), Localization.Format("<b>$ui:newExperiment</b>"), enabled: !waitForNewExp))
            {
                NewExperiment();
            }

            GUI.color = Color.white;

            int expCount = ((restorableSessions?.Count ?? 0) + (savedExperiments?.Count ?? 0));
            GUI.Box(new Rect(340, 75, screen.x - 350, screen.y - 150), Localization.Format("$ui:experimentRecords"), "window");

            popupScroll = GUI.BeginScrollView(new Rect(350, 95, screen.x - 370, screen.y - 200), popupScroll, new Rect(0, 0, 340, 25 + 60 * expCount));
            {
                if (expCount == 0) GUI.Label(new Rect(5, 5, 300, 20), Localization.Format("$ui:noExperimentRecord"));

                Vector2 anchor = Vector2.zero;
                Vector2 dim = new Vector2(screen.x - 370, 60);

                if (this.restorableSessions != null)
                {
                    foreach (var exp in this.restorableSessions)
                    {
                        GUI.BeginGroup(new Rect(anchor, dim));
                        ExperimentIndexGUI(exp, dim);
                        GUI.EndGroup();
                        anchor.y += 60;
                    }
                }

                if (this.savedExperiments != null)
                {
                    foreach (var exp in this.savedExperiments)
                    {
                        GUI.BeginGroup(new Rect(anchor, dim));
                        ExperimentGUI(exp, dim);
                        GUI.EndGroup();
                        anchor.y += 60;
                    }
                }
            }
            GUI.EndScrollView();
        }

        public void OnViewGUI_Old(Vector2 screen)
        {
            GUI.color = Color.white;

            string patient = API.Instance?.CurrentSubject?.anonymizationId ?? "";
            string study = API.Instance?.CurrentStudy?.title ?? "";

            Rect rect = new Rect(75, screen.y / 2 - 200, 240, 90);
            //MainUI.LightBox(rect);
            GUI.Label(rect, Localization.Format("$ui:menuStudyAndPatientStatus::2", study, patient));

            GUI.color = Color.green;

            if (MainUI.Button(new Rect(75, screen.y / 2 - 100, 240, 65), Localization.Format("<b>$ui:newExperiment</b>"), enabled: !waitForNewExp))
            {
                NewExperiment();
            }

            if (restorableSessions != null && restorableSessions.Count > 0)
            {
                GUI.color = MainUI.BlinkColor(Color.yellow);
                if (GUI.Button(new Rect(75, screen.y / 2 - 25, 240, 40), Localization.Format("$ui:continueExperiment")))
                {
                    showRestoreWindow = !showRestoreWindow;
                    showSavedWindow = false;
                }
            }

            GUI.color = Color.white;
            if (GUI.Button(new Rect(75, screen.y / 2 + 25, 240, 40), Localization.Format("$ui:exploreData")))
            {
                showSavedWindow = !showSavedWindow;
                showRestoreWindow = false;
            }

            GUI.color = Color.white;

            OnViewGUIRestoreSessionWindow(screen);
            OnViewGUISavedExperiments(screen);
        }

        public void OnViewGUIRestoreSessionWindow(Vector2 screen)
        {
            if (!showRestoreWindow || restorableSessions == null || waitForNewExp) return;

            var rect = MainUI.CenteredRect(screen, new Vector2(300, 275));

            GUI.Box(MainUI.CenteredRect(screen, new Vector2(320, 350)), Localization.Format("<b>$ui:selectExperimentToContinue</b>"), "window");

            MainUI.SelectFromList(ref selectedExperimentToRestore, i => restorableSessions[i], restorableSessions.Count, rect, ref popupScroll);

            if (MainUI.Button(new Rect(rect.position + new Vector2(150, 278), MainUI.ButtonSize), Localization.Format("$ui:select"), enabled: selectedExperimentToRestore != null))
            {
                LoadExperiment();
            }
        }

        public void OnViewGUISavedExperiments(Vector2 screen)
        {
            if (!showSavedWindow || waitForNewExp) return;

            var rect = MainUI.CenteredRect(screen, new Vector2(300, 275));

            GUI.Box(MainUI.CenteredRect(screen, new Vector2(320, 350)), Localization.Format("<b>$ui:exploreData</b>"), "window");

            MainUI.SelectFromList(ref selectedExperimentToView, i => savedExperiments[i], savedExperiments.Count, rect, ref popupScroll, toString: e => ExperimentIndex.ToString((Experiment)e));

            if (MainUI.Button(new Rect(rect.position + new Vector2(150, 278), MainUI.ButtonSize), Localization.Format("$ui:select"), enabled: selectedExperimentToView != null))
            {
                ViewExperimentData();
            }
        }

        public void ViewExperimentData()
        {
            API.Instance.UnserializeFile<ExperimentIndex>((Experiment)selectedExperimentToView, "index.json", (eIndex) =>
            {
                dataView.SetData(eIndex, fromLocal: false);
                MainUI.PushView(dataView);
            }, err =>
            {
                MainUI.Notify("$ui:noDataFound", 1);
                Debug.LogError("Experiment Data could not be loaded: " + err.FullError);

            });
        }

        public void NewExperiment()
        {
            waitForNewExp = true;

            API.Instance.CreateExperimentIndex(
                (exp, eIndex) =>
                {
                    testSuite.StartWith(eIndex, currentStudy.configuration);
                    testSuiteUI.formMode = eIndex.readyForQuestionnaire;
                    MainUI.PushView(testSuiteUI);
                    waitForNewExp = false;
                },
                err =>
                {
                    MainUI.Notify("$ui:cannotCreateExperiment " + err.MessageToDisplay, 2);
                    waitForNewExp = false;
                }
                );
        }

        public void LoadExperiment()
        {
            waitForNewExp = true;

            API.Instance.GetExperiment(this.selectedExperimentToRestore.experimentId,
                (exp) =>
                {
                    testSuite.StartWith(this.selectedExperimentToRestore, currentStudy.configuration);
                    testSuiteUI.formMode = this.selectedExperimentToRestore.readyForQuestionnaire;
                    MainUI.PushView(testSuiteUI);
                    waitForNewExp = false;
                },
                err =>
                {
                    Debug.LogError(err.exception);
                    MainUI.Notify("$ui:cannotLoadExperiment", 2);
                    waitForNewExp = false;
                }
                );
        }

        /// <summary>
        /// Searches this machine for previous experiment sessions that were not completed
        /// </summary>
        private List<ExperimentIndex> FindRestorableSessions()
        {
            var restorablesSessions = new List<ExperimentIndex>();

            string subjectDir = Config.OutputDirectory + "\\" + API.Instance.CurrentStudy?.uuid + "\\" + API.Instance.CurrentSubject?.uuid; 

            if (Directory.Exists(subjectDir))
                foreach (string expDir in Directory.GetDirectories(subjectDir))
                {
                    try
                    {
                        var index = JSONSerializer.FromJSONFile<ExperimentIndex>(expDir + "\\index.json");
                        restorablesSessions.Add(index);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e.Message);
                    }
                }

            return restorablesSessions.OrderBy(e => -e.timestamp).ToList();
        }
    }
}