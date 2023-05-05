using UnityEngine;
using System.Collections;

namespace Utils
{
    public class TestCharacterController : MonoBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            transform.localRotation *= Quaternion.AngleAxis(h * Time.deltaTime * 180, Vector3.up);
            transform.position += transform.forward * v * Time.deltaTime * 2;
        }
    }
}
