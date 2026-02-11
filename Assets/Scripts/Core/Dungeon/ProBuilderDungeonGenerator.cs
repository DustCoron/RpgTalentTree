using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Procedural dungeon generator using ProBuilder API for mesh generation
    /// Generates rooms and corridors without using prefabs
    /// </summary>
    public class ProBuilderDungeonGenerator : MonoBehaviour
    {
        [Header("Dungeon Settings")]
        [SerializeField] private int dungeonWidth = 80;
        [SerializeField] private int dungeonDepth = 80;
        [SerializeField] private int bspDepth = 4;
        [SerializeField] private int minPartitionSize = 12;
        [SerializeField] private Vector2Int minRoomSize = new Vector2Int(4, 4);
        [SerializeField] private Vector2Int maxRoomSize = new Vector2Int(10, 10);
        [SerializeField] private float wallHeight = 3f;
        [SerializeField] private float wallThickness = 0.2f;
        [SerializeField] private int corridorWidth = 2;

        [Header("Corridor Style")]
        [SerializeField] private bool useHardCorners = true;
        [SerializeField] private int segmentsPerUnit = 2;
        [Tooltip("Maximum corridors per room (1-4)")]
        [Range(1, 4)]
        [SerializeField] private int maxCorridorsPerRoom = 4;
        [Tooltip("Chance (0-1) of creating extra connections between nearby rooms for maze effect")]
        [Range(0f, 1f)]
        [SerializeField] private float extraConnectionChance = 0.6f;
        [Tooltip("Maximum length before adding extra 90-degree corners")]
        [SerializeField] private float maxCorridorSegmentLength = 15f;

        [Header("Materials")]
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material corridorMaterial;
        [SerializeField] private Material stairMaterial;

        [Header("Multi-Level Settings")]
        [SerializeField] private bool enableMultiLevel = true;  // Enabled by default for stairs
        [SerializeField] private float minFloorHeight = 0f;
        [SerializeField] private float maxFloorHeight = 9f;     // Increased from 6f for more variation
        [SerializeField] private float floorHeightStep = 3f;
        [Tooltip("Probability (0-1) that adjacent rooms will be on different levels")]
        [Range(0f, 1f)]
        [SerializeField] private float heightVariationChance = 0.4f;  // 40% chance of height difference
        [SerializeField] private bool enableStairs = true;
        [SerializeField] private float stepHeight = 0.2f;
        [SerializeField] private float stepDepth = 0.5f;

        [Header("Generation")]
        [SerializeField] private bool generateOnStart = false;
        [SerializeField] private int seed = 0;

        [Header("Optimization")]
        [SerializeField] private bool combineMeshes = true;
        [Tooltip("Add decorative pillars in room corners")]
        [SerializeField] private bool addPillars = true;
        [Tooltip("Add torch holders on walls")]
        [SerializeField] private bool addTorchHolders = false;
        [Tooltip("Add ceiling decorations")]
        [SerializeField] private bool addCeilingDecorations = false;

        [Header("Marker System")]
        [Tooltip("Visualize marker positions in Scene view (for debugging)")]
        [SerializeField] private bool showMarkers = false;
        [Tooltip("Size of marker gizmos in Scene view")]
        [SerializeField] private float markerGizmoSize = 0.3f;

        private List<DungeonRoom> rooms = new List<DungeonRoom>();
        private GameObject dungeonParent;
        private System.Random random;
        private RoomGenerator roomGenerator;
        private StairsGenerator stairsGenerator;
        private CorridorGenerator corridorGenerator;
        private BSPNode bspRoot;
        private DungeonGrid dungeonGrid;

        private List<DungeonMarker> dungeonMarkers = new List<DungeonMarker>();

        private void Start()
        {
            if (generateOnStart)
            {
                GenerateDungeon();
            }
        }

        /// <summary>
        /// Main entry point for dungeon generation
        /// </summary>
        public void GenerateDungeon()
        {
            ClearDungeon();
            InitializeGenerator();
            GenerateRooms();

            // Setup grid for overlap-free corridor pathfinding
            InitializeDungeonGrid();

            // Setup corridor generator with room collision data and grid
            corridorGenerator?.ClearPaths();
            corridorGenerator?.SetGrid(dungeonGrid);
            RegisterRoomBoundsForCorridors();

            GenerateCorridorsAndDoorways();

            // Post-process: detect all intersections, clean overlapping walls, build junctions
            corridorGenerator?.DetectAllIntersections();
            corridorGenerator?.CleanWallsAtJunctions();
            corridorGenerator?.CreateJunctions(dungeonParent.transform);

            CreateRoomMeshes();
            GenerateMarkers();
            AddDecorations();
            OptimizeMeshes();
            int junctionCount = corridorGenerator?.GetJunctionPoints().Count ?? 0;
            Debug.Log($"Dungeon generated with {rooms.Count} rooms, {dungeonMarkers.Count} markers, {junctionCount} junctions");
        }

        /// <summary>
        /// Create a 2D occupancy grid covering the dungeon area and mark all rooms.
        /// The grid is used by CorridorGenerator for A* pathfinding that avoids overlap.
        /// </summary>
        private void InitializeDungeonGrid()
        {
            float minX = -dungeonWidth / 2f;
            float minZ = -dungeonDepth / 2f;
            float maxX = dungeonWidth / 2f;
            float maxZ = dungeonDepth / 2f;

            dungeonGrid = new DungeonGrid(minX, minZ, maxX, maxZ);

            foreach (var room in rooms)
            {
                dungeonGrid.MarkRoom(room, wallThickness);
            }

            Debug.Log($"Dungeon grid initialized: {dungeonGrid.Width}x{dungeonGrid.Height} cells");
        }

        /// <summary>
        /// Register all room bounds with corridor generator for collision avoidance
        /// </summary>
        private void RegisterRoomBoundsForCorridors()
        {
            if (corridorGenerator == null) return;

            // Set max segment length
            corridorGenerator.SetMaxSegmentLength(maxCorridorSegmentLength);

            foreach (var room in rooms)
            {
                // Create bounds for room
                Vector3 center = new Vector3(
                    room.Position.x + room.Size.x / 2f,
                    room.FloorHeight + wallHeight / 2f,
                    room.Position.z + room.Size.z / 2f
                );
                Vector3 size = new Vector3(room.Size.x, wallHeight, room.Size.z);
                Bounds bounds = new Bounds(center, size);
                corridorGenerator.RegisterRoomBounds(bounds);
            }
        }

        /// <summary>
        /// Clear existing dungeon
        /// </summary>
        public void ClearDungeon()
        {
            if (dungeonParent != null)
            {
                if (Application.isPlaying)
                    Destroy(dungeonParent);
                else
                    DestroyImmediate(dungeonParent);
            }

            rooms.Clear();
            dungeonMarkers.Clear();
            dungeonGrid = null;
            DungeonRoom.ResetIdCounter();
        }

        private void InitializeGenerator()
        {
            dungeonParent = new GameObject("Dungeon");
            dungeonParent.transform.SetParent(transform);
            dungeonParent.transform.localPosition = Vector3.zero;

            // Initialize random with seed (0 = random seed)
            random = seed == 0 ? new System.Random() : new System.Random(seed);

            // Initialize room generator
            roomGenerator = new RoomGenerator(floorMaterial, wallMaterial, wallHeight, wallThickness);

            // Initialize stairs generator
            Material stairMat = stairMaterial != null ? stairMaterial : floorMaterial;
            stairsGenerator = new StairsGenerator(stairMat, stepHeight, stepDepth, corridorWidth);

            // Initialize corridor generator
            Material corridorFloorMat = corridorMaterial != null ? corridorMaterial : floorMaterial;
            corridorGenerator = new CorridorGenerator(corridorFloorMat, wallMaterial, floorMaterial, wallHeight, wallThickness, corridorWidth);
        }

        /// <summary>
        /// Generate all rooms using Binary Space Partitioning
        /// </summary>
        private void GenerateRooms()
        {
            // Create root BSP node covering the entire dungeon area
            Rect dungeonBounds = new Rect(-dungeonWidth / 2f, -dungeonDepth / 2f, dungeonWidth, dungeonDepth);
            bspRoot = new BSPNode(dungeonBounds);

            // Recursively split the space
            SplitBSPNode(bspRoot, 0);

            // Create rooms in leaf nodes
            List<BSPNode> leaves = new List<BSPNode>();
            bspRoot.GetLeaves(leaves);

            // Assign heights to rooms with controlled variation
            float currentHeight = minFloorHeight;
            int heightLevels = enableMultiLevel ? Mathf.FloorToInt((maxFloorHeight - minFloorHeight) / floorHeightStep) + 1 : 1;

            foreach (var leaf in leaves)
            {
                // Generate floor height with controlled randomness
                float floorHeight = 0f;
                if (enableMultiLevel)
                {
                    // Use heightVariationChance to control level changes
                    if (random.NextDouble() < heightVariationChance)
                    {
                        // Change to a different random level
                        int randomLevel = random.Next(0, heightLevels);
                        currentHeight = minFloorHeight + (randomLevel * floorHeightStep);
                    }
                    // else keep currentHeight (creates clusters of same-height rooms)

                    floorHeight = currentHeight;
                }

                leaf.CreateRoom(random, minRoomSize, maxRoomSize, floorHeight);
                if (leaf.Room != null)
                {
                    rooms.Add(leaf.Room);
                }
            }

            Debug.Log($"BSP generated {rooms.Count} rooms from {leaves.Count} partitions");
        }

        /// <summary>
        /// Recursively split BSP node
        /// </summary>
        private void SplitBSPNode(BSPNode node, int depth)
        {
            if (depth >= bspDepth)
                return;

            if (node.Split(minPartitionSize, random))
            {
                SplitBSPNode(node.LeftChild, depth + 1);
                SplitBSPNode(node.RightChild, depth + 1);
            }
        }

        /// <summary>
        /// Create ProBuilder meshes for all rooms
        /// </summary>
        private void CreateRoomMeshes()
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                roomGenerator.CreateRoom(rooms[i], dungeonParent.transform, i);
            }
        }

        /// <summary>
        /// Generate corridors connecting rooms following BSP tree structure
        /// </summary>
        private void GenerateCorridorsAndDoorways()
        {
            if (rooms.Count < 2 || bspRoot == null)
                return;

            int corridorIndex = 0;

            // Phase 1: BSP spanning tree - mandatory connections guarantee reachability
            ConnectBSPNodes(bspRoot, ref corridorIndex);
            int bspCount = corridorIndex;

            // Phase 2: Verify and fix full connectivity (BFS + force-connect isolates)
            EnsureFullConnectivity(ref corridorIndex);
            int fixCount = corridorIndex - bspCount;

            // Phase 3: Extra connections between nearby rooms to create maze loops
            AddExtraConnections(ref corridorIndex);
            int extraCount = corridorIndex - bspCount - fixCount;

            Debug.Log($"Created {corridorIndex} corridors ({bspCount} BSP, {fixCount} fixes, {extraCount} extra loops)");
        }

        /// <summary>
        /// Recursively connect rooms in BSP tree
        /// </summary>
        private void ConnectBSPNodes(BSPNode node, ref int corridorIndex)
        {
            if (node.IsLeaf())
                return;

            // Recursively connect children first
            if (node.LeftChild != null)
                ConnectBSPNodes(node.LeftChild, ref corridorIndex);
            if (node.RightChild != null)
                ConnectBSPNodes(node.RightChild, ref corridorIndex);

            // Connect rooms from left and right subtrees
            if (node.LeftChild != null && node.RightChild != null)
            {
                // Get a random room from each subtree
                DungeonRoom leftRoom = GetRandomRoomFromNode(node.LeftChild);
                DungeonRoom rightRoom = GetRandomRoomFromNode(node.RightChild);

                if (leftRoom != null && rightRoom != null)
                {
                    ConnectRooms(leftRoom, rightRoom, corridorIndex++, true);
                }
            }
        }

        /// <summary>
        /// Get a random room from a BSP node's subtree
        /// </summary>
        private DungeonRoom GetRandomRoomFromNode(BSPNode node)
        {
            if (node.IsLeaf())
                return node.Room;

            // Randomly choose left or right subtree
            if (node.LeftChild != null && node.RightChild != null)
            {
                return random.Next(0, 2) == 0
                    ? GetRandomRoomFromNode(node.LeftChild)
                    : GetRandomRoomFromNode(node.RightChild);
            }
            else if (node.LeftChild != null)
            {
                return GetRandomRoomFromNode(node.LeftChild);
            }
            else if (node.RightChild != null)
            {
                return GetRandomRoomFromNode(node.RightChild);
            }

            return null;
        }

        /// <summary>
        /// Connect two rooms with an L-shaped corridor (with optional stairs).
        /// mandatory=true for BSP spanning tree connections (ignores maxCorridorsPerRoom limit).
        /// </summary>
        private void ConnectRooms(DungeonRoom roomA, DungeonRoom roomB, int corridorIndex, bool mandatory)
        {
            // Skip if already connected
            if (roomA.ConnectedRoomIds.Contains(roomB.Id))
                return;

            Vector3 startPos = roomA.GetCenter();
            Vector3 endPos = roomB.GetCenter();

            float heightDiff = endPos.y - startPos.y;
            bool needsStairs = enableStairs && Mathf.Abs(heightDiff) > 0.1f;

            if (needsStairs)
            {
                ConnectRoomsWithStairs(roomA, roomB, corridorIndex, startPos, endPos, mandatory);
            }
            else
            {
                ConnectRoomsSameLevel(roomA, roomB, corridorIndex, startPos, endPos, mandatory);
            }
        }

        /// <summary>
        /// Connect two rooms on the same level
        /// </summary>
        private void ConnectRoomsSameLevel(DungeonRoom roomA, DungeonRoom roomB, int corridorIndex, Vector3 startPos, Vector3 endPos, bool mandatory = false)
        {
            // Check corridor limits (mandatory connections bypass maxCorridorsPerRoom)
            if (!CanAddCorridor(roomA, mandatory) || !CanAddCorridor(roomB, mandatory))
            {
                if (!mandatory) return;
                Debug.Log($"Skipping corridor {corridorIndex}: no available walls");
                return;
            }

            // Get best available exit points from each room
            var connectionA = roomA.GetBestConnectionPoint(roomB);
            var connectionB = roomB.GetBestConnectionPoint(roomA);

            if (!connectionA.HasValue || !connectionB.HasValue)
            {
                Debug.Log($"Skipping corridor {corridorIndex}: no available walls");
                return;
            }

            var (wallA, exitA, dirA) = connectionA.Value;
            var (wallB, exitB, dirB) = connectionB.Value;

            // Reserve walls
            roomA.ReserveWall(wallA);
            roomB.ReserveWall(wallB);

            // Add doorways
            roomA.AddDoorway(exitA, corridorWidth);
            roomB.AddDoorway(exitB, corridorWidth);

            // Track connectivity
            roomA.ConnectTo(roomB);

            // Create corridor with appropriate style
            if (useHardCorners)
            {
                corridorGenerator.CreateLShapedCorridor(exitA, dirA, exitB, dirB, dungeonParent.transform, corridorIndex, segmentsPerUnit);
            }
            else
            {
                corridorGenerator.CreateSplineCorridor(exitA, dirA, exitB, dirB, dungeonParent.transform, corridorIndex, segmentsPerUnit * 4);
            }
        }

        /// <summary>
        /// Check if room can accept more corridors.
        /// mandatory=true bypasses the maxCorridorsPerRoom limit (still needs a free wall).
        /// </summary>
        private bool CanAddCorridor(DungeonRoom room, bool mandatory = false)
        {
            if (room.GetAvailableWallCount() == 0) return false;
            if (!mandatory && room.ConnectedRoomIds.Count >= maxCorridorsPerRoom) return false;
            return true;
        }

        /// <summary>
        /// Connect two rooms at different heights with stairs
        /// </summary>
        private void ConnectRoomsWithStairs(DungeonRoom roomA, DungeonRoom roomB, int corridorIndex, Vector3 startPos, Vector3 endPos, bool mandatory = false)
        {
            // Check corridor limits (mandatory connections bypass maxCorridorsPerRoom)
            if (!CanAddCorridor(roomA, mandatory) || !CanAddCorridor(roomB, mandatory))
            {
                if (!mandatory) return;
                Debug.Log($"Skipping stairs {corridorIndex}: no available walls");
                return;
            }

            var connectionA = roomA.GetBestConnectionPoint(roomB);
            var connectionB = roomB.GetBestConnectionPoint(roomA);

            if (!connectionA.HasValue || !connectionB.HasValue)
            {
                Debug.Log($"Skipping stairs {corridorIndex}: no available walls");
                return;
            }

            var (wallA, exitA, dirA) = connectionA.Value;
            var (wallB, exitB, dirB) = connectionB.Value;

            roomA.ReserveWall(wallA);
            roomB.ReserveWall(wallB);
            roomA.AddDoorway(exitA, corridorWidth);
            roomB.AddDoorway(exitB, corridorWidth);
            roomA.ConnectTo(roomB);

            // Calculate stairs position
            Vector3 stairsStart = new Vector3((exitA.x + exitB.x) / 2f, exitA.y, (exitA.z + exitB.z) / 2f);
            Vector3 stairsEnd = new Vector3(stairsStart.x, exitB.y, stairsStart.z);
            Vector3 toStairsA = (stairsStart - exitA).normalized;
            Vector3 toStairsB = (stairsEnd - exitB).normalized;

            if (useHardCorners)
            {
                corridorGenerator.CreateLShapedCorridor(exitA, dirA, stairsStart, -toStairsA, dungeonParent.transform, corridorIndex, segmentsPerUnit);
                if (stairsGenerator != null)
                    stairsGenerator.CreateStairs(stairsStart, stairsEnd, dungeonParent.transform, corridorIndex);
                corridorGenerator.CreateLShapedCorridor(stairsEnd, toStairsB, exitB, dirB, dungeonParent.transform, corridorIndex, segmentsPerUnit);
            }
            else
            {
                corridorGenerator.CreateSplineCorridor(exitA, dirA, stairsStart, -toStairsA, dungeonParent.transform, corridorIndex, segmentsPerUnit * 2);
                if (stairsGenerator != null)
                    stairsGenerator.CreateStairs(stairsStart, stairsEnd, dungeonParent.transform, corridorIndex);
                corridorGenerator.CreateSplineCorridor(stairsEnd, toStairsB, exitB, dirB, dungeonParent.transform, corridorIndex, segmentsPerUnit * 2);
            }
        }

        /// <summary>
        /// BFS connectivity check - find and fix any disconnected rooms.
        /// Guarantees every room is reachable from every other room.
        /// </summary>
        private void EnsureFullConnectivity(ref int corridorIndex)
        {
            if (rooms.Count < 2) return;

            // BFS from first room
            HashSet<int> visited = new HashSet<int>();
            Queue<DungeonRoom> queue = new Queue<DungeonRoom>();
            visited.Add(rooms[0].Id);
            queue.Enqueue(rooms[0]);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (int connectedId in current.ConnectedRoomIds)
                {
                    if (!visited.Contains(connectedId))
                    {
                        visited.Add(connectedId);
                        var connectedRoom = rooms.Find(r => r.Id == connectedId);
                        if (connectedRoom != null)
                            queue.Enqueue(connectedRoom);
                    }
                }
            }

            // Find unreachable rooms and force-connect them
            List<DungeonRoom> unreachable = rooms.FindAll(r => !visited.Contains(r.Id));
            while (unreachable.Count > 0)
            {
                // Find closest (reachable, unreachable) pair
                float bestDist = float.MaxValue;
                DungeonRoom bestReachable = null;
                DungeonRoom bestUnreachable = null;

                foreach (var ur in unreachable)
                {
                    foreach (var vr in rooms)
                    {
                        if (!visited.Contains(vr.Id)) continue;
                        float dist = Vector3.Distance(ur.GetCenter(), vr.GetCenter());
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestReachable = vr;
                            bestUnreachable = ur;
                        }
                    }
                }

                if (bestReachable == null || bestUnreachable == null) break;

                // Force-connect (mandatory)
                ConnectRooms(bestUnreachable, bestReachable, corridorIndex++, true);

                // BFS again from the newly connected room
                visited.Add(bestUnreachable.Id);
                queue.Enqueue(bestUnreachable);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    foreach (int connectedId in current.ConnectedRoomIds)
                    {
                        if (!visited.Contains(connectedId))
                        {
                            visited.Add(connectedId);
                            var connectedRoom = rooms.Find(r => r.Id == connectedId);
                            if (connectedRoom != null)
                                queue.Enqueue(connectedRoom);
                        }
                    }
                }

                unreachable = rooms.FindAll(r => !visited.Contains(r.Id));
            }
        }

        /// <summary>
        /// Add extra connections between nearby rooms to create loops (maze effect).
        /// Iterates room pairs by distance, connecting with extraConnectionChance probability.
        /// </summary>
        private void AddExtraConnections(ref int corridorIndex)
        {
            if (extraConnectionChance <= 0f || rooms.Count < 3) return;

            // Build list of all room pairs sorted by distance (closest first)
            List<(DungeonRoom a, DungeonRoom b, float dist)> pairs = new List<(DungeonRoom, DungeonRoom, float)>();
            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    float dist = Vector3.Distance(rooms[i].GetCenter(), rooms[j].GetCenter());
                    pairs.Add((rooms[i], rooms[j], dist));
                }
            }
            pairs.Sort((a, b) => a.dist.CompareTo(b.dist));

            int extraCount = 0;
            foreach (var (roomA, roomB, dist) in pairs)
            {
                // Skip already connected pairs
                if (roomA.ConnectedRoomIds.Contains(roomB.Id)) continue;

                // Both rooms must have available walls
                if (!CanAddCorridor(roomA) || !CanAddCorridor(roomB)) continue;

                // Random chance gate
                if (random.NextDouble() > extraConnectionChance) continue;

                ConnectRooms(roomA, roomB, corridorIndex++, false);
                extraCount++;
            }
        }

        /// <summary>
        /// Generate markers throughout the dungeon for decoration placement
        /// </summary>
        private void GenerateMarkers()
        {
            dungeonMarkers.Clear();

            foreach (var room in rooms)
            {
                // Room floor center marker
                Vector3 floorCenter = new Vector3(
                    room.Position.x + room.Size.x / 2f,
                    room.FloorHeight,
                    room.Position.z + room.Size.z / 2f
                );
                dungeonMarkers.Add(DungeonMarker.CreateSimple(MarkerType.RoomFloorCenter, floorCenter, room));

                // Room ceiling center marker
                Vector3 ceilingCenter = floorCenter + Vector3.up * wallHeight;
                dungeonMarkers.Add(DungeonMarker.CreateSimple(MarkerType.RoomCeilingCenter, ceilingCenter, room));

                // Room corner markers
                Vector3[] corners = new Vector3[]
                {
                    new Vector3(room.Position.x, room.FloorHeight, room.Position.z),
                    new Vector3(room.Position.x + room.Size.x, room.FloorHeight, room.Position.z),
                    new Vector3(room.Position.x + room.Size.x, room.FloorHeight, room.Position.z + room.Size.z),
                    new Vector3(room.Position.x, room.FloorHeight, room.Position.z + room.Size.z)
                };

                foreach (var corner in corners)
                {
                    dungeonMarkers.Add(DungeonMarker.CreateSimple(MarkerType.RoomCorner, corner, room));
                }

                // Doorway markers
                foreach (var doorway in room.Doorways)
                {
                    Vector3 doorwayPos = doorway.Position;
                    Vector3 doorwayNormal = GetDoorwayNormal(doorway.Wall);

                    // Left and right doorway markers
                    Vector3 perpendicular = Vector3.Cross(doorwayNormal, Vector3.up);
                    Vector3 leftPos = doorwayPos - perpendicular * (doorway.Width / 2f);
                    Vector3 rightPos = doorwayPos + perpendicular * (doorway.Width / 2f);

                    dungeonMarkers.Add(DungeonMarker.CreateWithNormal(MarkerType.DoorwayLeft, leftPos, doorwayNormal, room));
                    dungeonMarkers.Add(DungeonMarker.CreateWithNormal(MarkerType.DoorwayRight, rightPos, doorwayNormal, room));

                    // Doorway top marker (above doorway)
                    Vector3 topPos = doorwayPos + Vector3.up * wallHeight * 0.7f;
                    dungeonMarkers.Add(DungeonMarker.CreateWithNormal(MarkerType.DoorwayTop, topPos, doorwayNormal, room));
                }

                // Room wall mid-point markers (along walls between doorways)
                EmitWallMarkers(room);
            }

            // Corridor markers from generated paths
            if (corridorGenerator != null)
            {
                foreach (var path in corridorGenerator.GetCorridorPaths())
                {
                    for (int i = 0; i < path.Points.Count - 1; i++)
                    {
                        Vector3 midPoint = (path.Points[i] + path.Points[i + 1]) / 2f;
                        dungeonMarkers.Add(DungeonMarker.CreateSimple(MarkerType.CorridorFloor, midPoint));
                    }
                }

                foreach (var junction in corridorGenerator.GetJunctionPoints())
                {
                    dungeonMarkers.Add(DungeonMarker.CreateSimple(MarkerType.CorridorIntersection, junction));
                }
            }

            Debug.Log($"Generated {dungeonMarkers.Count} markers for decoration");
        }

        /// <summary>
        /// Get normal direction for a doorway wall side
        /// </summary>
        private Vector3 GetDoorwayNormal(Doorway.WallSide wallSide)
        {
            switch (wallSide)
            {
                case Doorway.WallSide.North: return Vector3.forward;
                case Doorway.WallSide.South: return Vector3.back;
                case Doorway.WallSide.East: return Vector3.right;
                case Doorway.WallSide.West: return Vector3.left;
                default: return Vector3.forward;
            }
        }

        /// <summary>
        /// Emit markers along room walls
        /// </summary>
        private void EmitWallMarkers(DungeonRoom room)
        {
            // North wall
            Vector3 northMid = new Vector3(
                room.Position.x + room.Size.x / 2f,
                room.FloorHeight + wallHeight / 2f,
                room.Position.z + room.Size.z
            );
            dungeonMarkers.Add(DungeonMarker.CreateWithNormal(MarkerType.RoomWallMid, northMid, Vector3.back, room));

            // South wall
            Vector3 southMid = new Vector3(
                room.Position.x + room.Size.x / 2f,
                room.FloorHeight + wallHeight / 2f,
                room.Position.z
            );
            dungeonMarkers.Add(DungeonMarker.CreateWithNormal(MarkerType.RoomWallMid, southMid, Vector3.forward, room));

            // East wall
            Vector3 eastMid = new Vector3(
                room.Position.x + room.Size.x,
                room.FloorHeight + wallHeight / 2f,
                room.Position.z + room.Size.z / 2f
            );
            dungeonMarkers.Add(DungeonMarker.CreateWithNormal(MarkerType.RoomWallMid, eastMid, Vector3.left, room));

            // West wall
            Vector3 westMid = new Vector3(
                room.Position.x,
                room.FloorHeight + wallHeight / 2f,
                room.Position.z + room.Size.z / 2f
            );
            dungeonMarkers.Add(DungeonMarker.CreateWithNormal(MarkerType.RoomWallMid, westMid, Vector3.right, room));
        }

        /// <summary>
        /// Add decorative elements to the dungeon
        /// </summary>
        private void AddDecorations()
        {
            if (!addPillars && !addTorchHolders && !addCeilingDecorations)
                return;

            foreach (var room in rooms)
            {
                GameObject roomObj = dungeonParent.transform.Find($"Room_{rooms.IndexOf(room)}")?.gameObject;
                if (roomObj == null) continue;

                if (addPillars)
                {
                    AddRoomPillars(roomObj, room);
                }

                if (addTorchHolders)
                {
                    AddWallTorchHolders(roomObj, room);
                }

                if (addCeilingDecorations)
                {
                    AddCeilingDecoration(roomObj, room);
                }
            }
        }

        /// <summary>
        /// Add decorative pillars to room corners using ProBuilder's GenerateCylinder
        /// </summary>
        private void AddRoomPillars(GameObject roomObj, DungeonRoom room)
        {
            float pillarRadius = 0.2f;
            float pillarHeight = wallHeight * 0.9f; // Slightly shorter than walls

            // Corner positions
            Vector3[] corners = new Vector3[]
            {
                new Vector3(0.5f, 0, 0.5f),                          // Bottom-left
                new Vector3(room.Size.x - 0.5f, 0, 0.5f),           // Bottom-right
                new Vector3(room.Size.x - 0.5f, 0, room.Size.z - 0.5f), // Top-right
                new Vector3(0.5f, 0, room.Size.z - 0.5f)            // Top-left
            };

            for (int i = 0; i < corners.Length; i++)
            {
                ProBuilderMesh pillarMesh = ShapeGenerator.GenerateCylinder(
                    PivotLocation.Center,   // Pivot at center
                    8,                      // 8-sided cylinder
                    pillarRadius,           // Radius
                    pillarHeight,           // Height
                    0,                      // No height cuts
                    -1                      // No smoothing groups
                );

                GameObject pillarObj = pillarMesh.gameObject;
                pillarObj.name = $"Pillar_Corner_{i}";
                pillarObj.transform.SetParent(roomObj.transform);
                // Offset position upward by half height since pivot is at center
                Vector3 pillarPosition = corners[i] + new Vector3(0, pillarHeight / 2f, 0);
                pillarObj.transform.localPosition = pillarPosition;

                // Apply material
                if (wallMaterial != null)
                {
                    var renderer = pillarMesh.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.sharedMaterial = wallMaterial;
                    }
                }

                pillarMesh.ToMesh();
                pillarMesh.Refresh();
            }
        }

        /// <summary>
        /// Add torch holders to walls using ProBuilder's GeneratePipe
        /// </summary>
        private void AddWallTorchHolders(GameObject roomObj, DungeonRoom room)
        {
            float torchHeight = wallHeight * 0.6f; // Position at 60% of wall height
            float torchRadius = 0.15f;
            float torchThickness = 0.05f;
            float wallOffset = 0.2f; // Distance from wall

            // Place torch holders on each wall (centered)
            Vector3[] torchPositions = new Vector3[]
            {
                new Vector3(room.Size.x / 2f, torchHeight, wallOffset),              // South wall
                new Vector3(room.Size.x / 2f, torchHeight, room.Size.z - wallOffset), // North wall
                new Vector3(wallOffset, torchHeight, room.Size.z / 2f),              // West wall
                new Vector3(room.Size.x - wallOffset, torchHeight, room.Size.z / 2f) // East wall
            };

            for (int i = 0; i < torchPositions.Length; i++)
            {
                ProBuilderMesh torchMesh = ShapeGenerator.GeneratePipe(
                    PivotLocation.Center,
                    torchRadius,            // Outer radius
                    0.1f,                   // Height (thin ring)
                    torchThickness,         // Thickness
                    12,                     // 12 segments around
                    1                       // 1 height segment
                );

                GameObject torchObj = torchMesh.gameObject;
                torchObj.name = $"TorchHolder_{i}";
                torchObj.transform.SetParent(roomObj.transform);
                torchObj.transform.localPosition = torchPositions[i];

                // Apply material
                if (wallMaterial != null)
                {
                    var renderer = torchMesh.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.sharedMaterial = wallMaterial;
                    }
                }

                torchMesh.ToMesh();
                torchMesh.Refresh();
            }
        }

        /// <summary>
        /// Add ceiling decoration using ProBuilder's GenerateIcosahedron
        /// </summary>
        private void AddCeilingDecoration(GameObject roomObj, DungeonRoom room)
        {
            // Create a decorative ceiling element at room center
            float decorationRadius = Mathf.Min(room.Size.x, room.Size.z) * 0.15f;
            Vector3 centerPosition = new Vector3(room.Size.x / 2f, wallHeight - 0.3f, room.Size.z / 2f);

            ProBuilderMesh decorationMesh = ShapeGenerator.GenerateIcosahedron(
                PivotLocation.Center,
                decorationRadius,
                0,                      // No subdivisions for simpler geometry
                true,                   // Weld vertices
                true                    // Manual UVs
            );

            GameObject decorationObj = decorationMesh.gameObject;
            decorationObj.name = "CeilingDecoration";
            decorationObj.transform.SetParent(roomObj.transform);
            decorationObj.transform.localPosition = centerPosition;

            // Apply material
            if (wallMaterial != null)
            {
                var renderer = decorationMesh.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = wallMaterial;
                }
            }

            decorationMesh.ToMesh();
            decorationMesh.Refresh();
        }

        /// <summary>
        /// Optimize dungeon by combining meshes using ProBuilder's CombineMeshes
        /// </summary>
        private void OptimizeMeshes()
        {
            if (!combineMeshes)
                return;

            // Get all ProBuilderMesh objects in the dungeon
            var allMeshes = dungeonParent.GetComponentsInChildren<ProBuilderMesh>().ToList();

            if (allMeshes.Count == 0)
                return;

            Debug.Log($"Optimizing dungeon: Combining {allMeshes.Count} meshes...");

            // Create a target mesh for combining
            GameObject targetObj = new GameObject("CombinedDungeonMesh");
            targetObj.transform.SetParent(dungeonParent.transform);
            targetObj.transform.localPosition = Vector3.zero;
            ProBuilderMesh targetMesh = targetObj.AddComponent<ProBuilderMesh>();

            // Use ProBuilder's CombineMeshes to merge all meshes into target
            List<ProBuilderMesh> combinedMeshes = CombineMeshes.Combine(allMeshes, targetMesh);

            if (combinedMeshes != null && combinedMeshes.Count > 0)
            {
                Debug.Log($"Optimization complete: Meshes combined into {combinedMeshes.Count} mesh(es)");
            }
            else
            {
                Debug.LogWarning("Mesh combining failed or returned no meshes");
                if (targetObj != null)
                {
                    if (Application.isPlaying)
                        Destroy(targetObj);
                    else
                        DestroyImmediate(targetObj);
                }
            }
        }

        private void OnValidate()
        {
            // Clamp values
            dungeonWidth = Mathf.Max(20, dungeonWidth);
            dungeonDepth = Mathf.Max(20, dungeonDepth);
            bspDepth = Mathf.Clamp(bspDepth, 1, 8);
            minPartitionSize = Mathf.Max(8, minPartitionSize);
            minRoomSize.x = Mathf.Max(2, minRoomSize.x);
            minRoomSize.y = Mathf.Max(2, minRoomSize.y);
            maxRoomSize.x = Mathf.Max(minRoomSize.x, maxRoomSize.x);
            maxRoomSize.y = Mathf.Max(minRoomSize.y, maxRoomSize.y);
            wallHeight = Mathf.Max(1f, wallHeight);
            corridorWidth = Mathf.Max(1, corridorWidth);

            // Clamp multi-level settings
            maxFloorHeight = Mathf.Max(minFloorHeight, maxFloorHeight);
            floorHeightStep = Mathf.Max(0.5f, floorHeightStep);
            heightVariationChance = Mathf.Clamp01(heightVariationChance);
            stepHeight = Mathf.Clamp(stepHeight, 0.1f, 0.5f);
            stepDepth = Mathf.Clamp(stepDepth, 0.3f, 1f);
            markerGizmoSize = Mathf.Max(0.1f, markerGizmoSize);
        }

        /// <summary>
        /// Public accessor for dungeon markers (for external theme systems)
        /// </summary>
        public List<DungeonMarker> GetMarkers()
        {
            return new List<DungeonMarker>(dungeonMarkers);
        }

        /// <summary>
        /// Draw marker gizmos in Scene view for debugging
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showMarkers || dungeonMarkers == null || dungeonMarkers.Count == 0)
                return;

            foreach (var marker in dungeonMarkers)
            {
                // Color code markers by type
                Gizmos.color = GetMarkerColor(marker.Type);

                // Draw sphere at marker position
                Gizmos.DrawWireSphere(marker.Position, markerGizmoSize);

                // Draw direction arrow for markers with rotation
                if (marker.Type == MarkerType.DoorwayLeft ||
                    marker.Type == MarkerType.DoorwayRight ||
                    marker.Type == MarkerType.RoomWallMid)
                {
                    Vector3 forward = marker.Rotation * Vector3.forward;
                    Gizmos.DrawRay(marker.Position, forward * markerGizmoSize * 2f);
                }
            }
        }

        /// <summary>
        /// Get color for marker type visualization
        /// </summary>
        private Color GetMarkerColor(MarkerType type)
        {
            switch (type)
            {
                case MarkerType.DoorwayLeft:
                case MarkerType.DoorwayRight:
                case MarkerType.DoorwayTop:
                    return Color.yellow;

                case MarkerType.RoomFloorCenter:
                    return Color.green;

                case MarkerType.RoomCeilingCenter:
                    return Color.cyan;

                case MarkerType.RoomCorner:
                    return Color.magenta;

                case MarkerType.RoomWallMid:
                    return Color.blue;

                case MarkerType.CorridorFloor:
                case MarkerType.CorridorIntersection:
                    return Color.red;

                case MarkerType.StairBottom:
                case MarkerType.StairTop:
                case MarkerType.StairMid:
                    return new Color(1f, 0.5f, 0f); // Orange

                case MarkerType.LightPoint:
                    return Color.white;

                default:
                    return Color.gray;
            }
        }
    }
}
