using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FullBodyTracking;
using System.Linq;

namespace TestSuite.Metrology
{
	public class StepsIndicator : TestIndicator
	{
		public BodyPart selectedFoot = (BodyPart)(-1);

		FootTracker tracker;

		List<float> distances;
		List<float> durations;
		List<float> hmaxes;

		[IndicatorValue, Metadata(importance = 1, aggregation = "sum")] 
			int steps => distances.Count;

		[IndicatorValue, Metadata(unit = "m", importance = 1, aggregation = "max")] 
			float maxCycleLength => distances.Max();

		[IndicatorValue, Metadata(unit = "m", importance = 1, aggregation = "avg", aggregationWeightAttribute = "steps")] 
			float averageCycleLength => distances.Average();

		[IndicatorValue, Metadata(unit = "s", aggregation = "max")] 
			float maxCycleDuration => durations.Max();

		[IndicatorValue, Metadata(unit = "s", aggregation = "avg", aggregationWeightAttribute = "steps")] 
			float averageCycleDuration => durations.Average();

		[IndicatorValue, Metadata(unit = "m", importance = 1, aggregation = "max")] 
			float maxHeight => hmaxes.Max();

		[IndicatorValue, Metadata(unit = "m", importance = 1, aggregation = "avg", aggregationWeightAttribute = "steps")] 
			float averageHeight => hmaxes.Average();

		public override string Name => "$ind:steps.$" + selectedFoot;

		protected override void Begin()
		{
			distances = new List<float>();
			durations = new List<float>();
			hmaxes = new List<float>();

			tracker = Test.Suite.IKRig[selectedFoot]?.GetComponent<FootTracker>();

			tracker.onStepCompleted += OnStep;
		}

		private void OnValidate()
		{
			if (selectedFoot == (BodyPart)(-1) && gameObject.GetComponents<StepsIndicator>().Length < 2)
			{
				selectedFoot = BodyPart.Rfoot;
				var left = gameObject.AddComponent<StepsIndicator>();
				left.selectedFoot = BodyPart.Lfoot;
			}
		}

		protected void OnStep(Vector2 origin, Vector2 end, float distance, float duration, float hmax, string discriminator)
		{
			distances.Add(distance);
			durations.Add(duration);
			hmaxes.Add(hmax);
		}

		protected override void End()
		{
			tracker.onStepCompleted -= OnStep;
		}

		protected override void RecordFrame()
		{

		}
	}
}