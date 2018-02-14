using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RVP
{
    //AIAgent
    //This class is a base class of the agent
    public abstract class AIAgent : MonoBehaviour
    {
        protected Rigidbody m_Rigidbody;

        //The rigidbody mass
        public float mass
        {
            get
            {
                if(!m_Rigidbody)
                {
                    return 0f;
                }
                return m_Rigidbody.mass;
            }
        }

        //The current position
        public Vector3 position
        {
            get
            {
                return transform.position;
            }
        }

        //The velocity
        public Vector3 velocity
        {
            get
            {
                if (!m_Rigidbody)
                {
                    return Vector3.zero;
                }
                return m_Rigidbody.velocity;
            }
        }

        //The Speed
        public float speed
        {
            get
            {
                return velocity.magnitude;
            }
        }

        //The heading
        public Vector3 heading
        {
            get
            {
                return transform.forward;
            }
        }

        //The side
        public Vector3 side
        {
            get
            {
                return transform.right;
            }
        }

        public virtual void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }
    }
}
