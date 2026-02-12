using UnityEngine;
using UnityEngine.ProBuilder;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Generates room geometry (floor, ceiling, walls) using ProBuilder cubes
    /// for reliable mesh creation with correct normals on all faces
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
        /// Create a complete room with floor, walls, and ceiling
        /// </summary>
        public GameObject CreateRoom(DungeonRoom room, Transform parent, int roomIndex)
        {
            GameObject roomObj = new GameObject($"Room_{roomIndex}");
            roomObj.transform.SetParent(parent);
            roomObj.transform.position = new Vector3(room.Position.x, room.FloorHeight, room.Position.z);
            room.RoomObject = roomObj;

            float ft = 0.1f;
            float sx = room.Size.x;
            float sz = room.Size.z;

            // Floor slab (top surface at y=0, the walking surface)
            CreateCube(roomObj.transform, "Floor",
                new Vector3(sx / 2f, -ft / 2f, sz / 2f),
                new Vector3(sx, ft, sz),
                floorMaterial);

            // Ceiling slab (bottom surface at y=wallHeight)
            CreateCube(roomObj.transform, "Ceiling",
                new Vector3(sx / 2f, wallHeight + ft / 2f, sz / 2f),
                new Vector3(sx, ft, sz),
                floorMaterial);

            // Walls with doorway cuts and corner pillars
            var wallGen = new WallGenerator(wallMaterial, wallHeight, wallThickness);
            wallGen.CreateWalls(roomObj, room);

            return roomObj;
        }

        private void CreateCube(Transform parent, string name, Vector3 localPos, Vector3 size, Material material)
        {
            var mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            mesh.gameObject.name = name;
            mesh.gameObject.transform.SetParent(parent);
            mesh.gameObject.transform.localPosition = localPos;

            if (material != null)
            {
                var renderer = mesh.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.sharedMaterial = material;
            }

            mesh.ToMesh();
            mesh.Refresh();
        }
    }
}
