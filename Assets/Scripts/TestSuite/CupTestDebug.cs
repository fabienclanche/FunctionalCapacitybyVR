using UnityEngine;
using TestSuite.Metrology;
using FullBodyTracking;
using Interaction;

namespace TestSuite
{
    public class CupTestDebug : MonoBehaviour
    {
        public TestSuite suite;
        public GameObject dummy;
        public TakeItemObjective takeItemObjective;
        public ObjectPlacementObjective placementObjectiveIn;
        public ObjectPlacementObjective placementObjectiveOut;

        public InteractiveObject item;

        public void Update()
        {
#if UNITY_EDITOR
            if (UserConfig.DevMode)
            {
                if (Input.GetKey(KeyCode.T) && Input.GetKey(KeyCode.Z))
                {
                    dummy.transform.position += dummy.transform.forward * Time.deltaTime * 2;
                }
                if (Input.GetKey(KeyCode.T) && (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.D)))
                {
                    dummy.transform.rotation *= Quaternion.AngleAxis(180, Vector3.up);
                }
                if (Input.GetKey(KeyCode.T) && Input.GetKeyDown(KeyCode.Y))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        var hand = suite.IKRig[i == 0 ? BodyPart.Rhand : BodyPart.Lhand].GetComponent<InteractiveHand>();
                        item = takeItemObjective.itemsClones[i];
                        hand.GrabObject(item);
                        Debug.Log(item.transform.localPosition);
                        Debug.Log(item.transform.localRotation.eulerAngles);
                    }
                }
                if (Input.GetKey(KeyCode.T) && Input.GetKeyDown(KeyCode.U))
                {
                    item.transform.position = placementObjectiveIn.triggerCollider.transform.position;
                    item.transform.rotation = placementObjectiveIn.triggerCollider.transform.rotation;
                }
            }
#endif 
        }

    }
}
