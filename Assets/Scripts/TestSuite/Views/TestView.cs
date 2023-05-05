using UnityEngine;
using TestSuite.Metrology;
using FullBodyTracking;
using System.Collections.Generic;

namespace TestSuite.Views
{
    public class TestView<T> : MonoBehaviour where T : TestIndicator
    {
        public TestSuite suite;

        private List<T> indicators = new List<T>();

        private Test runningTest;

        public IEnumerable<T> Indicators => indicators;
        public int Count => indicators.Count;

        public void Init(Test test)
        {
            this.runningTest = test;
            this.indicators.Clear();
            this.indicators.AddRange(test.GetComponents<T>());

            InitView();
        }

        public void Start()
        {
            if (suite == null) suite = GetComponentInParent<TestSuite>();
        }

        public void OnValidate()
        {
            if (suite == null) suite = GetComponentInParent<TestSuite>();
        }

        public void Update()
        {
            Test test = suite.RunningTest;

            while (test?.RunningSubtest != null)
            {
                test = test.RunningSubtest;
            }

            if (test != null && test != this.runningTest)
            {
                Init(test);
            }

            if (test != null) UpdateView();
        }

        public virtual void InitView()
        {

        }

        public virtual void UpdateView()
        {

        }
    }
}