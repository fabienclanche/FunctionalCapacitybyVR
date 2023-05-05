using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FullBodyTracking;
using Interaction;
using Utils;

namespace TestSuite.Metrology
{
    public class GlassManualTaskIndicator : TestIndicator
    {
        public Vector3 glassUp = Vector3.up;

        [IndicatorValue, Metadata(unit = "°"), Init(0)] float averageAngle;
        [IndicatorValue, Metadata(unit = "°", importance = 1, aggregation = "max"), Init(0)] float maxAngle;
        [IndicatorValue, Metadata(unit = "°/s²"), Init(0)] float averageAngularAcceleration;
        [IndicatorValue, Metadata(unit = "°/s²", importance = 1, aggregation = "max"), Init(0)] float maxAngularAcceleration;
        [IndicatorValue, Metadata(unit = "m/s²"), Init(0)] float averageAcceleration;
        [IndicatorValue, Metadata(unit = "m/s²", importance = 1, aggregation = "max"), Init(0)] float maxAcceleration;

        [Header("Debug attributes")]
        public InteractiveObject glass;
        public Vector3 lastPosition, lastLinearVelocity;
        [Init(0)] public float lastAngle, lastAngularVelocity;
        [Init(2)] public int skipFrame;

        [Init(0)] private float angleCaptureTime, accelCaptureTime;

        public LookAtIndicator lookAt;

        public override string Name => "$ind:glassManualTask";

        protected float Angle
        {
            get
            {
                Vector3 up = glass.transform.rotation * glassUp;
                return Mathf.Rad2Deg * Mathf.Acos(up.normalized.y);
            }
        }

        public void OnValidate()
        {
            if (!lookAt) lookAt = this.gameObject.AddComponent<LookAtIndicator>();
            lookAt.label = "$item:cup";
        }

        protected override void Begin()
        {
            glass = Test.Suite.IKRig[BodyPart.Rhand].GetComponent<InteractiveHand>().HeldObject ?? Test.Suite.IKRig[BodyPart.Lhand].GetComponent<InteractiveHand>().HeldObject;
            lookAt.@object = glass.gameObject;
            lastPosition = glass.transform.position;
            lastAngle = this.Angle;
        }

        protected override void End()
        {
            if (this.angleCaptureTime >= 0)
            {
                averageAngle /= this.angleCaptureTime;
            }

            if (this.accelCaptureTime >= 0)
            {
                averageAcceleration /= this.accelCaptureTime;
                averageAngularAcceleration /= this.accelCaptureTime;
            }
        }

        protected override void RecordFrame()
        {
            float angle = Angle;

            // capture angle
            this.angleCaptureTime += Time.deltaTime;
            this.averageAngle += angle * Time.deltaTime;
            maxAngle = Mathf.Max(maxAngle, angle);

            // compute acceleration
            Vector3 lVelocity = (glass.transform.position - lastPosition) / Time.deltaTime;
            Vector3 lAcceleration = (lVelocity - lastLinearVelocity) / Time.deltaTime;

            float aVelocity = Mathf.Abs(angle - lastAngle) / Time.deltaTime;
            float aAcceleration = Mathf.Abs(aVelocity - lastAngularVelocity) / Time.deltaTime;

            // capture acceleration after 2 skipped frames
            if (skipFrame == 0)
            {
                this.accelCaptureTime += Time.deltaTime;

                float accelValue = lAcceleration.magnitude;

                this.averageAcceleration += accelValue * Time.deltaTime;
                this.averageAngularAcceleration += aAcceleration * Time.deltaTime;

                this.maxAcceleration = Mathf.Max(this.maxAcceleration, accelValue);
                this.maxAngularAcceleration = Mathf.Max(this.maxAngularAcceleration, aAcceleration);
            }

            if (skipFrame > 0) skipFrame--;

            // store state for next frame
            this.lastAngle = angle;
            this.lastPosition = glass.transform.position;
            this.lastLinearVelocity = lVelocity;
            this.lastAngularVelocity = aVelocity;
        }
    }
}