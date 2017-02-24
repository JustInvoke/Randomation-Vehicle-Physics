using UnityEngine;
using System.Collections;

namespace RVP
{
    [RequireComponent(typeof(VehicleParent))]
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Input/Basic Input", 0)]

    //Class for setting the input with the input manager
    public class BasicInput : MonoBehaviour
    {
        VehicleParent vp;
        public string accelAxis;
        public string brakeAxis;
        public string steerAxis;
        public string ebrakeAxis;
        public string boostButton;
        public string upshiftButton;
        public string downshiftButton;
        public string pitchAxis;
        public string yawAxis;
        public string rollAxis;

        void Start()
        {
            vp = GetComponent<VehicleParent>();
        }

        void Update()
        {
            //Get single-frame input presses
            if (!string.IsNullOrEmpty(upshiftButton))
            {
                if (Input.GetButtonDown(upshiftButton))
                {
                    vp.PressUpshift();
                }
            }

            if (!string.IsNullOrEmpty(downshiftButton))
            {
                if (Input.GetButtonDown(downshiftButton))
                {
                    vp.PressDownshift();
                }
            }
        }

        void FixedUpdate()
        {
            //Get constant inputs
            if (!string.IsNullOrEmpty(accelAxis))
            {
                vp.SetAccel(Input.GetAxis(accelAxis));
            }

            if (!string.IsNullOrEmpty(brakeAxis))
            {
                vp.SetBrake(Input.GetAxis(brakeAxis));
            }

            if (!string.IsNullOrEmpty(steerAxis))
            {
                vp.SetSteer(Input.GetAxis(steerAxis));
            }

            if (!string.IsNullOrEmpty(ebrakeAxis))
            {
                vp.SetEbrake(Input.GetAxis(ebrakeAxis));
            }

            if (!string.IsNullOrEmpty(boostButton))
            {
                vp.SetBoost(Input.GetButton(boostButton));
            }

            if (!string.IsNullOrEmpty(pitchAxis))
            {
                vp.SetPitch(Input.GetAxis(pitchAxis));
            }

            if (!string.IsNullOrEmpty(yawAxis))
            {
                vp.SetYaw(Input.GetAxis(yawAxis));
            }

            if (!string.IsNullOrEmpty(rollAxis))
            {
                vp.SetRoll(Input.GetAxis(rollAxis));
            }

            if (!string.IsNullOrEmpty(upshiftButton))
            {
                vp.SetUpshift(Input.GetAxis(upshiftButton));
            }

            if (!string.IsNullOrEmpty(downshiftButton))
            {
                vp.SetDownshift(Input.GetAxis(downshiftButton));
            }
        }
    }
}