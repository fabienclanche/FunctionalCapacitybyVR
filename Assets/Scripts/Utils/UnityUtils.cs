using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utils
{
    public static class UnityUtils
    {
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
        }
    }

#if UNITY_EDITOR
#endif
}