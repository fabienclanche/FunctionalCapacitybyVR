using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ExtractMeshes
{
    [MenuItem("GameObject/Extract Meshes", false, 0)]
    public static void Extract()
    {
        var root = Selection.activeObject as GameObject;
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            GameObject prefabRoot = null;
            while (prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(renderer.gameObject))
            {
                PrefabUtility.UnpackPrefabInstance(prefabRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            renderer.transform.parent = root.transform;
            
            if (renderer.GetComponent<Collider>() == null)
            {
                var collider = renderer.gameObject.AddComponent<MeshCollider>();
                collider.convex = !renderer.gameObject.isStatic;
            }
        }

        RemoveChildrenWithoutMeshes(root.transform);
    }

    public static void RemoveChildrenWithoutMeshes(Transform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);

            if (child.GetComponent<MeshRenderer>())
            {
                RemoveChildrenWithoutMeshes(child);
            }
            else
            {
                GameObject.DestroyImmediate(child.gameObject);
                i--;
            }
        }
    }

    [MenuItem("GameObject/Extract Meshes", true)]
    private static bool ExtractValidation()
    {
        return Selection.activeObject?.GetType() == typeof(GameObject);
    }
}
