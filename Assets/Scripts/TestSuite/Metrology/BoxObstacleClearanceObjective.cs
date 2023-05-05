using UnityEngine;
using System.Collections;
using FullBodyTracking;
using System.Collections.Generic;
using System.Linq;
using System;
using Utils;
using System.Runtime.Serialization;

namespace TestSuite.Metrology
{
	[Serializable]
	public class ObstacleClearanceData
	{
		[DataMember] internal BodyPart foot;
		[DataMember, SerializeField] internal float h_max, h_min, d1_grounded, d2_grounded, d_side;
		public Vector3? lastFramePosition = null;

		internal bool leading = false;

		public bool CrossingOK => (!float.IsInfinity(h_max) && !float.IsInfinity(h_min)) || !float.IsInfinity(d_side);

		public bool Done => !float.IsInfinity(d2_grounded);

		public ObstacleClearanceData(BodyPart ft)
		{
			foot = ft;

			h_min = float.PositiveInfinity;
			h_max = float.NegativeInfinity;

			d1_grounded = d2_grounded = float.PositiveInfinity;

			d_side = float.PositiveInfinity;
		}

		public override string ToString()
		{
			return foot + " hmin: " + h_min + " hmax: " + h_max + " d1g: " + d1_grounded + " d2g: " + d2_grounded + " dside: " + d_side;
		}
	}

	public enum ObstacleClearanceState
	{
		BEFORE, OVER, AFTER, DONE
	}

	[DataContract]
	public class BoxObstacleClearanceObjective : TestObjective
	{
		public bool invertZAxis = false;
		public BoxCollider obstacle;
		public string obstacleName;

		private CollisionListener obstacleCollisionListener;

		private HashSet<FootTracker> collidingFeet;

		private ObstacleClearanceData rfoot = null, lfoot = null;

		// leading foot
		private ObstacleClearanceData ld_foot => (rfoot.leading) ? rfoot : (lfoot.leading) ? lfoot : (rfoot.d2_grounded > lfoot.d2_grounded) ? rfoot : lfoot;
		// trailing foot
		private ObstacleClearanceData tr_foot => (rfoot == ld_foot) ? lfoot : rfoot;

		[IndicatorValue, Metadata(importance = 1)] string leading_foot => ld_foot == rfoot ? "$Rfoot" : "$Lfoot";

		[IndicatorValue, Metadata(unit = "m", importance = -1)] float leading_foot_h_min => ld_foot.h_min;
		[IndicatorValue, Metadata(unit = "m", importance = -1)] float leading_foot_h_max => ld_foot.h_max;
		[IndicatorValue, Metadata(unit = "m", importance = 0)] float leading_foot_d1_grounded => ld_foot.d1_grounded;
		[IndicatorValue, Metadata(unit = "m", importance = 0)] float leading_foot_d2_grounded => ld_foot.d2_grounded;
		[IndicatorValue, Metadata(unit = "m", importance = -1)] float leading_foot_d_side => ld_foot.d_side;

		[IndicatorValue, Metadata(unit = "m", importance = -1)] float trailing_foot_h_min => tr_foot.h_min;
		[IndicatorValue, Metadata(unit = "m", importance = -1)] float trailing_foot_h_max => tr_foot.h_max;
		[IndicatorValue, Metadata(unit = "m", importance = 0)] float trailing_foot_d1_grounded => tr_foot.d1_grounded;
		[IndicatorValue, Metadata(unit = "m", importance = 0)] float trailing_foot_d2_grounded => tr_foot.d2_grounded;
		[IndicatorValue, Metadata(unit = "m", importance = -1)] float trailing_foot_d_side => tr_foot.d_side;


		[IndicatorValue, Metadata(importance = 1)]
		string margin_leading
		{
			get
			{
				if (leading_foot_d_side < 0 || leading_foot_h_min > leading_foot_d_side) return "↨" + leading_foot_h_min.ToString("F2") + "m";
				else return "↔" + leading_foot_d_side.ToString("F2") + "m";
			}
		}

