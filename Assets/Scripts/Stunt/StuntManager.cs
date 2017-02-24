using UnityEngine;
using System.Collections;

namespace RVP
{
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Stunt/Stunt Manager", 0)]

    //Class for managing stunts
    public class StuntManager : MonoBehaviour
    {
        public float driftScoreRate;
        public static float driftScoreRateStatic;

        [Tooltip("Maximum time gap between connected drifts")]
        public float driftConnectDelay;
        public static float driftConnectDelayStatic;

        public float driftBoostAdd;
        public static float driftBoostAddStatic;

        public float jumpScoreRate;
        public static float jumpScoreRateStatic;

        public float jumpBoostAdd;
        public static float jumpBoostAddStatic;

        public Stunt[] stunts;
        public static Stunt[] stuntsStatic;

        void Start()
        {
            //Set static variables
            driftScoreRateStatic = driftScoreRate;
            driftConnectDelayStatic = driftConnectDelay;
            driftBoostAddStatic = driftBoostAdd;
            jumpScoreRateStatic = jumpScoreRate;
            jumpBoostAddStatic = jumpBoostAdd;
            stuntsStatic = stunts;
        }
    }

    //Stunt class
    [System.Serializable]
    public class Stunt
    {
        public string name;
        public Vector3 rotationAxis;//Local rotation axis of the stunt
        [Range(0, 1)]
        public float precision = 0.8f;//Limit for the dot product between the rotation axis and the stunt axis
        public float scoreRate;
        public float multiplier = 1;//Multiplier for when the stunt is performed more than once in the same jump
        public float angleThreshold;
        [System.NonSerialized]
        public float progress;//How much rotation has happened during the stunt in radians?
        public float boostAdd;

        //Use this to duplicate a stunt
        public Stunt(Stunt oldStunt)
        {
            name = oldStunt.name;
            rotationAxis = oldStunt.rotationAxis;
            precision = oldStunt.precision;
            scoreRate = oldStunt.scoreRate;
            angleThreshold = oldStunt.angleThreshold;
            multiplier = oldStunt.multiplier;
            boostAdd = oldStunt.boostAdd;
        }
    }
}