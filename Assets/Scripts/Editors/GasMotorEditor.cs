#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace RVP
{
    [CustomEditor(typeof(GasMotor))]
    [CanEditMultipleObjects]

    public class GasMotorEditor : Editor
    {
        float topSpeed = 0;

        public override void OnInspectorGUI()
        {
            GasMotor targetScript = (GasMotor)target;
            DriveForce nextOutput;
            Transmission nextTrans;
            GearboxTransmission nextGearbox;
            ContinuousTransmission nextConTrans;
            Suspension nextSus;
            bool reachedEnd = false;
            string endOutput = "";

            if (targetScript.outputDrives != null)
            {
                if (targetScript.outputDrives.Length > 0)
                {
                    topSpeed = targetScript.torqueCurve.keys[targetScript.torqueCurve.length - 1].time * 1000;
                    nextOutput = targetScript.outputDrives[0];

                    while (!reachedEnd)
                    {
                        if (nextOutput)
                        {
                            if (nextOutput.GetComponent<Transmission>())
                            {
                                nextTrans = nextOutput.GetComponent<Transmission>();

                                if (nextTrans is GearboxTransmission)
                                {
                                    nextGearbox = (GearboxTransmission)nextTrans;
                                    topSpeed /= nextGearbox.gears[nextGearbox.gears.Length - 1].ratio;
                                }
                                else if (nextTrans is ContinuousTransmission)
                                {
                                    nextConTrans = (ContinuousTransmission)nextTrans;
                                    topSpeed /= nextConTrans.maxRatio;
                                }

                                if (nextTrans.outputDrives.Length > 0)
                                {
                                    nextOutput = nextTrans.outputDrives[0];
                                }
                                else
                                {
                                    topSpeed = -1;
                                    reachedEnd = true;
                                    endOutput = nextTrans.transform.name;
                                }
                            }
                            else if (nextOutput.GetComponent<Suspension>())
                            {
                                nextSus = nextOutput.GetComponent<Suspension>();

                                if (nextSus.wheel)
                                {
                                    topSpeed /= Mathf.PI * 100;
                                    topSpeed *= nextSus.wheel.tireRadius * 2 * Mathf.PI;
                                }
                                else
                                {
                                    topSpeed = -1;
                                }

                                reachedEnd = true;
                                endOutput = nextSus.transform.name;
                            }
                            else
                            {
                                topSpeed = -1;
                                reachedEnd = true;
                                endOutput = targetScript.transform.name;
                            }
                        }
                        else
                        {
                            topSpeed = -1;
                            reachedEnd = true;
                            endOutput = targetScript.transform.name;
                        }
                    }
                }
                else
                {
                    topSpeed = -1;
                    endOutput = targetScript.transform.name;
                }
            }
            else
            {
                topSpeed = -1;
                endOutput = targetScript.transform.name;
            }

            if (topSpeed == -1)
            {
                EditorGUILayout.HelpBox("Motor drive doesn't reach any wheels.  (Ends at " + endOutput + ")", MessageType.Warning);
            }
            else if (targets.Length == 1)
            {
                EditorGUILayout.LabelField("Top Speed (Estimate): " + (topSpeed * 2.23694f).ToString("0.00") + " mph || " + (topSpeed * 3.6f).ToString("0.00") + " km/h", EditorStyles.boldLabel);
            }

            DrawDefaultInspector();
        }
    }
}
#endif