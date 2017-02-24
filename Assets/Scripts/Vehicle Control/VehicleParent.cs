using UnityEngine;
using System.Collections;

namespace RVP
{
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Vehicle Controllers/Vehicle Parent", 0)]

    //Vehicle root class
    public class VehicleParent : MonoBehaviour
    {
        [System.NonSerialized]
        public Rigidbody rb;
        [System.NonSerialized]
        public Transform tr;
        [System.NonSerialized]
        public Transform norm;//Normal orientation object

        [System.NonSerialized]
        public float accelInput;
        [System.NonSerialized]
        public float brakeInput;
        [System.NonSerialized]
        public float steerInput;
        [System.NonSerialized]
        public float ebrakeInput;
        [System.NonSerialized]
        public bool boostButton;
        [System.NonSerialized]
        public bool upshiftPressed;
        [System.NonSerialized]
        public bool downshiftPressed;
        [System.NonSerialized]
        public float upshiftHold;
        [System.NonSerialized]
        public float downshiftHold;
        [System.NonSerialized]
        public float pitchInput;
        [System.NonSerialized]
        public float yawInput;
        [System.NonSerialized]
        public float rollInput;

        [Tooltip("Accel axis is used for brake input")]
        public bool accelAxisIsBrake;

        [Tooltip("Brake input will act as reverse input")]
        public bool brakeIsReverse;

        [Tooltip("Automatically hold ebrake if it's pressed while parked")]
        public bool holdEbrakePark;

        public float burnoutThreshold = 0.9f;
        [System.NonSerialized]
        public float burnout;
        public float burnoutSpin = 5;
        [Range(0, 0.9f)]
        public float burnoutSmoothness = 0.5f;
        public Motor engine;

        bool stopUpshift;
        bool stopDownShift;

        [System.NonSerialized]
        public Vector3 localVelocity;//Local space velocity
        [System.NonSerialized]
        public Vector3 localAngularVel;//Local space angular velocity
        [System.NonSerialized]
        public Vector3 forwardDir;//Forward direction
        [System.NonSerialized]
        public Vector3 rightDir;//Right direction
        [System.NonSerialized]
        public Vector3 upDir;//Up direction
        [System.NonSerialized]
        public float forwardDot;//Dot product between forwardDir and GlobalControl.worldUpDir
        [System.NonSerialized]
        public float rightDot;//Dot product between rightDir and GlobalControl.worldUpDir
        [System.NonSerialized]
        public float upDot;//Dot product between upDir and GlobalControl.worldUpDir
        [System.NonSerialized]
        public float velMag;//Velocity magnitude
        [System.NonSerialized]
        public float sqrVelMag;//Velocity squared magnitude

        [System.NonSerialized]
        public bool reversing;

        public Wheel[] wheels;
        public HoverWheel[] hoverWheels;
        public WheelCheckGroup[] wheelGroups;
        bool wheelLoopDone = false;
        public bool hover;
        [System.NonSerialized]
        public int groundedWheels;//Number of wheels grounded
        [System.NonSerialized]
        public Vector3 wheelNormalAverage;//Average normal of the wheel contact points
        Vector3 wheelContactsVelocity;//Average velocity of wheel contact points

        [Tooltip("Lower center of mass by suspension height")]
        public bool suspensionCenterOfMass;
        public Vector3 centerOfMassOffset;

        [Tooltip("Tow vehicle to instantiate")]
        public GameObject towVehicle;
        GameObject newTow;
        [System.NonSerialized]
        public VehicleParent inputInherit;//Vehicle which to inherit input from

        [System.NonSerialized]
        public bool crashing;

        [Header("Crashing")]

        public bool canCrash = true;
        public AudioSource crashSnd;
        public AudioClip[] crashClips;
        [System.NonSerialized]
        public bool playCrashSounds = true;
        public ParticleSystem sparks;
        [System.NonSerialized]
        public bool playCrashSparks = true;

