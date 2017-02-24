#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace RVP
{
    [CustomEditor(typeof(GroundSurfaceInstance))]
    [CanEditMultipleObjects]

    public class GroundSurfaceInstanceEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GroundSurfaceMaster surfaceMaster = FindObjectOfType<GroundSurfaceMaster>();
            GroundSurfaceInstance targetScript = (GroundSurfaceInstance)target;
            GroundSurfaceInstance[] allTargets = new GroundSurfaceInstance[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                Undo.RecordObject(targets[i], "Ground Surface Change");
                allTargets[i] = targets[i] as GroundSurfaceInstance;
            }

            string[] surfaceNames = new string[surfaceMaster.surfaceTypes.Length];

            for (int i = 0; i < surfaceNames.Length; i++)
            {
                surfaceNames[i] = surfaceMaster.surfaceTypes[i].name;
            }

            foreach (GroundSurfaceInstance curTarget in allTargets)
            {
                curTarget.surfaceType = EditorGUILayout.Popup("Surface Type", curTarget.surfaceType, surfaceNames);
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(targetScript);
            }
        }
    }
}
#endif