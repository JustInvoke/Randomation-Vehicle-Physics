using UnityEngine;
using System.Collections;

namespace RVP
{
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Drivetrain/Transmission/Gearbox Transmission", 0)]

    //Transmission subclass for gearboxes
    public class GearboxTransmission : Transmission
    {
        public Gear[] gears;
        public int startGear;
        [System.NonSerialized]
        public int currentGear;
        int firstGear;
        [System.NonSerialized]
        public float curGearRatio;//Ratio of the current gear

        public bool skipNeutral;

        [Tooltip("Calculate the RPM ranges of the gears in play mode.  This will overwrite the current values")]
        public bool autoCalculateRpmRanges = true;

        [Tooltip("Number of physics steps a shift should last")]
        public float shiftDelay;
        [System.NonSerialized]
        public float shiftTime;

        Gear upperGear;//Next gear above current
        Gear lowerGear;//Next gear below current
        float upshiftDifference;//RPM difference between current gear and upper gear
        float downshiftDifference;//RPM difference between current gear and lower gear

        [Tooltip("Multiplier for comparisons in automatic shifting calculations, should be 2 in most cases")]
        public float shiftThreshold;

        public override void Start()
        {
            base.Start();

            currentGear = Mathf.Clamp(startGear, 0, gears.Length - 1);

            //Get gear number 1 (first one above neutral)
            GetFirstGear();
        }

        void Update()
        {
            //Check for manual shift button presses
            if (!automatic)
            {
                if (vp.upshiftPressed && currentGear < gears.Length - 1)
                {
                    Shift(1);
                }

                if (vp.downshiftPressed && currentGear > 0)
                {
                    Shift(-1);
                }
            }
        }

        void FixedUpdate()
        {
            health = Mathf.Clamp01(health);
            shiftTime = Mathf.Max(0, shiftTime - Time.timeScale * TimeMaster.inverseFixedTimeFactor);
            curGearRatio = gears[currentGear].ratio;

            //Calculate upperGear and lowerGear
            float actualFeedbackRPM = targetDrive.feedbackRPM / Mathf.Abs(curGearRatio);
            int upGearOffset = 1;
            int downGearOffset = 1;

            while ((skipNeutral || automatic) && gears[Mathf.Clamp(currentGear + upGearOffset, 0, gears.Length - 1)].ratio == 0 && currentGear + upGearOffset != 0 && currentGear + upGearOffset != gears.Length - 1)
            {
                upGearOffset++;
            }

            while ((skipNeutral || automatic) && gears[Mathf.Clamp(currentGear - downGearOffset, 0, gears.Length - 1)].ratio == 0 && currentGear - downGearOffset != 0 && currentGear - downGearOffset != 0)
            {
                downGearOffset++;
            }

            upperGear = gears[Mathf.Min(gears.Length - 1, currentGear + upGearOffset)];
            lowerGear = gears[Mathf.Max(0, currentGear - downGearOffset)];

            //Perform RPM calculations
            if (maxRPM == -1)
            {
                maxRPM = targetDrive.curve.keys[targetDrive.curve.length - 1].time;

                if (autoCalculateRpmRanges)
                {
                    CalculateRpmRanges();
                }
            }

            //Set RPMs and torque of output
            newDrive.curve = targetDrive.curve;

            if (curGearRatio == 0 || shiftTime > 0)
            {
                newDrive.rpm = 0;
                newDrive.torque = 0;
            }
            else
            {
                newDrive.rpm = (automatic && skidSteerDrive ? Mathf.Abs(targetDrive.rpm) * Mathf.Sign(vp.accelInput - (vp.brakeIsReverse ? vp.brakeInput * (1 - vp.burnout) : 0)) : targetDrive.rpm) / curGearRatio;
                newDrive.torque = Mathf.Abs(curGearRatio) * targetDrive.torque;
            }

            //Perform automatic shifting
            upshiftDifference = gears[currentGear].maxRPM - upperGear.minRPM;
            downshiftDifference = lowerGear.maxRPM - gears[currentGear].minRPM;

            if (automatic && shiftTime == 0 && vp.groundedWheels > 0)
            {
                if (!skidSteerDrive && vp.burnout == 0)
                {
                    if (Mathf.Abs(vp.localVelocity.z) > 1 || vp.accelInput > 0 || (vp.brakeInput > 0 && vp.brakeIsReverse))
                    {
                        if (currentGear < gears.Length - 1 && (upperGear.minRPM + upshiftDifference * (curGearRatio < 0 ? Mathf.Min(1, shiftThreshold) : shiftThreshold) - actualFeedbackRPM <= 0 || (curGearRatio <= 0 && upperGear.ratio > 0 && (!vp.reversing || (vp.accelInput > 0 && vp.localVelocity.z > curGearRatio * 10)))) && !(vp.brakeInput > 0 && vp.brakeIsReverse && upperGear.ratio >= 0) && !(vp.localVelocity.z < 0 && vp.accelInput == 0))
                        {
                            Shift(1);
                        }
                        else if (currentGear > 0 && (actualFeedbackRPM - (lowerGear.maxRPM - downshiftDifference * shiftThreshold) <= 0 || (curGearRatio >= 0 && lowerGear.ratio < 0 && (vp.reversing || ((vp.accelInput < 0 || (vp.brakeInput > 0 && vp.brakeIsReverse)) && vp.localVelocity.z < curGearRatio * 10)))) && !(vp.accelInput > 0 && lowerGear.ratio <= 0) && (lowerGear.ratio > 0 || vp.localVelocity.z < 1))
                        {
                            Shift(-1);
                        }
                    }
                }
                else if (currentGear != firstGear)
                {
                    //Shift into first gear if skid steering or burning out
                    ShiftToGear(firstGear);
                }
            }

            SetOutputDrives(curGearRatio);
        }

