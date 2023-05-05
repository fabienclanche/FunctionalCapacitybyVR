using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RoomCulling
{
    public class RoomCullingNode : MonoBehaviour
    {
        internal bool visible;

        public RoomCullingNode[] neighbors;
        public BoxCollider boxCollider;
        public bool isExterior = false;
        public bool destroyOnLoad = false;

        public Bounds bounds => boxCollider.bounds;
 
        public void SetVisibleRecursive(int depth = 1)
        {
            this.visible = true;
            if (depth > 0) foreach (var n in neighbors) n.SetVisibleRecursive(depth - 1);
        }

        public void OnValidate()
        {
            if (boxCollider != null) return;

            bool init = false;
            Bounds bounds = new Bounds();

            foreach (var renderer in this.GetComponentsInChildren<Renderer>())
            {
                var cbounds = renderer.bounds;

                if (!init)
                {
                    init = true;
                    bounds = cbounds;
                }
                else
                {
                    bounds.Encapsulate(cbounds);
                }
            }

            this.boxCollider = this.gameObject.AddComponent<BoxCollider>();
            this.boxCollider.center = bounds.center;
            this.boxCollider.size = bounds.extents * 2;
            this.boxCollider.isTrigger = true; 
        }

        internal void CommitVisibilityRecursive(int depth)
        {
            if (visible != this.gameObject.activeInHierarchy) this.gameObject.SetActive(visible);
            visible = false;

            if (depth > 0) foreach (var n in neighbors) n.CommitVisibilityRecursive(depth - 1);
        }

        public void OnSceneGUI()
        {
            Gizmos.DrawCube(bounds.center, bounds.extents);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(RoomCullingNode))]
    public class DrawLineEditor : Editor
    {
        void OnSceneGUI()
        {
            // get the chosen game object
            RoomCullingNode node = target as RoomCullingNode;

            if (node == null) return;

            Handles.DrawWireCube(node.bounds.center, node.bounds.extents * 2);
        }
    }
#endif
}