        [Header("Camera")]

        public float cameraDistanceChange;
        public float cameraHeightChange;

        void Start()
        {
            tr = transform;
            rb = GetComponent<Rigidbody>();

            //Create normal orientation object
            GameObject normTemp = new GameObject(tr.name + "'s Normal Orientation");
            norm = normTemp.transform;

            SetCenterOfMass();

            //Instantiate tow vehicle
            if (towVehicle)
            {
                newTow = Instantiate(towVehicle, Vector3.zero, tr.rotation) as GameObject;
                newTow.SetActive(false);
                newTow.transform.position = tr.TransformPoint(newTow.GetComponent<Joint>().connectedAnchor - newTow.GetComponent<Joint>().anchor);
                newTow.GetComponent<Joint>().connectedBody = rb;
                newTow.SetActive(true);
                newTow.GetComponent<VehicleParent>().inputInherit = this;
            }

            if (sparks)
            {
                sparks.transform.parent = null;
            }

            if (wheelGroups.Length > 0)
            {
                StartCoroutine(WheelCheckLoop());
            }
        }

        void Update()
        {
            //Shift single frame pressing logic
            if (stopUpshift)
            {
                upshiftPressed = false;
                stopUpshift = false;
            }

            if (stopDownShift)
            {
                downshiftPressed = false;
                stopDownShift = false;
            }

            if (upshiftPressed)
            {
                stopUpshift = true;
            }

            if (downshiftPressed)
            {
                stopDownShift = true;
            }

            if (inputInherit)
            {
                InheritInputOneShot();
            }

            //Norm orientation visualizing
            //Debug.DrawRay(norm.position, norm.forward, Color.blue);
            //Debug.DrawRay(norm.position, norm.up, Color.green);
            //Debug.DrawRay(norm.position, norm.right, Color.red);
        }

        void FixedUpdate()
        {
            if (inputInherit)
            {
                InheritInput();
            }

            if (wheelLoopDone && wheelGroups.Length > 0)
            {
                wheelLoopDone = false;
                StartCoroutine(WheelCheckLoop());
            }

            GetGroundedWheels();

            if (groundedWheels > 0)
            {
                crashing = false;
            }

            localVelocity = tr.InverseTransformDirection(rb.velocity - wheelContactsVelocity);
            localAngularVel = tr.InverseTransformDirection(rb.angularVelocity);
            velMag = rb.velocity.magnitude;
            sqrVelMag = rb.velocity.sqrMagnitude;
            forwardDir = tr.forward;
            rightDir = tr.right;
            upDir = tr.up;
            forwardDot = Vector3.Dot(forwardDir, GlobalControl.worldUpDir);
            rightDot = Vector3.Dot(rightDir, GlobalControl.worldUpDir);
            upDot = Vector3.Dot(upDir, GlobalControl.worldUpDir);
            norm.transform.position = tr.position;
            norm.transform.rotation = Quaternion.LookRotation(groundedWheels == 0 ? upDir : wheelNormalAverage, forwardDir);

            //Check if performing a burnout
            if (groundedWheels > 0 && !hover && !accelAxisIsBrake && burnoutThreshold >= 0 && accelInput > burnoutThreshold && brakeInput > burnoutThreshold)
            {
                burnout = Mathf.Lerp(burnout, ((5 - Mathf.Min(5, Mathf.Abs(localVelocity.z))) / 5) * Mathf.Abs(accelInput), Time.fixedDeltaTime * (1 - burnoutSmoothness) * 10);
            }
            else if (burnout > 0.01f)
            {
                burnout = Mathf.Lerp(burnout, 0, Time.fixedDeltaTime * (1 - burnoutSmoothness) * 10);
            }
            else
            {
                burnout = 0;
            }

            if (engine)
            {
                burnout *= engine.health;
            }

            //Check if reversing
            if (brakeIsReverse && brakeInput > 0 && localVelocity.z < 1 && burnout == 0)
            {
                reversing = true;
            }
            else if (localVelocity.z >= 0 || burnout > 0)
            {
                reversing = false;
            }
        }

