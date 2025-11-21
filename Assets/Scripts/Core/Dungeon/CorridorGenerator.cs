using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Handles generation of corridors with walls and ceilings using ProBuilder API
    /// Supports both straight and spline-based curved corridors
    /// </summary>
    public class CorridorGenerator
    {
        private Material floorMaterial;
        private Material wallMaterial;
        private Material ceilingMaterial;
        private float wallHeight;
        private float wallThickness;
        private int corridorWidth;

        public CorridorGenerator(Material floorMaterial, Material wallMaterial, Material ceilingMaterial, float wallHeight, float wallThickness, int corridorWidth)
        {
            this.floorMaterial = floorMaterial;
            this.wallMaterial = wallMaterial;
            this.ceilingMaterial = ceilingMaterial ?? floorMaterial;
            this.wallHeight = wallHeight;
            this.wallThickness = wallThickness;
            this.corridorWidth = corridorWidth;
        }

        /// <summary>
        /// Create a curved corridor using Bezier spline between two doorways
        /// </summary>
        public GameObject CreateSplineCorridor(Vector3 startPos, Vector3 startDir, Vector3 endPos, Vector3 endDir, Transform parent, int corridorIndex, int segments = 8)
        {
            if (Vector3.Distance(startPos, endPos) < 0.1f)
                return null;

            GameObject corridorObj = new GameObject($"SplineCorridor_{corridorIndex}");
            corridorObj.transform.SetParent(parent);
            corridorObj.transform.position = Vector3.zero;

            // Calculate control points for cubic Bezier
            float dist = Vector3.Distance(startPos, endPos);
            float controlDist = dist * 0.4f; // Control point distance

            Vector3 p0 = startPos;
            Vector3 p1 = startPos + startDir * controlDist;
            Vector3 p2 = endPos + endDir * controlDist;
            Vector3 p3 = endPos;

            // Generate spline points
            List<Vector3> splinePoints = new List<Vector3>();
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                splinePoints.Add(CubicBezier(p0, p1, p2, p3, t));
            }

            // Create corridor mesh along spline
            CreateSplineCorridorMesh(corridorObj, splinePoints);

            return corridorObj;
        }

        /// <summary>
        /// Cubic Bezier interpolation
        /// </summary>
        private Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            return uuu * p0 + 3 * uu * t * p1 + 3 * u * tt * p2 + ttt * p3;
        }

        /// <summary>
        /// Create corridor mesh along spline points
        /// </summary>
        private void CreateSplineCorridorMesh(GameObject parent, List<Vector3> splinePoints)
        {
            float halfWidth = corridorWidth / 2f;
            List<Vector3> vertices = new List<Vector3>();
            List<Face> faces = new List<Face>();

            // Generate cross-sections along spline
            List<Vector3> leftFloor = new List<Vector3>();
            List<Vector3> rightFloor = new List<Vector3>();
            List<Vector3> leftCeiling = new List<Vector3>();
            List<Vector3> rightCeiling = new List<Vector3>();

            for (int i = 0; i < splinePoints.Count; i++)
            {
                Vector3 pos = splinePoints[i];
                Vector3 forward;

                if (i < splinePoints.Count - 1)
                    forward = (splinePoints[i + 1] - pos).normalized;
                else
                    forward = (pos - splinePoints[i - 1]).normalized;

                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized * halfWidth;

                leftFloor.Add(pos - right);
                rightFloor.Add(pos + right);
                leftCeiling.Add(pos - right + Vector3.up * wallHeight);
                rightCeiling.Add(pos + right + Vector3.up * wallHeight);
            }

            // Build floor quads
            int vertOffset = 0;
            for (int i = 0; i < splinePoints.Count - 1; i++)
            {
                vertices.Add(leftFloor[i]);
                vertices.Add(rightFloor[i]);
                vertices.Add(rightFloor[i + 1]);
                vertices.Add(leftFloor[i + 1]);
                faces.Add(new Face(new int[] { vertOffset, vertOffset + 3, vertOffset + 2, vertOffset, vertOffset + 2, vertOffset + 1 }));
                vertOffset += 4;
            }

            // Build ceiling quads
            for (int i = 0; i < splinePoints.Count - 1; i++)
            {
                vertices.Add(leftCeiling[i]);
                vertices.Add(rightCeiling[i]);
                vertices.Add(rightCeiling[i + 1]);
                vertices.Add(leftCeiling[i + 1]);
                faces.Add(new Face(new int[] { vertOffset, vertOffset + 1, vertOffset + 2, vertOffset, vertOffset + 2, vertOffset + 3 }));
                vertOffset += 4;
            }

            // Build left wall quads
            for (int i = 0; i < splinePoints.Count - 1; i++)
            {
                vertices.Add(leftFloor[i]);
                vertices.Add(leftFloor[i + 1]);
                vertices.Add(leftCeiling[i + 1]);
                vertices.Add(leftCeiling[i]);
                faces.Add(new Face(new int[] { vertOffset, vertOffset + 1, vertOffset + 2, vertOffset, vertOffset + 2, vertOffset + 3 }));
                vertOffset += 4;
            }

            // Build right wall quads
            for (int i = 0; i < splinePoints.Count - 1; i++)
            {
                vertices.Add(rightFloor[i]);
                vertices.Add(rightFloor[i + 1]);
                vertices.Add(rightCeiling[i + 1]);
                vertices.Add(rightCeiling[i]);
                faces.Add(new Face(new int[] { vertOffset, vertOffset + 3, vertOffset + 2, vertOffset, vertOffset + 2, vertOffset + 1 }));
                vertOffset += 4;
            }

            // Create ProBuilder mesh
            ProBuilderMesh pbMesh = ProBuilderMesh.Create(vertices.ToArray(), faces.ToArray());
            pbMesh.gameObject.transform.SetParent(parent.transform);
            pbMesh.gameObject.transform.localPosition = Vector3.zero;
            pbMesh.gameObject.name = "SplineMesh";

            var renderer = pbMesh.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = floorMaterial;

            pbMesh.ToMesh();
            pbMesh.Refresh();
        }

        /// <summary>
        /// Create a complete corridor segment with floor, walls, and ceiling as a single mesh
        /// </summary>
        public GameObject CreateCorridor(Vector3 start, Vector3 end, Transform parent, int corridorIndex, string direction)
        {
            if (Vector3.Distance(start, end) < 0.1f)
                return null;

            GameObject corridorObj = new GameObject($"Corridor_{corridorIndex}_{direction}");
            corridorObj.transform.SetParent(parent);
            corridorObj.transform.position = start;

            Vector3 localEnd = end - start;
            float distance = localEnd.magnitude;
            Vector3 dir = localEnd.normalized;

            // Determine corridor orientation
            bool isHorizontalX = Mathf.Abs(dir.x) > Mathf.Abs(dir.z);

            // Create single unified mesh for entire corridor
            CreateUnifiedCorridorMesh(corridorObj, distance, isHorizontalX, dir);

            return corridorObj;
        }

        /// <summary>
        /// Create unified corridor mesh with floor, walls, and ceiling
        /// </summary>
        private void CreateUnifiedCorridorMesh(GameObject parent, float distance, bool isHorizontalX, Vector3 dir)
        {
            GameObject meshObj = new GameObject("CorridorMesh");
            meshObj.transform.SetParent(parent.transform);
            meshObj.transform.localPosition = Vector3.zero;

            float halfWidth = corridorWidth / 2f;
            List<Vector3> vertices = new List<Vector3>();
            List<Face> faces = new List<Face>();
            int vertexOffset = 0;

            // Build floor
            Vector3[] floorVerts = isHorizontalX
                ? new Vector3[] {
                    new Vector3(0, 0, -halfWidth),
                    new Vector3(distance, 0, -halfWidth),
                    new Vector3(distance, 0, halfWidth),
                    new Vector3(0, 0, halfWidth)
                }
                : new Vector3[] {
                    new Vector3(-halfWidth, 0, 0),
                    new Vector3(halfWidth, 0, 0),
                    new Vector3(halfWidth, 0, distance),
                    new Vector3(-halfWidth, 0, distance)
                };

            vertices.AddRange(floorVerts);
            faces.Add(new Face(new int[] { 0, 3, 2, 0, 2, 1 }));
            vertexOffset += 4;

            // Build ceiling
            Vector3[] ceilingVerts = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                ceilingVerts[i] = floorVerts[i] + Vector3.up * wallHeight;
            }
            vertices.AddRange(ceilingVerts);
            faces.Add(new Face(new int[] {
                vertexOffset + 0,
                vertexOffset + 2,
                vertexOffset + 1,
                vertexOffset + 0,
                vertexOffset + 3,
                vertexOffset + 2
            }));
            vertexOffset += 4;

            // Build walls (left and right)
            if (isHorizontalX)
            {
                // Left wall (negative Z)
                AddWallToMesh(vertices, faces, ref vertexOffset,
                    new Vector3(0, 0, -halfWidth),
                    new Vector3(distance, 0, -halfWidth),
                    new Vector3(-dir.z, 0, dir.x));

                // Right wall (positive Z)
                AddWallToMesh(vertices, faces, ref vertexOffset,
                    new Vector3(0, 0, halfWidth),
                    new Vector3(distance, 0, halfWidth),
                    new Vector3(-dir.z, 0, dir.x));
            }
            else
            {
                // Left wall (negative X)
                AddWallToMesh(vertices, faces, ref vertexOffset,
                    new Vector3(-halfWidth, 0, 0),
                    new Vector3(-halfWidth, 0, distance),
                    new Vector3(-dir.z, 0, dir.x));

                // Right wall (positive X)
                AddWallToMesh(vertices, faces, ref vertexOffset,
                    new Vector3(halfWidth, 0, 0),
                    new Vector3(halfWidth, 0, distance),
                    new Vector3(-dir.z, 0, dir.x));
            }

            // Create ProBuilder mesh
            ProBuilderMesh pbMesh = ProBuilderMesh.Create(vertices.ToArray(), faces.ToArray());
            pbMesh.gameObject.transform.SetParent(meshObj.transform);
            pbMesh.gameObject.transform.localPosition = Vector3.zero;
            pbMesh.gameObject.name = "UnifiedMesh";

            // Apply materials
            ApplyMaterialsToFaces(pbMesh);

            pbMesh.ToMesh();
            pbMesh.Refresh();
        }

        /// <summary>
        /// Add wall vertices and faces to the mesh
        /// </summary>
        private void AddWallToMesh(List<Vector3> vertices, List<Face> faces, ref int vertexOffset,
            Vector3 start, Vector3 end, Vector3 perpendicular)
        {
            Vector3 thickness = perpendicular.normalized * wallThickness;

            // Wall vertices (4 bottom + 4 top)
            vertices.Add(start);
            vertices.Add(start + thickness);
            vertices.Add(end + thickness);
            vertices.Add(end);
            vertices.Add(start + Vector3.up * wallHeight);
            vertices.Add(start + thickness + Vector3.up * wallHeight);
            vertices.Add(end + thickness + Vector3.up * wallHeight);
            vertices.Add(end + Vector3.up * wallHeight);

            // Wall faces
            int v = vertexOffset;
            faces.Add(new Face(new int[] { v+0, v+1, v+5, v+0, v+5, v+4 })); // Front
            faces.Add(new Face(new int[] { v+1, v+2, v+6, v+1, v+6, v+5 })); // Right
            faces.Add(new Face(new int[] { v+2, v+3, v+7, v+2, v+7, v+6 })); // Back
            faces.Add(new Face(new int[] { v+3, v+0, v+4, v+3, v+4, v+7 })); // Left

            vertexOffset += 8;
        }

        /// <summary>
        /// Apply materials to different parts of the corridor mesh
        /// </summary>
        private void ApplyMaterialsToFaces(ProBuilderMesh pbMesh)
        {
            var renderer = pbMesh.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                // For now, use single material for entire corridor
                // Can be enhanced later to use different materials per face
                renderer.sharedMaterial = floorMaterial;
            }
        }

        /// <summary>
        /// Create a corner piece to connect two perpendicular corridor segments as unified mesh
        /// </summary>
        public GameObject CreateCorridorCorner(Vector3 cornerPosition, Transform parent, int corridorIndex)
        {
            GameObject cornerObj = new GameObject($"CorridorCorner_{corridorIndex}");
            cornerObj.transform.SetParent(parent);
            cornerObj.transform.position = cornerPosition;

            float halfWidth = corridorWidth / 2f;
            List<Vector3> vertices = new List<Vector3>();
            List<Face> faces = new List<Face>();

            // Corner floor vertices
            Vector3[] floorVerts = new Vector3[] {
                new Vector3(-halfWidth, 0, -halfWidth),
                new Vector3(halfWidth, 0, -halfWidth),
                new Vector3(halfWidth, 0, halfWidth),
                new Vector3(-halfWidth, 0, halfWidth)
            };
            vertices.AddRange(floorVerts);
            faces.Add(new Face(new int[] { 0, 3, 2, 0, 2, 1 }));

            // Corner ceiling vertices
            Vector3[] ceilingVerts = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                ceilingVerts[i] = floorVerts[i] + Vector3.up * wallHeight;
            }
            vertices.AddRange(ceilingVerts);
            faces.Add(new Face(new int[] { 4, 6, 5, 4, 7, 6 }));

            // Create unified mesh
            GameObject meshObj = new GameObject("CornerMesh");
            meshObj.transform.SetParent(cornerObj.transform);
            meshObj.transform.localPosition = Vector3.zero;

            ProBuilderMesh pbMesh = ProBuilderMesh.Create(vertices.ToArray(), faces.ToArray());
            pbMesh.gameObject.transform.SetParent(meshObj.transform);
            pbMesh.gameObject.transform.localPosition = Vector3.zero;
            pbMesh.gameObject.name = "UnifiedCornerMesh";

            // Apply material
            var renderer = pbMesh.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = floorMaterial;
            }

            pbMesh.ToMesh();
            pbMesh.Refresh();

            return cornerObj;
        }
    }
}
