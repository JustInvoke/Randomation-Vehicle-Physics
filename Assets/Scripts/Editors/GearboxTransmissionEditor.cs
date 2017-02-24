#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace RVP
{
    [CustomEditor(typeof(GearboxTransmission))]
    [CanEditMultipleObjects]

    public class GearboxTransmissionEditor : Editor
    {
        bool isPrefab = false;
        static bool showButtons = true;

        public override void OnInspectorGUI()
        {
            GUIStyle boldFoldout = new GUIStyle(EditorStyles.foldout);
            boldFoldout.fontStyle = FontStyle.Bold;
            GearboxTransmission targetScript = (GearboxTransmission)target;
            GearboxTransmission[] allTargets = new GearboxTransmission[targets.Length];
            isPrefab = PrefabUtility.GetPrefabType(targetScript) == PrefabType.Prefab;

            for (int i = 0; i < targets.Length; i++)
            {
                Undo.RecordObject(targets[i], "Transmission Change");
                allTargets[i] = targets[i] as GearboxTransmission;
            }

            DrawDefaultInspector();

            if (!isPrefab && targetScript.gameObject.activeInHierarchy)
            {
                showButtons = EditorGUILayout.Foldout(showButtons, "Quick Actions", boldFoldout);
                EditorGUI.indentLevel++;
                if (showButtons)
                {
                    if (GUILayout.Button("Calculate RPM Ranges"))
                    {
                        foreach (GearboxTransmission curTarget in allTargets)
                        {
                            curTarget.CalculateRpmRanges();
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(targetScript);
            }
        }
    }
}
#endif