using UnityEngine;
using System.Collections;

namespace RVP
{
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/AI/Vehicle Waypoint", 1)]

    //Class for vehicle waypoints
    public class VehicleWaypoint : MonoBehaviour
    {
        public VehicleWaypoint nextPoint;
        public float radius = 10;

        [Tooltip("Percentage of a vehicle's max speed to drive at")]
        [Range(0, 1)]
        public float speed = 1;

        void OnDrawGizmos()
        {
            //Visualize waypoint
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);

            //Draw line to next point
            if (nextPoint)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, nextPoint.transform.position);
            }
        }
    }
}