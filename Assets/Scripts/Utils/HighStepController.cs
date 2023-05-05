using UnityEngine;
using System.Collections;
using RootMotion.FinalIK;
using FullBodyTracking;

namespace Utils
{
	[RequireComponent(typeof(FullBodyBipedIK))]
	public class HighStepController : MonoBehaviour
	{
		public float stepTime = 0.5f;
		public float halfstepDistance = 0.5f;
		public float h_max = 0.5f;

		private Coroutine coroutine;

		IEnumerator AnimationCoroutine()
		{
			Debug.Log("start");
			var ik = GetComponent<FullBodyBipedIK>();

			if (ik)
			{
				Debug.Log("startn");

				float t = 0;

				var lfoot = ik.GetIKBodyReference(BodyPart.Lfoot);
				var rfoot = ik.GetIKBodyReference(BodyPart.Rfoot);

				var root = ik.references.root;

				var movingFoot = rfoot;
				var stationaryFoot = lfoot;
				var movingFootEffector = ik.GetIKEffector(BodyPart.Rfoot);
				var stationaryFootEffector = ik.GetIKEffector(BodyPart.Lfoot);

				if (Vector3.Dot(root.forward, rfoot.position - root.position) > Vector3.Dot(root.forward, lfoot.position - root.position))
				{
					movingFoot = lfoot;
					stationaryFoot = rfoot;

					var tmp = movingFootEffector;
					movingFootEffector = stationaryFootEffector;
					stationaryFootEffector = tmp;
				}

				Vector3 forward = Vector3.ProjectOnPlane(root.forward, Vector3.up).normalized * halfstepDistance;

				Vector3 rootPosAtStart = ik.GetIKBodyReference(BodyPart.Back).position, rootPosAtEnd = rootPosAtStart + forward;
				Vector3 footPosAtStart = movingFoot.position, footPosAtEnd = footPosAtStart + forward * 2;
				Vector3 otherFootPos = stationaryFoot.position;

				ik.GetIKEffector(BodyPart.Rfoot).positionWeight = 1;
				stationaryFootEffector.positionWeight = 1;
				ik.GetIKEffector(BodyPart.Back).positionWeight = 1;

				while (t < this.stepTime)
				{
					t += Time.deltaTime;
					if (t > this.stepTime) t = this.stepTime;

					float ratio = t / stepTime;
					float h = ratio * (1 - ratio) * 4 * h_max;
					Vector3 movPos = Vector3.Lerp(footPosAtStart, footPosAtEnd, ratio) + Vector3.up * h;



					ik.GetIKEffector(BodyPart.Back).position = Vector3.Lerp(rootPosAtStart, rootPosAtEnd, ratio);
					movingFootEffector.position = movPos;
					stationaryFootEffector.position = otherFootPos;


					Debug.Log(root.position);

					yield return null;
				}

				movingFootEffector.positionWeight = 0;
				stationaryFootEffector.positionWeight = 0;
				ik.GetIKEffector(BodyPart.Back).positionWeight = 0;

				coroutine = null;
				yield return null;
			}
		}

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space) && coroutine == null)
			{
				coroutine = StartCoroutine(AnimationCoroutine());
			}
		}
	}
}