		[IndicatorValue, Metadata(importance = 1)]
		string margin_trailing
		{
			get
			{
				if (trailing_foot_d_side < 0 || trailing_foot_h_min > trailing_foot_d_side) return "↨" + trailing_foot_h_min.ToString("F2") + "m";
				else return "↔" + trailing_foot_d_side.ToString("F2") + "m";
			}
		}

		private string debug;

		public override bool ConditionVerified => lfoot != null && rfoot != null && lfoot.Done && rfoot.Done;

		public override string Name => "$ind:obstacle " + obstacleName;

		public override string DebugMessage => this.debug;

		public Vector3 ObstacleWorldSize => Vector3.Scale(obstacle.transform.lossyScale, obstacle.size);


		public void OnValidate()
		{
			if (obstacle && (obstacleName == null || obstacleName.Length == 0)) obstacleName = obstacle.transform.name;
		}

		protected override void Begin()
		{
			if (obstacleCollisionListener) Destroy(obstacleCollisionListener);

			collidingFeet = new HashSet<FootTracker>();

			obstacleCollisionListener = obstacle.gameObject.AddComponent<CollisionListener>();

			// obstacle clearance data
			rfoot = new ObstacleClearanceData(BodyPart.Rfoot);
			lfoot = new ObstacleClearanceData(BodyPart.Lfoot);

			// collision listener
			var add = CollisionListener.FootTrackerAdapter(f => collidingFeet.Add(f));
			var remove = CollisionListener.FootTrackerAdapter(f => collidingFeet.Remove(f));

			obstacleCollisionListener.onTriggerEnter += add;
			obstacleCollisionListener.onTriggerStay += add;
			obstacleCollisionListener.onTriggerExit += remove;
		}

