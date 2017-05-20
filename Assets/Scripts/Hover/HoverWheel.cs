using UnityEngine;
using System.Collections;

namespace RVP
{
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Hover/Hover Wheel", 1)]

    //Class for hover vehicle wheels
    public class HoverWheel : MonoBehaviour
    {
        Transform tr;
        Rigidbody rb;
        VehicleParent vp;

        [System.NonSerialized]
        public HoverContact contactPoint = new HoverContact();//Contact points of the wheels
        [System.NonSerialized]
        public bool getContact = true;//Should the wheel try to get contact info?
        [System.NonSerialized]
        public bool grounded;
        public float hoverDistance;
        [Tooltip("If the distance to the ground is less than this, extra hovering force will be applied based on the buffer float force")]
        public float bufferDistance;
        Vector3 upDir;//Local up direction

        [System.NonSerialized]
        public bool doFloat;//Is the wheel turned on?
        public float floatForce = 1;
        public float bufferFloatForce = 2;

        [Tooltip("Strength of the suspension depending on how compressed it is, x-axis = compression, y-axis = force")]
        public AnimationCurve floatForceCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public float floatExponent = 1;
        public float floatDampening;
        float compression;//How compressed the suspension is

        [System.NonSerialized]
        public float targetSpeed;
        [System.NonSerialized]
        public float targetForce;
        float flippedSideFactor;//Multiplier for inverting the forces on opposite sides
        public float brakeForce = 1;
        public float ebrakeForce = 2;
        [System.NonSerialized]
        public float steerRate;

        [Tooltip("How much the wheel steers")]
        public float steerFactor;
        public float sideFriction;

        [Header("Visual Wheel")]

        public Transform visualWheel;
        public float visualTiltRate = 10;
        public float visualTiltAmount = 0.5f;

        GameObject detachedWheel;
        MeshCollider detachedCol;
        Rigidbody detachedBody;
        MeshFilter detachFilter;

        [Header("Damage")]

        public float detachForce = Mathf.Infinity;
        public float mass = 0.05f;
        [System.NonSerialized]
        public bool connected = true;
        [System.NonSerialized]
        public bool canDetach;
        public Mesh wheelMeshLoose;//Mesh for detached wheel collider
        public PhysicMaterial detachedWheelMaterial;

        void Start()
        {
            tr = transform;
            rb = (Rigidbody)F.GetTopmostParentComponent<Rigidbody>(tr);
            vp = (VehicleParent)F.GetTopmostParentComponent<VehicleParent>(tr);
            flippedSideFactor = Vector3.Dot(tr.forward, vp.transform.right) < 0 ? 1 : -1;
            canDetach = detachForce < Mathf.Infinity && Application.isPlaying;
            bufferDistance = Mathf.Min(hoverDistance, bufferDistance);

            if (canDetach)
            {
                detachedWheel = new GameObject(vp.transform.name + "'s Detached Wheel");
                detachedWheel.layer = LayerMask.NameToLayer("Detachable Part");
                detachFilter = detachedWheel.AddComponent<MeshFilter>();
                detachFilter.sharedMesh = visualWheel.GetComponent<MeshFilter>().sharedMesh;
                MeshRenderer detachRend = detachedWheel.AddComponent<MeshRenderer>();
                detachRend.sharedMaterial = visualWheel.GetComponent<MeshRenderer>().sharedMaterial;
                detachedCol = detachedWheel.AddComponent<MeshCollider>();
                detachedCol.convex = true;
                detachedBody = detachedWheel.AddComponent<Rigidbody>();
                detachedBody.mass = mass;
                detachedWheel.SetActive(false);
            }
        }

        void Update()
        {
            //Tilt the visual wheel
            if (visualWheel && connected)
            {
                TiltWheel();
            }
        }

        void FixedUpdate()
        {
            upDir = tr.up;

            if (getContact)
            {
                GetWheelContact();
            }
            else if (grounded)
            {
                contactPoint.point += rb.GetPointVelocity(tr.position) * Time.fixedDeltaTime;
            }

            compression = Mathf.Clamp01(contactPoint.distance / (hoverDistance));

            if (grounded && doFloat && connected)
            {
                ApplyFloat();
                ApplyFloatDrive();
            }
        }

        //Get the contact point of the wheel
        void GetWheelContact()
        {
            RaycastHit hit = new RaycastHit();
            Vector3 localVel = rb.GetPointVelocity(tr.position);
            RaycastHit[] wheelHits = Physics.RaycastAll(tr.position, -upDir, hoverDistance, GlobalControl.wheelCastMaskStatic);
            bool validHit = false;
            float hitDist = Mathf.Infinity;

            //Loop through contact points to get the closest one
            foreach (RaycastHit curHit in wheelHits)
            {
                if (!curHit.transform.IsChildOf(vp.tr) && curHit.distance < hitDist)
                {
                    hit = curHit;
                    hitDist = curHit.distance;
                    validHit = true;
                }
            }

            //Set contact point variables
            if (validHit)
            {
                if (!hit.collider.transform.IsChildOf(vp.tr))
                {
                    grounded = true;
                    contactPoint.distance = hit.distance;
                    contactPoint.point = hit.point + localVel * Time.fixedDeltaTime;
                    contactPoint.grounded = true;
                    contactPoint.normal = hit.normal;
                    contactPoint.relativeVelocity = tr.InverseTransformDirection(localVel);
                    contactPoint.col = hit.collider;
                }
            }
            else
            {
                grounded = false;
                contactPoint.distance = hoverDistance;
                contactPoint.point = Vector3.zero;
                contactPoint.grounded = false;
                contactPoint.normal = upDir;
                contactPoint.relativeVelocity = Vector3.zero;
                contactPoint.col = null;
            }
        }

