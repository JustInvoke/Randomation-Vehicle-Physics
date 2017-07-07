using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RVP
{
    /***
     * AIActuator
     * 
     * This class converts the desired velocity to the physical transform limitation such as steer right and left
     * and move forward and backwards
     *  
     * ***/
    public class AIActuator
    {
        private AIAgentAutonomous m_AIAgent;

        public AIActuator(AIAgentAutonomous aiAgent)
        {
            m_AIAgent = aiAgent;
        }

        public void Update()
        {
            m_AIAgent.vehicleRoot.SetAccel(0f);
            m_AIAgent.vehicleRoot.SetSteer(0f);
            m_AIAgent.vehicleRoot.SetBrake(0f);
            m_AIAgent.vehicleRoot.SetEbrake(0f);

            if (m_AIAgent.direction == AIAgentAutonomous.Direction.Forward)
            {
                UpdateActuatorForward();
            }
            else if (m_AIAgent.direction == AIAgentAutonomous.Direction.Backward)
            {
                UpdateActuatorBackward();
            }
            else if (m_AIAgent.direction == AIAgentAutonomous.Direction.Auto)
            {
                UpdateActuatorAuto();
            }
            else if (m_AIAgent.direction == AIAgentAutonomous.Direction.AutoFlip)
            {
                UpdateActuatorAutoFlip();
            }
        }

        void UpdateActuatorForward()
        {
            Vector3 desired = m_AIAgent.desiredVelocity;
            m_AIAgent.vehicleRoot.brakeIsReverse = true;
            if (m_AIAgent.transmission)
            {
                m_AIAgent.transmission.automatic = true;
            }
            m_AIAgent.vehicleRoot.SetAccel(desired.normalized.magnitude);

            float angle = 0f;
            if (desired.magnitude == 0f)
            {
                angle = 0f;
            }
            else
            {
                angle = FindAngleSign(m_AIAgent.heading, desired);
            }

            m_AIAgent.vehicleRoot.SetSteer(angle / 180f);

            if (Mathf.Abs(angle) > m_AIAgent.actuatorForwardMaxAngleToBrake && desired.magnitude != 0f)
            {
                m_AIAgent.vehicleRoot.SetBrake((Mathf.Abs(angle) - m_AIAgent.actuatorForwardMaxAngleToBrake) / (180f - m_AIAgent.actuatorForwardMaxAngleToBrake));
            }
        }

        void UpdateActuatorBackward()
        {
            Vector3 desired = m_AIAgent.desiredVelocity;
            //Debug.Log(desired.normalized.magnitude);
            m_AIAgent.vehicleRoot.brakeIsReverse = false;
            if (m_AIAgent.transmission)
            {
                m_AIAgent.transmission.automatic = false;
                if (m_AIAgent.transmission is GearboxTransmission)
                {
                    GearboxTransmission tg = m_AIAgent.transmission as GearboxTransmission;
                    if (tg.currentGear != 0)
                    {
                        tg.ShiftToGear(0);
                    }
                    m_AIAgent.vehicleRoot.SetAccel(desired.normalized.magnitude);
                }
            }
            else
            {
                m_AIAgent.vehicleRoot.SetAccel(-desired.normalized.magnitude);
            }

            float angle = 0f;
            if (desired.magnitude == 0f)
            {
                angle = 0f;
            }
            else
            {
                angle = FindAngleSign(-m_AIAgent.heading, desired);
            }

            m_AIAgent.vehicleRoot.SetSteer(-angle / 180f);

            if (Mathf.Abs(angle) > m_AIAgent.actuatorBackwardMaxAngleToBrake && desired.magnitude != 0f)
            {
                m_AIAgent.vehicleRoot.SetBrake((Mathf.Abs(angle) - m_AIAgent.actuatorBackwardMaxAngleToBrake) / (180f - m_AIAgent.actuatorBackwardMaxAngleToBrake));
            }
        }

        void UpdateActuatorAuto()
        {
            Vector3 desired = m_AIAgent.desiredVelocity;
            bool movingForward = false;

            float angle = 0f;
            if (desired.magnitude == 0f)
            {
                angle = 0f;
            }
            else
            {
                angle = FindAngleSign(m_AIAgent.heading, desired);
            }

            float absAngle = Mathf.Abs(angle);
            if (absAngle <= m_AIAgent.actuatorForwardMaxAngleToBrake)
            {
                m_AIAgent.vehicleRoot.brakeIsReverse = true;
                if (m_AIAgent.transmission)
                {
                    m_AIAgent.transmission.automatic = true;
                    //Debug.Log("forward full : " + ((TransmissionGearbox)m_AIAgent.transmission).currentGear);
                    if (m_AIAgent.transmission is GearboxTransmission)
                    {
                        GearboxTransmission trg = m_AIAgent.transmission as GearboxTransmission;
                        if (trg.currentGear < 2)
                        {
                            trg.ShiftToGear(2);
                        }
                    }
                }
                m_AIAgent.vehicleRoot.SetAccel(desired.normalized.magnitude);

                m_AIAgent.vehicleRoot.SetSteer(angle / 180f);
                movingForward = true;
            }
            else if (
                absAngle > m_AIAgent.actuatorForwardMaxAngleToBrake
                && absAngle < 180f - m_AIAgent.actuatorBackwardMaxAngleToBrake
                )
            {

                m_AIAgent.vehicleRoot.brakeIsReverse = true;
                if (m_AIAgent.transmission)
                {
                    m_AIAgent.transmission.automatic = true;
                    //Debug.Log("forward brake : " + ((TransmissionGearbox)m_AIAgent.transmission).currentGear);
                    if (m_AIAgent.transmission is GearboxTransmission)
                    {
                        GearboxTransmission trg = m_AIAgent.transmission as GearboxTransmission;
                        if (trg.currentGear < 2)
                        {
                            trg.ShiftToGear(2);
                        }
                    }
                }
                m_AIAgent.vehicleRoot.SetAccel(desired.normalized.magnitude);

                m_AIAgent.vehicleRoot.SetSteer(angle / 180f);

                if (desired.magnitude != 0f)
                {
                    m_AIAgent.vehicleRoot.SetBrake((Mathf.Abs(angle) - m_AIAgent.actuatorForwardMaxAngleToBrake) / (180f - m_AIAgent.actuatorBackwardMaxAngleToBrake - m_AIAgent.actuatorForwardMaxAngleToBrake));
                }
                movingForward = true;
            }
            else if (absAngle >= 180f - m_AIAgent.actuatorBackwardMaxAngleToBrake)
            {
                //Debug.Log("backward");
                m_AIAgent.vehicleRoot.brakeIsReverse = false;
                if (m_AIAgent.transmission)
                {
                    m_AIAgent.transmission.automatic = false;
                    if (m_AIAgent.transmission is GearboxTransmission)
                    {
                        GearboxTransmission tg = m_AIAgent.transmission as GearboxTransmission;
                        if (tg.currentGear != 0)
                        {
                            tg.ShiftToGear(0);
                        }
                        m_AIAgent.vehicleRoot.SetAccel(desired.normalized.magnitude);
                    }
                }
                else
                {
                    m_AIAgent.vehicleRoot.SetAccel(-desired.normalized.magnitude);
                }

                angle = FindAngleSign(-m_AIAgent.heading, desired);

                m_AIAgent.vehicleRoot.SetSteer(-angle / 180f);
                movingForward = false;
            }

            float what = Vector3.Dot(m_AIAgent.heading, m_AIAgent.velocity);
            if ((movingForward && what < 0f) || (!movingForward && what > 0f))
            {
                //Debug.Log("Braking");
                m_AIAgent.vehicleRoot.SetBrake(1f);
            }
            else
            {
                //Debug.Log("Not Braking");
            }
        }

        void UpdateActuatorAutoFlip()
        {
            Debug.LogError("Auto Flip Is Not Implemented Yet!");
        }

        float FindAngleSign(Vector3 v1, Vector3 v2)
        {
            float angle = Vector3.Angle(v1, v2);
            Vector3 cross = Vector3.Cross(v1, v2);
            if (cross.y < 0)
            {
                angle = -angle;
            }
            return angle;
        }
    }
}
