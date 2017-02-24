using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RVP
{
    [RequireComponent(typeof(VehicleParent))]
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Stunt/Stunt Detector", 1)]

    //Class for detecting stunts
    public class StuntDetect : MonoBehaviour
    {
        Transform tr;
        Rigidbody rb;
        VehicleParent vp;

        [System.NonSerialized]
        public float score;
        List<Stunt> stunts = new List<Stunt>();
        List<Stunt> doneStunts = new List<Stunt>();
        bool drifting;
        float driftDist;
        float driftScore;
        float endDriftTime;//Time during which drifting counts even if the vehicle is not actually drifting
        float jumpDist;
        float jumpTime;
        Vector3 jumpStart;

        public bool detectDrift = true;
        public bool detectJump = true;
        public bool detectFlips = true;

        string driftString;//String indicating drift distance
        string jumpString;//String indicating jump distance
        string flipString;//String indicating flips
        [System.NonSerialized]
        public string stuntString;//String containing all stunts

        public Motor engine;

        void Start()
        {
            tr = transform;
            rb = GetComponent<Rigidbody>();
            vp = GetComponent<VehicleParent>();
        }

        void FixedUpdate()
        {
            //Detect drifts
            if (detectDrift && !vp.crashing)
            {
                DetectDrift();
            }
            else
            {
                drifting = false;
                driftDist = 0;
                driftScore = 0;
                driftString = "";
            }

            //Detect jumps
            if (detectJump && !vp.crashing)
            {
                DetectJump();
            }
            else
            {
                jumpTime = 0;
                jumpDist = 0;
                jumpString = "";
            }

            //Detect flips
            if (detectFlips && !vp.crashing)
            {
                DetectFlips();
            }
            else
            {
                stunts.Clear();
                flipString = "";
            }

            //Combine strings into final stunt string
            stuntString = vp.crashing ? "Crashed" : driftString + jumpString + (string.IsNullOrEmpty(flipString) || string.IsNullOrEmpty(jumpString) ? "" : " + ") + flipString;
        }

        void DetectDrift()
        {
            endDriftTime = vp.groundedWheels > 0 ? (Mathf.Abs(vp.localVelocity.x) > 5 ? StuntManager.driftConnectDelayStatic : Mathf.Max(0, endDriftTime - Time.timeScale * TimeMaster.inverseFixedTimeFactor)) : 0;
            drifting = endDriftTime > 0;

            if (drifting)
            {
                driftScore += (StuntManager.driftScoreRateStatic * Mathf.Abs(vp.localVelocity.x)) * Time.timeScale * TimeMaster.inverseFixedTimeFactor;
                driftDist += vp.velMag * Time.fixedDeltaTime;
                driftString = "Drift: " + driftDist.ToString("n0") + " m";

                if (engine)
                {
                    engine.boost += (StuntManager.driftBoostAddStatic * Mathf.Abs(vp.localVelocity.x)) * Time.timeScale * 0.0002f * TimeMaster.inverseFixedTimeFactor;
                }
            }
            else
            {
                score += driftScore;
                driftDist = 0;
                driftScore = 0;
                driftString = "";
            }
        }

        void DetectJump()
        {
            if (vp.groundedWheels == 0)
            {
                jumpDist = Vector3.Distance(jumpStart, tr.position);
                jumpTime += Time.fixedDeltaTime;
                jumpString = "Jump: " + jumpDist.ToString("n0") + " m";

                if (engine)
                {
                    engine.boost += StuntManager.jumpBoostAddStatic * Time.timeScale * 0.01f * TimeMaster.inverseFixedTimeFactor;
                }
            }
            else
            {
                score += (jumpDist + jumpTime) * StuntManager.jumpScoreRateStatic;

                if (engine)
                {
                    engine.boost += (jumpDist + jumpTime) * StuntManager.jumpBoostAddStatic * Time.timeScale * 0.01f * TimeMaster.inverseFixedTimeFactor;
                }

                jumpStart = tr.position;
                jumpDist = 0;
                jumpTime = 0;
                jumpString = "";
            }
        }

        void DetectFlips()
        {
            if (vp.groundedWheels == 0)
            {
                //Check to see if vehicle is performing a stunt and add it to the stunts list
                foreach (Stunt curStunt in StuntManager.stuntsStatic)
                {
                    if (Vector3.Dot(vp.localAngularVel.normalized, curStunt.rotationAxis) >= curStunt.precision)
                    {
                        bool stuntExists = false;

                        foreach (Stunt checkStunt in stunts)
                        {
                            if (curStunt.name == checkStunt.name)
                            {
                                stuntExists = true;
                                break;
                            }
                        }

                        if (!stuntExists)
                        {
                            stunts.Add(new Stunt(curStunt));
                        }
                    }
                }

                //Check the progress of stunts and compile the flip string listing all stunts
                foreach (Stunt curStunt2 in stunts)
                {
                    if (Vector3.Dot(vp.localAngularVel.normalized, curStunt2.rotationAxis) >= curStunt2.precision)
                    {
                        curStunt2.progress += rb.angularVelocity.magnitude * Time.fixedDeltaTime;
                    }

                    if (curStunt2.progress * Mathf.Rad2Deg >= curStunt2.angleThreshold)
                    {
                        bool stuntDoneExists = false;

                        foreach (Stunt curDoneStunt in doneStunts)
                        {
                            if (curDoneStunt == curStunt2)
                            {
                                stuntDoneExists = true;
                                break;
                            }
                        }

                        if (!stuntDoneExists)
                        {
                            doneStunts.Add(curStunt2);
                        }
                    }
                }

                string stuntCount = "";
                flipString = "";

                foreach (Stunt curDoneStunt2 in doneStunts)
                {
                    stuntCount = curDoneStunt2.progress * Mathf.Rad2Deg >= curDoneStunt2.angleThreshold * 2 ? " x" + Mathf.FloorToInt((curDoneStunt2.progress * Mathf.Rad2Deg) / curDoneStunt2.angleThreshold).ToString() : "";
                    flipString = string.IsNullOrEmpty(flipString) ? curDoneStunt2.name + stuntCount : flipString + " + " + curDoneStunt2.name + stuntCount;
                }
            }
            else
            {
                //Add stunt points to the score
                foreach (Stunt curStunt in stunts)
                {
                    score += curStunt.progress * Mathf.Rad2Deg * curStunt.scoreRate * Mathf.FloorToInt((curStunt.progress * Mathf.Rad2Deg) / curStunt.angleThreshold) * curStunt.multiplier;

                    //Add boost to the engine
                    if (engine)
                    {
                        engine.boost += curStunt.progress * Mathf.Rad2Deg * curStunt.boostAdd * curStunt.multiplier * 0.01f;
                    }
                }

                stunts.Clear();
                doneStunts.Clear();
                flipString = "";
            }
        }
    }
}