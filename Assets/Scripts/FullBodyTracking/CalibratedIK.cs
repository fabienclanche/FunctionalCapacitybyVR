using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;
using System;
using Interaction;

namespace FullBodyTracking
{
    /// <summary>
    /// Animates a biped avatar using IK and calibrated tracker/anchor pairs, updating IK constraints in realtime.
    /// </summary>
    public class CalibratedIK : MonoBehaviour
    {
        FullBodyBipedIK ik;
        FBBIKHeadEffector headIkEffector;
        public Dictionary<BodyPart, TrackedObject> bodyTrackedObjects = new Dictionary<BodyPart, TrackedObject>();
        Dictionary<BodyPart, TPoseAnchor> anchors = new Dictionary<BodyPart, TPoseAnchor>();

        public float ModelScale
        {
            get { return ik.references.root.localScale.x; }
            set
            {
                ik.references.root.localScale = Vector3.one * value;
            }
        }

        public float LegScale
        {
            get { return ik.references.rightThigh.transform.localScale.x; }
            set { ik.references.rightThigh.transform.localScale = ik.references.leftThigh.transform.localScale = Vector3.one * value; }
        }

        public float ArmScale
        {
            get { return ik.references.rightUpperArm.transform.localScale.x; }
            set
            {
                ik.references.rightUpperArm.transform.localScale = ik.references.leftUpperArm.transform.localScale = Vector3.one * value;
                ik.references.rightHand.transform.localScale = ik.references.leftHand.transform.localScale = Vector3.one;
            }
        }

        public void InitIK(FullBodyBipedIK ik)
        {
            this.ik = ik;

            if (!headIkEffector)
            {
                var headTarget = new GameObject();
                headTarget.transform.parent = this.transform;
                headIkEffector = headTarget.AddComponent<FBBIKHeadEffector>();
            }

            headIkEffector.handsPullBody = false;
            headIkEffector.ik = ik;
        }

        public void ClearCalibration()
        {
            if (!ik) return;

            ModelScale = LegScale = ArmScale = 1;

            ik.solver.FixTransforms();

            bodyTrackedObjects = new Dictionary<BodyPart, TrackedObject>();
            anchors = new Dictionary<BodyPart, TPoseAnchor>();
        }

        public TrackedObject this[BodyPart bodyPart]
        {
            get
            {
                TrackedObject tobj;
                bodyTrackedObjects.TryGetValue(bodyPart, out tobj);
                return tobj;
            }
        }

        public Transform TrackingReference => bodyTrackedObjects[BodyPart.Head].Reference;

        public void CopyTrack(CalibratedIK @base)
        {
            this.bodyTrackedObjects.Clear();
            this.anchors.Clear();

            foreach (var part in @base.bodyTrackedObjects.Keys)
            {
                this.bodyTrackedObjects[part] = @base.bodyTrackedObjects[part];
                this.anchors[part] = @base.anchors[part];

                if (part != BodyPart.Head)
                {
                    IKEffector effector = ik.GetIKEffector(part);
                    effector.positionWeight = 1;
                    effector.rotationWeight = this.anchors[part].degradedMode ? 0 : 1;
                }
            }
        }

        public void SetEffectorDegradedMode(BodyPart part, bool degradedTracking)
        {
            var effector = ik.GetIKEffector(part);

            if (effector == null) return;

            effector.positionWeight = 1;

            if (!degradedTracking) effector.rotationWeight = 1;
            else effector.rotationWeight = 0;
        }

        public bool GetEffectorDegradedMode(BodyPart part)
        {
            var effector = ik.GetIKEffector(part);

            if (effector == null) return false;
            else return effector.rotationWeight == 0;
        }

