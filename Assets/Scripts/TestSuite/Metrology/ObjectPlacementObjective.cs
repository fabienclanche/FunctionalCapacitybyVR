using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Utils;
using FullBodyTracking;
using System;

namespace TestSuite.Metrology
{
    /// <summary>
    /// A Test Objective that checks when a object is placed within or removed from a collider
    /// </summary>     
    public class ObjectPlacementObjective : TestObjective
    {
        public bool onEnter = true;

        public String componentName;

        [Header("Rotation Constraints")]
        [Tooltip("The axis, in local space for both the trigger collider and colliding object, that must be aligned")] public Vector3 comparedAxis;
        [Tooltip("The maximum angle between the two 'Compared Axes'  of the trigger collider and colliding object for them to be considered aligned"), Range(0, 360)] public float rotationTolerance;

        public Collider triggerCollider;

        [Header("Debug")]
        public CollisionListener listener;
        public Component found;

        [IndicatorValue, Metadata(debug_only = true)] public float Angle => Vector3.Angle(found.transform.rotation * comparedAxis, triggerCollider.transform.rotation * comparedAxis);
        [IndicatorValue, Metadata(debug_only = true)] public bool AngleOK => comparedAxis.sqrMagnitude == 0 || rotationTolerance >= 360 || Angle <= rotationTolerance;

        public override bool ConditionVerified
        {
            get { return found != null && (!onEnter || AngleOK); }
        }

        public override string Name => "Object Placement Objective";

        protected override void Begin()
        {
            if (listener) Destroy(listener);

            listener = triggerCollider.gameObject.AddComponent<CollisionListener>();

            Func<bool, Action<Collider>> triggerAction = entering => o =>
            {
                Debug.Log(o + " collided");
                Type t = Type.GetType(componentName);
                if (entering == onEnter)
                {
                    found = o.GetComponent(t) ?? found;
                }
                else if (found == o.GetComponent(t))
                {
                    found = null;
                }
            };

            this.listener.onTriggerEnter += triggerAction(true);
            this.listener.onTriggerEnter += triggerAction(true);
            this.listener.onTriggerExit += triggerAction(false);
        }

        public void OnValidate()
        {
            Type t = Type.GetType(componentName);
            if (t != null)
            {
                this.componentName = t.AssemblyQualifiedName;
            }
        }

        protected override void RecordFrame()
        {

        }

        protected override void End()
        {
            if (listener) Destroy(listener);
        }

    }
}