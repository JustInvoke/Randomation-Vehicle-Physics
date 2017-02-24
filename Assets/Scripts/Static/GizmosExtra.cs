using UnityEngine;
using System.Collections;

namespace RVP
{

    //Static class with extra gizmo drawing functions
    public static class GizmosExtra
    {
        //Draws a wire cylinder like DrawWireCube and DrawWireSphere
        //pos = position, dir = direction of the caps, radius = radius, height = height or length
        public static void DrawWireCylinder(Vector3 pos, Vector3 dir, float radius, float height)
        {
            float halfHeight = height * 0.5f;
            Quaternion quat = Quaternion.LookRotation(dir, new Vector3(-dir.y, dir.x, 0));

            Gizmos.DrawLine(pos + quat * new Vector3(radius, 0, halfHeight), pos + quat * new Vector3(radius, 0, -halfHeight));
            Gizmos.DrawLine(pos + quat * new Vector3(-radius, 0, halfHeight), pos + quat * new Vector3(-radius, 0, -halfHeight));
            Gizmos.DrawLine(pos + quat * new Vector3(0, radius, halfHeight), pos + quat * new Vector3(0, radius, -halfHeight));
            Gizmos.DrawLine(pos + quat * new Vector3(0, -radius, halfHeight), pos + quat * new Vector3(0, -radius, -halfHeight));

            Vector3 circle0Point0;
            Vector3 circle0Point1;
            Vector3 circle1Point0;
            Vector3 circle1Point1;

            for (float i = 0; i < 6.28f; i += 0.1f)
            {
                circle0Point0 = pos + quat * new Vector3(Mathf.Sin(i) * radius, Mathf.Cos(i) * radius, halfHeight);
                circle0Point1 = pos + quat * new Vector3(Mathf.Sin(i + 0.1f) * radius, Mathf.Cos(i + 0.1f) * radius, halfHeight);
                Gizmos.DrawLine(circle0Point0, circle0Point1);

                circle1Point0 = pos + quat * new Vector3(Mathf.Sin(i) * radius, Mathf.Cos(i) * radius, -halfHeight);
                circle1Point1 = pos + quat * new Vector3(Mathf.Sin(i + 0.1f) * radius, Mathf.Cos(i + 0.1f) * radius, -halfHeight);
                Gizmos.DrawLine(circle1Point0, circle1Point1);
            }
        }
    }
}