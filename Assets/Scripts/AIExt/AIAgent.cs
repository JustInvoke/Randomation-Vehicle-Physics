using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RVP
{
    /***
     * AIAgent
     * 
     * This class is a base class of the agent
     * 
     * ***/
    public abstract class AIAgent : MonoBehaviour
    {
        protected Rigidbody m_Rigidbody;

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

        public Vector3 position
        {
            get
            {
                return transform.position;
            }
        }

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

        public float speed
        {
            get
            {
                return velocity.magnitude;
            }
        }

        public Vector3 heading
        {
            get
            {
                return transform.forward;
            }
        }

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
