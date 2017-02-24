using UnityEngine;
using System.Collections;

namespace RVP
{
    [RequireComponent(typeof(DriveForce))]

    //Class for transmissions
    public abstract class Transmission : MonoBehaviour
    {
        [Range(0, 1)]
        public float strength = 1;
        [System.NonSerialized]
        public float health = 1;
        protected VehicleParent vp;
        protected DriveForce targetDrive;
        protected DriveForce newDrive;
        public bool automatic;

        [Tooltip("Apply special drive to wheels for skid steering")]
        public bool skidSteerDrive;

        public DriveForce[] outputDrives;

        [Tooltip("Exponent for torque output on each wheel")]
        public float driveDividePower = 3;

        [System.NonSerialized]
        public float maxRPM = -1;

        public virtual void Start()
        {
            vp = (VehicleParent)F.GetTopmostParentComponent<VehicleParent>(transform);
            targetDrive = GetComponent<DriveForce>();
            newDrive = gameObject.AddComponent<DriveForce>();
        }

        protected void SetOutputDrives(float ratio)
        {
            //Distribute drive to wheels
            if (outputDrives.Length > 0)
            {
                int enabledDrives = 0;

                //Check for which outputs are enabled
                foreach (DriveForce curOutput in outputDrives)
                {
                    if (curOutput.active)
                    {
                        enabledDrives++;
                    }
                }

                float torqueFactor = Mathf.Pow(1f / enabledDrives, driveDividePower);
                float tempRPM = 0;

                foreach (DriveForce curOutput in outputDrives)
                {
                    if (curOutput.active)
                    {
                        tempRPM += skidSteerDrive ? Mathf.Abs(curOutput.feedbackRPM) : curOutput.feedbackRPM;
                        curOutput.SetDrive(newDrive, torqueFactor);
                    }
                }

                targetDrive.feedbackRPM = (tempRPM / enabledDrives) * ratio;
            }
        }

        public void ResetMaxRPM()
        {
            maxRPM = -1;//Setting this to -1 triggers subclasses to recalculate things
        }
    }
}