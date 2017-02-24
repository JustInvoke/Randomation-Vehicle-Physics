using UnityEngine;
using System.Collections;

namespace RVP
{
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Ground Surface/Ground Surface Master", 0)]

    //Class managing surface types
    public class GroundSurfaceMaster : MonoBehaviour
    {
        public GroundSurface[] surfaceTypes;
        public static GroundSurface[] surfaceTypesStatic;

        void Start()
        {
            surfaceTypesStatic = surfaceTypes;
        }
    }

    //Class for individual surface types
    [System.Serializable]
    public class GroundSurface
    {
        public string name = "Surface";
        public bool useColliderFriction;
        public float friction;
        [Tooltip("Always leave tire marks")]
        public bool alwaysScrape;
        [Tooltip("Rims leave sparks on this surface")]
        public bool leaveSparks;
        public AudioClip tireSnd;
        public AudioClip rimSnd;
        public AudioClip tireRimSnd;
    }
}