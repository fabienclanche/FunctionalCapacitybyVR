using TestSuite.Metrology;
using FullBodyTracking;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace TestSuite.Views
{
    public class StepsIndicatorView : RealtimePlot3DView<StepsIndicator>
    {
        public Material materialRight, materialLeft;

        public Vector3 rightPeak, leftPeak;
        public int leftCounter, rightCounter;

        public override void InitPlot(StepsIndicator indicator, LineRenderer plot)
        {
            plot.material = (indicator.selectedFoot == BodyPart.Rfoot) ? materialRight :
                (indicator.selectedFoot == BodyPart.Lfoot) ? materialLeft : plot.material;

            plot.startWidth = plot.endWidth = 0.03f;
        }

        public override Vector3 Point(StepsIndicator indicator)
        {
            TrackedObject tobj = this.suite.IKRig[indicator.selectedFoot];

            GL.Color(Color.red);
            GL.Begin(GL.LINES);
            GL.Vertex(tobj.WorldPosition+Vector3.up);
            GL.Vertex(tobj.Reference.TransformPoint(tobj.TrackedPosition.x0z()));
            GL.End();

            return tobj.WorldPosition;
        }
    }

    public abstract class RealtimePlot3DView<T> : TestView<T> where T : TestIndicator
    {
        public float sampleInterval = 0.033f;
        private float lastSample = -1;
        private List<LineRenderer> plots = new List<LineRenderer>();

        public override void InitView()
        {
            foreach (var plot in plots)
            {
                plot.gameObject.SetActive(false);
                plot.positionCount = 0;
            }

            int i = 0;
            foreach (var stepsInd in Indicators)
            {
                if (i >= plots.Count)
                {
                    var obj = new GameObject("Plot " + (i + 1));
                    obj.transform.parent = this.transform;
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localRotation = Quaternion.identity;

                    var plot = obj.AddComponent<LineRenderer>();
                    plot.positionCount = 0;

                    plots.Add(plot);
                }

                plots[i].gameObject.SetActive(true);
                plots[i].positionCount = 0;
                plots[i].gameObject.layer = this.gameObject.layer;

                InitPlot(stepsInd, plots[i]);

                i++;
            }

            this.lastSample = -this.sampleInterval;
        }

        public virtual void InitPlot(T indicator, LineRenderer plot)
        {

        }

        public abstract Vector3 Point(T indicator);

        public override void UpdateView()
        {
            bool newSample = (Time.time - this.lastSample) >= this.sampleInterval;

            int i = 0;
            foreach (var stepsInd in Indicators)
            {
                if (newSample) plots[i].positionCount++;
                plots[i].SetPosition(plots[i].positionCount - 1, Point(stepsInd));

                i++;
            }

            if (newSample) this.lastSample = Time.time;
        }
    }
}