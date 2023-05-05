using UnityEngine;
using System.Collections;
using System;
using FullBodyTracking;

namespace Utils
{
    public class CollisionListener : MonoBehaviour
    {
        public event Action<Collider> onTriggerEnter, onTriggerExit, onTriggerStay;
        public event Action<Collider> onCollisionEnter, onCollisionExit, onCollisionStay;

        public static Action<Collider> TrackedObjectAdapter(Action<TrackedObject> listener)
        {
            return collider =>
            {
                var tobj = collider.gameObject.GetComponent<TrackedObject>();
                if (tobj && tobj.bodyPart != null) listener(tobj);
            };
        }

        public static Action<Collider> FootTrackerAdapter(Action<FootTracker> listener)
        {
            return collider =>
            {
                var tobj = collider.gameObject.GetComponent<FootTracker>();
                if (tobj) listener(tobj);
            };
        }

        void OnCollisionEnter(Collision collision)
        {
            onCollisionEnter?.Invoke(collision.collider);
        }

        void OnCollisionStay(Collision collision)
        {
            onCollisionStay?.Invoke(collision.collider);
        }

  		void OnCollisionExit(Collision collision)
        {
            onCollisionExit?.Invoke(collision.collider);
        }

        void OnTriggerEnter(Collider other)
        {
            onTriggerEnter?.Invoke(other);
        }

        void OnTriggerExit(Collider other)
        {
            onTriggerExit?.Invoke(other);
        }

        void OnTriggerStay(Collider other)
        {
            onTriggerStay?.Invoke(other);
        }
    }
}