        public void Track(BodyPart part, TrackedObject tobj, TPoseAnchor anchor = null)
        {
            if (tobj == null) return;

            tobj.bodyPart = part;
            bodyTrackedObjects[part] = tobj;
            anchors[part] = anchor;

            IKEffector effector = ik.GetIKEffector(part);
            if (effector == null)
            {
                if (part != BodyPart.Head) Debug.LogError(part + " has no FinalIK effector");
                return;
            }

            SetEffectorDegradedMode(part, this.anchors[part].degradedMode);
             
            tobj.ROffset = Quaternion.Inverse(tobj.transform.rotation) * anchor.transform.rotation;
            
            if (part.IsFoot()) tobj.SetFloorOffset();
        }

        public void AdjustCharacterScale(Vector3 origin, float avatarHeight)
        {
            this.ModelScale = this.LegScale = this.ArmScale = 1;

            Update(BodyPart.Back); // position the model root

            // patient height (HMD height)
            TrackedObject headTO = bodyTrackedObjects[BodyPart.Head];
            float patientHeight = (headTO.TrackedPosition + headTO.TrackedRotation * anchors[BodyPart.Head].ikPosOffset).y;

            if (avatarHeight > 0)
            {
                this.ModelScale = patientHeight / avatarHeight;

                // reposition the scaled model
                Update(BodyPart.Back);

                //	ik.GetIKSolver().Update();

                Debug.Log(this.ModelScale + " " + this.LegScale + " " + this.ArmScale);

                // compute limb length
                float legScale = Mathf.Max(GetLimbScale(BodyPart.Rfoot, Vector3.one), GetLimbScale(BodyPart.Lfoot, Vector3.one));
                float armScale = Mathf.Max(GetLimbScale(BodyPart.Rhand, Vector3.one), GetLimbScale(BodyPart.Lhand, Vector3.one));

                this.LegScale = legScale;
                this.ArmScale = armScale;
            }
            else
            {
                // model height (HMD target height)
                Transform headTransform = ik.GetIKBodyReference(BodyPart.Head);
                Vector3 offset = anchors[BodyPart.Head].ikPosOffset;
                float modelHeight = headTO.Reference.InverseTransformPoint(headTransform.position).y;

                // float hipsY = headTO.Reference.InverseTransformPoint(ik.GetIKBodyReference(BodyPart.Back).TransformPoint(anchors[BodyPart.Back].ikPosOffset)).y;

                // global scale is computed by comparing body sizes above the legs, as legs are resized separately
                float globalScale = (patientHeight) / (modelHeight);
                this.ModelScale = globalScale;
                Debug.Log(headTO.Reference.gameObject + " " + patientHeight + " " + modelHeight + " " + globalScale);

                // reposition the scaled model
                Update(BodyPart.Back);

                // compute limb length
                float legScale = Mathf.Max(GetLimbScale(BodyPart.Rfoot, Vector3.one), GetLimbScale(BodyPart.Lfoot, Vector3.one));
                float armScale = Mathf.Max(GetLimbScale(BodyPart.Rhand, Vector3.one), GetLimbScale(BodyPart.Lhand, Vector3.one));

                this.LegScale = legScale * globalScale;
                this.ArmScale = armScale * globalScale;
            }
        }

