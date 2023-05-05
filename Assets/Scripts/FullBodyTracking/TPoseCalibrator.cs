using RootMotion.FinalIK;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using FullBodyTracking.Mocap;
using Interaction;

namespace FullBodyTracking
{

    public enum CalibrationMode { FULL, NO_RCONTROLLER, NO_LCONTROLLER }

    /// <summary>
    /// Serves as a root of the VR IK Rig. Manages calibration of the IK avatar.
    /// Pairs tracked objects with anchors to identify their corresponding body parts.
    /// </summary>
    public class TPoseCalibrator : MonoBehaviour
    {
        private Dictionary<BodyPart, TPoseAnchor> anchorPoints = new Dictionary<BodyPart, TPoseAnchor>();
        private TrackedObjectsManager tObjMngr;

        public event Action<CalibratedIK, MocapRecorder> onSuccessfulCalibration;

        public float maximumBindDistance = 0.6f;
        [Header("Avatars"), Tooltip("The main VR avatar")] public FullBodyBipedIK ikAvatar;
        [Tooltip("Female VR avatar")] public FullBodyBipedIK ikAvatarF;
        public FullBodyBipedIK dummySubject;
		public float avatarHeight = 0;

        public Camera vrCamera;

        [Header("Footprints")]
        public GameObject rightFootprint;
        public GameObject leftFootprint;

        [Header("VR Hardware Prefabs")]
        public GameObject trackerPrefab;
        public GameObject hmdPrefab, controllerPrefab, lighthousePrefab;

        private MocapRecorder mocapRecorder;

        private Vector3 originalPosition;
        private Quaternion originalOrientation;

        public IEnumerable<FullBodyBipedIK> AllAvatars()
        {
            yield return ikAvatar;
            yield return ikAvatarF;
        }

        public TrackedObject HMD => tObjMngr.HMD;
        public IEnumerable<TrackedObject> TrackedObjects => tObjMngr.TrackedObjects;

        void Start()
        {
            foreach (var anchor in GetComponentsInChildren<TPoseAnchor>())
            {
                if (anchorPoints.ContainsKey(anchor.bodypart))
                {
                    Debug.LogError("Invalid T-Pose setup, too many " + anchor.bodypart);
                    this.enabled = false;
                    return;
                }
                Debug.Log("Found " + anchor.bodypart);
                anchorPoints[anchor.bodypart] = anchor;

                anchor.transform.rotation = ikAvatar.GetIKBodyReference(anchor.bodypart).rotation;
            } 
		
			avatarHeight = this.transform.InverseTransformPoint(ikAvatar.GetIKBodyReference(BodyPart.Head).position).y;

            if (this.GetComponent<MocapReplay>() == null)
            {
                var tomObj = new GameObject("TrackedObjectManager");
                tomObj.transform.parent = this.transform;
                tomObj.transform.localPosition = Vector3.zero;
                tomObj.transform.localRotation = Quaternion.identity;
                tomObj.transform.localScale = Vector3.one;

                tObjMngr = tomObj.AddComponent<TrackedObjectsManager>();

                tObjMngr.trackerPrefab = this.trackerPrefab;
                tObjMngr.hmdPrefab = this.hmdPrefab;
                tObjMngr.controllerPrefab = this.controllerPrefab;
                tObjMngr.lighthousePrefab = this.lighthousePrefab;

                dummySubject.transform.parent = tObjMngr.transform;
                if (vrCamera)
                {
                    vrCamera.transform.parent = tObjMngr.transform;
                }

                // restore last calibration offset

                this.tObjMngr.transform.localRotation = UserConfig.RoomOffsetRotation;
                this.tObjMngr.transform.localPosition = UserConfig.RoomOffsetTranslation;
            }

            this.originalOrientation = this.transform.rotation;
            this.originalPosition = this.transform.position;

            UndoCalibration();
        }

        struct AnchorObjectPair
        {
            public readonly float sqDistance;
            public readonly TrackedObject tobj;
            public readonly TPoseAnchor anchor;