        //Make the vehicle hover
        void ApplyFloat()
        {
            if (grounded)
            {
                //Get the vertical speed of the wheel
                float travelVel = vp.norm.InverseTransformDirection(rb.GetPointVelocity(tr.position)).z;

                rb.AddForceAtPosition(upDir * floatForce * (Mathf.Pow(floatForceCurve.Evaluate(1 - compression), Mathf.Max(1, floatExponent)) - floatDampening * Mathf.Clamp(travelVel, -1, 1))
                    , tr.position
                    , vp.suspensionForceMode);

                if (contactPoint.distance < bufferDistance)
                {
                    rb.AddForceAtPosition(-upDir * bufferFloatForce * floatForceCurve.Evaluate(contactPoint.distance / bufferDistance) * Mathf.Clamp(travelVel, -1, 0)
                        , tr.position
                        , vp.suspensionForceMode);
                }
            }
        }

        //Drive the vehicle
        void ApplyFloatDrive()
        {
            //Get proper brake force
            float actualBrake = (vp.localVelocity.z > 0 ? vp.brakeInput : Mathf.Clamp01(vp.accelInput)) * brakeForce + vp.ebrakeInput * ebrakeForce;

            rb.AddForceAtPosition(
                tr.TransformDirection(
                    (Mathf.Clamp(targetSpeed, -1, 1) * targetForce - actualBrake * Mathf.Max(5, Mathf.Abs(contactPoint.relativeVelocity.x)) * Mathf.Sign(contactPoint.relativeVelocity.x) * flippedSideFactor) * flippedSideFactor,
                    0,
                    -steerRate * steerFactor * flippedSideFactor - contactPoint.relativeVelocity.z * sideFriction) * (1 - compression),
                tr.position,
                vp.wheelForceMode);
        }

        //Tilt the visual wheel
        void TiltWheel()
        {
            float sideTilt = Mathf.Clamp(-steerRate * steerFactor * flippedSideFactor - Mathf.Clamp(contactPoint.relativeVelocity.z * 0.1f, -1, 1) * sideFriction, -1, 1);
            float actualBrake = (vp.localVelocity.z > 0 ? vp.brakeInput : Mathf.Clamp01(vp.accelInput)) * brakeForce + vp.ebrakeInput * ebrakeForce;
            float forwardTilt = Mathf.Clamp((Mathf.Clamp(targetSpeed, -1, 1) * targetForce - actualBrake * Mathf.Clamp(contactPoint.relativeVelocity.x * 0.1f, -1, 1) * flippedSideFactor) * flippedSideFactor, -1, 1);

            visualWheel.localRotation = Quaternion.Lerp(visualWheel.localRotation, Quaternion.LookRotation(new Vector3(-forwardTilt * visualTiltAmount, -1 + Mathf.Abs(F.MaxAbs(sideTilt, forwardTilt)) * visualTiltAmount, -sideTilt * visualTiltAmount).normalized, Vector3.forward), visualTiltRate * Time.deltaTime);
        }

        public void Detach()
        {
            if (connected && canDetach)
            {
                connected = false;
                detachedWheel.SetActive(true);
                detachedWheel.transform.position = visualWheel.position;
                detachedWheel.transform.rotation = visualWheel.rotation;
                detachedCol.sharedMaterial = detachedWheelMaterial;
                detachedCol.sharedMesh = wheelMeshLoose ? wheelMeshLoose : detachFilter.sharedMesh;

                rb.mass -= mass;
                detachedBody.velocity = rb.GetPointVelocity(visualWheel.position);
                detachedBody.angularVelocity = rb.angularVelocity;

                visualWheel.gameObject.SetActive(false);
            }
        }

        public void Reattach()
        {
            if (!connected)
            {
                connected = true;
                detachedWheel.SetActive(false);
                rb.mass += mass;
                visualWheel.gameObject.SetActive(true);
            }
        }

        void OnDrawGizmosSelected()
        {
            tr = transform;
            //Draw a ray to show the distance of the "suspension"
            Gizmos.color = Color.white;
            Gizmos.DrawRay(tr.position, -tr.up * hoverDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(tr.position, -tr.up * bufferDistance);
        }

        //Destroy detached wheel
        void OnDestroy()
        {
            if (detachedWheel)
            {
                Destroy(detachedWheel);
            }
        }
    }

    //Class for the contact point
    public class HoverContact
    {
        public bool grounded;//Is it grounded?
        public Collider col;//Collider of the contact point
        public Vector3 point;//Position of the contact point
        public Vector3 normal;//Normal of the contact point
        public Vector3 relativeVelocity;//Velocity of the wheel relative to the contact point
        public float distance;//Distance from the wheel to the contact point
    }
}