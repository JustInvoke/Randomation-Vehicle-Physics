using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RVP
{
    [RequireComponent(typeof(DriveForce))]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Suspension/Suspension", 0)]

    //Class for the suspensions
    public class Suspension : MonoBehaviour
    {
        [System.NonSerialized]
        public Transform tr;
        Rigidbody rb;
        VehicleParent vp;

        //Variables for inverting certain values on opposite sides of the vehicle
        [System.NonSerialized]
        public bool flippedSide;
        [System.NonSerialized]
        public float flippedSideFactor;
        [System.NonSerialized]
        public Quaternion initialRotation;

        public Wheel wheel;
        CapsuleCollider compressCol;//The hard collider

        [Tooltip("Generate a capsule collider for hard compressions")]
        public bool generateHardCollider = true;

        [Tooltip("Multiplier for the radius of the hard collider")]
        public float hardColliderRadiusFactor = 1;
        float hardColliderRadiusFactorPrev;
        float setHardColliderRadiusFactor;
        Transform compressTr;//Transform component of the hard collider

        [Header("Brakes and Steering")]
        public float brakeForce;
        public float ebrakeForce;

        [Range(-180, 180)]
        public float steerRangeMin;
        [Range(-180, 180)]
        public float steerRangeMax;

        [Tooltip("How much the wheel is steered")]
        public float steerFactor = 1;
        [Range(-1, 1)]
        public float steerAngle;
        [System.NonSerialized]
        public float steerDegrees;

        [Tooltip("Effect of Ackermann steering geometry")]
        public float ackermannFactor;

        [Tooltip("The camber of the wheel as it travels, x-axis = compression, y-axis = angle")]
        public AnimationCurve camberCurve = AnimationCurve.Linear(0, 0, 1, 0);
        [Range(-89.999f, 89.999f)]
        public float camberOffset;
        [System.NonSerialized]
        public float camberAngle;

        [Tooltip("Adjust the camber as if it was connected to a solid axle, opposite wheel must be set")]
        public bool solidAxleCamber;
        public Suspension oppositeWheel;

        [Tooltip("Angle at which the suspension points out to the side")]
        [Range(-89.999f, 89.999f)]
        public float sideAngle;
        [Range(-89.999f, 89.999f)]
        public float casterAngle;
        [Range(-89.999f, 89.999f)]
        public float toeAngle;

        [Tooltip("Wheel offset from its pivot point")]
        public float pivotOffset;
        [System.NonSerialized]
        public List<SuspensionPart> movingParts = new List<SuspensionPart>();

        [Header("Spring")]
        public float suspensionDistance;
        [System.NonSerialized]
        public float compression;

        [Tooltip("Should be left at 1 unless testing suspension travel")]
        [Range(0, 1)]
        public float targetCompression;
        [System.NonSerialized]
        public float penetration;//How deep the ground is interesecting with the wheel's tire
        public float springForce;

        [Tooltip("Force of the curve depending on it's compression, x-axis = compression, y-axis = force")]
        public AnimationCurve springForceCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("Exponent for spring force based on compression")]
        public float springExponent = 1;
        public float springDampening;

        [Tooltip("How quickly the suspension extends if it's not grounded")]
        public float extendSpeed = 20;

        [Tooltip("Apply forces to prevent the wheel from intersecting with the ground, not necessary if generating a hard collider")]
        public bool applyHardContactForce = true;
        public float hardContactForce = 50;
        public float hardContactSensitivity = 2;

        [Tooltip("Apply suspension forces at ground point")]
        public bool applyForceAtGroundContact = true;

        [Tooltip("Apply suspension forces along local up direction instead of ground normal")]
        public bool leaningForce;

        [System.NonSerialized]
        public Vector3 maxCompressPoint;//Position of the wheel when the suspension is compressed all the way
        [System.NonSerialized]
        public Vector3 springDirection;
        [System.NonSerialized]
        public Vector3 upDir;//Local up direction
        [System.NonSerialized]
        public Vector3 forwardDir;//Local forward direction

        [System.NonSerialized]
        public DriveForce targetDrive;//The drive being passed into the wheel

        [System.NonSerialized]
        public SuspensionPropertyToggle properties;//Property toggler
        [System.NonSerialized]
        public bool steerEnabled = true;
        [System.NonSerialized]
        public bool steerInverted;
        [System.NonSerialized]
        public bool driveEnabled = true;
        [System.NonSerialized]
        public bool driveInverted;
        [System.NonSerialized]
        public bool ebrakeEnabled = true;
        [System.NonSerialized]
        public bool skidSteerBrake;

        [Header("Damage")]

        [Tooltip("Point around which the suspension pivots when damaged")]
        public Vector3 damagePivot;

        [Tooltip("Compression amount to remain at when wheel is detached")]
        [Range(0, 1)]
        public float detachedCompression = 0.5f;

        public float jamForce = Mathf.Infinity;
        [System.NonSerialized]
        public bool jammed;

        void Start()
        {
            tr = transform;
            rb = (Rigidbody)F.GetTopmostParentComponent<Rigidbody>(tr);
            vp = (VehicleParent)F.GetTopmostParentComponent<VehicleParent>(tr);
            targetDrive = GetComponent<DriveForce>();
            flippedSide = Vector3.Dot(tr.forward, vp.transform.right) < 0;
            flippedSideFactor = flippedSide ? -1 : 1;
            initialRotation = tr.localRotation;

            if (Application.isPlaying)
            {
                GetCamber();

                //Generate the hard collider
                if (generateHardCollider)
                {
                    GameObject cap = new GameObject("Compress Collider");
                    cap.layer = GlobalControl.ignoreWheelCastLayer;
                    compressTr = cap.transform;
                    compressTr.parent = tr;
                    compressTr.localPosition = Vector3.zero;
                    compressTr.localEulerAngles = new Vector3(camberAngle, 0, -casterAngle * flippedSideFactor);
                    compressCol = cap.AddComponent<CapsuleCollider>();
                    compressCol.direction = 1;
                    setHardColliderRadiusFactor = hardColliderRadiusFactor;
                    hardColliderRadiusFactorPrev = setHardColliderRadiusFactor;
                    compressCol.radius = wheel.rimWidth * hardColliderRadiusFactor;
                    compressCol.height = (wheel.popped ? wheel.rimRadius : Mathf.Lerp(wheel.rimRadius, wheel.tireRadius, wheel.tirePressure)) * 2;
                    compressCol.material = GlobalControl.frictionlessMatStatic;
                }

                steerRangeMax = Mathf.Max(steerRangeMin, steerRangeMax);

                properties = GetComponent<SuspensionPropertyToggle>();
                if (properties)
                {
                    UpdateProperties();
                }
            }
        }

        void FixedUpdate()
        {
            upDir = tr.up;
            forwardDir = tr.forward;
            targetCompression = 1;

            GetCamber();

            GetSpringVectors();

            if (wheel.connected)
            {
                compression = Mathf.Min(targetCompression, suspensionDistance > 0 ? Mathf.Clamp01(wheel.contactPoint.distance / suspensionDistance) : 0);
                penetration = Mathf.Min(0, wheel.contactPoint.distance);
            }
            else
            {
                compression = detachedCompression;
                penetration = 0;
            }

            if (targetCompression > 0)
            {
                ApplySuspensionForce();
            }

            //Set hard collider size if it is changed during play mode
            if (generateHardCollider)
            {
                setHardColliderRadiusFactor = hardColliderRadiusFactor;

                if (hardColliderRadiusFactorPrev != setHardColliderRadiusFactor || wheel.updatedSize || wheel.updatedPopped)
                {
                    if (wheel.rimWidth > wheel.actualRadius)
                    {
                        compressCol.direction = 2;
                        compressCol.radius = wheel.actualRadius * hardColliderRadiusFactor;
                        compressCol.height = wheel.rimWidth * 2;
                    }
                    else
                    {
                        compressCol.direction = 1;
                        compressCol.radius = wheel.rimWidth * hardColliderRadiusFactor;
                        compressCol.height = wheel.actualRadius * 2;
                    }
                }

                hardColliderRadiusFactorPrev = setHardColliderRadiusFactor;
            }

            //Set the drive of the wheel
            if (wheel.connected)
            {
                if (wheel.targetDrive)
                {
                    targetDrive.active = driveEnabled;
                    targetDrive.feedbackRPM = wheel.targetDrive.feedbackRPM;
                    wheel.targetDrive.SetDrive(targetDrive);
                }
            }
            else
            {
                targetDrive.feedbackRPM = targetDrive.rpm;
            }
        }

        void Update()
        {
            GetCamber();

            if (!Application.isPlaying)
            {
                GetSpringVectors();
            }

            //Set steer angle for the wheel
            steerDegrees = Mathf.Abs(steerAngle) * (steerAngle > 0 ? steerRangeMax : steerRangeMin);
        }

        void ApplySuspensionForce()
        {
            if (wheel.grounded && wheel.connected)
            {
                //Get the local vertical velocity
                float travelVel = vp.norm.InverseTransformDirection(rb.GetPointVelocity(tr.position)).z;

                //Apply the suspension force
                if (suspensionDistance > 0 && targetCompression > 0)
                {
                    Vector3 appliedSuspensionForce = (leaningForce ? Vector3.Lerp(upDir, vp.norm.forward, Mathf.Abs(Mathf.Pow(Vector3.Dot(vp.norm.forward, vp.upDir), 5))) : vp.norm.forward) * springForce * (Mathf.Pow(springForceCurve.Evaluate(1 - compression), Mathf.Max(1, springExponent)) - (1 - targetCompression) - springDampening * Mathf.Clamp(travelVel, -1, 1));

                    rb.AddForceAtPosition(
                        appliedSuspensionForce
                        , applyForceAtGroundContact ? wheel.contactPoint.point : wheel.tr.position
                        , ForceMode.Acceleration);

                    //If wheel is resting on a rigidbody, apply opposing force to it
                    if (wheel.contactPoint.col.attachedRigidbody)
                    {
                        wheel.contactPoint.col.attachedRigidbody.AddForceAtPosition(
                            -appliedSuspensionForce
                            , wheel.contactPoint.point
                            , ForceMode.Acceleration);
                    }
                }

                //Apply hard contact force
                if (compression == 0 && !generateHardCollider && applyHardContactForce)
                {
                    rb.AddForceAtPosition(
                        -vp.norm.TransformDirection(0, 0, Mathf.Clamp(travelVel, -hardContactSensitivity * TimeMaster.fixedTimeFactor, 0) + penetration) * hardContactForce * Mathf.Clamp01(TimeMaster.fixedTimeFactor)
                        , applyForceAtGroundContact ? wheel.contactPoint.point : wheel.tr.position
                        , ForceMode.Acceleration);
                }
            }
        }

        void GetSpringVectors()
        {
            if (!Application.isPlaying)
            {
                tr = transform;
                flippedSide = Vector3.Dot(tr.forward, vp.transform.right) < 0;
                flippedSideFactor = flippedSide ? -1 : 1;
            }

            maxCompressPoint = tr.position;

            float casterDir = -Mathf.Sin(casterAngle * Mathf.Deg2Rad) * flippedSideFactor;
            float sideDir = -Mathf.Sin(sideAngle * Mathf.Deg2Rad);

            springDirection = tr.TransformDirection(casterDir, Mathf.Max(Mathf.Abs(casterDir), Mathf.Abs(sideDir)) - 1, sideDir).normalized;
        }

        void GetCamber()
        {
            if (solidAxleCamber && oppositeWheel && wheel.connected)
            {
                if (oppositeWheel.wheel.rim && wheel.rim)
                {
                    Vector3 axleDir = tr.InverseTransformDirection((oppositeWheel.wheel.rim.position - wheel.rim.position).normalized);
                    camberAngle = Mathf.Atan2(axleDir.z, axleDir.y) * Mathf.Rad2Deg + 90 + camberOffset;
                }
            }
            else
            {
                camberAngle = camberCurve.Evaluate((Application.isPlaying && wheel.connected ? wheel.travelDist : targetCompression)) + camberOffset;
            }
        }

        //Update the toggleable properties
        public void UpdateProperties()
        {
            if (properties)
            {
                foreach (SuspensionToggledProperty curProperty in properties.properties)
                {
                    switch ((int)curProperty.property)
                    {
                        case 0:
                            steerEnabled = curProperty.toggled;
                            break;
                        case 1:
                            steerInverted = curProperty.toggled;
                            break;
                        case 2:
                            driveEnabled = curProperty.toggled;
                            break;
                        case 3:
                            driveInverted = curProperty.toggled;
                            break;
                        case 4:
                            ebrakeEnabled = curProperty.toggled;
                            break;
                        case 5:
                            skidSteerBrake = curProperty.toggled;
                            break;
                    }
                }
            }
        }

        //Visualize steer range
        void OnDrawGizmosSelected()
        {
            if (!tr)
            {
                tr = transform;
            }

            if (wheel)
            {
                if (wheel.rim)
                {
                    Vector3 wheelPoint = wheel.rim.position;

                    float camberSin = -Mathf.Sin(camberAngle * Mathf.Deg2Rad);
                    float steerSin = Mathf.Sin(Mathf.Lerp(steerRangeMin, steerRangeMax, (steerAngle + 1) * 0.5f) * Mathf.Deg2Rad);
                    float minSteerSin = Mathf.Sin(steerRangeMin * Mathf.Deg2Rad);
                    float maxSteerSin = Mathf.Sin(steerRangeMax * Mathf.Deg2Rad);

                    Gizmos.color = Color.magenta;

                    Gizmos.DrawWireSphere(wheelPoint, 0.05f);

                    Gizmos.DrawLine(wheelPoint, wheelPoint + tr.TransformDirection(minSteerSin
                        , camberSin * (1 - Mathf.Abs(minSteerSin))
                        , Mathf.Cos(steerRangeMin * Mathf.Deg2Rad) * (1 - Mathf.Abs(camberSin))
                        ).normalized);

                    Gizmos.DrawLine(wheelPoint, wheelPoint + tr.TransformDirection(maxSteerSin
                        , camberSin * (1 - Mathf.Abs(maxSteerSin))
                        , Mathf.Cos(steerRangeMax * Mathf.Deg2Rad) * (1 - Mathf.Abs(camberSin))
                        ).normalized);

                    Gizmos.DrawLine(wheelPoint + tr.TransformDirection(minSteerSin
                        , camberSin * (1 - Mathf.Abs(minSteerSin))
                        , Mathf.Cos(steerRangeMin * Mathf.Deg2Rad) * (1 - Mathf.Abs(camberSin))
                        ).normalized * 0.9f
                    , wheelPoint + tr.TransformDirection(maxSteerSin
                        , camberSin * (1 - Mathf.Abs(maxSteerSin))
                         , Mathf.Cos(steerRangeMax * Mathf.Deg2Rad) * (1 - Mathf.Abs(camberSin))
                        ).normalized * 0.9f);

                    Gizmos.DrawLine(wheelPoint, wheelPoint + tr.TransformDirection(steerSin
                        , camberSin * (1 - Mathf.Abs(steerSin))
                        , Mathf.Cos(steerRangeMin * Mathf.Deg2Rad) * (1 - Mathf.Abs(camberSin))
                        ).normalized);
                }
            }

            Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(tr.TransformPoint(damagePivot), 0.05f);
        }
    }
}