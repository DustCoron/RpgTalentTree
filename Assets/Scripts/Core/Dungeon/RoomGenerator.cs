using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Handles generation of individual room meshes using ProBuilder API
    /// </summary>
    public class RoomGenerator
    {
        private Material floorMaterial;
        private Material wallMaterial;
        private float wallHeight;
        private float wallThickness;

        public RoomGenerator(Material floorMaterial, Material wallMaterial, float wallHeight, float wallThickness)
        {
            this.floorMaterial = floorMaterial;
            this.wallMaterial = wallMaterial;
            this.wallHeight = wallHeight;
            this.wallThickness = wallThickness;
        }

        /// <summary>
        /// Create a complete room with floor and walls
        /// </summary>
        public GameObject CreateRoom(DungeonRoom room, Transform parent, int roomIndex)
        {
            GameObject roomObject = new GameObject($"Room_{roomIndex}");
            roomObject.transform.SetParent(parent);
            roomObject.transform.position = room.Position;

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
        /// Create walls around the room using ProBuilder
        /// </summary>
        private void CreateWalls(GameObject parent, DungeonRoom room)
        {
            // Create four walls
            CreateWall(parent, "Wall_North",
                new Vector3(0, 0, room.Size.z),
                new Vector3(room.Size.x, 0, room.Size.z + wallThickness),
                wallHeight);

            CreateWall(parent, "Wall_South",
                new Vector3(0, 0, -wallThickness),
                new Vector3(room.Size.x, 0, 0),
                wallHeight);

            CreateWall(parent, "Wall_East",
                new Vector3(room.Size.x, 0, 0),
                new Vector3(room.Size.x + wallThickness, 0, room.Size.z),
                wallHeight);

            CreateWall(parent, "Wall_West",
                new Vector3(-wallThickness, 0, 0),
                new Vector3(0, 0, room.Size.z),
                wallHeight);
        }

        /// <summary>
        /// Create a single wall segment using ProBuilder
        /// </summary>
        private void CreateWall(GameObject parent, string name, Vector3 corner1, Vector3 corner2, float height)
        {
            GameObject wallObj = new GameObject(name);
            wallObj.transform.SetParent(parent.transform);
            wallObj.transform.localPosition = Vector3.zero;

            ProBuilderMesh pbMesh = wallObj.AddComponent<ProBuilderMesh>();

            // Create extruded wall from polygon
            pbMesh.CreateShapeFromPolygon(
                new Vector3[] {
                    corner1,
                    new Vector3(corner2.x, corner1.y, corner1.z),
                    corner2,
                    new Vector3(corner1.x, corner2.y, corner2.z)
                },
                height,
                false
            );

            // Apply material
            if (wallMaterial != null)
            {
                var renderer = wallObj.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = wallMaterial;
                }
            }

            pbMesh.ToMesh();
            pbMesh.Refresh();
        }
    }
}
