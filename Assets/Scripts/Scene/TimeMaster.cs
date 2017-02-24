using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

namespace RVP
{
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Scene Controllers/Time Master", 1)]

    //Class for managing time
    public class TimeMaster : MonoBehaviour
    {
        float initialFixedTime;//Intial Time.fixedDeltaTime

        [Tooltip("Master audio mixer")]
        public AudioMixer masterMixer;
        public bool destroyOnLoad;
        public static float fixedTimeFactor;//Multiplier for certain variables to change consistently over varying time steps
        public static float inverseFixedTimeFactor;

        void Awake()
        {
            initialFixedTime = Time.fixedDeltaTime;

            if (!destroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        void Update()
        {
            //Set the pitch of all audio to the time scale
            if (masterMixer)
            {
                masterMixer.SetFloat("MasterPitch", Time.timeScale);
            }
        }

        void FixedUpdate()
        {
            //Set the fixed update rate based on time scale
            Time.fixedDeltaTime = Time.timeScale * initialFixedTime;
            fixedTimeFactor = 0.01f / initialFixedTime;
            inverseFixedTimeFactor = 1 / fixedTimeFactor;
        }
    }
}