using UnityEngine;
using System.Collections;

namespace RVP
{
    [RequireComponent(typeof(VehicleParent))]
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Vehicle Controllers/Vehicle Assist", 1)]

    //Class for assisting vehicle performance
    public class VehicleAssist : MonoBehaviour
    {
        Transform tr;
        Rigidbody rb;
        VehicleParent vp;

        [Header("Drift")]

        [Tooltip("Variables are multiplied based on the number of wheels grounded out of the total number of wheels")]
        public bool basedOnWheelsGrounded;
        float groundedFactor;

        [Tooltip("How much to assist with spinning while drifting")]
        public float driftSpinAssist;
        public float driftSpinSpeed;
        public float driftSpinExponent = 1;

        [Tooltip("Automatically adjust drift angle based on steer input magnitude")]
        public bool autoSteerDrift;
        public float maxDriftAngle = 70;
        float targetDriftAngle;

        [Tooltip("Adjusts the force based on drift speed, x-axis = speed, y-axis = force")]
        public AnimationCurve driftSpinCurve = AnimationCurve.Linear(0, 0, 10, 1);

        [Tooltip("How much to push the vehicle forward while drifting")]
        public float driftPush;

        [Tooltip("Straighten out the vehicle when sliding slightly")]
        public bool straightenAssist;

        [Header("Downforce")]
        public float downforce = 1;
        public bool invertDownforceInReverse;
        public bool applyDownforceInAir;

        [Tooltip("X-axis = speed, y-axis = force")]
        public AnimationCurve downforceCurve = AnimationCurve.Linear(0, 0, 20, 1);

        [Header("Roll Over")]

        [Tooltip("Automatically roll over when rolled over")]
        public bool autoRollOver;

        [Tooltip("Roll over with steer input")]
        public bool steerRollOver;

        [System.NonSerialized]
        public bool rolledOver;

        [Tooltip("Distance to check on sides to see if rolled over")]
        public float rollCheckDistance = 1;
        public float rollOverForce = 1;

        [Tooltip("Maximum speed at which vehicle can be rolled over with assists")]
        public float rollSpeedThreshold;

        [Header("Air")]

        [Tooltip("Increase angular drag immediately after jumping")]
        public bool angularDragOnJump;
        float initialAngularDrag;
        float angDragTime = 0;

        public float fallSpeedLimit = Mathf.Infinity;
        public bool applyFallLimitUpwards;

        void Start()
        {
            tr = transform;
            rb = GetComponent<Rigidbody>();
            vp = GetComponent<VehicleParent>();
            initialAngularDrag = rb.angularDrag;
        }

        void FixedUpdate()
        {
            if (vp.groundedWheels > 0)
            {
                groundedFactor = basedOnWheelsGrounded ? vp.groundedWheels / (vp.hover ? vp.hoverWheels.Length : vp.wheels.Length) : 1;

                angDragTime = 20;
                rb.angularDrag = initialAngularDrag;

                if (driftSpinAssist > 0)
                {
                    ApplySpinAssist();
                }

                if (driftPush > 0)
                {
                    ApplyDriftPush();
                }
            }
            else
            {
                if (angularDragOnJump)
                {
                    angDragTime = Mathf.Max(0, angDragTime - Time.timeScale * TimeMaster.inverseFixedTimeFactor);
                    rb.angularDrag = angDragTime > 0 && vp.upDot > 0.5 ? 10 : initialAngularDrag;
                }
            }

            if (downforce > 0)
            {
                ApplyDownforce();
            }

            if (autoRollOver || steerRollOver)
            {
                RollOver();
            }

            if (Mathf.Abs(vp.localVelocity.y) > fallSpeedLimit && (vp.localVelocity.y < 0 || applyFallLimitUpwards))
            {
                rb.AddRelativeForce(Vector3.down * vp.localVelocity.y, ForceMode.Acceleration);
            }
        }