        public void SetAccel(float f)
        {
            f = Mathf.Clamp(f, -1, 1);
            accelInput = f;
        }

        public void SetBrake(float f)
        {
            brakeInput = accelAxisIsBrake ? -Mathf.Clamp(accelInput, -1, 0) : Mathf.Clamp(f, -1, 1);
        }

        public void SetSteer(float f)
        {
            steerInput = Mathf.Clamp(f, -1, 1);
        }

        public void SetEbrake(float f)
        {
            if ((f > 0 || ebrakeInput > 0) && holdEbrakePark && velMag < 1 && accelInput == 0 && (brakeInput == 0 || !brakeIsReverse))
            {
                ebrakeInput = 1;
            }
            else
            {
                ebrakeInput = Mathf.Clamp01(f);
            }
        }

        public void SetBoost(bool b)
        {
            boostButton = b;
        }

        public void SetPitch(float f)
        {
            pitchInput = Mathf.Clamp(f, -1, 1);
        }

        public void SetYaw(float f)
        {
            yawInput = Mathf.Clamp(f, -1, 1);
        }

        public void SetRoll(float f)
        {
            rollInput = Mathf.Clamp(f, -1, 1);
        }

        public void PressUpshift()
        {
            upshiftPressed = true;
        }

        public void PressDownshift()
        {
            downshiftPressed = true;
        }

        public void SetUpshift(float f)
        {
            upshiftHold = f;
        }

        public void SetDownshift(float f)
        {
            downshiftHold = f;
        }

        void InheritInput()
        {
            accelInput = inputInherit.accelInput;
            brakeInput = inputInherit.brakeInput;
            steerInput = inputInherit.steerInput;
            ebrakeInput = inputInherit.ebrakeInput;
            pitchInput = inputInherit.pitchInput;
            yawInput = inputInherit.yawInput;
            rollInput = inputInherit.rollInput;
        }

        void InheritInputOneShot()
        {
            upshiftPressed = inputInherit.upshiftPressed;
            downshiftPressed = inputInherit.downshiftPressed;
        }

        void SetCenterOfMass()
        {
            float susAverage = 0;

            //Get average suspension height
            if (suspensionCenterOfMass)
            {
                if (hover)
                {
                    for (int i = 0; i < hoverWheels.Length; i++)
                    {
                        susAverage = i == 0 ? hoverWheels[i].hoverDistance : (susAverage + hoverWheels[i].hoverDistance) * 0.5f;
                    }
                }
                else
                {
                    for (int i = 0; i < wheels.Length; i++)
                    {
                        float newSusDist = wheels[i].transform.parent.GetComponent<Suspension>().suspensionDistance;
                        susAverage = i == 0 ? newSusDist : (susAverage + newSusDist) * 0.5f;
                    }
                }
            }

            rb.centerOfMass = centerOfMassOffset + new Vector3(0, -susAverage, 0);
            rb.inertiaTensor = rb.inertiaTensor;//This is required due to decoupling of inertia tensor from center of mass in Unity 5.3
        }

        void GetGroundedWheels()
        {
            groundedWheels = 0;
            wheelContactsVelocity = Vector3.zero;

            if (hover)
            {
                for (int i = 0; i < hoverWheels.Length; i++)
                {

                    if (hoverWheels[i].grounded)
                    {
                        wheelNormalAverage = i == 0 ? hoverWheels[i].contactPoint.normal : (wheelNormalAverage + hoverWheels[i].contactPoint.normal).normalized;
                    }

                    if (hoverWheels[i].grounded)
                    {
                        groundedWheels++;
                    }
                }
            }
            else
            {
                for (int i = 0; i < wheels.Length; i++)
                {
                    if (wheels[i].grounded)
                    {
                        wheelContactsVelocity = i == 0 ? wheels[i].contactVelocity : (wheelContactsVelocity + wheels[i].contactVelocity) * 0.5f;
                        wheelNormalAverage = i == 0 ? wheels[i].contactPoint.normal : (wheelNormalAverage + wheels[i].contactPoint.normal).normalized;
                    }

                    if (wheels[i].grounded)
                    {
                        groundedWheels++;
                    }
                }
            }
        }

