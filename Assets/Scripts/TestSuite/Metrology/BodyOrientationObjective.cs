using UnityEngine;
using System.Collections;
using FullBodyTracking;
using System.Collections.Generic;
using System.Linq;
using System;
using Utils;

namespace TestSuite.Metrology
{
    public class BodyOrientationObjective : TestObjective
    {
        public BodyPart bodyPart = BodyPart.Back;
        [Tooltip("Set to a non-null vector to project the forward direction of the tracked body part on a plane normal to this vector")]
        public Vector3 planeProjectionNormal = Vector3.zero;

        public Vector3 forwardTarget = Vector3.forward;
        [Range(0, 180)]
        public float angleToTarget = 45;

        public override bool ConditionVerified
        {
            get
            {
                if (Test?.Suite?.IKRig == null) return false;

                var forward = Test.Suite.IKRig[bodyPart].WorldRotation * Vector3.forward;

                if (planeProjectionNormal.sqrMagnitude > 0)
                {
                    forward = Vector3.ProjectOnPlane(forward, planeProjectionNormal.normalized);
                }

                return Vector3.Angle(forward, transform.TransformDirection(forwardTarget)) < angleToTarget;
            }
        }

        public void OnValidate()
        {
            if (planeProjectionNormal.sqrMagnitude > 0)
                forwardTarget = Vector3.ProjectOnPlane(forwardTarget, planeProjectionNormal.normalized);
        }

        public override string Name => "Body Orientation Target";

        protected override void Begin()
        {

        }

        protected override void End()
        {

        }

        protected override void RecordFrame()
        {

        }
    }
}