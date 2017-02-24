using UnityEngine;
using System.Collections;

namespace RVP
{
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Vehicle Controllers/Vehicle Debug", 3)]

    //Class for easily resetting vehicles
    public class VehicleDebug : MonoBehaviour
    {
        public Vector3 spawnPos;
        public Vector3 spawnRot;

        [Tooltip("Y position below which the vehicle will be reset")]
        public float fallLimit = -10;

        void Update()
        {
            if (Input.GetButtonDown("Reset Rotation"))
            {
                StartCoroutine(ResetRotation());
            }

            if (Input.GetButtonDown("Reset Position") || transform.position.y < fallLimit)
            {
                StartCoroutine(ResetPosition());
            }
        }

        IEnumerator ResetRotation()
        {
            if (GetComponent<VehicleDamage>())
            {
                GetComponent<VehicleDamage>().Repair();
            }

            yield return new WaitForFixedUpdate();
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
            transform.Translate(Vector3.up, Space.World);
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }

        IEnumerator ResetPosition()
        {
            if (GetComponent<VehicleDamage>())
            {
                GetComponent<VehicleDamage>().Repair();
            }

            transform.position = spawnPos;
            yield return new WaitForFixedUpdate();
            transform.rotation = Quaternion.LookRotation(spawnRot, GlobalControl.worldUpDir);
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }
    }
}