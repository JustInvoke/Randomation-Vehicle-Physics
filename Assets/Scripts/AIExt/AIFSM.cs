using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RVP
{
    //AIFSMState
    //This is an interface of fsm state, will be derived when used.
    public interface AIFSMState
    {
        //Called when the AIFSM begins to change the FSM state
        void OnEnter(AIAgentAutonomous aiAgent);

        //Called when the AIFSM is updated
        void OnExecute(AIAgentAutonomous aiAgent);

        //Called before the AIFSM changes the FSM state
        void OnExit(AIAgentAutonomous aiAgent);
    }

    //AIFSM
    //This is a Finite State Machine class
    public class AIFSM
    {
        //AIFSMState_Dummy
        //Dummy class that is being used when there are no registered FSM States
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

        //Register an FSM state
        public void AddState(string name, AIFSMState state)
        {
            if (m_FSMStates.ContainsKey(name))
                return;

            m_FSMStates.Add(name, state);
        }

        //Get FSM state by its name
        public AIFSMState GetState(string name)
        {
            if (!m_FSMStates.ContainsKey(name))
                return null;
            return m_FSMStates[name];
        }

        //Unregister an FSM state
        public void RemoveState(string name)
        {
            m_FSMStates.Remove(name);
        }

        //Unregister all FSM states
        public void RemoveAllStates()
        {
            m_FSMStates.Clear();
        }

        //Gets the current FSM state name
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

        //Gets the previous FSM state
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

        //Changes the current state to another one
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

        //Reload the current FSM state
        public void ReloadState()
        {
            string prevStName = GetCurrentStateName();
            if (prevStName == null)
                return;

            ChangeToDummyState();

            ChangeState(prevStName);
        }

        //Change to the dummy state
        public void ChangeToDummyState()
        {
            ChangeState(m_DummyState.GetType().Name);
        }

        //Change to the previous state
        public void ChangeToPreviousState()
        {
            string prevStName = GetPreviousStateName();
            if (prevStName == null)
                return;

            ChangeState(prevStName);
        }

        //The FSM update method
        public void Update()
        {
            if (m_CurrentState == null)
                return;

            m_CurrentState.OnExecute(m_AIAgent);
        }
    }
}