        private float GetLimbScale(BodyPart extremity, Vector3 scale)
        {
            if (this.anchors[extremity].degradedMode) return 0;

            Transform limbRoot, limbMiddle;

            switch (extremity)
            {
                case BodyPart.Rhand:
                    limbRoot = ik.references.rightUpperArm;
                    limbMiddle = ik.references.rightForearm;
                    break;

                case BodyPart.Lhand:
                    limbRoot = ik.references.leftUpperArm;
                    limbMiddle = ik.references.leftForearm;
                    break;

                case BodyPart.Rfoot:
                    limbRoot = ik.references.rightThigh;
                    limbMiddle = ik.references.rightCalf;
                    break;

                case BodyPart.Lfoot:
                    limbRoot = ik.references.leftThigh;
                    limbMiddle = ik.references.leftCalf;
                    break;

                default:
                    throw new ArgumentException("extremity parameter must be set to a hand or foot");
            }


            Transform handTransform = ik.GetIKBodyReference(extremity);

            TrackedObject tObj = this.bodyTrackedObjects[extremity];

            // computes model arm length in tracking space
            Vector3 rootPos = tObj.Reference.InverseTransformPoint(limbRoot.position);
            Vector3 midPos = tObj.Reference.InverseTransformPoint(limbMiddle.position);
            Vector3 modelExtremityPos = tObj.Reference.InverseTransformPoint(handTransform.position);
            float modelLimbLength = (rootPos - midPos).magnitude + (modelExtremityPos - midPos).magnitude;

            Vector3 trackedExtremityPos = tObj.TrackedPosition + tObj.TrackedRotation * this.anchors[extremity].ikPosOffset;

            //    Debug.DrawLine(tObj.Reference.TransformPoint(rootPos), tObj.Reference.TransformPoint(midPos), Color.red, duration: 10000f);
            //    Debug.DrawLine(tObj.Reference.TransformPoint(modelExtremityPos), tObj.Reference.TransformPoint(midPos), Color.red, duration: 10000f);
            //    Debug.DrawLine(tObj.Reference.TransformPoint(rootPos), tObj.Reference.TransformPoint(modelExtremityPos), Color.yellow, duration: 10000f);
            //    Debug.DrawLine(tObj.Reference.TransformPoint(rootPos), tObj.Reference.TransformPoint(trackedExtremityPos), Color.green, duration: 10000f);
            //    Debug.DrawLine(tObj.Reference.TransformPoint(tObj.TrackedPosition), tObj.Reference.TransformPoint(trackedExtremityPos), Color.blue, duration: 10000f);

            float patientLimbLength = (rootPos - trackedExtremityPos).magnitude;

            Debug.Log(limbRoot + " scale: model AB-BC: " + modelLimbLength + " model AC: " + Vector3.Distance(rootPos, modelExtremityPos) + " patient: " + patientLimbLength + " ratio: " + patientLimbLength / modelLimbLength);

            float ratio = patientLimbLength / modelLimbLength;
            return (ratio);
        }

        public void Update()
        {
            foreach (var part in this.bodyTrackedObjects.Keys)
            {
                Update(part);
            }
        }

        private void Update(BodyPart part)
        {
            var tObj = this.bodyTrackedObjects[part];

            var rotation = tObj.WorldRotation;
            var position = tObj.WorldPosition;

            // Apply IK Offsets
            TPoseAnchor anchor = this.anchors[part];

            Vector3 anchorPosOffset = anchor ? anchor.ikPosOffset : Vector3.zero;

            position = position + rotation * anchorPosOffset;

            if (part == BodyPart.Head)
            {
                if (headIkEffector)
                {
                    headIkEffector.positionWeight = 1;
                    headIkEffector.rotationWeight = 1;

                    headIkEffector.transform.rotation = rotation;
                    headIkEffector.transform.position = position;
                }
            }
            else if (part == BodyPart.Back)
            {
                var back = ik.GetIKBodyReference(part);
                var effector = ik.GetIKEffector(part);

                effector.positionWeight = 1;

                ik.references.root.rotation = rotation;
                ik.references.root.position = position;
                effector.position = position;
            }
            else
            {
                var effector = ik.GetIKEffector(part); 

                if (part == BodyPart.Rhand || part == BodyPart.Lhand)
                {
                    var ihand = tObj.GetComponent<InteractiveHand>();
                    if(ihand && ihand.HeldObject)
                    {
                        effector.target = ihand.HeldObject.HandPose.HandTargetTransform;                        
                        return;
                    }
                }

                effector.target = null;

                // effector.positionWeight = 1;
                // effector.rotationWeight = 1;

                if (GetEffectorDegradedMode(part)) // cancels offset if in degradedMode
                {
                    position = tObj.WorldPosition;
                }

                effector.rotation = rotation;
                effector.position = position;
            }
        }
    }
}