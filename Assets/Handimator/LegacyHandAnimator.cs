using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RootMotion.FinalIK;
using Utils;

/*
Copyright 2020 Julie#8169 STREAM_DOGS#4199

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the 
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF
OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace Handimator
{
    [Serializable]
    public abstract class Finger
    {
        public float thickness = .022f, distalLength = .01f;
        public Vector3 forward = new Vector3(1, 0, 0);
        public Vector3 flexionAxisClockwise = new Vector3(0, 0, 1);

        public Vector3 initialRotation = Vector3.zero;

        private float[] cachedLengths = null;

        /// <summary>
        /// Stores the current flexion targets for the phalanxes of this finger. Elements before the last holds finalized computations
        /// </summary>
        public List<float> flexionTargets = new List<float>();

        public Transform IKtarget = null, bendGoal = null;
        public float restFlexion = 30;

        public float GetPhalanxLength(int i)
        {
            if (cachedLengths == null) cachedLengths = new float[this.Length];

            if (i < Length - 1)
            {
                if (cachedLengths[i] > 0) return cachedLengths[i];
                else return cachedLengths[i] = Vector3.Distance(this[i].position, this[i + 1].position);
            }
            else
            {
                return distalLength;
            }
        }

        public float GetLengthToFingerEnd(int i)
        {
            float l = 0;
            for (int j = i; j < Length; j++) l += GetPhalanxLength(i);
            return l;
        }

        public Transform ProximalPhalanx => this[0];
        public Transform DistalPhalanx => this[Length - 1];

        public abstract int Length { get; }

        public abstract Transform this[int i] { get; }

        public abstract void AutodetectTransforms(Transform fingerTransform);

        public virtual void AutodetectAxes()
        {
            this.initialRotation = ProximalPhalanx.localRotation.eulerAngles;

            this.forward = DistalPhalanx.localPosition.normalized;

            for (int i = 0; i < 3; i++) if (Math.Abs(this.forward[i]) < 0.00001f) this.forward[i] = 0;

            this.forward = forward.normalized;
        }

        public bool CheckPhalanxCollision(int i, Collider targetCollider)
        {
            Transform phalanx = this[i];
            Vector3 origin = phalanx.position;
            Vector3 end = origin + phalanx.TransformDirection(forward) * GetLengthToFingerEnd(i);

            Collider[] colliders = Physics.OverlapCapsule(origin, end, thickness, 1 << targetCollider.gameObject.layer, QueryTriggerInteraction.Ignore);

            foreach (var collider in colliders)
            {
                if (collider == targetCollider) return true;
            }

            return false;
        }

        private TrigonometricIK trigIK;

        public void TrigIK(float flexDelta, Transform target)
        {
            if (trigIK == null)
            {
                trigIK = ProximalPhalanx.gameObject.AddComponent<TrigonometricIK>();
                if (Length > 2) trigIK.solver.SetChain(this[0], this[1], this[2], this[0].parent);
                else
                {
                    var tip = new GameObject("tip");
                    tip.transform.parent = this.DistalPhalanx;
                    tip.transform.localPosition = this.forward.Divide( this[0].transform.lossyScale)  * distalLength ; 
                    trigIK.solver.SetChain(this[0], this[1], tip.transform, this[0].parent);
                }
            }

            if (Length > 2) this[2].localRotation = this[1].localRotation;

            trigIK.solver.target = target; 
            trigIK.solver.SetIKPositionWeight(1);
            trigIK.solver.SetIKRotationWeight(0);
            trigIK.solver.bendNormal = -flexionAxisClockwise;

            if (bendGoal != null)
            {
                trigIK.solver.SetBendGoalPosition(bendGoal.position, .5f);
            }
        }

        public void FlexFinger(float flexDelta, Collider targetCollider)
        {
            // update targets
            if (flexionTargets.Count == 0) flexionTargets.Add(0);

            for (int i = 0; i < Length; i++)
            {
                if (i < flexionTargets.Count - 1)
                {
                    FlexPhalanx(i, flexionTargets[i], 1);
                }
                else if (i == flexionTargets.Count - 1)
                {
                    var oldFlex = flexionTargets[i];
                    flexionTargets[i] += flexDelta;

                    bool solved = false;

                    if (flexionTargets[i] > 90)
                    {
                        flexionTargets[i] = 90;
                        solved = true;
                    }

                    FlexPhalanx(i, flexionTargets[i], 1);

                    if (targetCollider != null && CheckPhalanxCollision(i, targetCollider))
                    {
                        flexionTargets[i] = oldFlex;
                        FlexPhalanx(i, flexionTargets[i], 1);
                        solved = true;
                    }

                    if (solved) flexionTargets.Add(0);
                }
                else
                {
                    FlexPhalanx(i, 0, 1);
                }
            }
        }

        public void FlexPhalanx(int i, float flexion, float interp)
        {
            Quaternion target;

            if (i == 0) target = Quaternion.AngleAxis(flexion, this.flexionAxisClockwise) * Quaternion.Euler(this.initialRotation);
            else target = Quaternion.AngleAxis(flexion, this.flexionAxisClockwise);

            this[i].localRotation = Quaternion.Slerp(this[i].localRotation, target, interp);
        }

        public void Update(float flexDelta, Collider targetCollider)
        {
            if (IKtarget != null) TrigIK(flexDelta, IKtarget);
            else if (targetCollider != null) FlexFinger(flexDelta, targetCollider);
            else
            {
                float interp = 1 - Mathf.Exp(-flexDelta / 20);
                for (int i = 0; i < Length; i++) FlexPhalanx(i, restFlexion, interp);
            }

            if (trigIK != null)
            {
                trigIK.enabled = IKtarget != null;                
            }
        }

        public void ResetTargets()
        {
            flexionTargets.Clear();
        }
    }

    [Serializable]
    public class Thumb : Finger
    {
        public Transform proximalPhalanx, distalPhalanx;

        public override int Length => 2;

        public override Transform this[int i] => (i == 0) ? proximalPhalanx : (i == 1) ? distalPhalanx : throw new ArgumentOutOfRangeException(i + " is an invalid phalanx number (must be < " + Length + ")");

        public override void AutodetectTransforms(Transform transform)
        {
            proximalPhalanx = transform;

            for (int i = 0; i < proximalPhalanx.childCount; i++)
            {
                var child = proximalPhalanx.GetChild(i);
                if (child.localPosition.sqrMagnitude > 0) distalPhalanx = child;
            }
        }
    }

    [Serializable]
    public class Finger3 : Finger
    {
        public Transform proximalPhalanx, middlePhalanx, distalPhalanx;

        public override int Length => 3;

        public override Transform this[int i] => (i == 0) ? proximalPhalanx : (i == 1) ? middlePhalanx : (i == 2) ? distalPhalanx : throw new ArgumentOutOfRangeException(i + " is an invalid phalanx number (must be < " + Length + ")");

        public override void AutodetectTransforms(Transform transform)
        {
            proximalPhalanx = transform;

            for (int i = 0; i < proximalPhalanx.childCount; i++)
            {
                var child = proximalPhalanx.GetChild(i);
                if (child.localPosition.sqrMagnitude > 0) middlePhalanx = child;
            }

            for (int i = 0; i < middlePhalanx.childCount; i++)
            {
                var child = middlePhalanx.GetChild(i);
                if (child.localPosition.sqrMagnitude > 0) distalPhalanx = child;
            }
        }
    }

    public class LegacyHandAnimator : MonoBehaviour
    {
        [SerializeField] bool autodectect = true;

        [Header("Bones")] [SerializeField] public Thumb thumb;
        [SerializeField] public Finger3 index, middle, ring, pinky;

        [Header("Targets")]
        [SerializeField] bool reset = true;
        public Collider targetCollider;

        [Header("Speed")]
        public float flexionSpeed = 360, flexionResolution = 6;

        public void Update()
        {
            if (reset)
            {
                ResetTargets();
                reset = false;
            }

            var flexDelta = Mathf.Min(flexionResolution, flexionSpeed * Time.deltaTime);

            foreach (var finger in Fingers()) finger.Update(flexDelta, targetCollider);
        }

        public void ResetTargets()
        {
            foreach (var finger in Fingers()) finger.ResetTargets();
        }

        public IEnumerable<Finger> Fingers()
        {
            yield return thumb;
            yield return index;
            yield return middle;
            yield return ring;
            yield return pinky;
        }

        public void OnValidate()
        {
            if (autodectect) Autodetect();
            autodectect = false;
        }

        public void Autodetect()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.ToLower().Contains("thumb")) thumb.AutodetectTransforms(child);
                else if (child.name.ToLower().Contains("index")) index.AutodetectTransforms(child);
                else if (child.name.ToLower().Contains("ring")) ring.AutodetectTransforms(child);
                else if (child.name.ToLower().Contains("middle")) middle.AutodetectTransforms(child);
                else if (child.name.ToLower().Contains("pinky")) pinky.AutodetectTransforms(child);
            }

            foreach (var finger in Fingers()) finger.AutodetectAxes();
        }

    }
}


