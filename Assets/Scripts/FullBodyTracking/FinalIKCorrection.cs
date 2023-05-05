using RootMotion.FinalIK;
using UnityEngine;

namespace FullBodyTracking
{
    public class FinalIKCorrection : MonoBehaviour
    {
        public FullBodyBipedIK fullBodyBipedIK;

        public LimbIK rightArmIK, leftArmIK;
        public Transform[] fixedPoints;

        void Start()
        {
            // Disable all the IK components so they won't update their solvers. Use Disable() instead of enabled = false, the latter does not guarantee solver initiation.
            fullBodyBipedIK.Disable();
            rightArmIK.Disable(); leftArmIK.Disable();
        }

        public void CopyIKConstraints(IKSolverLimb target, IKEffector source)
        {
            target.IKPosition = source.position;
            target.IKRotation = source.rotation;
            target.IKPositionWeight = source.positionWeight;
            target.IKRotationWeight = source.rotationWeight;

        }

        void Update()
        {

        }

        void LateUpdate()
        {
            Quaternion[] rots = new Quaternion[fixedPoints.Length];
            for (int i = 0; i < fixedPoints.Length; i++) rots[i] = fixedPoints[i].transform.localRotation;

            CopyIKConstraints(rightArmIK.solver, fullBodyBipedIK.solver.rightHandEffector);
            CopyIKConstraints(leftArmIK.solver, fullBodyBipedIK.solver.leftHandEffector);

            fullBodyBipedIK.GetIKSolver().FixTransforms();
            fullBodyBipedIK.GetIKSolver().Update();
            rightArmIK.GetIKSolver().Update();
            leftArmIK.GetIKSolver().Update();

            for (int i = 0; i < fixedPoints.Length; i++)
            {
                Quaternion offset = Quaternion.Inverse(rots[i]) * fixedPoints[i].transform.localRotation;
                fixedPoints[i].transform.localRotation = rots[i];
                for (int c = 0; c < fixedPoints[i].childCount; c++)
                {
                    fixedPoints[i].GetChild(c).localRotation = offset * fixedPoints[i].GetChild(c).localRotation;
                }
            }
        }
    }
}