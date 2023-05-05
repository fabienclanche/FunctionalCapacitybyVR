using UnityEngine;
using UnityEngine.UI;
using System;
using FullBodyTracking;
using TestSuite.Metrology;
using Utils;
using System.Collections.Generic;

namespace TestSuite.UI
{
    public class TestSuiteUI : View
    {
        public bool formMode;

        public DataViewUI dataViewUI;
        public UploadUI uploadUI;
        public CalibrationUI calibrationUI;

        public TPoseCalibrator calibrator;
        public TestSuite suite;
        public Text currentDescription;
        public Text testList;
        public TextMesh testList_textMesh;

        [Header("Audio Source")]
        public AudioSource audioSource;
        public AudioClip goSignal;

        private string doneLabel = Localization.LocalizeDefault("$ui:testDone");
        private string runningLabel = Localization.LocalizeDefault("$ui:testRunning");

        private List<InputHelper> inputHelpers = new List<InputHelper>();

        public override bool BackButtonEnabled => suite.RunningTest == null;

        public override bool HidesCameraView => formMode;
        public override bool ShowBackgroundScreen => formMode;

        private int calibrationStepsLeft = 0;

        public override void OnMadeVisible()
        {
            base.OnMadeVisible();
            this.enabled = true;
            if (suite && suite.IKRig == null && !formMode) MainUI.SwapView(this, calibrationUI);
        }

        public override void OnClosed()
        {
            base.OnClosed();
            this.enabled = false;
        }

        public void Start()
        {

#if UNITY_EDITOR
            if (UserConfig.DevMode)
            {
                // debug inputs
                inputHelpers.Add(new HoldInput(
                    () => (suite.RunningTest == null && (Input.GetAxisRaw("RTrigger") > 0.9 || Input.GetAxisRaw("LTrigger") > 0.9)),
                    1f,
                    () => { suite.RunNextTest(); suite.InstructionsOK = true; }
                ));
            }
#endif
             
            this.enabled = false;
        }

        private void Update()
        {
            foreach (var helper in this.inputHelpers) helper.Update();
        }

        public override void OnViewGUI(Vector2 screen)
        {
            if (suite == null) return;

            if (this.calibrationStepsLeft > 0)
            {
                try { calibrator.StartFullBodyTracking(); }
                catch { }

                this.calibrationStepsLeft--;
                return;
            }

            OnGUITestList(screen);
            OnGUIInstructions(screen);

            if (false && GUI.Button(new Rect(5, 10 + .45f * screen.y, 100, 30), "DataViewUI"))
            {
                List<Test.TestData> testData = new List<Test.TestData>();
                for (int i = 0; i < suite.TestCount; i++) ;

                //dataView.SetData();
                this.MainUI.PushView(dataViewUI);
            }
        }

        bool instructionsClipStarted = false;
        Vector2 instructionsScroll = default;

        public void OnGUIInstructions(Vector2 screen)
        {
            if (this.audioSource == null)
            {
                this.audioSource = suite.gameObject.AddComponent<AudioSource>();
            }

            if (!suite.RunningTest) return;

            if (!suite.InstructionsOK)
            {
                float audioTotalTime = (audioSource.clip != null) ? audioSource.clip.length : 0;
                float audioPlayTime = (audioSource.clip != null) ? audioSource.time : 0;
                float audioPlayTimeLeft = audioTotalTime - audioPlayTime;
                float audioProgress = (audioSource.clip != null) ? audioPlayTime / audioTotalTime : 0;

                if (instructionsClipStarted && !audioSource.isPlaying)
                {
                    audioProgress = 1;
                    audioPlayTimeLeft = 0;
                }

                string header = "<b>" + Localization.LocalizeDefault("$ui:instructionsHeader") + "</b>";
                string text = Localization.LocalizeDefault(suite.RunningTest.Metadata?.description);

                float height = Mathf.Max(95, GUI.skin.label.CalcHeight(new GUIContent(text), 280));

                GUI.color = Color.cyan;

                if (!MainUI.HasActiveModalWindow)
                    GUI.Window(1, MainUI.ModalWindowRect, id =>
                    {
                        GUI.color = Color.white;

                        instructionsScroll = GUI.BeginScrollView(new Rect(50, 50, 300, 100), instructionsScroll, new Rect(0, 0, 280, height));

                        GUI.Label(new Rect(0, 0, 280, height), text);

                        GUI.EndScrollView();

                        if (GUI.Button(new Rect(MainUI.ModalWindowRect.width - 150, MainUI.ModalWindowRect.height - 40, 125, 30), Localization.LocalizeDefault("$ui:skipInstructions")))
                        {
                            if (this.goSignal != null) AudioSource.PlayClipAtPoint(goSignal, audioSource.transform.position);

                            suite.InstructionsOK = true;
                        }

                        if (audioSource.clip != null && GUI.Button(new Rect(MainUI.ModalWindowRect.width - 300, MainUI.ModalWindowRect.height - 40, 125, 30), Localization.LocalizeDefault("$ui:replayInstructions")))
                        {
                            audioSource.Stop();
                            audioSource.Play();
                        }

                        if (audioSource.clip != null)
                        {
                            if (audioProgress < 1)
                            {
                                GUI.color = ((int)(Time.time * 2) & 1) == 1 ? Color.white : Color.yellow;
                                GUI.Label(new Rect(50, 155, MainUI.ModalWindowRect.width - 100, 20), Localization.LocalizeDefault("$ui:instructionsPlaying " + (int)audioPlayTimeLeft + " $unit:secondsLeft"));
                            }
                            else
                            {
                                GUI.Label(new Rect(50, 155, MainUI.ModalWindowRect.width - 100, 20), Localization.LocalizeDefault("$ui:instructionsFinished"));
                            }

                            GUI.color = new Color(1, 1, 1, 0.5f);
                            GUI.Box(new Rect(50, 175, MainUI.ModalWindowRect.width - 100, 10), "");
                            GUI.color = new Color(3, 1, 1, 1);
                            GUI.Box(new Rect(50, 175, (MainUI.ModalWindowRect.width - 110) * audioProgress + 10, 10), "");
                        }

                    }, header);

                GUI.color = Color.white;
            }
            else
            {
                audioSource.Stop();
                audioSource.clip = null;
                instructionsClipStarted = false;
            }
        }

