#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace RVP
{
    [CustomEditor(typeof(Wheel))]
    [CanEditMultipleObjects]

    public class WheelEditor : Editor
    {
        bool isPrefab = false;
        static bool showButtons = true;
        static float radiusMargin = 0;
        static float widthMargin = 0;

        public override void OnInspectorGUI()
        {
            GUIStyle boldFoldout = new GUIStyle(EditorStyles.foldout);
            boldFoldout.fontStyle = FontStyle.Bold;
            Wheel targetScript = (Wheel)target;
            Wheel[] allTargets = new Wheel[targets.Length];
            isPrefab = PrefabUtility.GetPrefabType(targetScript) == PrefabType.Prefab;

            for (int i = 0; i < targets.Length; i++)
            {
                Undo.RecordObject(targets[i], "Wheel Change");
                allTargets[i] = targets[i] as Wheel;
            }

            DrawDefaultInspector();

            if (!isPrefab && targetScript.gameObject.activeInHierarchy)
            {
                showButtons = EditorGUILayout.Foldout(showButtons, "Quick Actions", boldFoldout);
                EditorGUI.indentLevel++;
                if (showButtons)
                {
                    if (GUILayout.Button("Get Wheel Dimensions"))
                    {
                        foreach (Wheel curTarget in allTargets)
                        {
                            curTarget.GetWheelDimensions(radiusMargin, widthMargin);
                        }
                    }

                    EditorGUI.indentLevel++;
                    radiusMargin = EditorGUILayout.FloatField("Radius Margin", radiusMargin);
                    widthMargin = EditorGUILayout.FloatField("Width Margin", widthMargin);
                    EditorGUI.indentLevel--;
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