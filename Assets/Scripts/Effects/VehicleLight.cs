using UnityEngine;
using System.Collections;

namespace RVP
{
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Effects/Vehicle Light", 3)]

    //Class for individual vehicle lights
    public class VehicleLight : MonoBehaviour
    {
        Renderer rend;
        ShatterPart shatter;
        public bool on;

        [Tooltip("Example: a brake light would be half on when the night lights are on, and fully on when the brakes are pressed")]
        public bool halfOn;
        public Light targetLight;

        [Tooltip("A light shared with another vehicle light, will turn off if one of the lights break, then the unbroken light will turn on its target light")]
        public Light sharedLight;

        [Tooltip("Vehicle light that the shared light is shared with")]
        public VehicleLight sharer;
        public Material onMaterial;
        Material offMaterial;

        [System.NonSerialized]
        public bool shattered;

        void Start()
        {
            rend = GetComponent<Renderer>();
            if (rend)
            {
                offMaterial = rend.sharedMaterial;
            }

            shatter = GetComponent<ShatterPart>();
        }

        void Update()
        {
            if (shatter)
            {
                shattered = shatter.shattered;
            }

            //Configure shared light
            if (sharedLight && sharer)
            {
                sharedLight.enabled = on && sharer.on && !shattered && !sharer.shattered;
            }

            //Configure target light
            if (targetLight)
            {
                if (sharedLight && sharer)
                {
                    targetLight.enabled = !shattered && on && !sharedLight.enabled;
                }
            }

            //Shatter logic
            if (rend)
            {
                if (shattered)
                {
                    if (shatter.brokenMaterial)
                    {
                        rend.sharedMaterial = shatter.brokenMaterial;
                    }
                    else
                    {
                        rend.sharedMaterial = on || halfOn ? onMaterial : offMaterial;
                    }
                }
                else
                {
                    rend.sharedMaterial = on || halfOn ? onMaterial : offMaterial;
                }
            }
        }
    }
}