        private Vector2 testListScroll;

        private void RunCalibration()
        {
            this.calibrationStepsLeft = 3;
        }

        private void StartTestWithInstructions(int index)
        {
            suite.RunTest(index);

            audioSource.clip = null;
            instructionsClipStarted = false;

            Localization.LocalizedAudioDefault(suite.RunningTest.Metadata.instructionsAudio, clip =>
            {
                audioSource.clip = clip;
                audioSource.loop = false;
                audioSource.Play();
                instructionsClipStarted = clip != null;
            });
        }

        public void OnGUITestList(Vector2 screen)
        {
            const float BUTTON_WIDTH = 250;

            Vector2 viewportPos = new Vector2(5, 5);
            Vector2 viewportSize = Vector2.Scale(new Vector2(.3f, .45f), screen);

            // TEST LIST

            float minW,  maxW ;
            GUI.skin.label.CalcMinMaxWidth(new GUIContent(""), out minW, out maxW);

            float contentHeight = 10 + 30;
            float contentWidth = BUTTON_WIDTH * 1.5f;
               
            ForEachTest(i => suite[i], suite.TestCount, t => t.IsRunning || t == suite.RunningTest,
                (t, index, level) => {
                    contentHeight += 20;
                    if ((t.Completed || UserConfig.DevMode) && !suite.RunningTest) contentHeight += 30;
                });

            contentHeight = Mathf.Max(viewportSize.y, contentHeight);

            GUI.Box(new Rect(viewportPos, viewportSize), "");
            testListScroll = GUI.BeginScrollView(new Rect(viewportPos, viewportSize), testListScroll, new Rect(0, 0, contentWidth, contentHeight));

            Vector2 anchor = new Vector2();

            int firstNotCompleted = -1; // keeps track of how many uncompleted tests we have

            ForEachTest(i => suite[i], suite.TestCount, t => t.IsRunning || t == suite.RunningTest, (test, index, level) =>
            {
                // TEST LABELS
                var metadata = test.Metadata;
                string label = Localization.LocalizeDefault(metadata.label);

                label = (level == 0 ? "<b>" + label + "</b>" : label);
                if (test.Completed) label += " [" + this.doneLabel + "]";
                else if (test.IsRunning) label += " [" + this.runningLabel + "]";

                if (test.IsRunning)
                {
                    var timer = test.GetComponent<TimerIndicator>();
                    if (timer) label += " " + timer.TimerString;
                }

                GUI.Label(new Rect(level * 25 + 5, anchor.y + 5, contentWidth, 20), label);
                anchor.y += 20;

                // TEST CONTROLS

                // START & STOP (draw start button for the 1st not completed test, draw stop for the running test)
                if (level == 0 && !test.Completed && ((firstNotCompleted == -1 && !suite.RunningTest) || suite.RunningTest == test))
                {
                    if (suite.IKRig || this.formMode)
                    {
                        string btnLabel;
                        if (formMode) btnLabel = Localization.LocalizeDefault((!suite.RunningTest) ? "$ui:startForm" : "$ui:stopForm");
                        else btnLabel = Localization.LocalizeDefault((!suite.RunningTest) ? "$ui:startTest" : "$ui:stopTest");

                        GUI.color = (!suite.RunningTest) ? Color.green : Color.red;

                        if (GUI.Button(new Rect(5, anchor.y + 8, BUTTON_WIDTH, 24), btnLabel))
                        {
                            if (!suite.RunningTest) StartTestWithInstructions(index);
                            else AttemptStopTest();
                        }

                        GUI.color = Color.white;
                        anchor.y += 30;
                    }

                    firstNotCompleted = index;
                }
                // RESTART COMPLETED TEST
                else if (level == 0 && test.Completed && !suite.RunningTest && (suite.IKRig || this.formMode))
                {
                    string btnLabel = formMode ? Localization.LocalizeDefault("$ui:restartForm") : Localization.LocalizeDefault("$ui:restartTest");
                    GUI.color = Color.white;

                    if (GUI.Button(new Rect(5, anchor.y + 8, BUTTON_WIDTH, 24), btnLabel))
                    {
                        AttemptRestartTest(index);
                    }

                    anchor.y += 30;
                }
                // DEV MODE
                else if (level == 0 && !suite.RunningTest && (suite.IKRig || this.formMode) && UserConfig.DevMode)
                {
                    GUI.color = MainUI.DEV_COLOR;

                    if (GUI.Button(new Rect(5, anchor.y + 8, BUTTON_WIDTH, 24), Localization.LocalizeDefault("$ui:startTest")))
                    {
                        StartTestWithInstructions(index);
                    }

                    GUI.color = Color.white;
                    anchor.y += 30;
                }
            });

            GUI.EndScrollView();

            if (!suite.RunningTest)
            {
                // UPLOAD / COMPLETION BUTTON
                if (suite.AllTestsComplete())
                {
                    GUI.color = (Color.green);

                    if (GUI.Button(new Rect(5, 10 + .45f * screen.y + 120, .3f * screen.x, 40), Localization.Format("$ui:uploadDataButton")))
                    {
                        uploadUI.BeginUpload(suite.ExperimentIndex, MainUI.Console.GetErrorLog());
                        MainUI.PushView(uploadUI);
                    }
                }
                else if (suite.ExperimentIndex.readyForQuestionnaire && !this.formMode)
                {
                    GUI.color = (Color.green);

                    if (GUI.Button(new Rect(5, 10 + .45f * screen.y + 120, .3f * screen.x, 40), Localization.Format("$ui:endTests")))
                    {
                        MainUI.CloseView(this);
                    }
                }
                else if (UserConfig.DevMode)
                {
                    GUI.color = MainUI.DEV_COLOR;

                    if (GUI.Button(new Rect(5, 10 + .45f * screen.y + 120, .3f * screen.x, 40), Localization.Format("$ui:uploadDataButton")))
                    {
                        uploadUI.BeginUpload(suite.ExperimentIndex, MainUI.Console.GetErrorLog());
                        MainUI.PushView(uploadUI);
                    }
                }

                // NEXT TEST BUTTON
                bool nextTestAvailable = (firstNotCompleted >= 0 && (suite.IKRig || this.formMode));

                GUI.color = nextTestAvailable ? Color.green : Color.white;

                string label = formMode ? Localization.Format("$ui:nextForm") : Localization.Format("$ui:nextTest");

                if (!nextTestAvailable)
                {
                    GUI.Box(new Rect(5, 10 + .45f * screen.y + 20, .3f * screen.x, 40), label);
                }
                else if (GUI.Button(new Rect(5, 10 + .45f * screen.y + 20, .3f * screen.x, 40), label))
                {
                    StartTestWithInstructions(firstNotCompleted);
                }

                // VIEW DATA BUTTON
                GUI.color = Color.white;

                if (GUI.Button(new Rect(5, 10 + .45f * screen.y + 70, .3f * screen.x, 40), Localization.Format("$ui:viewDataButton")))
                {
                    MainUI.PushView(dataViewUI);
                    dataViewUI.SetData(suite.ExperimentIndex, fromLocal: true);
                }

                // CALIBRATION BUTTON
                if (!formMode)
                {
                    GUI.color = Color.white;

                    if (GUI.Button(new Rect(5, 10 + .45f * screen.y + 170, .3f * screen.x, 40), Localization.Format("$ui:goToCalibration")))
                    {
                        MainUI.PushView(calibrationUI);
                    }
                }
            }
        }

        public void ForEachTest(Func<int, Test> testIndex, int testCount, Func<Test, bool> checkSubTestsFilter, Action<Test, int, int> action, int level = 0)
        {
            for (int i = 0; i < testCount; i++)
            {
                var test = testIndex(i);

                if (level == 0) if ((test.IsForm != this.formMode) && !(UserConfig.DevMode && test.IsForm)) continue;

                action(test, i, level);

                if (checkSubTestsFilter(test)) ForEachTest(ti => test[ti], test.SubtestCount, checkSubTestsFilter, action, level + 1);
            }
        }

        public void AttemptStopTest()
        {
            MainUI.ConfirmAction("$ui:confirmStopTestHeader", "$ui:confirmStopTest", suite.StopTest, severity: 1, confirmText: "$ui:stopTest", cancelText: "$ui:continueTest");
        }

        public void AttemptRestartTest(int index)
        {
            MainUI.ConfirmAction("$ui:confirmRestartTestHeader", "$ui:confirmRestartTest", () => StartTestWithInstructions(index), severity: 1);
        }
    }
}