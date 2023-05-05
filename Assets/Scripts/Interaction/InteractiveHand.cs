using UnityEngine;
using FullBodyTracking;
using UnityEngine.XR;
using System.Collections.Generic;
using System;
using SDUU.Hands;
using Utils;

namespace Interaction
{
    [RequireComponent(typeof(TrackedObject))]
    public class InteractiveHand : MonoBehaviour
    {
        private float previousInput = 0f;
        [SerializeField] private TrackedObject trackedObject;
        private SphereCollider triggerSphere;
        [SerializeField] private string inputAxis;
        [SerializeField] HandAnimator handAnimator, handAnimatorF;

        public InteractiveObject grabDebug = null;
        public Vector3 ikOffset;

        private HashSet<InteractiveObject> objectsInRange = new HashSet<InteractiveObject>();

        [Header("Held Object")]
        [SerializeField] InteractiveObject heldObject;
        [SerializeField] Vector3 heldObjectOriginalPosition = Vector3.zero;
        [SerializeField] Quaternion heldObjectOriginalRotation = Quaternion.identity;

        public Action Release;
        public InteractiveObject HeldObject => this.heldObject;

        [Header("Debug Settings")]
        public float inputOverride = 0f;

        public BodyPart Hand { get; private set; }

        public void Start()
        {
            if (this.Hand != BodyPart.Lhand) this.Hand = BodyPart.Rhand;

            this.handAnimator?.SetPose(null);
            this.handAnimatorF?.SetPose(null);
        }

        public void Init(Vector3 interactionTriggerOffset, Vector3 handIKOffset, HandAnimator handAnimator, HandAnimator handAnimatorF, BodyPart hand)
        {
            this.Hand = hand;
            this.transform.localScale = Vector3.one;
            var meshRenderer = this.GetComponent<MeshRenderer>();
            if (meshRenderer) meshRenderer.enabled = false;

            trackedObject = GetComponent<TrackedObject>();

            // initialize the trigger collider to grab objects
            foreach (var collider in gameObject.GetComponents<SphereCollider>())
            {
                Destroy(collider);
            }

            triggerSphere = gameObject.AddComponent<SphereCollider>();
            triggerSphere.isTrigger = true;
            triggerSphere.radius = 0.06f;
            triggerSphere.center = (trackedObject.ROffset) * interactionTriggerOffset;

            var body = gameObject.GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
            if (body) body.isKinematic = true;

            if (trackedObject.nodeType == XRNode.RightHand) inputAxis = "RTrigger";
            else if (trackedObject.nodeType == XRNode.LeftHand) inputAxis = "LTrigger";
            else inputAxis = null;

            this.ikOffset = handIKOffset;

            this.handAnimator = handAnimator;
            this.handAnimatorF = handAnimatorF;

            this.handAnimator.SetPose(null);
            this.handAnimatorF.SetPose(null);
        }

        public void OnTriggerEnter(Collider other)
        {
            OnTriggerStayOrEnter(other);
        }

        public void OnTriggerStay(Collider other)
        {
            OnTriggerStayOrEnter(other);
        }

        private void OnTriggerStayOrEnter(Collider other)
        {
            var io = other.gameObject.GetComponent<InteractiveObject>();

            if (io) objectsInRange.Add(io);
        }

        public void OnTriggerExit(Collider other)
        {
            var io = other.gameObject.GetComponent<InteractiveObject>();

            if (io) objectsInRange.Remove(io);
        }

        private InteractiveObject ClosestObject()
        {
            InteractiveObject closest = null;
            float bestDistance = float.PositiveInfinity;

            foreach (InteractiveObject io in this.objectsInRange)
            {
                if (!io.isActiveAndEnabled) continue;

                float d = DistanceToHand(io);
                if (d < bestDistance)
                {
                    closest = io;
                }
            }

            return closest;
        }

        public void GrabObject(InteractiveObject @object)
        {
            if (this.heldObject != null) return;

            Rigidbody body = @object.GetComponent<Rigidbody>();
            bool isKinematic = body != null && body.isKinematic;

            this.heldObjectOriginalPosition = @object.transform.position;
            this.heldObjectOriginalRotation = @object.transform.rotation;

            switch (@object.GrabType)
            {
                case GrabType.NONE:
                    return;

                case GrabType.ATTACH_TRANSFORM:
                    @object.transform.parent = handAnimator.transform;

                    handAnimator.SetPose(@object.HandPose);
                    handAnimatorF.SetPose(@object.HandPose);

                    Vector3 pos;
                    Quaternion rot;
                    HandAnimator.PreferredGrabPositionAndRotation(@object.transform, @object.HandPose, out pos, out rot);

                    //@object.transform.position = handAnimator.transform.position + (handAnimator.transform.rotation) * (pos);
                    @object.transform.rotation = (handAnimator.transform.rotation) * rot;
                    @object.transform.parent = this.transform;
                    @object.transform.localPosition = handAnimator.IsRightHand ? @object.preferredOffset : @object.preferredOffsetL;
                    @object.transform.localRotation = Quaternion.Euler(handAnimator.IsRightHand ? @object.preferredRotation : @object.preferredRotationL);

                    if (body) body.isKinematic = true;
                    Release = () =>
                    {
                        handAnimator.SetPose(null);
                        handAnimatorF.SetPose(null);
                        @object.transform.parent = null;
                        if (body) body.isKinematic = isKinematic;
                    };
                    break;
            }

            this.heldObject = @object;
        }

        public void ReleaseHeldObject()
        {
            if (this.heldObject != null && this.heldObject.Releasable)
            {
                ForceReleaseHeldObject();
            }
        }

        public void ForceReleaseHeldObject()
        {
            this.objectsInRange.Remove(this.heldObject);

            if (this.heldObject != null)
            {
                if (Release != null) Release();
                Release = null;
                this.heldObject = null;
            }

            if (this.handAnimator)
            {
                this.handAnimator.SetPose(null);
                this.handAnimatorF.SetPose(null);
            }
        }

        private float DistanceToHand(InteractiveObject io)
        {
            var center = triggerSphere.transform.TransformPoint(triggerSphere.center);
            return Vector3.Distance(center, io.transform.position);
        }

        private bool buttonReleased = true;

        public void Update()
        {
            if (grabDebug != null)
            {
                ForceReleaseHeldObject();
                this.GrabObject(grabDebug);
                return;
            }

            float input = inputAxis != null && inputAxis.Length > 0 ? Input.GetAxisRaw(inputAxis) : 0;

            input = Mathf.Max(input, inputOverride);

            if (input > 0.75 && buttonReleased)
            {
                buttonReleased = false;

                if (this.HeldObject == null)
                {
                    var closest = ClosestObject();
                    if (closest) GrabObject(closest);
                }
            }
            else if (input < 0.25)
            {
                if (!buttonReleased) this.ReleaseHeldObject();
                buttonReleased = true;
            }

            // input animation
            {
                handAnimator.NeutralPose.transform.localScale = new Vector3(1, 1 - input * .1f, 1);

                handAnimatorF.NeutralPose.transform.localScale = new Vector3(1, 1 - input * .1f, 1);
            }

            previousInput = input;
        }

        Transform thumbIKTarget = null;
        Transform[] ikTargets;
    }

}