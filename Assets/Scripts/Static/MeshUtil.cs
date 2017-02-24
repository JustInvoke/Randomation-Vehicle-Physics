using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace RVP
{
    //Class with extra mesh functions
    public class MeshUtil
    {
        //Mesh tangent calculation by hemik1 using info from http://answers.unity3d.com/questions/7789/calculating-tangents-vector4.html
        public static void CalculateMeshTangents(Mesh mesh)
        {
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uv = mesh.uv;

            int triangleCount = mesh.triangles.Length;
            int vertexCount = mesh.vertices.Length;

            Vector3[] tan1 = new Vector3[vertexCount];
            Vector3[] tan2 = new Vector3[vertexCount];

            Vector4[] tangents = new Vector4[vertexCount];

            int i1;
            int i2;
            int i3;

            Vector3 v1;
            Vector3 v2;
            Vector3 v3;

            Vector2 w1;
            Vector2 w2;
            Vector2 w3;

            float x1;
            float x2;
            float y1;
            float y2;
            float z1;
            float z2;

            float s1;
            float s2;
            float t1;
            float t2;

            float div;
            float r;

            Vector3 sdir;
            Vector3 tdir;

            for (int a = 0; a < triangleCount; a += 3)
            {
                i1 = triangles[a];
                i2 = triangles[a + 1];
                i3 = triangles[a + 2];

                v1 = vertices[i1];
                v2 = vertices[i2];
                v3 = vertices[i3];

                w1 = uv[i1];
                w2 = uv[i2];
                w3 = uv[i3];

                x1 = v2.x - v1.x;
                x2 = v3.x - v1.x;
                y1 = v2.y - v1.y;
                y2 = v3.y - v1.y;
                z1 = v2.z - v1.z;
                z2 = v3.z - v1.z;

                s1 = w2.x - w1.x;
                s2 = w3.x - w1.x;
                t1 = w2.y - w1.y;
                t2 = w3.y - w1.y;

                div = s1 * t2 - s2 * t1;
                r = div == 0.0f ? 0.0f : 1.0f / div;

                sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;
            }

            Vector3 n = Vector3.zero;
            Vector3 t;

            for (int a = 0; a < vertexCount; ++a)
            {
                try
                {
                    n = normals[a];
                }
                catch// (Exception e) 
                {
                    Debug.LogError("OUT OF RANGE: index " + a + ", length " + normals.Length);
                }

                t = tan1[a];

                Vector3.OrthoNormalize(ref n, ref t);
                tangents[a].x = t.x;
                tangents[a].y = t.y;
                tangents[a].z = t.z;

                tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
            }

            mesh.tangents = tangents;
        }
    }
}