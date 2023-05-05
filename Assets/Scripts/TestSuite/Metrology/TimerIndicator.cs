using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TestSuite.Metrology
{
	public class TimerIndicator : TestIndicator
	{
		[SerializeField] GameObject[] pauseWhenEnabled;
		[IndicatorValue, Metadata(unit = "s", importance = -1)] float startTime;
		[IndicatorValue, Metadata(unit = "s", importance = -1)] float endTime;
		[IndicatorValue, Init(0), Metadata(unit = "s", importance = 1, aggregation = "sum")] float duration;

		public string TimerString => duration.ToString("0.00") + "s";

		public override string Name => "$ind:timer";

		protected override void Begin()
		{
			startTime = Time.time;
		}

		protected override void End()
		{
			endTime = Time.time;
		}

		protected override void RecordFrame()
		{
			for (int i = 0; i < pauseWhenEnabled.Length; i++)
			{
				if (pauseWhenEnabled[i].activeInHierarchy) return;
			}

			duration += Time.deltaTime;
		}
	}
}