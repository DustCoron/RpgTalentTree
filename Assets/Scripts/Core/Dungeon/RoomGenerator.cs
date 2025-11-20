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
        private float wallHeight;

        public RoomGenerator(Material floorMaterial, Material wallMaterial, float wallHeight, float wallThickness)
        {
            this.floorMaterial = floorMaterial;
            this.wallHeight = wallHeight;
            this.wallGenerator = new WallGenerator(wallMaterial, wallHeight, wallThickness);
        }

        /// <summary>
        /// Create a complete room with floor, walls, and ceiling
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

            // Create ceiling
            CreateCeiling(roomObject, room);

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

            // Create floor vertices
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(room.Size.x, 0, 0),
                new Vector3(room.Size.x, 0, room.Size.z),
                new Vector3(0, 0, room.Size.z)
            };

            // Create face (counter-clockwise winding for upward-facing normals)
            Face face = new Face(new int[] { 0, 3, 2, 0, 2, 1 });

            ProBuilderMesh pbMesh = ProBuilderMesh.Create(vertices, new Face[] { face });
            pbMesh.gameObject.transform.SetParent(floorObj.transform);
            pbMesh.gameObject.transform.localPosition = Vector3.zero;
            pbMesh.gameObject.name = "FloorMesh";

            // Apply material
            if (floorMaterial != null)
            {
                var renderer = pbMesh.GetComponent<MeshRenderer>();
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

        /// <summary>
        /// Create ceiling mesh using ProBuilder
        /// </summary>
        private void CreateCeiling(GameObject parent, DungeonRoom room)
        {
            GameObject ceilingObj = new GameObject("Ceiling");
            ceilingObj.transform.SetParent(parent.transform);
            ceilingObj.transform.localPosition = new Vector3(0, wallHeight, 0);

            // Create ceiling vertices (inverted winding order for downward-facing normals)
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(room.Size.x, 0, 0),
                new Vector3(room.Size.x, 0, room.Size.z),
                new Vector3(0, 0, room.Size.z)
            };

            // Create face with reversed winding for ceiling (faces down)
            Face face = new Face(new int[] { 0, 2, 1, 0, 3, 2 });

            ProBuilderMesh pbMesh = ProBuilderMesh.Create(vertices, new Face[] { face });
            pbMesh.gameObject.transform.SetParent(ceilingObj.transform);
            pbMesh.gameObject.transform.localPosition = Vector3.zero;
            pbMesh.gameObject.name = "CeilingMesh";

            // Apply material
            if (floorMaterial != null)
            {
                var renderer = pbMesh.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = floorMaterial;
                }
            }

            pbMesh.ToMesh();
            pbMesh.Refresh();
        }
    }
}
