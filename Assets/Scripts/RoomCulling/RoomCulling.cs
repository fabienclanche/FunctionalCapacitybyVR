using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoomCulling
{
    public class RoomCulling : MonoBehaviour
    {
        public Camera mainCamera;
        public RoomCullingNode[] rooms;

        public void OnValidate()
        {
            if (rooms.Length == 0) rooms = this.gameObject.GetComponentsInChildren<RoomCullingNode>();
        }

        private void Start()
        {
            foreach(var room in rooms)
            {
                if(room.destroyOnLoad)
                {
                    for (int i = 0; i < room.transform.childCount; i++) Destroy(room.transform.GetChild(i).gameObject);
                }
            }
        }

        void Update()
        {
            var cameraPos = mainCamera.transform.position;

            int insideRoomsVisible = 0;

            foreach (var room in rooms) if (!(room.isExterior) && room.bounds.Contains(cameraPos))
                {
                    insideRoomsVisible++;
                    room.SetVisibleRecursive(1);
                }

            foreach (var room in rooms)
            {
                if (room.isExterior && insideRoomsVisible == 0 && room.bounds.Contains(cameraPos))
                {
                    room.SetVisibleRecursive(1);
                }
            }

            foreach(var room in rooms) room.CommitVisibilityRecursive(0);
        }
    }
}
