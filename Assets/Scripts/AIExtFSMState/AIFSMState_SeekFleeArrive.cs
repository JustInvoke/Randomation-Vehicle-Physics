using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RVP
{
    //FSM state for testing the seek, flee, and arrive behavior
    public class AIFSMState_SeekFleeArrive : MonoBehaviour, AIFSMState
    {
        public int method = 0;
        public UnityEngine.UI.Dropdown dropDown;

        public void OnEnter(AIAgentAutonomous aiAgent)
        {
            Debug.Log(ToString() + "OnEnter");
        }

        public void OnExecute(AIAgentAutonomous aiAgent)
        {
            method = dropDown.value;

            if (method == 0)
            {
                //Debug.Log("Seek");
                aiAgent.Seek(GameObject.Find("Target").transform.position);
            }
            else if(method == 1)
            {
                //Debug.Log("Flee");
                aiAgent.Flee(GameObject.Find("Target").transform.position);
            }
            else if(method == 2)
            {
                //Debug.Log("Arrive");
                aiAgent.Arrive(GameObject.Find("Target").transform.position);
            }
        }

        public void OnExit(AIAgentAutonomous aiAgent)
        {
            Debug.Log(ToString() + "OnExit");
        }
    }
}