		/// <summary>
		/// Transforms a world space position to a position in local space, relative to the center of the obstacle collider, ignoring scaling
		/// </summary>
		/// <param name="pos">A position in world space</param>
		/// <returns>The argument position in local space, relative to the center of the obstacle collider, ignoring scaling</returns>
		private Vector3 RelativeToObstacle(Vector3 pos)
		{
			var localPos = Quaternion.Inverse(obstacle.transform.rotation) * (pos - obstacle.transform.position - obstacle.center);
			if (invertZAxis) localPos.z = -localPos.z;

			return localPos;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="tracker">The foot tracker to locate relative to the obstacle</param>
		/// <param name="outLocalPosition">Coordinates of point on foot closest to collider, in local space</param>
		/// <param name="outDistance">Distance between foot and collider on each axis. <code>outDistance[i] == Abs(outLocalPosition[i])</code> if the foot is outside of the collider. 
		/// If the foot collides with the obstacle, <code>outDistance</code> represents the distance to the lowest point of the foot to the collider surface on each axis.</param>
		private Action<Color> PositionRelativeToObstacle(FootTracker tracker, out Vector3 outLocalPosition, out Vector3 outDistance)
		{
			outLocalPosition = Vector3.zero;
			outDistance = Vector3.zero;

			Vector3 footRelative = RelativeToObstacle(tracker.transform.position);

			bool inside = this.collidingFeet.Contains(tracker);

			if (inside)
			{
				outLocalPosition = Vector3.zero;

				Vector3 footRelativeLowest = RelativeToObstacle(tracker.FootCollider.ClosestPoint(obstacle.transform.up * -1000));

				outDistance.x = -(ObstacleWorldSize.x / 2 - Mathf.Abs(footRelativeLowest.x));
				outDistance.y = -(ObstacleWorldSize.y / 2 - footRelativeLowest.y);
				outDistance.z = -(ObstacleWorldSize.z / 2 - Mathf.Abs(footRelativeLowest.z));

				return (color) => Debug.DrawLine(footRelativeLowest, footRelativeLowest, color, duration: 10);
			}
			else
			{
				Vector3 footClosestPoint, obsClosestPoint;
				AdvMath.ClosestPoints(tracker.FootCollider, obstacle, out footClosestPoint, out obsClosestPoint);

				Debug.DrawLine(footClosestPoint, obsClosestPoint, Color.green, duration: 0.01f);

				Vector3 footabs = footClosestPoint;

				footClosestPoint = RelativeToObstacle(footClosestPoint);

				outLocalPosition = footClosestPoint - Vector3.Scale(footClosestPoint.Map(Mathf.Sign), Vector3.Min(footClosestPoint.Map(Mathf.Abs), ObstacleWorldSize / 2));
				outDistance = outLocalPosition.Map(Mathf.Abs);

				return (color) => Debug.DrawLine(footabs, obsClosestPoint, color, duration: 10);
			}
		}

		protected override void End()
		{
			if (obstacleCollisionListener) Destroy(obstacleCollisionListener);
		}

		protected override void RecordFrame()
		{
			FootTracker rtracker = Test.Suite.IKRig[BodyPart.Rfoot]?.GetComponent<FootTracker>();
			FootTracker ltracker = Test.Suite.IKRig[BodyPart.Lfoot]?.GetComponent<FootTracker>();

			debug = "";

			RecordFrameForFoot(rtracker, rfoot);
			RecordFrameForFoot(ltracker, lfoot);

			if (rfoot.Done && !lfoot.Done) rfoot.leading = true;
			else if (lfoot.Done && !rfoot.Done) lfoot.leading = true;
		}

		private void RecordFrameForFoot(FootTracker tracker, ObstacleClearanceData data)
		{
			Vector3 outLocalPosition, outDistance;

			Action<Color> debugDrawer = this.PositionRelativeToObstacle(tracker, out outLocalPosition, out outDistance);

			float h = outDistance.y;
			float d = Mathf.Sign(outDistance.x + outDistance.z) * outDistance.xz().magnitude;

			if (outLocalPosition.z < 0 && tracker.Grounded)
			{
				data.d1_grounded = Mathf.Min(data.d1_grounded, d);
				data.lastFramePosition = outDistance;
			}

			if (outLocalPosition.z > 0 && tracker.Grounded) data.d2_grounded = Mathf.Min(data.d2_grounded, d);

			if (outLocalPosition.z == 0)
			{
				data.h_min = Mathf.Min(data.h_min, h);
				data.h_max = Mathf.Max(data.h_max, h);
				data.d_side = Mathf.Min(data.d_side, d);
			}

			// if the obstacle has been crossed within 1 update (i.e. we didn't capture a frame above the obstacle), we interpolate the value from last frame
			if (outLocalPosition.z >= 0 && data.lastFramePosition != null)
			{
				float obsZ = this.ObstacleWorldSize.z;

				Vector3 last = (Vector3)data.lastFramePosition;
				Vector3 now = outDistance;

				float totalZ = obsZ + last.z + now.z;

				for (int i = 0; i < 2; i++)
				{
					// interpolation to the closest point in the "above obstacle"-zone, that is z = 0
					Vector3 pos = i == 0 ? last : now;
					Vector3 otherPos = i == 0 ? now : last;
					float w = 1f - Mathf.Abs(pos.z) / totalZ;

					Vector3 interpolated = pos * w + otherPos * (1 - w);

					h = interpolated.y;
					d = interpolated.x;

					data.h_min = Mathf.Min(data.h_min, h);
					data.h_max = Mathf.Max(data.h_max, h);
					data.d_side = Mathf.Min(data.d_side, d);

					Debug.Log("interpolated" + i + "  " + w + " / " + last.z + " / " + totalZ + "\n" + last * 10 + " " + now * 10 + " " + interpolated * 10);
				}

				if (outLocalPosition.z > 0) data.lastFramePosition = null;
				else data.lastFramePosition = outDistance;
			}

			debug += tracker.Foot + " " + outLocalPosition + " " + outDistance + "\n";
			debug += data.ToString() + "\n\n";
		}
	}
}