        //Shift gears by the number entered
        public void Shift(int dir)
        {
            if (health > 0)
            {
                shiftTime = shiftDelay;
                currentGear += dir;

                while ((skipNeutral || automatic) && gears[Mathf.Clamp(currentGear, 0, gears.Length - 1)].ratio == 0 && currentGear != 0 && currentGear != gears.Length - 1)
                {
                    currentGear += dir;
                }

                currentGear = Mathf.Clamp(currentGear, 0, gears.Length - 1);
            }
        }

        //Shift straight to the gear specified
        public void ShiftToGear(int gear)
        {
            if (health > 0)
            {
                shiftTime = shiftDelay;
                currentGear = Mathf.Clamp(gear, 0, gears.Length - 1);
            }
        }

        //Caculate ideal RPM ranges for each gear (works most of the time)
        public void CalculateRpmRanges()
        {
            bool cantCalc = false;
            if (!Application.isPlaying)
            {
                GasMotor engine = F.GetTopmostParentComponent<VehicleParent>(transform).GetComponentInChildren<GasMotor>();

                if (engine)
                {
                    maxRPM = engine.torqueCurve.keys[engine.torqueCurve.length - 1].time;
                }
                else
                {
                    Debug.LogError("There is no <GasMotor> in the vehicle to get RPM info from.", this);
                    cantCalc = true;
                }
            }

            if (!cantCalc)
            {
                float prevGearRatio;
                float nextGearRatio;
                float actualMaxRPM = maxRPM * 1000;

                for (int i = 0; i < gears.Length; i++)
                {
                    prevGearRatio = gears[Mathf.Max(i - 1, 0)].ratio;
                    nextGearRatio = gears[Mathf.Min(i + 1, gears.Length - 1)].ratio;

                    if (gears[i].ratio < 0)
                    {
                        gears[i].minRPM = actualMaxRPM / gears[i].ratio;

                        if (nextGearRatio == 0)
                        {
                            gears[i].maxRPM = 0;
                        }
                        else
                        {
                            gears[i].maxRPM = actualMaxRPM / nextGearRatio + (actualMaxRPM / nextGearRatio - gears[i].minRPM) * 0.5f;
                        }
                    }
                    else if (gears[i].ratio > 0)
                    {
                        gears[i].maxRPM = actualMaxRPM / gears[i].ratio;

                        if (prevGearRatio == 0)
                        {
                            gears[i].minRPM = 0;
                        }
                        else
                        {
                            gears[i].minRPM = actualMaxRPM / prevGearRatio - (gears[i].maxRPM - actualMaxRPM / prevGearRatio) * 0.5f;
                        }
                    }
                    else
                    {
                        gears[i].minRPM = 0;
                        gears[i].maxRPM = 0;
                    }

                    gears[i].minRPM *= 0.55f;
                    gears[i].maxRPM *= 0.55f;
                }
            }
        }

        public void GetFirstGear()
        {
            for (int i = 0; i < gears.Length; i++)
            {
                if (gears[i].ratio == 0)
                {
                    firstGear = i + 1;
                    break;
                }
            }
        }
    }

    //Gear class
    [System.Serializable]
    public class Gear
    {
        public float ratio;
        public float minRPM;
        public float maxRPM;
    }
}