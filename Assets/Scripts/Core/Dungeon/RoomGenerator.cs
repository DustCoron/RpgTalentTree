using UnityEngine;
using UnityEngine.ProBuilder;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Handles generation of individual room meshes using ProBuilder API
    /// </summary>
    public class RoomGenerator
    {
        private Material floorMaterial;
        private WallGenerator wallGenerator;

        public RoomGenerator(Material floorMaterial, Material wallMaterial, float wallHeight, float wallThickness)
        {
            this.floorMaterial = floorMaterial;
            this.wallGenerator = new WallGenerator(wallMaterial, wallHeight, wallThickness);
        }

        /// <summary>
        /// Create a complete room with floor and walls
        /// </summary>
        public GameObject CreateRoom(DungeonRoom room, Transform parent, int roomIndex)
        {
            GameObject roomObject = new GameObject($"Room_{roomIndex}");
            roomObject.transform.SetParent(parent);

            // Position room at its XZ coordinates with the specified floor height
            roomObject.transform.position = new Vector3(room.Position.x, room.FloorHeight, room.Position.z);

            room.RoomObject = roomObject;

            // Create floor
            CreateFloor(roomObject, room);

            // Create walls
            CreateWalls(roomObject, room);

            return roomObject;
        }

        /// <summary>
        /// Create floor mesh using ProBuilder
        /// </summary>
        private void CreateFloor(GameObject parent, DungeonRoom room)
        {
            GameObject floorObj = new GameObject("Floor");
            floorObj.transform.SetParent(parent.transform);
            floorObj.transform.localPosition = Vector3.zero;

            ProBuilderMesh pbMesh = floorObj.AddComponent<ProBuilderMesh>();

            // Create a plane shape
            pbMesh.CreateShapeFromPolygon(
                new Vector3[] {
                    new Vector3(0, 0, 0),
                    new Vector3(room.Size.x, 0, 0),
                    new Vector3(room.Size.x, 0, room.Size.z),
                    new Vector3(0, 0, room.Size.z)
                },
                0f, // extrusion height
                false // flip normals
            );

            // Apply material
            if (floorMaterial != null)
            {
                var renderer = floorObj.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = floorMaterial;
                }
            }

            pbMesh.ToMesh();
            pbMesh.Refresh();
        }

        /// <summary>
        /// Create walls around the room using WallGenerator
        /// </summary>
        private void CreateWalls(GameObject parent, DungeonRoom room)
        {
            wallGenerator.CreateWalls(parent, room);
        }
    }
}
