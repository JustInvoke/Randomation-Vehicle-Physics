using UnityEngine;
using System.Collections;

namespace RVP
{
    [RequireComponent(typeof(VehicleParent))]
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Vehicle Controllers/Vehicle Balance", 4)]

    //Class for balancing vehicles
    public class VehicleBalance : MonoBehaviour
    {
        Transform tr;
        Rigidbody rb;
        VehicleParent vp;

        float actualPitchInput;
        Vector3 targetLean;
        Vector3 targetLeanActual;

        [Tooltip("Lean strength along each axis")]
        public Vector3 leanFactor;

        [Range(0, 0.99f)]
        public float leanSmoothness;

        [Tooltip("Adjusts the roll based on the speed, x-axis = speed, y-axis = roll amount")]
        public AnimationCurve leanRollCurve = AnimationCurve.Linear(0, 0, 10, 1);

        [Tooltip("Adjusts the pitch based on the speed, x-axis = speed, y-axis = pitch amount")]
        public AnimationCurve leanPitchCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("Adjusts the yaw based on the speed, x-axis = speed, y-axis = yaw amount")]
        public AnimationCurve leanYawCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("Speed above which endos (forward wheelies) aren't allowed")]
        public float endoSpeedThreshold;

        [Tooltip("Exponent for pitch input")]
        public float pitchExponent;

        [Tooltip("How much to lean when sliding sideways")]
        public float slideLeanFactor = 1;

        void Start()
        {
            tr = transform;
            rb = GetComponent<Rigidbody>();
            vp = GetComponent<VehicleParent>();
        }

        void FixedUpdate()
        {
            //Apply endo limit
            actualPitchInput = vp.wheels.Length == 1 ? 0 : Mathf.Clamp(vp.pitchInput, -1, vp.velMag > endoSpeedThreshold ? 0 : 1);

            if (vp.groundedWheels > 0)
            {
                if (leanFactor != Vector3.zero)
                {
                    ApplyLean();
                }
            }
        }

        void ApplyLean()
        {
            if (vp.groundedWheels > 0)
            {
                Vector3 inverseWorldUp;
                inverseWorldUp = vp.norm.InverseTransformDirection(Vector3.Dot(vp.wheelNormalAverage, GlobalControl.worldUpDir) <= 0 ? vp.wheelNormalAverage : Vector3.Lerp(GlobalControl.worldUpDir, vp.wheelNormalAverage, Mathf.Abs(Vector3.Dot(vp.norm.up, GlobalControl.worldUpDir)) * 2));
                Debug.DrawRay(tr.position, vp.norm.TransformDirection(inverseWorldUp), Color.white);

                //Calculate target lean direction
                targetLean = new Vector3(
                    Mathf.Lerp(inverseWorldUp.x, Mathf.Clamp(-vp.rollInput * leanFactor.z * leanRollCurve.Evaluate(Mathf.Abs(vp.localVelocity.z)) + Mathf.Clamp(vp.localVelocity.x * slideLeanFactor, -leanFactor.z * slideLeanFactor, leanFactor.z * slideLeanFactor), -leanFactor.z, leanFactor.z), Mathf.Max(Mathf.Abs(F.MaxAbs(vp.steerInput, vp.rollInput)))),
                    Mathf.Pow(Mathf.Abs(actualPitchInput), pitchExponent) * Mathf.Sign(actualPitchInput) * leanFactor.x,
                    inverseWorldUp.z * (1 - Mathf.Abs(F.MaxAbs(actualPitchInput * leanFactor.x, vp.rollInput * leanFactor.z))));
            }
            else
            {
                targetLean = vp.upDir;
            }

            //Transform targetLean to world space
            targetLeanActual = Vector3.Lerp(targetLeanActual, vp.norm.TransformDirection(targetLean), (1 - leanSmoothness) * Time.timeScale * TimeMaster.inverseFixedTimeFactor).normalized;
            Debug.DrawRay(tr.position, targetLeanActual, Color.black);

            //Apply pitch
            rb.AddTorque(
                vp.norm.right * -(Vector3.Dot(vp.forwardDir, targetLeanActual) * 20 - vp.localAngularVel.x) * 100 * (vp.wheels.Length == 1 ? 1 : leanPitchCurve.Evaluate(Mathf.Abs(actualPitchInput)))
                , ForceMode.Acceleration);

            //Apply yaw
            rb.AddTorque(
                vp.norm.forward * (vp.groundedWheels == 1 ? vp.steerInput * leanFactor.y - vp.norm.InverseTransformDirection(rb.angularVelocity).z : 0) * 100 * leanYawCurve.Evaluate(Mathf.Abs(vp.steerInput))
                , ForceMode.Acceleration);

            //Apply roll
            rb.AddTorque(
                vp.norm.up * (-Vector3.Dot(vp.rightDir, targetLeanActual) * 20 - vp.localAngularVel.z) * 100
                , ForceMode.Acceleration);

            //Turn vehicle during wheelies
            if (vp.groundedWheels == 1 && leanFactor.y > 0)
            {
                rb.AddTorque(vp.norm.TransformDirection(new Vector3(
                    0,
                    0,
                    vp.steerInput * leanFactor.y - vp.norm.InverseTransformDirection(rb.angularVelocity).z
                    )), ForceMode.Acceleration);
            }
        }
    }
}