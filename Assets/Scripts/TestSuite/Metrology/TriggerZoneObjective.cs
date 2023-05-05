using UnityEngine;
using System.Collections;
using FullBodyTracking;
using System.Collections.Generic;
using System.Linq;
using System;
using Utils;

namespace TestSuite.Metrology
{
    public class TriggerZoneObjective : TestObjective
    {
        public Collider triggerCollider;
        public BodyPart[] requiredBodyParts = { BodyPart.Rfoot, BodyPart.Lfoot };

        private CollisionListener listener;
        private HashSet<TrackedObject> trackedObjects;

        public override bool ConditionVerified
        {
            get
            {
                if(trackedObjects == null) return false;
                
                BodyPart[] partsInTrigger = trackedObjects.Where(o => o.bodyPart != null).Select(o => (BodyPart) o.bodyPart).ToArray();
                return requiredBodyParts.All(partsInTrigger.Contains);
            }
        }

        public override string Name => "TriggerZone";
		 
        protected override void Begin()
		{
			if (listener) Destroy(listener);

			trackedObjects = new HashSet<TrackedObject>();

			listener = triggerCollider.gameObject.AddComponent<CollisionListener>();

            listener.onTriggerEnter += CollisionListener.TrackedObjectAdapter(tobj => trackedObjects.Add(tobj));
            listener.onTriggerExit += CollisionListener.TrackedObjectAdapter(tobj => trackedObjects.Remove(tobj)); 
        }

        protected override void End()
        {
			if (listener) Destroy(listener);
		}

        protected override void RecordFrame()
        {

        }
    }
}