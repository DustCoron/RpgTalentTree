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

            ProBuilderMesh pbMesh = floorObj.AddComponent<ProBuilderMesh>();

            Vector3[] polygonPoints;
            float halfWidth = corridorWidth / 2f;

            if (isHorizontalX)
            {
                // Horizontal corridor (along X axis)
                polygonPoints = new Vector3[] {
                    new Vector3(0, 0, -halfWidth),
                    new Vector3(distance, 0, -halfWidth),
                    new Vector3(distance, 0, halfWidth),
                    new Vector3(0, 0, halfWidth)
                };
            }
            else
            {
                // Vertical corridor (along Z axis)
                polygonPoints = new Vector3[] {
                    new Vector3(-halfWidth, 0, 0),
                    new Vector3(halfWidth, 0, 0),
                    new Vector3(halfWidth, 0, distance),
                    new Vector3(-halfWidth, 0, distance)
                };
            }

            pbMesh.CreateShapeFromPolygon(polygonPoints, 0f, false);

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

            ProBuilderMesh pbMesh = wallObj.AddComponent<ProBuilderMesh>();

            // Create wall polygon
            pbMesh.CreateShapeFromPolygon(
                new Vector3[] { localStart, localEnd },
                wallHeight,
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

        /// <summary>
        /// Create corridor ceiling
        /// </summary>
        private void CreateCorridorCeiling(GameObject parent, Vector3 start, float distance, bool isHorizontalX)
        {
            GameObject ceilingObj = new GameObject("Ceiling");
            ceilingObj.transform.SetParent(parent.transform);
            ceilingObj.transform.position = start + Vector3.up * wallHeight;

            ProBuilderMesh pbMesh = ceilingObj.AddComponent<ProBuilderMesh>();

            Vector3[] polygonPoints;
            float halfWidth = corridorWidth / 2f;

            if (isHorizontalX)
            {
                // Horizontal corridor ceiling
                polygonPoints = new Vector3[] {
                    new Vector3(0, 0, halfWidth),
                    new Vector3(distance, 0, halfWidth),
                    new Vector3(distance, 0, -halfWidth),
                    new Vector3(0, 0, -halfWidth)
                };
            }
            else
            {
                // Vertical corridor ceiling
                polygonPoints = new Vector3[] {
                    new Vector3(halfWidth, 0, 0),
                    new Vector3(halfWidth, 0, distance),
                    new Vector3(-halfWidth, 0, distance),
                    new Vector3(-halfWidth, 0, 0)
                };
            }

            // Ceiling faces downward, so flip normals
            pbMesh.CreateShapeFromPolygon(polygonPoints, 0f, true);

            // Apply material
            if (ceilingMaterial != null)
            {
                var renderer = ceilingObj.GetComponent<MeshRenderer>();
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
