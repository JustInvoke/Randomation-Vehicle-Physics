using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RVP
{
    /***
     * AIFSMState
     * 
     * This is an interface of fsm state, will be derived when used.
     * 
     * ***/
    public interface AIFSMState
    {
        void OnEnter(AIAgentAutonomous aiAgent);
        void OnExecute(AIAgentAutonomous aiAgent);
        void OnExit(AIAgentAutonomous aiAgent);
    }

    /***
     * AIFSM
     * 
     * This is a Finite State Machine class
     * 
     * ***/
    public class AIFSM
    {
        public class AIFSMState_Dummy : AIFSMState
        {
            public void OnEnter(AIAgentAutonomous aiAgent)
            {
                Debug.Log(ToString() + "OnEnter");
            }

            public void OnExecute(AIAgentAutonomous aiAgent)
            {
                //Debug.Log(ToString() + "OnExecute");
            }

            public void OnExit(AIAgentAutonomous aiAgent)
            {
                Debug.Log(ToString() + "OnExit");
            }
        }

        private Dictionary<string, AIFSMState> m_FSMStates = new Dictionary<string, AIFSMState>();
        private AIFSMState_Dummy m_DummyState = new AIFSMState_Dummy();
        private AIFSMState m_CurrentState;
        private AIFSMState m_PreviousState;
        private AIAgentAutonomous m_AIAgent;

        public AIFSM(AIAgentAutonomous aiAgent)
        {
            AddState(m_DummyState.GetType().FullName, m_DummyState);
            m_CurrentState = m_DummyState;
            m_AIAgent = aiAgent;
        }

        public void AddState(string name, AIFSMState state)
        {
            if (m_FSMStates.ContainsKey(name))
                return;

            m_FSMStates.Add(name, state);
        }

        public AIFSMState GetState(string name)
        {
            if (!m_FSMStates.ContainsKey(name))
                return null;
            return m_FSMStates[name];
        }

        public void RemoveState(string name)
        {
            m_FSMStates.Remove(name);
        }

        public void RemoveAllStates()
        {
            m_FSMStates.Clear();
        }

        public string GetCurrentStateName()
        {
            foreach (KeyValuePair<string, AIFSMState> state in m_FSMStates)
            {
                if (state.Value == m_CurrentState)
                {
                    return state.Key;
                }
            }
            return null;
        }

        public string GetPreviousStateName()
        {
            foreach (KeyValuePair<string, AIFSMState> state in m_FSMStates)
            {
                if (state.Value == m_PreviousState)
                {
                    return state.Key;
                }
            }
            return null;
        }

        public void ChangeState(string name)
        {
            AIFSMState temp = GetState(name);
            if (temp == null)
                return;
            m_PreviousState = m_CurrentState;
            m_CurrentState.OnExit(m_AIAgent);
            m_CurrentState = temp;
            m_CurrentState.OnEnter(m_AIAgent);
        }

        public void ReloadState()
        {
            string prevStName = GetCurrentStateName();
            if (prevStName == null)
                return;

            ChangeToDummyState();

            ChangeState(prevStName);
        }

        public void ChangeToDummyState()
        {
            ChangeState(m_DummyState.GetType().Name);
        }

        public void ChangeToPreviousState()
        {
            string prevStName = GetPreviousStateName();
            if (prevStName == null)
                return;

            ChangeState(prevStName);
        }

        public void Update()
        {
            if (m_CurrentState == null)
                return;

            m_CurrentState.OnExecute(m_AIAgent);
        }
    }
}