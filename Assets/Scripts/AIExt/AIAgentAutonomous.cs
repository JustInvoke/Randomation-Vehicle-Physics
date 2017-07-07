using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RVP
{
    /***
     * AIAgentAutonomous
     * 
     * This class is the implementation of AIAgent class
     * This is where the substantial components of AI tasks are managed
     * 
     * This is where the user can choose how the behavior of the agents works.
     * 
     * ***/
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(VehicleParent))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class AIAgentAutonomous : AIAgent
    {
        public enum Direction
        {
            Forward,
            Backward,
            Auto,
            AutoFlip,
        }

        public enum Behavior
        {
            None = 0x00000,
            Seek = 0x00002,
            Flee = 0x00004,
            Arrive = 0x00008,
            Wander = 0x00010,
            Pursuit = 0x00020,
            Evade = 0x00040,
        }

        public GameObject AIFSMStateGameObject;
        public string initialFSMState = typeof(AIFSM.AIFSMState_Dummy).FullName;
        public Direction direction = Direction.Forward;
        public Behavior behavior = Behavior.None;

        [Range(0f, float.MaxValue)]
        public float maxSpeed = 100f;

        [Range(0f, float.MaxValue)]
        public float wanderJitter = 80f;

        [Range(0f, float.MaxValue)]
        public float wanderRadius = 10f;

        [Range(0f, float.MaxValue)]
        public float wanderDistance = 10f;

        [Range(0f, float.MaxValue)]
        public float wanderStoppingDistance = 10f;

        [Range(0f, float.MaxValue)]
        public float seekStoppingDistance = 10f;

        [Range(0f, float.MaxValue)]
        public float arriveStoppingDistance = 10f;

        [Range(0f, float.MaxValue)]
        public float fleeStoppingDistance = 10f;

        [Range(0f, float.MaxValue)]
        public float noneStoppingDistance = 10f;

        [Range(0f, float.MaxValue)]
        public float globalFleeDistance = 10f;

        public Vector3 seekTarget = Vector3.zero;
        public Vector3 fleeTarget = Vector3.zero;
        public Vector3 arriveTarget = Vector3.zero;
        public AIAgent pursuitTarget;
        public AIAgent evadeTarget;

        [Range(0f, 180f)]
        public float actuatorForwardMaxAngleToBrake = 10f;

        [Range(0f, 180f)]
        public float actuatorBackwardMaxAngleToBrake = 10f;

        private VehicleParent m_VehicleParent;
        private Transmission m_Transmission;
        private NavMeshAgent m_NavMeshAgent;
        private AISteering m_AISteering;
        private AIFSM m_AIFSM;
        private AIActuator m_AIActuator;
        private Vector3 m_SteeringForce;

        public NavMeshAgent navMeshAgent
        {
            get
            {
                return m_NavMeshAgent;
            }
        }

        public VehicleParent vehicleRoot
        {
            get
            {
                return m_VehicleParent;
            }
        }

        public Transmission transmission
        {
            get
            {
                return m_Transmission;
            }
        }

        public AISteering steering
        {
            get
            {
                return m_AISteering;
            }
        }

        public AIFSM fsm
        {
            get
            {
                return m_AIFSM;
            }
        }

        public AIActuator actuator
        {
            get
            {
                return m_AIActuator;
            }
        }

        public Vector3 desiredVelocity
        {
            get
            {
                return m_NavMeshAgent.desiredVelocity;
            }
        }

        public void None()
        {
            behavior = Behavior.None;
        }

        public void Seek(Vector3 target)
        {
            behavior = Behavior.Seek;
            seekTarget = target;
        }

        public void Arrive(Vector3 target)
        {
            behavior = Behavior.Arrive;
            arriveTarget = target;
        }

        public void Flee(Vector3 target)
        {
            behavior = Behavior.Flee;
            fleeTarget = target;
        }

        public void Wander()
        {
            behavior = Behavior.Wander;
        }

        public void Pursuit(AIAgent target)
        {
            behavior = Behavior.Pursuit;
            pursuitTarget = target;
        }

        public void Evade(AIAgent target)
        {
            behavior = Behavior.Evade;
            evadeTarget = target;
        }

        public override void Awake()
        {
            base.Awake();

            m_VehicleParent = GetComponent<VehicleParent>();
            m_Transmission = gameObject.GetComponentInChildren<Transmission>();
            //m_VehicleRoot.accelAxisIsBrake = false;
            //m_VehicleRoot.brakeIsReverse = false;
            //m_VehicleRoot.holdEbrakePark = false;
            if (m_Transmission)
            {
                //m_Transmission.automatic = false;
            }
            //((EngineGas)m_VehicleRoot.engine).transmission.automatic = false;//MARKED

            m_NavMeshAgent = GetComponent<NavMeshAgent>();
            m_NavMeshAgent.updatePosition = false;
            m_NavMeshAgent.updateRotation = false;

            m_AISteering = new AISteering(this);

            m_AIFSM = new AIFSM(this);

            m_AIActuator = new AIActuator(this);
        }

        void Start()
        {
            Component[] coms = AIFSMStateGameObject.GetComponents<MonoBehaviour>();
            foreach (Component com in coms)
            {
                if (com is AIFSMState)
                {
                    m_AIFSM.AddState(com.GetType().FullName, com as AIFSMState);
                    //Debug.Log(com.GetType().FullName);
                }
            }
            m_AIFSM.ChangeState(initialFSMState);
        }

        void Update()
        {
            UpdateFSM();
            UpdateSteering();
            UpdateActuator();
        }

        void UpdateFSM()
        {
            m_AIFSM.Update();
        }

        void UpdateSteering()
        {
            m_NavMeshAgent.nextPosition = transform.position;
            //m_NavMeshAgent.stoppingDistance = 10f;
            m_NavMeshAgent.destination = m_AISteering.Calculate();
            //m_NavMeshAgent.destination = position;
            Debug.DrawLine(position, m_NavMeshAgent.destination);
            //Debug.Log(m_NavMeshAgent.desiredVelocity.magnitude);
        }

        void UpdateActuator()
        {
            m_AIActuator.Update();
        }
    }
}