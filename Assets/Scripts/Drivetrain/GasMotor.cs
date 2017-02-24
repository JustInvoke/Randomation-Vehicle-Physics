using UnityEngine;
using System.Collections;

namespace RVP
{
    [RequireComponent(typeof(DriveForce))]
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Drivetrain/Gas Motor", 0)]

    //Motor subclass for internal combustion engines
    public class GasMotor : Motor
    {
        [Header("Performance")]

        [Tooltip("X-axis = RPM in thousands, y-axis = torque.  The rightmost key represents the maximum RPM")]
        public AnimationCurve torqueCurve = AnimationCurve.EaseInOut(0, 0, 8, 1);

        [Range(0, 0.99f)]
        [Tooltip("How quickly the engine adjusts its RPMs")]
        public float inertia;

        [Tooltip("Can the engine turn backwards?")]
        public bool canReverse;
        DriveForce targetDrive;
        [System.NonSerialized]
        public float maxRPM;

        public DriveForce[] outputDrives;

        [Tooltip("Exponent for torque output on each wheel")]
        public float driveDividePower = 3;
        float actualAccel;

        [Header("Transmission")]

        public GearboxTransmission transmission;
        [System.NonSerialized]
        public bool shifting;

        [Tooltip("Increase sound pitch between shifts")]
        public bool pitchIncreaseBetweenShift;

        public override void Start()
        {
            base.Start();
            targetDrive = GetComponent<DriveForce>();
            //Get maximum possible RPM
            GetMaxRPM();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            //Calculate proper input
            actualAccel = Mathf.Lerp(vp.brakeIsReverse && vp.reversing && vp.accelInput <= 0 ? vp.brakeInput : vp.accelInput, Mathf.Max(vp.accelInput, vp.burnout), vp.burnout);
            float accelGet = canReverse ? actualAccel : Mathf.Clamp01(actualAccel);
            actualInput = inputCurve.Evaluate(Mathf.Abs(accelGet)) * Mathf.Sign(accelGet);
            targetDrive.curve = torqueCurve;

            if (ignition)
            {
                float boostEval = boostPowerCurve.Evaluate(Mathf.Abs(vp.localVelocity.z));
                //Set RPM
                targetDrive.rpm = Mathf.Lerp(targetDrive.rpm, actualInput * maxRPM * 1000 * (boosting ? 1 + boostEval : 1), (1 - inertia) * Time.timeScale);
                //Set torque
                if (targetDrive.feedbackRPM > targetDrive.rpm)
                {
                    targetDrive.torque = 0;
                }
                else
                {
                    targetDrive.torque = torqueCurve.Evaluate(targetDrive.feedbackRPM * 0.001f - (boosting ? boostEval : 0)) * Mathf.Lerp(targetDrive.torque, power * Mathf.Abs(System.Math.Sign(actualInput)), (1 - inertia) * Time.timeScale) * (boosting ? 1 + boostEval : 1) * health;
                }

                //Send RPM and torque through drivetrain
                if (outputDrives.Length > 0)
                {
                    float torqueFactor = Mathf.Pow(1f / outputDrives.Length, driveDividePower);
                    float tempRPM = 0;

                    foreach (DriveForce curOutput in outputDrives)
                    {
                        tempRPM += curOutput.feedbackRPM;
                        curOutput.SetDrive(targetDrive, torqueFactor);
                    }

                    targetDrive.feedbackRPM = tempRPM / outputDrives.Length;
                }

                if (transmission)
                {
                    shifting = transmission.shiftTime > 0;
                }
                else
                {
                    shifting = false;
                }
            }
            else
            {
                //If turned off, set RPM and torque to 0 and distribute it through drivetrain
                targetDrive.rpm = 0;
                targetDrive.torque = 0;
                targetDrive.feedbackRPM = 0;
                shifting = false;

                if (outputDrives.Length > 0)
                {
                    foreach (DriveForce curOutput in outputDrives)
                    {
                        curOutput.SetDrive(targetDrive);
                    }
                }
            }
        }

        public override void Update()
        {
            //Set audio pitch
            if (snd && ignition)
            {
                airPitch = vp.groundedWheels > 0 || actualAccel != 0 ? 1 : Mathf.Lerp(airPitch, 0, 0.5f * Time.deltaTime);
                pitchFactor = (actualAccel != 0 || vp.groundedWheels == 0 ? 1 : 0.5f) * (shifting ? (pitchIncreaseBetweenShift ? Mathf.Sin((transmission.shiftTime / transmission.shiftDelay) * Mathf.PI) : Mathf.Min(transmission.shiftDelay, Mathf.Pow(transmission.shiftTime, 2)) / transmission.shiftDelay) : 1) * airPitch;
                targetPitch = Mathf.Abs((targetDrive.feedbackRPM * 0.001f) / maxRPM) * pitchFactor;
            }

            base.Update();
        }

        public void GetMaxRPM()
        {
            maxRPM = torqueCurve.keys[torqueCurve.length - 1].time;

            if (outputDrives.Length > 0)
            {
                foreach (DriveForce curOutput in outputDrives)
                {
                    curOutput.curve = targetDrive.curve;

                    if (curOutput.GetComponent<Transmission>())
                    {
                        curOutput.GetComponent<Transmission>().ResetMaxRPM();
                    }
                }
            }
        }
    }
}