        void ApplySpinAssist()
        {
            //Get desired rotation speed
            float targetTurnSpeed = 0;

            //Auto steer drift
            if (autoSteerDrift)
            {
                int steerSign = 0;
                if (vp.steerInput != 0)
                {
                    steerSign = (int)Mathf.Sign(vp.steerInput);
                }

                targetDriftAngle = (steerSign != Mathf.Sign(vp.localVelocity.x) ? vp.steerInput : steerSign) * -maxDriftAngle;
                Vector3 velDir = new Vector3(vp.localVelocity.x, 0, vp.localVelocity.z).normalized;
                Vector3 targetDir = new Vector3(Mathf.Sin(targetDriftAngle * Mathf.Deg2Rad), 0, Mathf.Cos(targetDriftAngle * Mathf.Deg2Rad)).normalized;
                Vector3 driftTorqueTemp = velDir - targetDir;
                targetTurnSpeed = driftTorqueTemp.magnitude * Mathf.Sign(driftTorqueTemp.z) * steerSign * driftSpinSpeed - vp.localAngularVel.y * Mathf.Clamp01(Vector3.Dot(velDir, targetDir)) * 2;
            }
            else
            {
                targetTurnSpeed = vp.steerInput * driftSpinSpeed * (vp.localVelocity.z < 0 ? (vp.accelAxisIsBrake ? Mathf.Sign(vp.accelInput) : Mathf.Sign(F.MaxAbs(vp.accelInput, -vp.brakeInput))) : 1);
            }

            rb.AddRelativeTorque(new Vector3(0,
                (targetTurnSpeed - vp.localAngularVel.y) * driftSpinAssist * driftSpinCurve.Evaluate(Mathf.Abs(Mathf.Pow(vp.localVelocity.x, driftSpinExponent))) * groundedFactor
                , 0), ForceMode.Acceleration);

            float rightVelDot = Vector3.Dot(tr.right, rb.velocity.normalized);

            if (straightenAssist && vp.steerInput == 0 && Mathf.Abs(rightVelDot) < 0.1f && vp.sqrVelMag > 5)
            {
                rb.AddRelativeTorque(new Vector3(0,
                    rightVelDot * 100 * Mathf.Sign(vp.localVelocity.z) * driftSpinAssist
                    , 0), ForceMode.Acceleration);
            }
        }

        void ApplyDownforce()
        {
            if (vp.groundedWheels > 0 || applyDownforceInAir)
            {
                rb.AddRelativeForce(new Vector3(0,
                    downforceCurve.Evaluate(Mathf.Abs(vp.localVelocity.z)) * -downforce * (applyDownforceInAir ? 1 : groundedFactor) * (invertDownforceInReverse ? Mathf.Sign(vp.localVelocity.z) : 1)
                    , 0), ForceMode.Acceleration);

                //Reverse downforce
                if (invertDownforceInReverse && vp.localVelocity.z < 0)
                {
                    rb.AddRelativeTorque(new Vector3(
                        downforceCurve.Evaluate(Mathf.Abs(vp.localVelocity.z)) * downforce * (applyDownforceInAir ? 1 : groundedFactor)
                        , 0, 0), ForceMode.Acceleration);
                }
            }
        }

        void RollOver()
        {
            RaycastHit rollHit;

            //Check if rolled over
            if (vp.groundedWheels == 0 && vp.velMag < rollSpeedThreshold && vp.upDot < 0.8 && rollCheckDistance > 0)
            {
                if (Physics.Raycast(tr.position, vp.upDir, out rollHit, rollCheckDistance, GlobalControl.groundMaskStatic) || Physics.Raycast(tr.position, vp.rightDir, out rollHit, rollCheckDistance, GlobalControl.groundMaskStatic) || Physics.Raycast(tr.position, -vp.rightDir, out rollHit, rollCheckDistance, GlobalControl.groundMaskStatic))
                {
                    rolledOver = true;
                }
                else
                {
                    rolledOver = false;
                }
            }
            else
            {
                rolledOver = false;
            }

            //Apply roll over force
            if (rolledOver)
            {
                if (steerRollOver && vp.steerInput != 0)
                {
                    rb.AddRelativeTorque(new Vector3(0, 0,
                        -vp.steerInput * rollOverForce
                        ), ForceMode.Acceleration);
                }
                else if (autoRollOver)
                {
                    rb.AddRelativeTorque(new Vector3(0, 0,
                        -Mathf.Sign(vp.rightDot) * rollOverForce
                        ), ForceMode.Acceleration);
                }
            }
        }

        void ApplyDriftPush()
        {
            float pushFactor = (vp.accelAxisIsBrake ? vp.accelInput : vp.accelInput - vp.brakeInput) * Mathf.Abs(vp.localVelocity.x) * driftPush * groundedFactor * (1 - Mathf.Abs(Vector3.Dot(vp.forwardDir, rb.velocity.normalized)));

            rb.AddForce(vp.norm.TransformDirection(new Vector3(
                Mathf.Abs(pushFactor) * Mathf.Sign(vp.localVelocity.x),
                Mathf.Abs(pushFactor) * Mathf.Sign(vp.localVelocity.z),
                0)), ForceMode.Acceleration);
        }
    }
}