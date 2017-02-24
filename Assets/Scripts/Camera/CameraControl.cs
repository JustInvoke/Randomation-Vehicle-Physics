using UnityEngine;
using System.Collections;

namespace RVP
{
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(AudioListener))]
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Camera/Camera Control", 0)]

    //Class for controlling the camera
    public class CameraControl : MonoBehaviour
    {
        Transform tr;
        Camera cam;
        VehicleParent vp;
        public Transform target;//The target vehicle
        Rigidbody targetBody;

        public float height;
        public float distance;

        float xInput;
        float yInput;

        Vector3 lookDir;
        float smoothYRot;
        Transform lookObj;
        Vector3 forwardLook;
        Vector3 upLook;
        Vector3 targetForward;
        Vector3 targetUp;
        [Tooltip("Should the camera stay flat? (Local y-axis always points up)")]
        public bool stayFlat;

        [Tooltip("Mask for which objects will be checked in between the camera and target vehicle")]
        public LayerMask castMask;

        void Start()
        {
            tr = transform;
            cam = GetComponent<Camera>();
            Initialize();
        }

        public void Initialize()
        {
            //lookObj is an object used to help position and rotate the camera
            if (!lookObj)
            {
                GameObject lookTemp = new GameObject("Camera Looker");
                lookObj = lookTemp.transform;
            }

            //Set variables based on target vehicle's properties
            if (target)
            {
                vp = target.GetComponent<VehicleParent>();
                distance += vp.cameraDistanceChange;
                height += vp.cameraHeightChange;
                forwardLook = target.forward;
                upLook = target.up;
                targetBody = target.GetComponent<Rigidbody>();
            }

            //Set the audio listener update mode to fixed, because the camera moves in FixedUpdate
            //This is necessary for doppler effects to sound correct
            GetComponent<AudioListener>().velocityUpdateMode = AudioVelocityUpdateMode.Fixed;
        }

        void FixedUpdate()
        {
            if (target && targetBody && target.gameObject.activeSelf)
            {
                if (vp.groundedWheels > 0)
                {
                    targetForward = stayFlat ? new Vector3(vp.norm.up.x, 0, vp.norm.up.z) : vp.norm.up;
                }

                targetUp = stayFlat ? GlobalControl.worldUpDir : vp.norm.forward;
                lookDir = Vector3.Slerp(lookDir, (xInput == 0 && yInput == 0 ? Vector3.forward : new Vector3(xInput, 0, yInput).normalized), 0.1f * TimeMaster.inverseFixedTimeFactor);
                smoothYRot = Mathf.Lerp(smoothYRot, targetBody.angularVelocity.y, 0.02f * TimeMaster.inverseFixedTimeFactor);

                //Determine the upwards direction of the camera
                RaycastHit hit;
                if (Physics.Raycast(target.position, -targetUp, out hit, 1, castMask) && !stayFlat)
                {
                    upLook = Vector3.Lerp(upLook, (Vector3.Dot(hit.normal, targetUp) > 0.5 ? hit.normal : targetUp), 0.05f * TimeMaster.inverseFixedTimeFactor);
                }
                else
                {
                    upLook = Vector3.Lerp(upLook, targetUp, 0.05f * TimeMaster.inverseFixedTimeFactor);
                }

                //Calculate rotation and position variables
                forwardLook = Vector3.Lerp(forwardLook, targetForward, 0.05f * TimeMaster.inverseFixedTimeFactor);
                lookObj.rotation = Quaternion.LookRotation(forwardLook, upLook);
                lookObj.position = target.position;
                Vector3 lookDirActual = (lookDir - new Vector3(Mathf.Sin(smoothYRot), 0, Mathf.Cos(smoothYRot)) * Mathf.Abs(smoothYRot) * 0.2f).normalized;
                Vector3 forwardDir = lookObj.TransformDirection(lookDirActual);
                Vector3 localOffset = lookObj.TransformPoint(-lookDirActual * distance - lookDirActual * Mathf.Min(targetBody.velocity.magnitude * 0.05f, 2) + new Vector3(0, height, 0));

                //Check if there is an object between the camera and target vehicle and move the camera in front of it
                if (Physics.Linecast(target.position, localOffset, out hit, castMask))
                {
                    tr.position = hit.point + (target.position - localOffset).normalized * (cam.nearClipPlane + 0.1f);
                }
                else
                {
                    tr.position = localOffset;
                }

                tr.rotation = Quaternion.LookRotation(forwardDir, lookObj.up);
            }
        }

        //function for setting the rotation input of the camera
        public void SetInput(float x, float y)
        {
            xInput = x;
            yInput = y;
        }

        //Destroy lookObj
        void OnDestroy()
        {
            if (lookObj)
            {
                Destroy(lookObj.gameObject);
            }
        }
    }
}