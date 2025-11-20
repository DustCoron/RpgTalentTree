using UnityEngine;
using UnityEngine.ProBuilder;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Handles generation of corridors with walls and ceilings using ProBuilder API
    /// </summary>
    public class CorridorGenerator
    {
        private Material floorMaterial;
        private Material wallMaterial;
        private Material ceilingMaterial;
        private float wallHeight;
        private int corridorWidth;

        public CorridorGenerator(Material floorMaterial, Material wallMaterial, Material ceilingMaterial, float wallHeight, int corridorWidth)
        {
            this.floorMaterial = floorMaterial;
            this.wallMaterial = wallMaterial;
            this.ceilingMaterial = ceilingMaterial ?? floorMaterial;
            this.wallHeight = wallHeight;
            this.corridorWidth = corridorWidth;
        }

        /// <summary>
        /// Create a complete corridor segment with floor, walls, and ceiling
        /// </summary>
        public GameObject CreateCorridor(Vector3 start, Vector3 end, Transform parent, int corridorIndex, string direction)
        {
            if (Vector3.Distance(start, end) < 0.1f)
                return null;

            GameObject corridorObj = new GameObject($"Corridor_{corridorIndex}_{direction}");
            corridorObj.transform.SetParent(parent);

            Vector3 dir = (end - start).normalized;
            float distance = Vector3.Distance(start, end);

            // Determine corridor orientation
            bool isHorizontalX = Mathf.Abs(dir.x) > Mathf.Abs(dir.z);

            // Create floor
            CreateCorridorFloor(corridorObj, start, distance, isHorizontalX);

            // Create walls
            CreateCorridorWalls(corridorObj, start, distance, isHorizontalX);

            // Create ceiling
            CreateCorridorCeiling(corridorObj, start, distance, isHorizontalX);

            return corridorObj;
        }

        /// <summary>
        /// Create corridor floor
        /// </summary>
        private void CreateCorridorFloor(GameObject parent, Vector3 start, float distance, bool isHorizontalX)
        {
            GameObject floorObj = new GameObject("Floor");
            floorObj.transform.SetParent(parent.transform);
            floorObj.transform.position = start;

            Vector3[] vertices;
            float halfWidth = corridorWidth / 2f;

            if (isHorizontalX)
            {
                // Horizontal corridor (along X axis)
                vertices = new Vector3[] {
                    new Vector3(0, 0, -halfWidth),
                    new Vector3(distance, 0, -halfWidth),
                    new Vector3(distance, 0, halfWidth),
                    new Vector3(0, 0, halfWidth)
                };
            }
            else
            {
                // Vertical corridor (along Z axis)
                vertices = new Vector3[] {
                    new Vector3(-halfWidth, 0, 0),
                    new Vector3(halfWidth, 0, 0),
                    new Vector3(halfWidth, 0, distance),
                    new Vector3(-halfWidth, 0, distance)
                };
            }

            // Create face (two triangles forming a quad)
            Face face = new Face(new int[] { 0, 1, 2, 0, 2, 3 });

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
        /// Create corridor walls
        /// </summary>
        private void CreateCorridorWalls(GameObject parent, Vector3 start, float distance, bool isHorizontalX)
        {
            float halfWidth = corridorWidth / 2f;

            if (isHorizontalX)
            {
                // Left wall (negative Z)
                CreateCorridorWall(parent, "Wall_Left",
                    new Vector3(0, 0, -halfWidth),
                    new Vector3(distance, 0, -halfWidth),
                    start);

                // Right wall (positive Z)
                CreateCorridorWall(parent, "Wall_Right",
                    new Vector3(0, 0, halfWidth),
                    new Vector3(distance, 0, halfWidth),
                    start);
            }
            else
            {
                // Left wall (negative X)
                CreateCorridorWall(parent, "Wall_Left",
                    new Vector3(-halfWidth, 0, 0),
                    new Vector3(-halfWidth, 0, distance),
                    start);

                // Right wall (positive X)
                CreateCorridorWall(parent, "Wall_Right",
                    new Vector3(halfWidth, 0, 0),
                    new Vector3(halfWidth, 0, distance),
                    start);
            }
        }

        /// <summary>
        /// Create a single corridor wall
        /// </summary>
        private void CreateCorridorWall(GameObject parent, string name, Vector3 localStart, Vector3 localEnd, Vector3 worldStart)
        {
            GameObject wallObj = new GameObject(name);
            wallObj.transform.SetParent(parent.transform);
            wallObj.transform.position = worldStart;

            // Create wall polygon with 4 corners (a thin rectangle)
            float wallThickness = 0.1f;
            Vector3 direction = (localEnd - localStart).normalized;
            Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x) * wallThickness;

            // Define base rectangle vertices
            Vector3 v0 = localStart;
            Vector3 v1 = localStart + perpendicular;
            Vector3 v2 = localEnd + perpendicular;
            Vector3 v3 = localEnd;

            // Create all 8 vertices (4 bottom + 4 top)
            Vector3[] vertices = new Vector3[]
            {
                // Bottom vertices (0-3)
                v0,
                v1,
                v2,
                v3,
                // Top vertices (4-7)
                v0 + Vector3.up * wallHeight,
                v1 + Vector3.up * wallHeight,
                v2 + Vector3.up * wallHeight,
                v3 + Vector3.up * wallHeight
            };

            // Define faces (6 faces, each as 2 triangles)
            Face[] faces = new Face[]
            {
                // Bottom face (facing down)
                new Face(new int[] { 0, 2, 1, 0, 3, 2 }),
                // Top face (facing up)
                new Face(new int[] { 4, 5, 6, 4, 6, 7 }),
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
            pbMesh.gameObject.transform.SetParent(wallObj.transform);
            pbMesh.gameObject.transform.localPosition = Vector3.zero;
            pbMesh.gameObject.name = name + "_Mesh";

            // Apply material
            if (wallMaterial != null)
            {
                var renderer = pbMesh.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = wallMaterial;
                }
            }

            pbMesh.ToMesh();
            pbMesh.Refresh();
        }

        /// <summary>
        /// Create corridor ceiling
        /// </summary>
        private void CreateCorridorCeiling(GameObject parent, Vector3 start, float distance, bool isHorizontalX)
        {
            GameObject ceilingObj = new GameObject("Ceiling");
            ceilingObj.transform.SetParent(parent.transform);
            ceilingObj.transform.position = start + Vector3.up * wallHeight;

            Vector3[] vertices;
            float halfWidth = corridorWidth / 2f;

            if (isHorizontalX)
            {
                // Horizontal corridor ceiling
                vertices = new Vector3[] {
                    new Vector3(0, 0, halfWidth),
                    new Vector3(distance, 0, halfWidth),
                    new Vector3(distance, 0, -halfWidth),
                    new Vector3(0, 0, -halfWidth)
                };
            }
            else
            {
                // Vertical corridor ceiling
                vertices = new Vector3[] {
                    new Vector3(halfWidth, 0, 0),
                    new Vector3(halfWidth, 0, distance),
                    new Vector3(-halfWidth, 0, distance),
                    new Vector3(-halfWidth, 0, 0)
                };
            }

            // Create face with reversed winding for ceiling (faces down)
            Face face = new Face(new int[] { 0, 2, 1, 0, 3, 2 });

            ProBuilderMesh pbMesh = ProBuilderMesh.Create(vertices, new Face[] { face });
            pbMesh.gameObject.transform.SetParent(ceilingObj.transform);
            pbMesh.gameObject.transform.localPosition = Vector3.zero;
            pbMesh.gameObject.name = "CeilingMesh";

            // Apply material
            if (ceilingMaterial != null)
            {
                var renderer = pbMesh.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = ceilingMaterial;
                }
            }

            pbMesh.ToMesh();
            pbMesh.Refresh();
        }
    }
}