        //Check for crashes and play collision sounds
        void OnCollisionEnter(Collision col)
        {
            if (col.contacts.Length > 0 && groundedWheels == 0)
            {
                foreach (ContactPoint curCol in col.contacts)
                {
                    if (!curCol.thisCollider.CompareTag("Underside") && curCol.thisCollider.gameObject.layer != GlobalControl.ignoreWheelCastLayer)
                    {
                        if (Vector3.Dot(curCol.normal, col.relativeVelocity.normalized) > 0.2f && col.relativeVelocity.sqrMagnitude > 20)
                        {
                            bool checkTow = true;
                            if (newTow)
                            {
                                checkTow = !curCol.otherCollider.transform.IsChildOf(newTow.transform);
                            }

                            if (checkTow)
                            {
                                crashing = canCrash;

                                if (crashSnd && crashClips.Length > 0 && playCrashSounds)
                                {
                                    crashSnd.PlayOneShot(crashClips[Random.Range(0, crashClips.Length)], Mathf.Clamp01(col.relativeVelocity.magnitude * 0.1f));
                                }

                                if (sparks && playCrashSparks)
                                {
                                    sparks.transform.position = curCol.point;
                                    sparks.transform.rotation = Quaternion.LookRotation(col.relativeVelocity.normalized, curCol.normal);
                                    sparks.Play();
                                }
                            }
                        }
                    }
                }
            }
        }

        void OnCollisionStay(Collision col)
        {
            if (col.contacts.Length > 0 && groundedWheels == 0)
            {
                foreach (ContactPoint curCol in col.contacts)
                {
                    if (!curCol.thisCollider.CompareTag("Underside") && curCol.thisCollider.gameObject.layer != GlobalControl.ignoreWheelCastLayer)
                    {
                        if (col.relativeVelocity.sqrMagnitude < 5)
                        {
                            bool checkTow = true;

                            if (newTow)
                            {
                                checkTow = !curCol.otherCollider.transform.IsChildOf(newTow.transform);
                            }

                            if (checkTow)
                            {
                                crashing = canCrash;
                            }
                        }
                    }
                }
            }
        }

        void OnDestroy()
        {
            if (norm)
            {
                Destroy(norm.gameObject);
            }

            if (sparks)
            {
                Destroy(sparks.gameObject);
            }
        }

        //Loop through all wheel groups to check for wheel contacts
        IEnumerator WheelCheckLoop()
        {
            for (int i = 0; i < wheelGroups.Length; i++)
            {
                wheelGroups[i].Activate();
                wheelGroups[i == 0 ? wheelGroups.Length - 1 : i - 1].Deactivate();
                yield return new WaitForFixedUpdate();
            }

            wheelLoopDone = true;
        }
    }

    //Class for groups of wheels to check each FixedUpdate
    [System.Serializable]
    public class WheelCheckGroup
    {
        public Wheel[] wheels;
        public HoverWheel[] hoverWheels;

        public void Activate()
        {
            foreach (Wheel curWheel in wheels)
            {
                curWheel.getContact = true;
            }

            foreach (HoverWheel curHover in hoverWheels)
            {
                curHover.getContact = true;
            }
        }

        public void Deactivate()
        {
            foreach (Wheel curWheel in wheels)
            {
                curWheel.getContact = false;
            }

            foreach (HoverWheel curHover in hoverWheels)
            {
                curHover.getContact = false;
            }
        }
    }
}