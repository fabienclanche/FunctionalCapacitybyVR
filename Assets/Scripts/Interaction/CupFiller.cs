using UnityEngine;
using Liquids;
using Utils;

namespace Interaction
{
    public class CupFiller : MonoBehaviour
    {
        public Transform origin;
        public LiquidSim target;
        public float flowRadius;

        private Material currentMaterial;
        private GameObject flow;

        public Collider triggerIn, triggerOut;

        public void Start()
        {
            var cLIn = triggerIn.gameObject.AddComponent<CollisionListener>();
            cLIn.onTriggerStay += (c) => { if (target == null) target = c.GetComponentInChildren<LiquidSim>(includeInactive: true); };
     //       cLIn.onCollisionStay += (c) => { if (target == null) target = c.GetComponentInChildren<LiquidSim>(includeInactive: true); };
            var cLOut = triggerOut.gameObject.AddComponent<CollisionListener>();
            cLOut.onTriggerExit += (c) => { if (c.GetComponentInChildren<LiquidSim>(includeInactive: true) == target) Reset(); };
      //      cLOut.onCollisionExit += (c) => { if (c.GetComponentInChildren<LiquidSim>(includeInactive: true) == target) Reset(); };
        }

        void Reset()
        {
            if (flow) GameObject.Destroy(flow);
            target = null;
        }

        private bool TargetIsFilled()
        {
            return target.volume >= target.MaxVolume * .7f;
        }

        public void Update()
        {
            if (target == null || TargetIsFilled())
            {
                Reset();
                return;
            }

            // initializes the liquid flow renderer between the nozzle and the cup
            if (flow == null)
            {
                flow = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                flow.GetComponent<MeshRenderer>().sharedMaterial = target.SpillMaterial;
                flow.GetComponent<Collider>().isTrigger = true;
            }

            // displays the liquid flow renderer between the nozzle and the cup, then adjusts its transform
            flow.SetActive(true);

            Vector3 targetPoint = target.transform.TransformPoint(target.NormalizedLiquidHeight * target.sphereRadius * Vector3.up);
            Vector3 delta = (origin.transform.position - targetPoint);

            float len = delta.magnitude;
            flow.transform.position = targetPoint + delta / 2;
            flow.transform.localScale = new Vector3(flowRadius, len / 2, flowRadius);

            flow.transform.rotation = Quaternion.LookRotation(delta) * Quaternion.AngleAxis(90, Vector3.right);

            if (!target.gameObject.activeInHierarchy) target.gameObject.SetActive(true);
            target.volume = Mathf.Min(target.MaxVolume, target.volume + target.MaxVolume * Time.deltaTime / 2);
        }
    }
}