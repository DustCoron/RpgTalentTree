using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Represents a corridor path for intersection detection
    /// </summary>
    public class CorridorPath
    {
        public List<Vector3> Points;
        public int CorridorIndex;
        public GameObject CorridorObject;

        public CorridorPath(List<Vector3> points, int index, GameObject obj)
        {
            Points = points;
            CorridorIndex = index;
            CorridorObject = obj;
        }
    }

    /// <summary>
    /// Generates corridors using ProBuilder cubes for clean, reliable geometry.
    /// Each corridor segment is composed of individual floor, wall, and ceiling cubes.
    /// Supports L-shaped hard-corner corridors and Bezier spline corridors.
    /// </summary>
    public class CorridorGenerator
    {
        private Material floorMaterial;
        private Material wallMaterial;
        private Material ceilingMaterial;
        private float wallHeight;
        private float wallThickness;
        private int corridorWidth;

        private List<CorridorPath> corridorPaths = new List<CorridorPath>();
        private List<Vector3> junctionPoints = new List<Vector3>();
        private List<Bounds> roomBounds = new List<Bounds>();
        private float maxSegmentLength = 15f;
        private DungeonGrid grid;

        public void SetMaxSegmentLength(float length) => maxSegmentLength = length;
        public void SetGrid(DungeonGrid dungeonGrid) => grid = dungeonGrid;

        public CorridorGenerator(Material floorMaterial, Material wallMaterial, Material ceilingMaterial,
            float wallHeight, float wallThickness, int corridorWidth)
        {
            this.floorMaterial = floorMaterial;
            this.wallMaterial = wallMaterial;
            this.ceilingMaterial = ceilingMaterial ?? floorMaterial;
            this.wallHeight = wallHeight;
            this.wallThickness = wallThickness;
            this.corridorWidth = corridorWidth;
        }

        public void ClearPaths()
        {
            corridorPaths.Clear();
            junctionPoints.Clear();
            roomBounds.Clear();
            grid = null;
        }

        public List<Vector3> GetJunctionPoints() => junctionPoints;
        public List<CorridorPath> GetCorridorPaths() => corridorPaths;

        public void RegisterRoomBounds(Bounds bounds)
        {
            bounds.Expand(corridorWidth * 0.5f);
            roomBounds.Add(bounds);
        }

        // ===================== Room Avoidance =====================

        private bool IsInsideAnyRoom(Vector3 point)
        {
            foreach (var bounds in roomBounds)
            {
                if (bounds.Contains(point)) return true;
            }
            return false;
        }

        private bool SegmentIntersectsRoom(Vector3 start, Vector3 end)
        {
            int steps = Mathf.CeilToInt(Vector3.Distance(start, end) / (corridorWidth * 0.5f));
            for (int i = 1; i < steps; i++)
            {
                Vector3 point = Vector3.Lerp(start, end, i / (float)steps);
                if (IsInsideAnyRoom(point)) return true;
            }
            return false;
        }

        private Vector3 FindSafeCorner(Vector3 start, Vector3 end, Vector3 startDir, Vector3 endDir)
        {
            Vector3 corner = CalculateCornerPoint(start, startDir, end, endDir);
            if (!IsInsideAnyRoom(corner)) return corner;

            Vector3[] alternatives =
            {
                new Vector3(start.x, start.y, end.z),
                new Vector3(end.x, start.y, start.z),
                new Vector3((start.x + end.x) / 2f, start.y, start.z),
                new Vector3(start.x, start.y, (start.z + end.z) / 2f),
            };

            foreach (var alt in alternatives)
            {
                if (!IsInsideAnyRoom(alt)) return alt;
            }
            return corner;
        }

        private Vector3 FindAlternativeCorner(Vector3 start, Vector3 end, Vector3 startDir, Vector3 endDir)
        {
            Vector3 dir = (end - start).normalized;
            Vector3 perp = Vector3.Cross(dir, Vector3.up).normalized;

            for (float offset = corridorWidth * 2; offset <= corridorWidth * 10; offset += corridorWidth * 2)
            {
                Vector3 midPoint = (start + end) / 2f;

                Vector3 corner1 = midPoint + perp * offset;
                if (!IsInsideAnyRoom(corner1) &&
                    !SegmentIntersectsRoom(start, corner1) &&
                    !SegmentIntersectsRoom(corner1, end))
                    return corner1;

                Vector3 corner2 = midPoint - perp * offset;
                if (!IsInsideAnyRoom(corner2) &&
                    !SegmentIntersectsRoom(start, corner2) &&
                    !SegmentIntersectsRoom(corner2, end))
                    return corner2;
            }
            return CalculateCornerPoint(start, startDir, end, endDir);
        }

        private Vector3 PushPointOutOfRooms(Vector3 point)
        {
            Vector3[] directions = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            foreach (var dir in directions)
            {
                for (float dist = corridorWidth; dist <= corridorWidth * 5; dist += corridorWidth)
                {
                    Vector3 test = point + dir * dist;
                    if (!IsInsideAnyRoom(test)) return test;
                }
            }
            return point;
        }

        private List<Vector3> StrictRoomAvoidance(List<Vector3> pathPoints)
        {
            if (pathPoints.Count < 3) return pathPoints;

            List<Vector3> result = new List<Vector3> { pathPoints[0] };
            for (int i = 1; i < pathPoints.Count - 1; i++)
            {
                Vector3 point = pathPoints[i];
                if (IsInsideAnyRoom(point))
                {
                    Vector3 pushed = PushPointOutOfRooms(point);
                    if (!IsInsideAnyRoom(pushed)) result.Add(pushed);
                }
                else
                {
                    result.Add(point);
                }
            }
            result.Add(pathPoints[pathPoints.Count - 1]);
            return result;
        }

        // ===================== Path Calculation =====================

        private Vector3 CalculateCornerPoint(Vector3 start, Vector3 startDir, Vector3 end, Vector3 endDir)
        {
            bool startIsHorizontal = Mathf.Abs(startDir.x) > Mathf.Abs(startDir.z);
            bool endIsHorizontal = Mathf.Abs(endDir.x) > Mathf.Abs(endDir.z);

            if (startIsHorizontal != endIsHorizontal)
            {
                // Perpendicular directions - clean L-shape
                if (startIsHorizontal)
                    return new Vector3(end.x, start.y, start.z);
                else
                    return new Vector3(start.x, start.y, end.z);
            }
            else
            {
                // Same axis - midpoint for U-shape
                return new Vector3(
                    (start.x + end.x) / 2f,
                    start.y,
                    (start.z + end.z) / 2f);
            }
        }

        private List<Vector3> CalculateRoomAvoidingPath(Vector3 start, Vector3 startDir, Vector3 end, Vector3 endDir)
        {
            List<Vector3> waypoints = new List<Vector3>();
            waypoints.Add(start);

            Vector3 startClearance = start + startDir * (corridorWidth + 1);
            Vector3 endClearance = end + endDir * (corridorWidth + 1);

            waypoints.Add(startClearance);

            Vector3 corner = FindSafeCorner(startClearance, endClearance, startDir, endDir);
            if (SegmentIntersectsRoom(startClearance, corner) || SegmentIntersectsRoom(corner, endClearance))
            {
                corner = FindAlternativeCorner(startClearance, endClearance, startDir, endDir);
            }

            if (Vector3.Distance(corner, startClearance) > 1f && Vector3.Distance(corner, endClearance) > 1f)
            {
                waypoints.Add(corner);
            }

            waypoints.Add(endClearance);
            waypoints.Add(end);

            waypoints = StrictRoomAvoidance(waypoints);
            return waypoints;
        }

        // ===================== Grid-Based Pathfinding =====================

        /// <summary>
        /// Find a corridor path using the dungeon grid A* pathfinding.
        /// Returns world-space waypoints (simplified to turns only), or null if grid unavailable / no path found.
        /// Marks the path on the grid to prevent future corridors from overlapping.
        /// </summary>
        private List<Vector3> FindGridPath(Vector3 startPos, Vector3 endPos)
        {
            if (grid == null) return null;

            Vector2Int gridStart = grid.WorldToGrid(startPos.x, startPos.z);
            Vector2Int gridEnd = grid.WorldToGrid(endPos.x, endPos.z);

            int halfWidth = Mathf.CeilToInt(corridorWidth / (2f * grid.CellSize));

            List<Vector2Int> gridPath = grid.FindPath(gridStart, gridEnd, halfWidth);
            if (gridPath == null || gridPath.Count < 2)
                return null;

            // Mark corridor footprint on grid (prevents future overlap)
            grid.MarkCorridorPath(gridPath, halfWidth);

            // Simplify cell-by-cell path to waypoints at turn points
            float worldY = (startPos.y + endPos.y) / 2f;
            List<Vector3> waypoints = grid.SimplifyPath(gridPath, worldY);

            if (waypoints == null || waypoints.Count < 2)
                return null;

            // Replace first/last waypoints with exact doorway positions
            waypoints[0] = startPos;
            waypoints[waypoints.Count - 1] = endPos;

            return waypoints;
        }

        /// <summary>
        /// Mark a legacy (non-grid) corridor path on the grid to prevent future corridors from overlapping it.
        /// Samples points along each segment and marks their grid cells as Corridor.
        /// </summary>
        private void MarkLegacyPathOnGrid(List<Vector3> waypoints)
        {
            if (grid == null || waypoints == null || waypoints.Count < 2) return;

            int halfWidth = Mathf.CeilToInt(corridorWidth / (2f * grid.CellSize));

            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vector3 a = waypoints[i];
                Vector3 b = waypoints[i + 1];
                float dist = Vector3.Distance(a, b);
                int steps = Mathf.Max(2, Mathf.CeilToInt(dist / grid.CellSize));

                var pathCells = new List<Vector2Int>();
                for (int s = 0; s <= steps; s++)
                {
                    Vector3 p = Vector3.Lerp(a, b, s / (float)steps);
                    Vector2Int cell = grid.WorldToGrid(p.x, p.z);
                    if (!pathCells.Contains(cell))
                        pathCells.Add(cell);
                }

                grid.MarkCorridorPath(pathCells, halfWidth);
            }
        }

        // ===================== L-Shaped Corridor =====================

        /// <summary>
        /// Create an L-shaped corridor with proper cube-based geometry.
        /// Uses grid-based A* pathfinding when grid is available (prevents overlapping).
        /// Falls back to legacy room-avoidance method otherwise.
        /// </summary>
        public GameObject CreateLShapedCorridor(Vector3 startPos, Vector3 startDir, Vector3 endPos, Vector3 endDir,
            Transform parent, int corridorIndex, int segmentsPerUnit = 2)
        {
            if (Vector3.Distance(startPos, endPos) < 0.1f) return null;

            // Try grid-based pathfinding first (overlap-free, maze-like)
            List<Vector3> waypoints = FindGridPath(startPos, endPos);

            // Fallback to legacy room avoidance
            if (waypoints == null || waypoints.Count < 2)
            {
                waypoints = CalculateRoomAvoidingPath(startPos, startDir, endPos, endDir);
                // Mark legacy path on grid so future corridors avoid it
                MarkLegacyPathOnGrid(waypoints);
            }

            if (waypoints.Count < 2) return null;

            // Create corridor game object
            GameObject corridorObj = new GameObject($"Corridor_{corridorIndex}");
            corridorObj.transform.SetParent(parent);
            corridorObj.transform.position = Vector3.zero;

            // Build tunnel segments between consecutive waypoints
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                CreateTunnelSegment(corridorObj, waypoints[i], waypoints[i + 1], $"Seg{i}");
            }

            // Floor/ceiling at internal corners to fill gaps
            for (int i = 1; i < waypoints.Count - 1; i++)
            {
                CreateCornerPiece(corridorObj, waypoints[i], $"Turn{i}");
            }

            corridorPaths.Add(new CorridorPath(waypoints, corridorIndex, corridorObj));
            return corridorObj;
        }

        // ===================== Spline Corridor =====================

        /// <summary>
        /// Create a curved corridor using Bezier spline with cube segments.
        /// Uses grid pathfinding when available, falls back to spline.
        /// </summary>
        public GameObject CreateSplineCorridor(Vector3 startPos, Vector3 startDir, Vector3 endPos, Vector3 endDir,
            Transform parent, int corridorIndex, int segments = 8)
        {
            if (Vector3.Distance(startPos, endPos) < 0.1f) return null;

            // When grid is available, delegate to grid-aligned corridor (prevents overlap)
            if (grid != null)
            {
                return CreateLShapedCorridor(startPos, startDir, endPos, endDir, parent, corridorIndex, segments);
            }

            // Fallback to spline path (no grid available)
            float dist = Vector3.Distance(startPos, endPos);
            float controlDist = dist * 0.4f;

            Vector3 p0 = startPos;
            Vector3 p1 = startPos + startDir * controlDist;
            Vector3 p2 = endPos + endDir * controlDist;
            Vector3 p3 = endPos;

            List<Vector3> splinePoints = new List<Vector3>();
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                splinePoints.Add(CubicBezier(p0, p1, p2, p3, t));
            }

            // Mark spline path on grid so future corridors avoid it
            MarkLegacyPathOnGrid(splinePoints);

            // Create corridor
            GameObject corridorObj = new GameObject($"SplineCorridor_{corridorIndex}");
            corridorObj.transform.SetParent(parent);
            corridorObj.transform.position = Vector3.zero;

            // Build tunnel segments along spline
            for (int i = 0; i < splinePoints.Count - 1; i++)
            {
                CreateTunnelSegment(corridorObj, splinePoints[i], splinePoints[i + 1], $"Seg{i}");
            }

            corridorPaths.Add(new CorridorPath(splinePoints, corridorIndex, corridorObj));
            return corridorObj;
        }

        private Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1 - t;
            return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
        }

        // ===================== Straight Corridor Segment =====================

        /// <summary>
        /// Create a simple straight corridor between two points
        /// </summary>
        public GameObject CreateCorridor(Vector3 start, Vector3 end, Transform parent, int corridorIndex, string direction)
        {
            if (Vector3.Distance(start, end) < 0.1f) return null;

            GameObject corridorObj = new GameObject($"Corridor_{corridorIndex}_{direction}");
            corridorObj.transform.SetParent(parent);
            corridorObj.transform.position = Vector3.zero;

            CreateTunnelSegment(corridorObj, start, end, "Seg0");
            return corridorObj;
        }

        // ===================== Corner Piece =====================

        /// <summary>
        /// Create a corner piece to connect two perpendicular corridor segments
        /// </summary>
        public GameObject CreateCorridorCorner(Vector3 cornerPosition, Transform parent, int corridorIndex)
        {
            GameObject cornerObj = new GameObject($"CorridorCorner_{corridorIndex}");
            cornerObj.transform.SetParent(parent);
            cornerObj.transform.position = Vector3.zero;

            CreateCornerPiece(cornerObj, cornerPosition, "Corner");
            return cornerObj;
        }

        // ===================== Junction System =====================

        /// <summary>
        /// Post-process: detect actual corridor crossings by checking line segment intersections.
        /// Only creates junction points where corridors genuinely cross each other geometrically.
        /// Must be called after ALL corridors are created.
        /// </summary>
        public void DetectAllIntersections()
        {
            float mergeThreshold = corridorWidth * 1.5f;

            for (int i = 0; i < corridorPaths.Count; i++)
            {
                for (int j = i + 1; j < corridorPaths.Count; j++)
                {
                    var pathA = corridorPaths[i].Points;
                    var pathB = corridorPaths[j].Points;

                    for (int ai = 0; ai < pathA.Count - 1; ai++)
                    {
                        for (int bi = 0; bi < pathB.Count - 1; bi++)
                        {
                            Vector2 a1 = new Vector2(pathA[ai].x, pathA[ai].z);
                            Vector2 a2 = new Vector2(pathA[ai + 1].x, pathA[ai + 1].z);
                            Vector2 b1 = new Vector2(pathB[bi].x, pathB[bi].z);
                            Vector2 b2 = new Vector2(pathB[bi + 1].x, pathB[bi + 1].z);

                            // Only detect actual geometric line segment crossings
                            if (TryGetSegmentIntersection2D(a1, a2, b1, b2, out Vector2 hit))
                            {
                                float y = Mathf.Min(pathA[ai].y, pathB[bi].y);
                                AddUniqueJunction(new Vector3(hit.x, y, hit.y), mergeThreshold);
                            }

                            // Tight proximity check - only for segments that physically overlap
                            // (within half corridor width, meaning the passages occupy the same space)
                            float closestDist = SegmentToSegmentDist2D(a1, a2, b1, b2);
                            if (closestDist < corridorWidth * 0.3f)
                            {
                                // Find the closest points between the two segments
                                Vector2 midA = (a1 + a2) / 2f;
                                Vector2 midB = (b1 + b2) / 2f;
                                float y = Mathf.Min(pathA[ai].y, pathB[bi].y);
                                Vector3 jPoint = new Vector3(
                                    (midA.x + midB.x) / 2f, y, (midA.y + midB.y) / 2f);
                                AddUniqueJunction(jPoint, mergeThreshold);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Minimum distance between two 2D line segments
        /// </summary>
        private static float SegmentToSegmentDist2D(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
        {
            // If they intersect, distance is 0
            if (TryGetSegmentIntersection2D(a1, a2, b1, b2, out _))
                return 0f;

            // Otherwise check all four point-to-segment distances
            float d1 = PointToSegmentDist2D(a1, b1, b2);
            float d2 = PointToSegmentDist2D(a2, b1, b2);
            float d3 = PointToSegmentDist2D(b1, a1, a2);
            float d4 = PointToSegmentDist2D(b2, a1, a2);
            return Mathf.Min(Mathf.Min(d1, d2), Mathf.Min(d3, d4));
        }

        private void AddUniqueJunction(Vector3 point, float mergeThreshold)
        {
            bool duplicate = junctionPoints.Exists(p => Vector3.Distance(p, point) < mergeThreshold);
            if (!duplicate)
                junctionPoints.Add(point);
        }

        /// <summary>
        /// Remove wall chunk objects whose center is near a junction point.
        /// Since tunnel segments are split into short chunks, only the small chunk
        /// at the actual crossing gets its walls removed (not the entire corridor length).
        /// Uses wall object world position for precise, localized removal.
        /// </summary>
        public void CleanWallsAtJunctions()
        {
            // Radius matches the short chunk length so only chunks AT the crossing are affected
            float junctionRadius = corridorWidth * 1.5f;

            foreach (var junction in junctionPoints)
            {
                Vector2 jPos = new Vector2(junction.x, junction.z);

                foreach (var corridor in corridorPaths)
                {
                    if (corridor.CorridorObject == null) continue;

                    // Collect wall objects near this junction by checking their actual world position
                    var wallsToRemove = new List<GameObject>();
                    foreach (Transform child in corridor.CorridorObject.transform)
                    {
                        if (!child.name.Contains("_WL") && !child.name.Contains("_WR"))
                            continue;

                        Vector2 wallPos = new Vector2(child.position.x, child.position.z);
                        if (Vector2.Distance(wallPos, jPos) < junctionRadius)
                        {
                            wallsToRemove.Add(child.gameObject);
                        }
                    }

                    foreach (var wall in wallsToRemove)
                    {
                        Object.DestroyImmediate(wall);
                    }
                }
            }
        }

        /// <summary>
        /// Create junction pieces (floor + ceiling, no walls) at all intersection points.
        /// Call after DetectAllIntersections and CleanWallsAtJunctions.
        /// </summary>
        public void CreateJunctions(Transform parent)
        {
            for (int i = 0; i < junctionPoints.Count; i++)
            {
                GameObject junctionObj = new GameObject($"Junction_{i}");
                junctionObj.transform.SetParent(parent);
                junctionObj.transform.position = Vector3.zero;

                float ft = 0.1f;
                // Large enough to cover the crossing area of two corridors
                float jSize = corridorWidth * 2f + wallThickness * 2f;

                // Junction floor
                PlaceCube(junctionObj, "JunctionFloor",
                    junctionPoints[i] + Vector3.down * (ft / 2f),
                    new Vector3(jSize, ft, jSize),
                    Quaternion.identity, floorMaterial);

                // Junction ceiling
                PlaceCube(junctionObj, "JunctionCeiling",
                    junctionPoints[i] + Vector3.up * (wallHeight + ft / 2f),
                    new Vector3(jSize, ft, jSize),
                    Quaternion.identity, ceilingMaterial);
            }
        }

        // ===================== Geometry Helpers =====================

        /// <summary>
        /// Shortest distance from a point to a line segment in 2D (XZ plane)
        /// </summary>
        private static float PointToSegmentDist2D(Vector2 point, Vector2 segA, Vector2 segB)
        {
            Vector2 ab = segB - segA;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 0.001f) return Vector2.Distance(point, segA);

            float t = Mathf.Clamp01(Vector2.Dot(point - segA, ab) / lenSq);
            Vector2 projection = segA + t * ab;
            return Vector2.Distance(point, projection);
        }

        /// <summary>
        /// Check if two 2D line segments intersect, and return the intersection point
        /// </summary>
        private static bool TryGetSegmentIntersection2D(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection)
        {
            intersection = Vector2.zero;

            Vector2 d1 = a2 - a1;
            Vector2 d2 = b2 - b1;

            float cross = d1.x * d2.y - d1.y * d2.x;
            if (Mathf.Abs(cross) < 0.001f) return false; // Parallel

            Vector2 d = b1 - a1;
            float t = (d.x * d2.y - d.y * d2.x) / cross;
            float u = (d.x * d1.y - d.y * d1.x) / cross;

            if (t >= 0f && t <= 1f && u >= 0f && u <= 1f)
            {
                intersection = a1 + t * d1;
                return true;
            }

            return false;
        }

        // ===================== Mesh Creation Helpers =====================

        /// <summary>
        /// Create a tunnel segment split into short chunks for granular wall control.
        /// Short chunks allow CleanWallsAtJunctions to remove only the walls near a crossing,
        /// instead of stripping walls from an entire long segment.
        /// </summary>
        private void CreateTunnelSegment(GameObject parent, Vector3 start, Vector3 end, string name)
        {
            Vector3 delta = end - start;
            Vector3 horizontalDelta = new Vector3(delta.x, 0, delta.z);
            float length = horizontalDelta.magnitude;
            if (length < 0.01f) return;

            // Break long segments into short chunks (max ~2x corridorWidth each)
            float maxChunkLen = corridorWidth * 2f;
            int numChunks = Mathf.Max(1, Mathf.CeilToInt(length / maxChunkLen));

            for (int c = 0; c < numChunks; c++)
            {
                float t0 = c / (float)numChunks;
                float t1 = (c + 1) / (float)numChunks;
                Vector3 chunkStart = Vector3.Lerp(start, end, t0);
                Vector3 chunkEnd = Vector3.Lerp(start, end, t1);

                CreateTunnelChunk(parent, chunkStart, chunkEnd, $"{name}_{c}");
            }
        }

        /// <summary>
        /// Create a single short tunnel chunk (floor + ceiling + walls) between two close points.
        /// Each chunk gets its own wall cubes so they can be individually removed at junctions.
        /// </summary>
        private void CreateTunnelChunk(GameObject parent, Vector3 start, Vector3 end, string name)
        {
            Vector3 delta = end - start;
            Vector3 horizontalDelta = new Vector3(delta.x, 0, delta.z);
            float length = horizontalDelta.magnitude;
            if (length < 0.01f) return;

            Vector3 dir = horizontalDelta.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3 mid = (start + end) / 2f;
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            float ft = 0.1f;
            float halfW = corridorWidth / 2f;
            float t = wallThickness;
            float h = wallHeight;

            // Floor slab
            PlaceCube(parent, name + "_F",
                mid + Vector3.down * (ft / 2f),
                new Vector3(corridorWidth, ft, length),
                rot, floorMaterial);

            // Ceiling slab
            PlaceCube(parent, name + "_C",
                mid + Vector3.up * (h + ft / 2f),
                new Vector3(corridorWidth, ft, length),
                rot, ceilingMaterial);

            // Left wall
            PlaceCube(parent, name + "_WL",
                mid + right * (-halfW - t / 2f) + Vector3.up * (h / 2f),
                new Vector3(t, h, length),
                rot, wallMaterial);

            // Right wall
            PlaceCube(parent, name + "_WR",
                mid + right * (halfW + t / 2f) + Vector3.up * (h / 2f),
                new Vector3(t, h, length),
                rot, wallMaterial);
        }

        /// <summary>
        /// Create a corner/turn piece (floor + ceiling) at a waypoint
        /// </summary>
        private void CreateCornerPiece(GameObject parent, Vector3 position, string name)
        {
            float ft = 0.1f;
            float cornerSize = corridorWidth + 2 * wallThickness;

            // Floor
            PlaceCube(parent, name + "_F",
                position + Vector3.down * (ft / 2f),
                new Vector3(cornerSize, ft, cornerSize),
                Quaternion.identity, floorMaterial);

            // Ceiling
            PlaceCube(parent, name + "_C",
                position + Vector3.up * (wallHeight + ft / 2f),
                new Vector3(cornerSize, ft, cornerSize),
                Quaternion.identity, ceilingMaterial);
        }

        /// <summary>
        /// Place a ProBuilder cube at the specified world position with rotation
        /// </summary>
        private void PlaceCube(GameObject parent, string name, Vector3 worldPos, Vector3 size,
            Quaternion rotation, Material material)
        {
            var mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            mesh.gameObject.name = name;
            mesh.gameObject.transform.SetParent(parent.transform);
            mesh.gameObject.transform.position = worldPos;
            mesh.gameObject.transform.rotation = rotation;

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
