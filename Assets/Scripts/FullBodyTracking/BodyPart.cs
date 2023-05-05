using RootMotion.FinalIK;
using UnityEngine;

namespace FullBodyTracking
{
    public enum BodyPart { Rfoot, Lfoot, Head, Rhand, Lhand, Back }

	public static class IKExtensions
	{
        /// <summary>
        /// Returns true iff this <code>BodyPart</code> is <code>Lfoot</code> or <code>Rfoot</code>
        /// </summary>
        /// <param name="part">this <code>BodyPart</code></param>
        /// <returns> true iff this <code>BodyPart</code> is <code>Lfoot</code> or <code>Rfoot</code></returns>
        public static bool IsFoot(this BodyPart part) => part == BodyPart.Rfoot || part == BodyPart.Lfoot;

		public static IKEffector GetIKEffector(this FullBodyBipedIK ik, BodyPart part)
		{
			switch (part)
			{
				case BodyPart.Head:
					return null;
				case BodyPart.Rhand:
					return ik.solver.rightHandEffector;
				case BodyPart.Lhand:
					return ik.solver.leftHandEffector;
				case BodyPart.Rfoot:
					return ik.solver.rightFootEffector;
				case BodyPart.Lfoot:
					return ik.solver.leftFootEffector;
				case BodyPart.Back:
					return ik.solver.bodyEffector;
			}

			return null;
		}

		public static Transform GetIKBodyReference(this FullBodyBipedIK ik, BodyPart part)
		{
			switch (part)
			{
				case BodyPart.Head:
					return ik.references.head;
				case BodyPart.Rhand:
					return ik.references.rightHand;
				case BodyPart.Lhand:
					return ik.references.leftHand;
				case BodyPart.Rfoot:
					return ik.references.rightFoot;
				case BodyPart.Lfoot:
					return ik.references.leftFoot;
				case BodyPart.Back:
					return ik.references.pelvis;
			}

			return null;
		}

	}
}