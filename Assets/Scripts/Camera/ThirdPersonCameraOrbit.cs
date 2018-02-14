using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RVP
{
    [AddComponentMenu("RVP/Camera/Third Person Camera Orbit", 0)]
    public class ThirdPersonCameraOrbit : MonoBehaviour
    {
        public Transform cameraTransform;
        public Transform target;

        public string mouseAxisXInput = "Mouse X";
        public string mouseAxisYInput = "Mouse Y";
        public string mouseScrollWheelInput = "Mouse ScrollWheel";

        public bool clickToMoveCamera = false;

        public float distance = 5.0f;
        public float xSpeed = 3000f;
        public float ySpeed = 3000f;

        public float yMinLimit = -20f;
        public float yMaxLimit = 80f;

        public float distanceMin = .5f;
        public float distanceMax = 15f;

        private float m_X = 0.0f;
        private float m_Y = 0.0f;

        private bool m_FirstRun = true;

        public void Init()
        {
            if (!target)
            {
                target = transform;
            }

            if (!cameraTransform)
            {
                cameraTransform = Camera.main.transform;
            }

            Vector3 angles = cameraTransform.eulerAngles;
            m_X = angles.y;
            m_Y = angles.x;

            m_FirstRun = true;
        }

        void Start()
        {
            Init();
        }

        void FixedUpdate()
        {
            if (!target)
            {
                return;
            }

            if (!cameraTransform)
            {
                return;
            }

            bool clicked = true;

            if (clickToMoveCamera)
            {
                clicked = Input.GetMouseButton(1) || m_FirstRun == true;
            }

            if (target && clicked)
            {
                
                m_X += Input.GetAxis(mouseAxisXInput) * xSpeed * distance * 0.02f * Time.deltaTime;
                m_Y -= Input.GetAxis(mouseAxisYInput) * ySpeed * 0.02f * Time.deltaTime;

                m_Y = ClampAngle(m_Y, yMinLimit, yMaxLimit);

                Quaternion rotation = Quaternion.Euler(m_Y, m_X, 0);

                distance = Mathf.Clamp(distance - Input.GetAxis(mouseScrollWheelInput) * 5, distanceMin, distanceMax);

                RaycastHit hit;
                if (Physics.Linecast(target.position, cameraTransform.position, out hit))
                {
                    //distance -= hit.distance;
                }
                Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
                Vector3 position = rotation * negDistance + target.position;

                cameraTransform.rotation = rotation;
                cameraTransform.position = position;
            }
            m_FirstRun = false;
        }

        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F)
                angle += 360F;
            if (angle > 360F)
                angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }
    }
}
