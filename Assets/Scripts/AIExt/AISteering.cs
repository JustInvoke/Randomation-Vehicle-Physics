using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RVP
{
    //AISteering
    //This is a steering behavior class.
    //I implemented the steering behavior to calculate target point instead of steering force.
    //So i'm not sure that it can be called as steering behavior or not.
    //The principle is for each behavior applied, get the correct point to be given to NavMeshAgent.destination.
    public class AISteering
    {
        private AIAgentAutonomous m_AIAgent;
        private Vector3 m_CalculatedTarget = Vector3.zero;
        private Vector3 m_WanderTarget = Vector3.zero;

        public AISteering(AIAgentAutonomous aiAgent)
        {
            m_AIAgent = aiAgent;
        }

        public Vector3 Calculate()
        {
            m_CalculatedTarget = m_AIAgent.position;

            if (m_AIAgent.behavior == AIAgentAutonomous.Behavior.None)
            {
                m_CalculatedTarget = None();
            }
            else if (m_AIAgent.behavior == AIAgentAutonomous.Behavior.Seek)
            {
                m_CalculatedTarget = Seek(m_AIAgent.seekTarget);
            }
            else if (m_AIAgent.behavior == AIAgentAutonomous.Behavior.Arrive)
            {
                m_CalculatedTarget = Arrive(m_AIAgent.arriveTarget);
            }
            else if (m_AIAgent.behavior == AIAgentAutonomous.Behavior.Flee)
            {
                m_CalculatedTarget = Flee(m_AIAgent.fleeTarget);
            }
            else if (m_AIAgent.behavior == AIAgentAutonomous.Behavior.Wander)
            {
                m_CalculatedTarget = Wander();
            }
            else if (m_AIAgent.behavior == AIAgentAutonomous.Behavior.Pursuit)
            {
                m_CalculatedTarget = Pursuit(m_AIAgent.pursuitTarget);
            }
            else if (m_AIAgent.behavior == AIAgentAutonomous.Behavior.Evade)
            {
                m_CalculatedTarget = Evade(m_AIAgent.evadeTarget);
            }

            return m_CalculatedTarget;
        }

        //The AI don't wanna move to anywhere
        Vector3 None()
        {
            m_AIAgent.navMeshAgent.autoBraking = true;
            m_AIAgent.navMeshAgent.stoppingDistance = m_AIAgent.noneStoppingDistance;
            m_AIAgent.navMeshAgent.isStopped = true;

            return m_AIAgent.position;
        }

        //The AI will go to the target, and keep moving event it reaches the target
        Vector3 Seek(Vector3 targetPos)
        {
            m_AIAgent.navMeshAgent.autoBraking = false;
            m_AIAgent.navMeshAgent.stoppingDistance = m_AIAgent.seekStoppingDistance;
            m_AIAgent.navMeshAgent.isStopped = false;

            return targetPos;
        }

        //Same as Seek but stop at destination
        Vector3 Arrive(Vector3 targetPos)
        {
            m_AIAgent.navMeshAgent.autoBraking = true;
            m_AIAgent.navMeshAgent.stoppingDistance = m_AIAgent.arriveStoppingDistance;
            m_AIAgent.navMeshAgent.isStopped = false;

            return targetPos;
        }

        //The AI moves away from the target
        Vector3 Flee(Vector3 targetPos)
        {
            m_AIAgent.navMeshAgent.autoBraking = false;
            m_AIAgent.navMeshAgent.stoppingDistance = m_AIAgent.fleeStoppingDistance;
            m_AIAgent.navMeshAgent.isStopped = false;

            if ((m_AIAgent.position - targetPos).magnitude > m_AIAgent.fleeDistance)
                return m_AIAgent.position;

            return m_AIAgent.position + (m_AIAgent.position - targetPos).normalized * m_AIAgent.fleeDistance;
        }

        //The AI will move by wandering around
        Vector3 Wander()
        {
            m_AIAgent.navMeshAgent.autoBraking = false;
            m_AIAgent.navMeshAgent.stoppingDistance = m_AIAgent.wanderStoppingDistance;
            m_AIAgent.navMeshAgent.isStopped = false;

            float jitterTime = m_AIAgent.wanderJitter * Time.deltaTime;

            m_WanderTarget += new Vector3(
                Random.Range(-1f, 1f) * jitterTime,
                0, //Random.Range(-1f, 1f) * jitterTime,
                Random.Range(-1f, 1f) * jitterTime
                );

            m_WanderTarget.Normalize();

            m_WanderTarget *= m_AIAgent.wanderRadius;

            Vector3 target = m_WanderTarget + new Vector3(0, 0, m_AIAgent.wanderDistance);

            return m_AIAgent.transform.TransformPoint(target);
        }

        //The AI will seek and chase the target (evader)
        Vector3 Pursuit(AIAgent evader)
        {
            if(!evader)
            {
                return None();
            }

            m_AIAgent.navMeshAgent.isStopped = false;

            Vector3 distanceVecBetweenEvaderAndAgent = evader.position - m_AIAgent.position;

            float dotProductBetweenHeadings = Vector3.Dot(m_AIAgent.heading, evader.heading);

            if (Vector3.Dot(distanceVecBetweenEvaderAndAgent, m_AIAgent.heading) > 0 && dotProductBetweenHeadings < -0.95f)
            {
                return Seek(evader.position);
            }

            float predictionTime = distanceVecBetweenEvaderAndAgent.magnitude / (m_AIAgent.maxSpeed + evader.speed);

            return Seek(evader.position + evader.velocity * predictionTime);
        }

        //The AI will run away from the target(pursuer)
        Vector3 Evade(AIAgent pursuer)
        {
            if (!pursuer)
            {
                return None();
            }

            m_AIAgent.navMeshAgent.isStopped = false;

            Vector3 distanceVecBetweenPursuerToAgent = pursuer.position - m_AIAgent.position;

            if (distanceVecBetweenPursuerToAgent.sqrMagnitude > m_AIAgent.fleeDistance * m_AIAgent.fleeDistance)
                return m_AIAgent.position;

            float predictionTime = distanceVecBetweenPursuerToAgent.magnitude / (m_AIAgent.maxSpeed + pursuer.speed);

            return Flee(pursuer.position + pursuer.velocity * predictionTime);
        }
    }
}
