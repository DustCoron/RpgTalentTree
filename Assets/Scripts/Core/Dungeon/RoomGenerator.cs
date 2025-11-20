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

            // Create floor border details
            CreateFloorBorder(roomObject, room);

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
        /// Create decorative floor border
        /// </summary>
        private void CreateFloorBorder(GameObject parent, DungeonRoom room)
        {
            GameObject borderObj = new GameObject("FloorBorder");
            borderObj.transform.SetParent(parent.transform);
            borderObj.transform.localPosition = Vector3.zero;

            float borderWidth = 0.3f;
            float borderHeight = 0.05f; // Slightly raised border

            // Create four border strips
            // North border
            CreateBorderStrip(borderObj, "Border_North",
                new Vector3(0, borderHeight, room.Size.z - borderWidth),
                room.Size.x, borderWidth, borderHeight);

            // South border
            CreateBorderStrip(borderObj, "Border_South",
                new Vector3(0, borderHeight, 0),
                room.Size.x, borderWidth, borderHeight);

            // East border
            CreateBorderStrip(borderObj, "Border_East",
                new Vector3(room.Size.x - borderWidth, borderHeight, 0),
                borderWidth, room.Size.z, borderHeight);

            // West border
            CreateBorderStrip(borderObj, "Border_West",
                new Vector3(0, borderHeight, 0),
                borderWidth, room.Size.z, borderHeight);
        }

        /// <summary>
        /// Create a single border strip
        /// </summary>
        private void CreateBorderStrip(GameObject parent, string name, Vector3 position, float width, float depth, float height)
        {
            GameObject stripObj = new GameObject(name);
            stripObj.transform.SetParent(parent.transform);
            stripObj.transform.localPosition = position;

            // Create raised border vertices
            Vector3[] vertices = new Vector3[]
            {
                // Bottom vertices (0-3)
                new Vector3(0, -height, 0),
                new Vector3(width, -height, 0),
                new Vector3(width, -height, depth),
                new Vector3(0, -height, depth),
                // Top vertices (4-7)
                new Vector3(0, 0, 0),
                new Vector3(width, 0, 0),
                new Vector3(width, 0, depth),
                new Vector3(0, 0, depth)
            };

            // Define faces (6 faces, each as 2 triangles)
            Face[] faces = new Face[]
            {
                // Bottom face (facing down)
                new Face(new int[] { 0, 2, 1, 0, 3, 2 }),
                // Top face (facing up)
                new Face(new int[] { 4, 7, 6, 4, 6, 5 }),
                // Front face
                new Face(new int[] { 0, 1, 5, 0, 5, 4 }),
                // Right face
                new Face(new int[] { 1, 2, 6, 1, 6, 5 }),
                // Back face
                new Face(new int[] { 2, 3, 7, 2, 7, 6 }),
                // Left face
                new Face(new int[] { 3, 0, 4, 3, 4, 7 })
            };

            ProBuilderMesh pbMesh = ProBuilderMesh.Create(vertices, faces);
            pbMesh.gameObject.transform.SetParent(stripObj.transform);
            pbMesh.gameObject.transform.localPosition = Vector3.zero;
            pbMesh.gameObject.name = name + "_Mesh";

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
