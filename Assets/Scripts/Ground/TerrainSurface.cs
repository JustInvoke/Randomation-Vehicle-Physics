using UnityEngine;
using System.Collections;

namespace RVP
{
    [RequireComponent(typeof(Terrain))]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Ground Surface/Terrain Surface", 2)]

    //Class for associating terrain textures with ground surface types
    public class TerrainSurface : MonoBehaviour
    {
        Transform tr;
        TerrainData terDat;
        float[,,] terrainAlphamap;
        public int[] surfaceTypes = new int[0];
        [System.NonSerialized]
        public float[] frictions;

        void Start()
        {
            tr = transform;
            if (GetComponent<Terrain>().terrainData)
            {
                terDat = GetComponent<Terrain>().terrainData;

                //Set frictions for each surface type
                if (Application.isPlaying)
                {
                    UpdateAlphamaps();
                    frictions = new float[surfaceTypes.Length];

                    for (int i = 0; i < frictions.Length; i++)
                    {
                        if (GroundSurfaceMaster.surfaceTypesStatic[surfaceTypes[i]].useColliderFriction)
                        {
                            frictions[i] = GetComponent<Collider>().material.dynamicFriction * 2;
                        }
                        else
                        {
                            frictions[i] = GroundSurfaceMaster.surfaceTypesStatic[surfaceTypes[i]].friction;
                        }
                    }
                }
            }
        }

        void Update()
        {
            if (!Application.isPlaying)
            {
                if (terDat)
                {
                    if (surfaceTypes.Length != terDat.alphamapLayers)
                    {
                        ChangeSurfaceTypesLength();
                    }
                }
            }
        }

        public void UpdateAlphamaps()
        {
            terrainAlphamap = terDat.GetAlphamaps(0, 0, terDat.alphamapWidth, terDat.alphamapHeight);
        }

        void ChangeSurfaceTypesLength()
        {
            int[] tempVals = surfaceTypes;

            surfaceTypes = new int[terDat.alphamapLayers];

            for (int i = 0; i < surfaceTypes.Length; i++)
            {
                if (i >= tempVals.Length)
                {
                    break;
                }
                else
                {
                    surfaceTypes[i] = tempVals[i];
                }
            }
        }

        //Returns index of dominant surface type at point on terrain, relative to surface types array in GroundSurfaceMaster
        public int GetDominantSurfaceTypeAtPoint(Vector3 pos)
        {
            Vector2 coord = new Vector2(Mathf.Clamp01((pos.z - tr.position.z) / terDat.size.z), Mathf.Clamp01((pos.x - tr.position.x) / terDat.size.x));

            float maxVal = 0;
            int maxIndex = 0;
            float curVal = 0;

            for (int i = 0; i < terrainAlphamap.GetLength(2); i++)
            {
                curVal = terrainAlphamap[Mathf.FloorToInt(coord.x * (terDat.alphamapWidth - 1)), Mathf.FloorToInt(coord.y * (terDat.alphamapHeight - 1)), i];

                if (curVal > maxVal)
                {
                    maxVal = curVal;
                    maxIndex = i;
                }
            }

            return surfaceTypes[maxIndex];
        }

        //Gets the friction of the indicated surface type
        public float GetFriction(int sType)
        {
            float returnedFriction = 1;

            for (int i = 0; i < surfaceTypes.Length; i++)
            {
                if (sType == surfaceTypes[i])
                {
                    returnedFriction = frictions[i];
                    break;
                }
            }

            return returnedFriction;
        }
    }
}