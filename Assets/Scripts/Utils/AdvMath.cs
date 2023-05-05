using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Utils
{
    public static class AdvMath
    {
        public static Vector3 x0z(this Vector3 v)
        {
            return new Vector3(v.x, 0, v.z);
        }

        public static Vector2 xz(this Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        public static Vector2 xy(this Vector3 v)
        {
            return new Vector2(v.x, v.y);
        }

        public static Vector2 yz(this Vector3 v)
        {
            return new Vector2(v.y, v.z);
        }

        public static Vector3 Map(this Vector3 v, Func<float, float> f)
        {
            return new Vector3(f(v.x), f(v.y), f(v.z));
        }

        public static Vector3 Scale(this Vector3 v1, Vector3 v2)
        {
            return Vector3.Scale(v1, v2);
        }

        public static Vector3 Divide(this Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z);
        }

        public static Vector3[] ExtractLocalTransform(Transform tr)
        {
            return new Vector3[] { tr.localPosition, tr.localRotation.eulerAngles, tr.localScale };
        }

        public static void ApplyLocalTransform(Transform tr, Vector3[] localPositionEulerAnglesAndScale)
        {
            tr.localPosition = localPositionEulerAnglesAndScale[0];
            tr.localRotation = Quaternion.Euler(localPositionEulerAnglesAndScale[1]);
            tr.localScale = localPositionEulerAnglesAndScale[2];
        }

        public static float Distance(this Vector3 point, Collider collider)
        {
            return Vector3.Distance(point, collider.ClosestPoint(point));
        }

        public static bool WithinBox(this Vector3 point, Vector3 boxCenter, Vector3 boxSize, out Vector3 insideOutsideMargin)
        {
            Vector3 relP = point - boxCenter;

            insideOutsideMargin = Vector3.zero;

            bool inside = true;

            for (int i = 0; i < 3; i++)
            {
                insideOutsideMargin[i] = Math.Abs(relP[i]) - boxSize[i] / 2;
                if (insideOutsideMargin[i] > 0)
                {
                    inside = false;
                }
            }

            return inside;
        }

        public static void ClosestPoints(Collider collider1, Collider collider2, out Vector3 closestOn1, out Vector3 closestOn2, int iterations = 5, float maxError = 0.001f)
        {
            closestOn1 = collider1.transform.position;
            closestOn2 = collider2.transform.position;

            for (int i = 0; i < iterations; i++)
            {
                Vector3 old1 = closestOn1, old2 = closestOn2;

                var tmp = collider1.ClosestPoint(closestOn2);
                closestOn2 = collider2.ClosestPoint(closestOn1);
                closestOn1 = tmp;

                if ((closestOn1 - old1).sqrMagnitude + (closestOn2 - old2).sqrMagnitude < maxError * maxError) break;
            }
        }

        public static Vector3 VectorTowards(this Collider collider1, Collider otherCollider, int iterations = 5)
        {
            Vector3 p1, p2;

            ClosestPoints(collider1, otherCollider, out p1, out p2, iterations);

            return p2 - p1;
        }


        public static float Distance(this Collider collider1, Collider collider2, int iterations = 5)
        {
            return collider1.VectorTowards(collider2, iterations).magnitude;
        }
    }
}