using UnityEngine;
using System.Collections;

namespace RVP
{
    [RequireComponent(typeof(VehicleParent))]
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Stunt/Flip Control", 2)]

    //Class for in-air rotation of vehicles
    public class FlipControl : MonoBehaviour
    {
        Transform tr;
        Rigidbody rb;
        VehicleParent vp;

        public bool disableDuringCrash;
        public Vector3 flipPower;

        [Tooltip("Continue spinning if input is stopped")]
        public bool freeSpinFlip;

        [Tooltip("Stop spinning if input is stopped and vehicle is upright")]
        public bool stopFlip;

        [Tooltip("How quickly the vehicle will rotate upright in air")]
        public Vector3 rotationCorrection;
        Quaternion velDir;

        [Tooltip("Distance to check for ground for reference normal for rotation correction")]
        public float groundCheckDistance = 100;

        [Tooltip("Minimum dot product between ground normal and global up direction for rotation correction")]
        public float groundSteepnessLimit = 0.5f;

        [Tooltip("How quickly the vehicle will dive in the direction it's soaring")]
        public float diveFactor;

        void Start()
        {
            tr = transform;
            rb = GetComponent<Rigidbody>();
            vp = GetComponent<VehicleParent>();
        }

        void FixedUpdate()
        {
            if (vp.groundedWheels == 0 && (!vp.crashing || (vp.crashing && !disableDuringCrash)))
            {
                velDir = Quaternion.LookRotation(GlobalControl.worldUpDir, rb.velocity);

                if (flipPower != Vector3.zero)
                {
                    ApplyFlip();
                }

                if (stopFlip)
                {
                    ApplyStopFlip();
                }

                if (rotationCorrection != Vector3.zero)
                {
                    ApplyRotationCorrection();
                }

                if (diveFactor > 0)
                {
                    Dive();
                }
            }
        }

        void ApplyFlip()
        {
            Vector3 flipTorque;

            if (freeSpinFlip)
            {
                flipTorque = new Vector3(
                    vp.pitchInput * flipPower.x,
                    vp.yawInput * flipPower.y,
                    vp.rollInput * flipPower.z
                    );
            }
            else
            {
                flipTorque = new Vector3(
                    vp.pitchInput != 0 && Mathf.Abs(vp.localAngularVel.x) > 1 && System.Math.Sign(vp.pitchInput * Mathf.Sign(flipPower.x)) != System.Math.Sign(vp.localAngularVel.x) ? -vp.localAngularVel.x * Mathf.Abs(flipPower.x) : vp.pitchInput * flipPower.x - vp.localAngularVel.x * (1 - Mathf.Abs(vp.pitchInput)) * Mathf.Abs(flipPower.x),
                    vp.yawInput != 0 && Mathf.Abs(vp.localAngularVel.y) > 1 && System.Math.Sign(vp.yawInput * Mathf.Sign(flipPower.y)) != System.Math.Sign(vp.localAngularVel.y) ? -vp.localAngularVel.y * Mathf.Abs(flipPower.y) : vp.yawInput * flipPower.y - vp.localAngularVel.y * (1 - Mathf.Abs(vp.yawInput)) * Mathf.Abs(flipPower.y),
                    vp.rollInput != 0 && Mathf.Abs(vp.localAngularVel.z) > 1 && System.Math.Sign(vp.rollInput * Mathf.Sign(flipPower.z)) != System.Math.Sign(vp.localAngularVel.z) ? -vp.localAngularVel.z * Mathf.Abs(flipPower.z) : vp.rollInput * flipPower.z - vp.localAngularVel.z * (1 - Mathf.Abs(vp.rollInput)) * Mathf.Abs(flipPower.z)
                    );
            }

            rb.AddRelativeTorque(flipTorque, ForceMode.Acceleration);
        }

        void ApplyStopFlip()
        {
            Vector3 stopFlipFactor = Vector3.zero;

            stopFlipFactor.x = vp.pitchInput * flipPower.x == 0 ? Mathf.Pow(Mathf.Clamp01(vp.upDot), Mathf.Clamp(10 - Mathf.Abs(vp.localAngularVel.x), 2, 10)) * 10 : 0;
            stopFlipFactor.y = vp.yawInput * flipPower.y == 0 && vp.sqrVelMag > 5 ? Mathf.Pow(Mathf.Clamp01(Vector3.Dot(vp.forwardDir, velDir * Vector3.up)), Mathf.Clamp(10 - Mathf.Abs(vp.localAngularVel.y), 2, 10)) * 10 : 0;
            stopFlipFactor.z = vp.rollInput * flipPower.z == 0 ? Mathf.Pow(Mathf.Clamp01(vp.upDot), Mathf.Clamp(10 - Mathf.Abs(vp.localAngularVel.z), 2, 10)) * 10 : 0;

            rb.AddRelativeTorque(new Vector3(-vp.localAngularVel.x * stopFlipFactor.x, -vp.localAngularVel.y * stopFlipFactor.y, -vp.localAngularVel.z * stopFlipFactor.z), ForceMode.Acceleration);
        }

        void ApplyRotationCorrection()
        {
            float actualForwardDot = vp.forwardDot;
            float actualRightDot = vp.rightDot;
            float actualUpDot = vp.upDot;

            if (groundCheckDistance > 0)
            {
                RaycastHit groundHit;

                if (Physics.Raycast(tr.position, (-GlobalControl.worldUpDir + rb.velocity).normalized, out groundHit, groundCheckDistance, GlobalControl.groundMaskStatic))
                {
                    if (Vector3.Dot(groundHit.normal, GlobalControl.worldUpDir) >= groundSteepnessLimit)
                    {
                        actualForwardDot = Vector3.Dot(vp.forwardDir, groundHit.normal);
                        actualRightDot = Vector3.Dot(vp.rightDir, groundHit.normal);
                        actualUpDot = Vector3.Dot(vp.upDir, groundHit.normal);
                    }
                }
            }

            rb.AddRelativeTorque(new Vector3(
                vp.pitchInput * flipPower.x == 0 ? actualForwardDot * (1 - Mathf.Abs(actualRightDot)) * rotationCorrection.x - vp.localAngularVel.x * Mathf.Pow(actualUpDot, 2) * 10 : 0,
                vp.yawInput * flipPower.y == 0 && vp.sqrVelMag > 10 ? Vector3.Dot(vp.forwardDir, velDir * Vector3.right) * Mathf.Abs(actualUpDot) * rotationCorrection.y - vp.localAngularVel.y * Mathf.Pow(actualUpDot, 2) * 10 : 0,
                vp.rollInput * flipPower.z == 0 ? -actualRightDot * (1 - Mathf.Abs(actualForwardDot)) * rotationCorrection.z - vp.localAngularVel.z * Mathf.Pow(actualUpDot, 2) * 10 : 0
                ), ForceMode.Acceleration);
        }

        void Dive()
        {
            rb.AddTorque(velDir * Vector3.left * Mathf.Clamp01(vp.velMag * 0.01f) * Mathf.Clamp01(vp.upDot) * diveFactor, ForceMode.Acceleration);
        }
    }
}