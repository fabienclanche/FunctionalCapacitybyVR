using UnityEngine;
using System.Collections.Generic;
using FullBodyTracking;
using FullBodyTracking.Mocap;
using System.Collections;
using System;
using Utils;
using System.IO;
using System.Linq;

namespace TestSuite
{
    public class TestSuite : MonoBehaviour
    {
        Coroutine runningRoutine = null;
        [SerializeField] List<Test> testList = new List<Test>();
        [SerializeField] TPoseCalibrator tPoseCalibrator = null;
        [SerializeField] int currentTestIndex = 0;

        public bool InstructionsOK { get; set; }

        public int TestCount => testList.Count;
        public int TestIndex => this.currentTestIndex;
        public Test this[int i] => testList[i];

        public Test RunningTest { get; private set; }

        public string OutputDirectory => this.ExperimentIndex.RootDirectory;

        public CalibratedIK IKRig { get; private set; }
        public MocapRecorder Mocap { get; private set; }
        public StudyConfig StudyConfig { get; private set; }

        public ExperimentIndex ExperimentIndex { get; private set; }

        // Use this for initialization
        void Start()
        {
            ExperimentIndex = new ExperimentIndex();
            ExperimentIndex.studyId = "DemoStudy";
            ExperimentIndex.subjectId = "DemoSubject";
            ExperimentIndex.experimentId = "DemoExp";

            StartWith(ExperimentIndex, new StudyConfig());

            tPoseCalibrator.onSuccessfulCalibration += (calibratedIK, mocap) =>
            {
                this.IKRig = calibratedIK;
                this.Mocap = mocap;
            };
        }

        public void StartWith(ExperimentIndex eIndex, StudyConfig studyConfig)
        {
            this.ExperimentIndex = eIndex;
            this.StudyConfig = studyConfig;
            StopTest();

            foreach (var test in GetComponentsInChildren<Test>())
            {
                if (test.OnlyEnabledWhileRunning) test.gameObject.SetActive(false);
            }

            for (int i = 0; i < this.TestCount; i++)
            {
                var test_i = this[i];

                ExperimentIndex.Entry ei_entry = i < eIndex.contents.Count ? eIndex.contents[i] : null;

                if (test_i.OnlyEnabledWhileRunning) test_i.gameObject.SetActive(false);

                test_i.Completed = ei_entry != null && ei_entry.name == (test_i.Metadata?.label ?? test_i.name);
            }

            this.SaveExperimentIndex();
        }

        public void OnValidate()
        {
            this.testList = Test.FetchDirectChildren<Test>(transform);
        }

        private IEnumerator RunTestCoroutine(int i)
        {
            var indexEntry = new ExperimentIndex.Entry();
            indexEntry.name = testList[i].Metadata?.label ?? testList[i].name;
            indexEntry.indicatorsFile = "test_" + (i + 1) + ".json";
            indexEntry.mocapFile = "test_" + (i + 1) + ".mocap.json";

            while (this.ExperimentIndex.contents.Count <= i) this.ExperimentIndex.contents.Add(null);
            this.ExperimentIndex.contents[i] = null;
            SaveExperimentIndex();

            // position VR rig at the test location
            Transform vrRef = tPoseCalibrator.transform;

            vrRef.parent = testList[i].Origin;
            vrRef.localPosition = Vector3.zero;
            vrRef.localRotation = Quaternion.identity;
            vrRef.localScale = Vector3.one;

            // run test
            testList[i].Origin.gameObject.SetActive(true);

            this.RunningTest = testList[i];

            this.InstructionsOK = testList[i].IsForm;
            yield return new WaitUntil(() => this.InstructionsOK);

            FootTracker.ClearFootsteps();
            testList[i].InitTestData();
            yield return testList[i].Run(outputFileName: OutputDirectory + "\\" + indexEntry.indicatorsFile,
                                            mocapFileName: OutputDirectory + "\\" + indexEntry.mocapFile);

            this.RunningTest = null;
            runningRoutine = null;

            // update experiment index
            if (testList[i].IsForm) indexEntry.mocapFile = null;
            else indexEntry.mocapLength = Mocap.RecordedTime;

            this.ExperimentIndex.contents[i] = indexEntry;
            SaveExperimentIndex();

            currentTestIndex = i + 1;

            vrRef.parent = null;
        }

        private bool IsReadyForQuestionnaire()
        {
            for (int i = 0; i < this.TestCount; i++)
            {
                var test_i = this[i];

                if (!test_i.IsForm && !test_i.Completed) return false;
            }

            return true;
        }

        public bool AllTestsComplete()
        {
            for (int i = 0; i < this.TestCount; i++)
            {
                var test_i = this[i];

                if (!test_i.Completed) return false;
            }

            return true;
        }

        private void SaveExperimentIndex()
        {
            ExperimentIndex.readyForQuestionnaire = IsReadyForQuestionnaire();

            JSONSerializer.MkDirParent(OutputDirectory + "\\index.json");
            JSONSerializer.ToJSONFile<ExperimentIndex>(OutputDirectory + "\\index.json", ExperimentIndex);
        }

        public void RunTest(int i)
        {
            if (IKRig == null && !this[i].IsForm) throw new Exception(Localization.LocalizeDefault("$exception:uncalibrated"));
            if (runningRoutine != null) throw new Exception(Localization.LocalizeDefault("$exception:testAlreadyRunning"));

            ClearTestData(this[i]);
            currentTestIndex = i;

            runningRoutine = StartCoroutine(RunTestCoroutine(i));
        }

        private void ClearTestData(Test test)
        {
            test.InitTestData();
        }

        public void RunNextTest()
        {
            if (currentTestIndex < testList.Count)
            {
                RunTest(currentTestIndex);
            }
        }

        public void StopTest()
        {
            if (this.RunningTest != null)
            {
                this.RunningTest.EndTest();
                if(Mocap) Mocap.StopRecording();
                ClearTestData(this.RunningTest);

                StopCoroutine(runningRoutine);
                runningRoutine = null;
                this.RunningTest = null;
                tPoseCalibrator.transform.parent = null;
            }
        }
    }
}
