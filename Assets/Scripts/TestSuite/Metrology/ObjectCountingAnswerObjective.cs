using UnityEngine;
using Utils;

namespace TestSuite.Metrology
{
	public class ObjectCountingAnswerObjective : TestObjective
	{
		public ObjectCountingIndicator objectCountingInitializer;

		[IndicatorValue, Metadata(debug_only = true)] public int aCount => objectCountingInitializer.aCount;
		[IndicatorValue, Metadata(debug_only = true)] public int bCount => objectCountingInitializer.bCount;

		public Collider aAnswerTrigger, bAnswerTrigger;

		public CollisionListener triggerColliderA, triggerColliderB;

		private bool? answer_isA = null;
		[IndicatorValue, Metadata(importance = 1)] public bool correctAnswer => answer_isA.HasValue && (answer_isA.Value == aCount > bCount);
		public override string Name => "$ind:objectCountingAnswer";

		public override bool ConditionVerified => answer_isA != null;

		protected override void Begin()
		{
			this.answer_isA = null;

			if (triggerColliderA) Destroy(triggerColliderA);
			if (triggerColliderB) Destroy(triggerColliderB);

			triggerColliderA = aAnswerTrigger.gameObject.AddComponent<CollisionListener>();
			triggerColliderB = bAnswerTrigger.gameObject.AddComponent<CollisionListener>();

			triggerColliderA.onTriggerStay += CollisionListener.TrackedObjectAdapter(c => RecordAnswer(true));
			triggerColliderB.onTriggerStay += CollisionListener.TrackedObjectAdapter(c => RecordAnswer(false));

			triggerColliderA.onCollisionStay += CollisionListener.TrackedObjectAdapter(c => RecordAnswer(true));
			triggerColliderB.onCollisionStay += CollisionListener.TrackedObjectAdapter(c => RecordAnswer(false));
		}

		protected void RecordAnswer(bool isA)
		{
			if (this.answer_isA.HasValue) return;
			this.answer_isA = isA;
		}

		protected override void RecordFrame()
		{

		}

		protected override void End()
		{
			if (triggerColliderA) Destroy(triggerColliderA);
			if (triggerColliderB) Destroy(triggerColliderB);
		}
	}
}