            public AnchorObjectPair(TrackedObject tobj, TPoseAnchor anchor)
            {
                sqDistance = (tobj.transform.position - anchor.WorldOrigin).sqrMagnitude;
                this.tobj = tobj;
                this.anchor = anchor;
            }

            public override string ToString()
            {
                return tobj + " " + anchor + " " + sqDistance;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void ResetTransform()
        {
            this.transform.parent = null;
            this.transform.position = this.originalPosition;
            this.transform.rotation = this.originalOrientation;
        }

        public void UndoCalibration()
        {
            foreach (var avatar in AllAvatars())
            {
                avatar.GetComponent<CalibratedIK>()?.ClearCalibration();
                avatar.gameObject.SetActive(false);
            }

            if (vrCamera)
            {
                vrCamera.cullingMask = vrCamera.cullingMask | LayerMask.GetMask("CalibrationView");
            }

            foreach (var tobj in this.TrackedObjects)
            {
                var comp = tobj.GetComponent<FootTracker>();
                if (comp) Destroy(comp);

                var comp2 = tobj.GetComponent<InteractiveHand>();
                if (comp2) Destroy(comp2);
            }

            this.onSuccessfulCalibration?.Invoke(null, null);
        }

        public TPoseAnchor GetAnchor(BodyPart part)
        {
            return anchorPoints[part];
        }

        private TrackedObject CreateDummyTracker(BodyPart part)
        {
            Transform parent = dummySubject.GetIKBodyReference(part);

            TrackedObject tObj = parent.GetComponentInChildren<TrackedObject>();

            if (tObj)
            {
                tObj.SetReference(this.tObjMngr.transform);
                var footTracker = tObj.GetComponent<FootTracker>();
                var iHand = tObj.GetComponent<InteractiveHand>();

                Destroy(iHand);
                Destroy(footTracker);
            }
            else
            {
                var dummyTracker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dummyTracker.transform.parent = parent;
                dummyTracker.transform.localPosition = Vector3.zero;
                Destroy(dummyTracker.GetComponent<Collider>());
                dummyTracker.transform.localScale = Vector3.one * 0.1f;
                dummyTracker.transform.localRotation = Quaternion.identity;

                tObj = dummyTracker.AddComponent<TrackedObject>();
            }

            tObj.SetReference(this.tObjMngr.transform);

            return tObj;
        }

        /// <summary>
        /// offsets the tracked objects so that the HMD is above the local position (0, 0, 0) and faces the local forward direction. 
        /// Effectively places the real-world starting point of tests at the subject current position.
        /// </summary>
        /// <param name="hmd">HMD tracked object</param>
        private void MakeOffset(TrackedObject hmd)
        {
            Vector3 hmdForward = hmd.TrackedRotation * Vector3.forward;
            Vector3 hmdPos = hmd.TrackedPosition - hmdForward * .1f;

            Func<BodyPart, Vector3> pos = bp => anchorPoints[bp].TrackedObject.transform.position;
            Func<BodyPart, BodyPart, BodyPart, Vector3> cross = (b1, b2, b3) =>
            {
                Vector3 x = Vector3.Cross((pos(b2) - pos(b1)).normalized, (pos(b2) - pos(b1)).normalized);
                if (Vector3.Dot(x, hmdForward) < 0) x = -x;
                return x;
            };

            //   Vector3 feetForward = (cross(BodyPart.Back, BodyPart.Rfoot, BodyPart.Lfoot) + cross(BodyPart.Head, BodyPart.Rfoot, BodyPart.Lfoot)) / 2;
            //   Vector3 handsForward = (cross(BodyPart.Back, BodyPart.Rhand, BodyPart.Lhand) + cross(BodyPart.Head, BodyPart.Rhand, BodyPart.Lhand)) / 2;
            Vector3 subjectForward = hmdForward;
            subjectForward.y = 0; // project on horizontal plane

            //  this.transform.parent = null;
            //   this.transform.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.forward, subjectForward, Vector3.up), Vector3.up);
            //   this.transform.position = Vector3.Scale(new Vector3(1, 0, 1), pos(BodyPart.Head));

            this.tObjMngr.transform.localRotation = Quaternion.AngleAxis(-Vector3.SignedAngle(Vector3.forward, subjectForward, Vector3.up), this.tObjMngr.transform.up);
            this.tObjMngr.transform.localPosition = this.tObjMngr.transform.localRotation * -Vector3.Scale(new Vector3(1, 0, 1), hmdPos);
        }

        public void EnableDummyTrackers()
        {
            this.StopAllCoroutines();
            StartCoroutine(this.tObjMngr.DummyTrackersCoroutine());
        }

        /// <summary>
        /// Calibrates the IK Avatar, matching it to tracked objects
        /// </summary>
        /// <param name="debug">If true, a keyboard-controlled avatar will be used instead of a VR-controlled one</param>
        /// <returns>The list of unmatched body parts on the avatar, null on a successful calibration</returns>
        public List<BodyPart> StartFullBodyTracking(bool debug = false)
        {
            UndoCalibration();

            if (!debug)
            {
                List<BodyPart> unmatched = new List<BodyPart>();

                if (tObjMngr.HMD == null)
                {
                    unmatched.Add(BodyPart.Head);
                    RestoreRoomOffset();
                    return unmatched;
                }

                this.dummySubject.solver.FixTransforms();
                MakeOffset(tObjMngr.HMD);
                FindNearestTrackers();
                foreach (var anchor in anchorPoints.Values)
                {
                    if (anchor.TrackedObject == null)
                    {
                        Debug.LogWarning(anchor.bodypart + " not bound");
                        unmatched.Add(anchor.bodypart);
                    }
                }

                if (unmatched.Count > 0)
                {
                    RestoreRoomOffset();
                    return unmatched;
                }
            }
            else
            {
                foreach (var anchor in anchorPoints.Values)
                {
                    anchor.TrackedObject = CreateDummyTracker(anchor.bodypart);
                }

                MakeOffset(anchorPoints[BodyPart.Head].TrackedObject);
                dummySubject.gameObject.SetActive(true);
            }

            foreach (var anchor in anchorPoints.Values)
            {
                var renderer = anchor.GetComponent<MeshRenderer>();
                if (renderer) renderer.enabled = false;
                var lrenderer = anchor.GetComponent<LineRenderer>();
                if (lrenderer) lrenderer.enabled = false;
            }

            if (mocapRecorder == null) mocapRecorder = this.gameObject.AddComponent<MocapRecorder>();

            // configure IK
            if (ikAvatar)
            {
                ikAvatar.gameObject.SetActive(true);

                var calIK = ikAvatar.gameObject.GetComponent<CalibratedIK>();

                if (!calIK)
                {
                    calIK = ikAvatar.gameObject.AddComponent<CalibratedIK>();
                    calIK.InitIK(ikAvatar);
                }

                Transform parent = tObjMngr.transform;

                foreach (var anchor in anchorPoints.Values)
                {
                    calIK.Track(anchor.bodypart, anchor.TrackedObject, anchor);
                }

                if (debug) dummySubject.gameObject.SetActive(true);

                calIK.AdjustCharacterScale(this.transform.position, this.avatarHeight);

                foreach (BodyPart part in new BodyPart[] { BodyPart.Lfoot, BodyPart.Rfoot }) AddFootTracker(part, calIK[part]);

                foreach (BodyPart part in new BodyPart[] { BodyPart.Lhand, BodyPart.Rhand }) AddInteractiveHand(part, calIK[part]);

                foreach (var anchor in anchorPoints.Values)
                {
                    var body = anchor.TrackedObject.gameObject.AddComponent<Rigidbody>();
                    if (body) body.isKinematic = true;
                }

                // IK Avatar F 
                ikAvatarF.gameObject.SetActive(true);

                var calIKF = ikAvatarF.gameObject.GetComponent<CalibratedIK>();
                if (!calIKF)
                {
                    calIKF = ikAvatarF.gameObject.AddComponent<CalibratedIK>();
                    calIKF.InitIK(ikAvatarF);
                }

                calIKF.CopyTrack(calIK);
                calIKF.AdjustCharacterScale(this.transform.position, this.avatarHeight);

                // succesful calibration
                mocapRecorder.SetAvatars(calIK, calIKF);

                vrCamera.cullingMask = vrCamera.cullingMask & ~LayerMask.GetMask("CalibrationView");

                this.onSuccessfulCalibration?.Invoke(calIK, mocapRecorder);
            } 

            if (UserConfig.LockRoomOffset)
            {
                RestoreRoomOffset();
            }
            else
            {
                UserConfig.RoomOffsetRotation = this.tObjMngr.transform.localRotation;
                UserConfig.RoomOffsetTranslation = this.tObjMngr.transform.localPosition;
            }

            return null;
        }

        private void RestoreRoomOffset()
        { 
            this.tObjMngr.transform.localRotation = UserConfig.RoomOffsetRotation;
            this.tObjMngr.transform.localPosition = UserConfig.RoomOffsetTranslation;
        }

        public void CalibrateScale()
        {
            foreach(var avatar in this.AllAvatars())
            {
                var calIK = avatar.GetComponent<CalibratedIK>();
                if(calIK) calIK.AdjustCharacterScale(this.transform.position, this.avatarHeight);
            } 
        }

        public void AddFootTracker(BodyPart part, TrackedObject tobj)
        {
            var foot = tobj.gameObject.AddComponent<FootTracker>();
            foot.Init(Vector3.one / 10, part == BodyPart.Rfoot ? this.rightFootprint : this.leftFootprint);
        }

        public InteractiveHand AddInteractiveHand(BodyPart part, TrackedObject tobj)
        {
            var ihand = tobj?.gameObject.AddComponent<InteractiveHand>();

            ihand.Init(anchorPoints[part].interactionPosOffset, anchorPoints[part].ikPosOffset,
                ikAvatar.GetIKBodyReference(part).GetComponentInChildren<SDUU.Hands.HandAnimator>(),
                ikAvatarF.GetIKBodyReference(part).GetComponentInChildren<SDUU.Hands.HandAnimator>(),
                part);

            return ihand;
        }

        void FindNearestTrackers()
        {
            // build a list of trackers and anchors pairs
            var pairs = new List<AnchorObjectPair>();

            var matchedObjects = new HashSet<TrackedObject>();
            var matchedAnchors = new HashSet<TPoseAnchor>();

            foreach (var anchor in anchorPoints.Values)
            {
                if (anchor.forcePairWithHMD)
                {
                    anchor.TrackedObject = tObjMngr.HMD;
                }
                else if (anchor.forcePairWith != null)
                {
                    anchor.TrackedObject = anchor.forcePairWith;
                    matchedObjects.Add(anchor.forcePairWith);
                    matchedAnchors.Add(anchor);
                }
                else
                {
                    anchor.TrackedObject = null;

                    foreach (var tobj in tObjMngr.TrackedObjects.Where(anchor.AcceptsPairing))
                    {
                        pairs.Add(new AnchorObjectPair(tobj, anchor));
                    }
                }
            }

            // sort the list of T-A pairs according to their distance
            pairs.Sort((a, b) =>
            {
                // degraded mode anchor get least priority for matching
                if (a.anchor.degradedMode && !b.anchor.degradedMode) return 1;
                if (!a.anchor.degradedMode && b.anchor.degradedMode) return -1;

                return (int)Mathf.Sign(a.sqDistance - b.sqDistance);
            });

            // find the best matches for trackers and anchors, iterating through T-A pairs starting with the pairs with smallest distance

            foreach (var pair in pairs)
            {
                if (!pair.anchor.degradedMode && pair.sqDistance > maximumBindDistance * maximumBindDistance) continue; // tracker-anchors pairs with a too great distance are ignored

                if (!matchedAnchors.Contains(pair.anchor) && !matchedObjects.Contains(pair.tobj))
                {
                    matchedAnchors.Add(pair.anchor);
                    matchedObjects.Add(pair.tobj);
                    pair.anchor.TrackedObject = pair.tobj;
                }
            }
        }
    }
}