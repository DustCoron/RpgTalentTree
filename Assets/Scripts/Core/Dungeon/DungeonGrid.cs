using System.Collections.Generic;
using UnityEngine;

namespace RpgTalentTree.Core.Dungeon
{
    public enum GridCellType
    {
        Empty,
        Room,
        Corridor
    }

    /// <summary>
    /// 2D occupancy grid for dungeon layout.
    /// Handles room marking, A* corridor pathfinding, and overlap prevention.
    /// Corridors routed on the grid cannot overlap rooms or each other.
    /// </summary>
    public class DungeonGrid
    {
        public GridCellType[,] Cells;
        public int Width;
        public int Height;
        public float CellSize;
        public Vector2 Origin; // World XZ of grid cell (0,0) corner

        public DungeonGrid(float worldMinX, float worldMinZ, float worldMaxX, float worldMaxZ, float cellSize = 1f)
        {
            CellSize = cellSize;
            float padding = cellSize * 6;
            Origin = new Vector2(worldMinX - padding, worldMinZ - padding);
            Width = Mathf.CeilToInt((worldMaxX - worldMinX + 2 * padding) / cellSize);
            Height = Mathf.CeilToInt((worldMaxZ - worldMinZ + 2 * padding) / cellSize);
            Cells = new GridCellType[Width, Height];
        }

