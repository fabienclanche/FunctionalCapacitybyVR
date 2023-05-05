using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FullBodyTracking;
using Interaction;

namespace TestSuite.Metrology
{
    /// <summary>
    /// Clear the hands of objects at the end of a test
    /// </summary>
    public class EOTClearHands : TestIndicator
    {
        public TakeItemObjective takeItemObj;

        public override string Name => "";

        protected override void Begin()
        {

        }

        protected override void End()
        {
            var h = Test.Suite.IKRig[BodyPart.Rhand].GetComponent<InteractiveHand>();
            Clear(h);
            h = Test.Suite.IKRig[BodyPart.Lhand].GetComponent<InteractiveHand>();
            Clear(h);

            if(takeItemObj)
            {
                takeItemObj.Reset();
            }
        }

        private void Clear(InteractiveHand h)
        {
            var held = h.HeldObject;
            if (held == null) return;

            h.ForceReleaseHeldObject();
            Destroy(held.gameObject);
        }

        protected override void RecordFrame()
        {
            
        }
    }
}