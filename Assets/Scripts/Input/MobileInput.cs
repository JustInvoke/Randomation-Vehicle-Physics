using UnityEngine;
using System.Collections;

namespace RVP
{
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Input/Mobile Input Setter", 1)]

    //Class for setting mobile input
    public class MobileInput : MonoBehaviour
    {
        //Orientation the screen is locked at
        public ScreenOrientation screenRot = ScreenOrientation.LandscapeLeft;

        [System.NonSerialized]
        public float accel;
        [System.NonSerialized]
        public float brake;
        [System.NonSerialized]
        public float steer;
        [System.NonSerialized]
        public float ebrake;
        [System.NonSerialized]
        public bool boost;

        //Set screen orientation
        void Start()
        {
            Screen.autorotateToPortrait = screenRot == ScreenOrientation.Portrait || screenRot == ScreenOrientation.AutoRotation;
            Screen.autorotateToPortraitUpsideDown = screenRot == ScreenOrientation.PortraitUpsideDown || screenRot == ScreenOrientation.AutoRotation;
            Screen.autorotateToLandscapeRight = screenRot == ScreenOrientation.LandscapeRight || screenRot == ScreenOrientation.Landscape || screenRot == ScreenOrientation.AutoRotation;
            Screen.autorotateToLandscapeLeft = screenRot == ScreenOrientation.LandscapeLeft || screenRot == ScreenOrientation.Landscape || screenRot == ScreenOrientation.AutoRotation;
            Screen.orientation = screenRot;
        }

        //Input setting functions that can be linked to buttons
        public void SetAccel(float f)
        {
            accel = Mathf.Clamp01(f);
        }

        public void SetBrake(float f)
        {
            brake = Mathf.Clamp01(f);
        }

        public void SetSteer(float f)
        {
            steer = Mathf.Clamp(f, -1, 1);
        }

        public void SetEbrake(float f)
        {
            ebrake = Mathf.Clamp01(f);
        }

        public void SetBoost(bool b)
        {
            boost = b;
        }
    }
}