using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Interaction;
using FullBodyTracking;
using System.Linq;
using FullBodyTracking.Mocap;

namespace TestSuite.Metrology
{
	public class TakeItemObjective : TestObjective
	{
		public List<InteractiveObject> itemsToTake;

		public List<InteractiveObject> itemsClones;

		public bool recordItems = true;

		public bool itemTaken = false;

		public override string Name => "Take Item";

		public override bool ConditionVerified => itemTaken;

		public void Reset()
		{
			// destroy clones
			foreach (var clone in itemsClones)
			{
				Destroy(clone.gameObject);
			}
			itemsClones.Clear();

			foreach (var item in itemsToTake)
			{
				item.gameObject.SetActive(true);
			}
		}

		protected override void Begin()
		{
			// destroy clones
			Reset();

			// create new clones
			foreach (var item in itemsToTake)
			{
				var copy = GameObject.Instantiate(item.gameObject).GetComponent<InteractiveObject>();
				itemsClones.Add(copy);

				copy.transform.parent = item.transform.parent;
				copy.transform.localPosition = item.transform.localPosition;
				copy.transform.localRotation = item.transform.localRotation;
				copy.transform.localScale = item.transform.localScale;

				item.gameObject.SetActive(false);
				copy.gameObject.SetActive(true);

				copy.GrabType = GrabType.ATTACH_TRANSFORM;

				RecordableObject recObj;
				if (recordItems && (recObj = copy.GetComponent<RecordableObject>())) Test.Mocap.AddRecordableObject(recObj);
			}

			itemTaken = false;
		}

		protected override void End()
		{
			// do nothing
		}

		protected override void RecordFrame()
		{
			InteractiveObject rhandObject = Test.Suite.IKRig[BodyPart.Rhand].GetComponent<InteractiveHand>().HeldObject;
			InteractiveObject lhandObject = Test.Suite.IKRig[BodyPart.Lhand].GetComponent<InteractiveHand>().HeldObject;

			if ((rhandObject != null && this.itemsClones.Contains(rhandObject)) || (lhandObject != null && this.itemsClones.Contains(lhandObject))) itemTaken = true;
		}
	}
}