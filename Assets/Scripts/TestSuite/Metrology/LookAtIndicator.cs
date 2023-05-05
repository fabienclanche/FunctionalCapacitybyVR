using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TestSuite;
using FullBodyTracking;

namespace TestSuite.Metrology
{
	public class LookAtIndicator : TestIndicator
	{
		public string label = "";
		public GameObject @object;
		[Range(0, 90)] public float lookAtAngleThreshold = 30;

		[IndicatorValue, Metadata(unit = "s"), Init(0)] float lookAtMaxContinuousDuration;
		[IndicatorValue, Metadata(unit = "s", importance = 1, aggregation = "sum"), Init(0)] float lookAtTotalDuration;
		[IndicatorValue, Metadata(unit = "°", importance = -1, aggregation = "avg", aggregationWeightAttribute = "lookAtTotalDuration"), Init(0)] float lookAtAverageAngle;
		[Init(0)] float currentContinuous;
		[Init(0)] float totalCaptureTime;

		public override string Name => "$ind:lookAt." + this.label;

		protected float LookAtAngleDelta
		{
			get
			{
				var head = Test.Suite.IKRig[BodyPart.Head];
				return Vector3.Angle(head.WorldRotation * Vector3.forward, @object.transform.position - head.WorldPosition);
			}
		}

		protected override void Begin()
		{

		}

		protected override void End()
		{
			if (totalCaptureTime > 0) lookAtAverageAngle /= totalCaptureTime;
		}

		protected override void RecordFrame()
		{
			if (@object == null) return;

			totalCaptureTime += Time.deltaTime;

			float lookAtAngle = LookAtAngleDelta;
			if (lookAtAngle < this.lookAtAngleThreshold)
			{
				this.currentContinuous += Time.deltaTime;
				this.lookAtTotalDuration += Time.deltaTime;
				if (this.currentContinuous > this.lookAtMaxContinuousDuration) this.lookAtMaxContinuousDuration = this.currentContinuous;
			}
			else
			{
				this.currentContinuous = 0;
			}
			this.lookAtAverageAngle += lookAtAngle * Time.deltaTime;
		}
	}
}