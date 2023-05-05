using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Utils;
using System;

namespace FullBodyTracking
{
    public class FootTracker : MonoBehaviour
    {
        public static float MOE_LIFT_Y = 0.02f, MOE_GROUND_Y = 0.01f;
        public static float MOE_LIFT_SPD = 0.1f, MOE_GROUND_SPD = 0.01f;

        private static Dictionary<GameObject, Pool> footprintPools = new Dictionary<GameObject, Pool>();

        public static void ClearFootsteps()
        {
            foreach (var pool in footprintPools.Values)
            {
                pool.ClearAllInstances();
            }
        }

        [SerializeField] protected TrackedObject trackedObject;

        [Header("Step stats")]
        [SerializeField] protected Vector3 lastGroundedPosition = Vector3.zero;
        [SerializeField] protected float lastGroundedTime = 0;

        [SerializeField] protected float currentPeak = 0;
        [SerializeField] protected string footUpDiscriminator = "";

        [Header("Grounding")]
        [SerializeField] protected bool grounded = false;
        [SerializeField] protected float groundLevel = 0;

        protected Pool footprintPool;
        protected SphereCollider footCollider;

        public delegate void StepListener(Vector2 stepOrigin, Vector2 stepEnd, float distance, float duration, float hmax, string detectionMethod);

        public event StepListener onStepCompleted;

        public void Init(Vector3 dimensions, GameObject footprint = null)
        {
            this.trackedObject = GetComponent<TrackedObject>();

            this.transform.localScale = Vector3.one;
            var meshRenderer = this.GetComponent<MeshRenderer>();
            if (meshRenderer) meshRenderer.enabled = false;

            foreach (var collider in gameObject.GetComponents<SphereCollider>())
            {
                Destroy(collider);
            }

            this.footCollider = gameObject.AddComponent<SphereCollider>();
            this.footCollider.radius = (dimensions.x + dimensions.y + dimensions.z) / 3f;

            // offset the collider so it barely touch the floor when this is at ground level
            this.footCollider.center = trackedObject.transform.InverseTransformVector(trackedObject.Reference.up * (this.footCollider.radius + trackedObject.YOffset));
            this.footCollider.isTrigger = true;

            var body = gameObject.GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
            if (body) body.isKinematic = true;

            groundLevel = trackedObject.TrackedPosition.y;

            // init footprint pool
            if (footprint)
            {
                //    if(!footprintPools.TryGetValue(footprint, out this.footprintPool))
                {
                    this.footprintPool = new Pool(footprint, 10);
                    footprintPools[footprint] = this.footprintPool;
                    Debug.Log("Generated footprint pool: " + footprint.name + " (" + footprintPools.Count + " pools in total)");
                }
            }
        }

        public BodyPart Foot => (BodyPart)this.trackedObject.bodyPart;

        public Vector3 LastGroundedPosition => this.lastGroundedPosition;
        public Collider FootCollider => this.footCollider;

        public bool IsAtGroundLevel(float moe)
        {
            return (trackedObject.TrackedPosition.y - groundLevel) < moe;
        }

        public bool Grounded => grounded;

        public void InitForNextStep()
        {
            grounded = true;
            lastGroundedPosition = trackedObject.TrackedPosition;
            lastGroundedTime = Time.time;
            currentPeak = 0;
        }

        public void OnFootDown()
        {
            Vector2 origin = lastGroundedPosition.xz(), end = trackedObject.TrackedPosition.xz();
            float d = Vector2.Distance(origin, end);

            onStepCompleted?.Invoke(origin, end, (origin - end).magnitude, Time.time - lastGroundedTime, currentPeak, footUpDiscriminator);

            footprintPool?.Create(
                trackedObject.Reference.TransformPoint(trackedObject.TrackedPosition.x0z()),
                Quaternion.LookRotation((trackedObject.WorldRotation * Vector3.forward).x0z(), Vector3.up)
            );

            InitForNextStep();
        }

        public void OnFootUp(string discriminator)
        {
            grounded = false;
            lastGroundedTime = Time.time - Time.deltaTime;

            footUpDiscriminator = (discriminator);
        }

        public void Update()
        {
            float speed = trackedObject.Velocity.magnitude;

            bool discriminator = false;

            if (!grounded && IsAtGroundLevel(MOE_GROUND_Y))
            {
                OnFootDown();
            }
            else if (grounded && (discriminator = !IsAtGroundLevel(MOE_LIFT_Y)))
            {
                OnFootUp(discriminator ? "height" : "speed");
            }

            if (!grounded)
            {
                currentPeak = Mathf.Max(trackedObject.TrackedPosition.y - groundLevel, currentPeak);
            }
        }
    }
}