        public Vector2Int WorldToGrid(float worldX, float worldZ)
        {
            return new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt((worldX - Origin.x) / CellSize), 0, Width - 1),
                Mathf.Clamp(Mathf.FloorToInt((worldZ - Origin.y) / CellSize), 0, Height - 1)
            );
        }

        public Vector3 GridToWorld(int gx, int gz, float worldY = 0f)
        {
            return new Vector3(
                Origin.x + (gx + 0.5f) * CellSize,
                worldY,
                Origin.y + (gz + 0.5f) * CellSize
            );
        }

        public bool InBounds(int x, int z)
        {
            return x >= 0 && x < Width && z >= 0 && z < Height;
        }

        /// <summary>
        /// Mark a room on the grid (room footprint + wall thickness buffer)
        /// </summary>
        public void MarkRoom(DungeonRoom room, float wallThickness)
        {
            Vector2Int min = WorldToGrid(
                room.Position.x - wallThickness - 1f,
                room.Position.z - wallThickness - 1f
            );
            Vector2Int max = WorldToGrid(
                room.Position.x + room.Size.x + wallThickness + 1f,
                room.Position.z + room.Size.z + wallThickness + 1f
            );

            for (int x = min.x; x <= max.x; x++)
                for (int z = min.y; z <= max.y; z++)
                    if (InBounds(x, z))
                        Cells[x, z] = GridCellType.Room;
        }

        /// <summary>
        /// Mark corridor cells on the grid after a path is created.
        /// Prevents future corridors from overlapping this one.
        /// </summary>
        public void MarkCorridorPath(List<Vector2Int> path, int halfWidth)
        {
            foreach (var cell in path)
            {
                for (int dx = -halfWidth; dx <= halfWidth; dx++)
                    for (int dz = -halfWidth; dz <= halfWidth; dz++)
                    {
                        int nx = cell.x + dx;
                        int nz = cell.y + dz;
                        if (InBounds(nx, nz) && Cells[nx, nz] != GridCellType.Room)
                            Cells[nx, nz] = GridCellType.Corridor;
                    }
            }
        }

        /// <summary>
        /// A* pathfinding on the grid.
        /// - Room cells are blocked (except near start/end for doorway access)
        /// - Existing corridor cells incur heavy penalty (discourages parallel runs, allows crossing)
        /// - Turn penalty creates maze-like paths with long straight segments
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, int corridorHalfWidth)
        {
            if (!InBounds(start.x, start.y) || !InBounds(end.x, end.y))
                return null;

            if (start == end)
                return new List<Vector2Int> { start };

            var openSet = new List<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float>();
            var fScore = new Dictionary<Vector2Int, float>();
            var closedSet = new HashSet<Vector2Int>();

            openSet.Add(start);
            gScore[start] = 0f;
            fScore[start] = ManhattanDist(start, end);

            Vector2Int[] dirs =
            {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1)
            };

            int maxIterations = Width * Height;
            int iterations = 0;

            while (openSet.Count > 0 && iterations++ < maxIterations)
            {
                // Pick node with lowest fScore
                int bestIdx = 0;
                float bestF = fScore.ContainsKey(openSet[0]) ? fScore[openSet[0]] : float.MaxValue;
                for (int i = 1; i < openSet.Count; i++)
                {
                    float f = fScore.ContainsKey(openSet[i]) ? fScore[openSet[i]] : float.MaxValue;
                    if (f < bestF)
                    {
                        bestF = f;
                        bestIdx = i;
                    }
                }

                Vector2Int current = openSet[bestIdx];
                openSet.RemoveAt(bestIdx);

                if (current == end)
                    return ReconstructPath(cameFrom, current);

                if (closedSet.Contains(current)) continue;
                closedSet.Add(current);

                foreach (var dir in dirs)
                {
                    Vector2Int next = current + dir;
                    if (closedSet.Contains(next)) continue;
                    if (!CanPlaceCorridor(next, corridorHalfWidth, start, end)) continue;

                    float cost = 1f;

                    // Turn penalty - prefer long straight segments (maze feel)
                    if (cameFrom.ContainsKey(current))
                    {
                        Vector2Int prevDir = current - cameFrom[current];
                        if (dir != prevDir) cost += 4f;
                    }

                    // Penalty for proximity to existing corridors (prevents parallel runs)
                    int nearbyCorridors = CountNearbyCorridor(next, corridorHalfWidth + 1);
                    if (nearbyCorridors > 0)
                        cost += nearbyCorridors * 3f;

                    float tentativeG = gScore[current] + cost;
                    if (!gScore.ContainsKey(next) || tentativeG < gScore[next])
                    {
                        cameFrom[next] = current;
                        gScore[next] = tentativeG;
                        fScore[next] = tentativeG + ManhattanDist(next, end);
                        if (!openSet.Contains(next))
                            openSet.Add(next);
                    }
                }
            }

            return null; // No path found
        }

        /// <summary>
        /// Check if a corridor of given half-width can be placed at this cell.
        /// Room cells are blocked except near start/end (doorway entry zone).
        /// </summary>
        private bool CanPlaceCorridor(Vector2Int cell, int halfWidth, Vector2Int start, Vector2Int end)
        {
            for (int dx = -halfWidth; dx <= halfWidth; dx++)
            {
                for (int dz = -halfWidth; dz <= halfWidth; dz++)
                {
                    int nx = cell.x + dx;
                    int nz = cell.y + dz;
                    if (!InBounds(nx, nz)) return false;

                    if (Cells[nx, nz] == GridCellType.Room)
                    {
                        // Allow near start or end (doorway entry zone)
                        int distToStart = Mathf.Max(Mathf.Abs(cell.x - start.x), Mathf.Abs(cell.y - start.y));
                        int distToEnd = Mathf.Max(Mathf.Abs(cell.x - end.x), Mathf.Abs(cell.y - end.y));
                        if (distToStart > halfWidth + 2 && distToEnd > halfWidth + 2)
                            return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Count existing corridor cells near a position (for overlap penalty)
        /// </summary>
        private int CountNearbyCorridor(Vector2Int cell, int radius)
        {
            int count = 0;
            for (int dx = -radius; dx <= radius; dx++)
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int nx = cell.x + dx;
                    int nz = cell.y + dz;
                    if (InBounds(nx, nz) && Cells[nx, nz] == GridCellType.Corridor)
                        count++;
                }
            return count;
        }

        private float ManhattanDist(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Simplify a cell-by-cell grid path into waypoints at turn points only.
        /// A straight run of cells becomes a single segment between two waypoints.
        /// </summary>
        public List<Vector3> SimplifyPath(List<Vector2Int> gridPath, float worldY)
        {
            if (gridPath == null || gridPath.Count < 2) return null;

            var waypoints = new List<Vector3>();
            waypoints.Add(GridToWorld(gridPath[0].x, gridPath[0].y, worldY));

            if (gridPath.Count == 2)
            {
                waypoints.Add(GridToWorld(gridPath[1].x, gridPath[1].y, worldY));
                return waypoints;
            }

            Vector2Int prevDir = gridPath[1] - gridPath[0];
            for (int i = 2; i < gridPath.Count; i++)
            {
                Vector2Int dir = gridPath[i] - gridPath[i - 1];
                if (dir != prevDir)
                {
                    // Direction changed - add turn waypoint
                    waypoints.Add(GridToWorld(gridPath[i - 1].x, gridPath[i - 1].y, worldY));
                    prevDir = dir;
                }
            }

            waypoints.Add(GridToWorld(gridPath[gridPath.Count - 1].x, gridPath[gridPath.Count - 1].y, worldY));
            return waypoints;
        }
    }
}
