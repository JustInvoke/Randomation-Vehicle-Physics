using UnityEngine;
using System.Collections;

namespace RVP
{

    // Static class with extra functions
    public static class F
    {
        // Returns the number with the greatest absolute value
        public static float MaxAbs(params float[] nums) {
            float result = 0;

            for (int i = 0; i < nums.Length; i++) {
                if (Mathf.Abs(nums[i]) > Mathf.Abs(result)) {
                    result = nums[i];
                }
            }

            return result;
        }

        // Returns the topmost parent of a Transform with a certain component
        public static T GetTopmostParentComponent<T>(this Transform tr) where T : Component {
            T getting = null;

            while (tr.parent != null) {
                if (tr.parent.GetComponent<T>() != null) {
                    getting = tr.parent.GetComponent<T>();
                }

                tr = tr.parent;
            }

            return getting;
        }

#if UNITY_EDITOR
        // Returns whether the given object is part of a prefab (meant to be used with selected objects in the inspector)
        public static bool IsPrefab(Object componentOrGameObject) {
            return UnityEditor.Selection.assetGUIDs.Length > 0
                && UnityEditor.PrefabUtility.GetPrefabAssetType(componentOrGameObject) != UnityEditor.PrefabAssetType.NotAPrefab
                && UnityEditor.PrefabUtility.GetPrefabAssetType(componentOrGameObject) != UnityEditor.PrefabAssetType.MissingAsset;
        }
#endif
    }
}
