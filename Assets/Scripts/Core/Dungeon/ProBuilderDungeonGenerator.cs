using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Procedural dungeon generator using ProBuilder API for mesh generation.
    /// Orchestrates room generation, corridor connections, decorations, and optimization.
    /// Room generation uses BSP with variable room scales (small/medium/large).
    /// </summary>
    public class ProBuilderDungeonGenerator : MonoBehaviour
    {
        [Header("Dungeon Settings")]
        [SerializeField] private int dungeonWidth = 80;
        [SerializeField] private int dungeonDepth = 80;
        [SerializeField] private int bspDepth = 4;
        [SerializeField] private int minPartitionSize = 12;
        [SerializeField] private float wallHeight = 3f;
        [SerializeField] private float wallThickness = 0.2f;
        [SerializeField] private int corridorWidth = 2;

        [Header("Room Scale Variety")]
        [Tooltip("Minimum size for small rooms")]
        [SerializeField] private Vector2Int smallRoomMin = new Vector2Int(3, 3);
        [Tooltip("Maximum size for small rooms")]
        [SerializeField] private Vector2Int smallRoomMax = new Vector2Int(5, 5);
        [Tooltip("Minimum size for medium rooms")]
        [SerializeField] private Vector2Int mediumRoomMin = new Vector2Int(6, 6);
        [Tooltip("Maximum size for medium rooms")]
        [SerializeField] private Vector2Int mediumRoomMax = new Vector2Int(10, 10);
        [Tooltip("Minimum size for large rooms")]
        [SerializeField] private Vector2Int largeRoomMin = new Vector2Int(10, 10);
        [Tooltip("Maximum size for large rooms")]
        [SerializeField] private Vector2Int largeRoomMax = new Vector2Int(16, 16);
        [Tooltip("Probability of generating a small room")]
        [Range(0f, 1f)]
        [SerializeField] private float smallRoomChance = 0.3f;
        [Tooltip("Probability of generating a large room (rest is medium)")]
        [Range(0f, 1f)]
        [SerializeField] private float largeRoomChance = 0.2f;

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
        [SerializeField] private bool enableMultiLevel = true;
        [SerializeField] private float minFloorHeight = 0f;
        [SerializeField] private float maxFloorHeight = 9f;
        [SerializeField] private float floorHeightStep = 3f;
        [Tooltip("Probability (0-1) that adjacent rooms will be on different levels")]
        [Range(0f, 1f)]
        [SerializeField] private float heightVariationChance = 0.4f;
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

        // Runtime state
        private List<DungeonRoom> rooms = new List<DungeonRoom>();
        private GameObject dungeonParent;
        private System.Random random;
        private BSPNode bspRoot;
        private DungeonGrid dungeonGrid;
        private List<DungeonMarker> dungeonMarkers = new List<DungeonMarker>();
        private DungeonValidator.ValidationResult lastValidation;

        // Start/End rooms (set by validator)
        private DungeonRoom startRoom;
        private DungeonRoom endRoom;

        // Sub-systems
        private RoomGenerator roomGenerator;
        private StairsGenerator stairsGenerator;
        private CorridorGenerator corridorGenerator;
        private CorridorConnectionManager connectionManager;
        private DungeonDecorator decorator;

        private void Start()
        {
            if (generateOnStart)
            {
                GenerateDungeon();
            }
        }

        // ===================== Main Generation Pipeline =====================

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

            // Setup corridor generator
            corridorGenerator?.ClearPaths();
            corridorGenerator?.SetGrid(dungeonGrid);
            RegisterRoomBoundsForCorridors();

            // Generate corridors via connection manager
            connectionManager.GenerateCorridorsAndDoorways(bspRoot, dungeonParent.transform);

            // Post-process corridors: detect intersections, clean walls, build junctions
            corridorGenerator?.DetectAllIntersections();
            corridorGenerator?.CleanWallsAtJunctions();
            corridorGenerator?.CreateJunctions(dungeonParent.transform);

            // Build room meshes and decorations
            CreateRoomMeshes();
            dungeonMarkers = decorator.GenerateMarkers();
            decorator.AddDecorations(dungeonParent.transform);

            // Validate dungeon: assign start/end, verify path, setup colliders
            lastValidation = DungeonValidator.Validate(rooms, dungeonParent.transform, corridorWidth);
            startRoom = lastValidation.StartRoom;
            endRoom = lastValidation.EndRoom;
            DungeonValidator.LogReport(lastValidation);

            // Add spawn/exit markers after validation assigns start/end
            AddStartEndMarkers();

            // Optimize last (after colliders are set up)
            decorator.OptimizeMeshes(dungeonParent.transform);

            int junctionCount = corridorGenerator?.GetJunctionPoints().Count ?? 0;
            Debug.Log($"Dungeon generated with {rooms.Count} rooms, {dungeonMarkers.Count} markers, {junctionCount} junctions");
        }

        /// <summary>
        /// Add spawn and exit markers at the start/end rooms.
        /// These provide world positions for placing the player and exit trigger.
        /// </summary>
        private void AddStartEndMarkers()
        {
            if (startRoom != null)
            {
                Vector3 spawnPos = startRoom.GetCenter() + Vector3.up * 0.5f;
                var marker = DungeonMarker.CreateSimple(MarkerType.SpawnPoint, spawnPos, startRoom);
                marker.SetMetadata("isEntrance", true);
                dungeonMarkers.Add(marker);
            }

            if (endRoom != null)
            {
                Vector3 exitPos = endRoom.GetCenter() + Vector3.up * 0.5f;
                var marker = DungeonMarker.CreateSimple(MarkerType.ExitPoint, exitPos, endRoom);
                marker.SetMetadata("isExit", true);
                dungeonMarkers.Add(marker);
            }
        }

        // ===================== Setup & Cleanup =====================

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
            startRoom = null;
            endRoom = null;
            lastValidation = null;
            DungeonRoom.ResetIdCounter();
        }

        private void InitializeGenerator()
        {
            dungeonParent = new GameObject("Dungeon");
            dungeonParent.transform.SetParent(transform);
            dungeonParent.transform.localPosition = Vector3.zero;

            random = seed == 0 ? new System.Random() : new System.Random(seed);

            // Room and stairs generators
            roomGenerator = new RoomGenerator(floorMaterial, wallMaterial, wallHeight, wallThickness);

            Material stairMat = stairMaterial != null ? stairMaterial : floorMaterial;
            stairsGenerator = new StairsGenerator(stairMat, stepHeight, stepDepth, corridorWidth);

            // Corridor generator
            Material corridorFloorMat = corridorMaterial != null ? corridorMaterial : floorMaterial;
            corridorGenerator = new CorridorGenerator(
                corridorFloorMat, wallMaterial, floorMaterial, wallHeight, wallThickness, corridorWidth);

            // Connection manager (corridor routing and linking)
            connectionManager = new CorridorConnectionManager(
                rooms, corridorGenerator, stairsGenerator, random,
                maxCorridorsPerRoom, extraConnectionChance, corridorWidth,
                useHardCorners, segmentsPerUnit, enableStairs);

            // Decorator (markers, decorations, mesh optimization)
            decorator = new DungeonDecorator(
                rooms, corridorGenerator, wallHeight, wallThickness, corridorWidth,
                floorMaterial, wallMaterial, addPillars, addTorchHolders,
                addCeilingDecorations, combineMeshes);
        }

        // ===================== Room Generation =====================

        /// <summary>
        /// Generate rooms using BSP with variable room scales (small/medium/large).
        /// Each BSP leaf gets a random scale, creating variety in room sizes.
        /// </summary>
        private void GenerateRooms()
        {
            Rect dungeonBounds = new Rect(-dungeonWidth / 2f, -dungeonDepth / 2f, dungeonWidth, dungeonDepth);
            bspRoot = new BSPNode(dungeonBounds);

            SplitBSPNode(bspRoot, 0);

            List<BSPNode> leaves = new List<BSPNode>();
            bspRoot.GetLeaves(leaves);

            // Height level setup
            float currentHeight = minFloorHeight;
            int heightLevels = enableMultiLevel
                ? Mathf.FloorToInt((maxFloorHeight - minFloorHeight) / floorHeightStep) + 1
                : 1;

            int smallCount = 0, mediumCount = 0, largeCount = 0;

            foreach (var leaf in leaves)
            {
                // Assign floor height with controlled variation
                float floorHeight = 0f;
                if (enableMultiLevel)
                {
                    if (random.NextDouble() < heightVariationChance)
                    {
                        int randomLevel = random.Next(0, heightLevels);
                        currentHeight = minFloorHeight + (randomLevel * floorHeightStep);
                    }
                    floorHeight = currentHeight;
                }

                // Pick room scale based on configured probabilities
                float roll = (float)random.NextDouble();
                Vector2Int minSize, maxSize;
                RoomScale scale;

                if (roll < smallRoomChance)
                {
                    minSize = smallRoomMin;
                    maxSize = smallRoomMax;
                    scale = RoomScale.Small;
                }
                else if (roll > 1f - largeRoomChance)
                {
                    minSize = largeRoomMin;
                    maxSize = largeRoomMax;
                    scale = RoomScale.Large;
                }
                else
                {
                    minSize = mediumRoomMin;
                    maxSize = mediumRoomMax;
                    scale = RoomScale.Medium;
                }

                leaf.CreateRoom(random, minSize, maxSize, floorHeight);

                if (leaf.Room != null)
                {
                    leaf.Room.Scale = scale;
                    rooms.Add(leaf.Room);

                    switch (scale)
                    {
                        case RoomScale.Small: smallCount++; break;
                        case RoomScale.Medium: mediumCount++; break;
                        case RoomScale.Large: largeCount++; break;
                    }
                }
            }

            Debug.Log($"BSP generated {rooms.Count} rooms ({smallCount} small, {mediumCount} medium, {largeCount} large) from {leaves.Count} partitions");
        }

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

        // ===================== Grid & Corridor Setup =====================

        /// <summary>
        /// Create a 2D occupancy grid covering the dungeon area and mark all rooms.
        /// Used by CorridorGenerator for A* pathfinding that avoids overlap.
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

        private void RegisterRoomBoundsForCorridors()
        {
            if (corridorGenerator == null) return;

            corridorGenerator.SetMaxSegmentLength(maxCorridorSegmentLength);

            foreach (var room in rooms)
            {
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

        // ===================== Room Mesh Creation =====================

        private void CreateRoomMeshes()
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                roomGenerator.CreateRoom(rooms[i], dungeonParent.transform, i);
            }
        }

        // ===================== Validation & Debug =====================

        private void OnValidate()
        {
            dungeonWidth = Mathf.Max(20, dungeonWidth);
            dungeonDepth = Mathf.Max(20, dungeonDepth);
            bspDepth = Mathf.Clamp(bspDepth, 1, 8);
            minPartitionSize = Mathf.Max(8, minPartitionSize);

            // Validate room scale ranges
            smallRoomMin.x = Mathf.Max(2, smallRoomMin.x);
            smallRoomMin.y = Mathf.Max(2, smallRoomMin.y);
            smallRoomMax.x = Mathf.Max(smallRoomMin.x, smallRoomMax.x);
            smallRoomMax.y = Mathf.Max(smallRoomMin.y, smallRoomMax.y);

            mediumRoomMin.x = Mathf.Max(2, mediumRoomMin.x);
            mediumRoomMin.y = Mathf.Max(2, mediumRoomMin.y);
            mediumRoomMax.x = Mathf.Max(mediumRoomMin.x, mediumRoomMax.x);
            mediumRoomMax.y = Mathf.Max(mediumRoomMin.y, mediumRoomMax.y);

            largeRoomMin.x = Mathf.Max(2, largeRoomMin.x);
            largeRoomMin.y = Mathf.Max(2, largeRoomMin.y);
            largeRoomMax.x = Mathf.Max(largeRoomMin.x, largeRoomMax.x);
            largeRoomMax.y = Mathf.Max(largeRoomMin.y, largeRoomMax.y);

            wallHeight = Mathf.Max(1f, wallHeight);
            corridorWidth = Mathf.Max(1, corridorWidth);

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
        /// Get the spawn room (dungeon entrance). Player should start here.
        /// </summary>
        public DungeonRoom GetStartRoom() => startRoom;

        /// <summary>
        /// Get the exit room (dungeon goal). Player must reach here.
        /// </summary>
        public DungeonRoom GetEndRoom() => endRoom;

        /// <summary>
        /// Get spawn position in world space (center of start room, slightly above floor)
        /// </summary>
        public Vector3 GetSpawnPosition() => startRoom != null
            ? startRoom.GetCenter() + Vector3.up * 0.5f
            : Vector3.zero;

        /// <summary>
        /// Get exit position in world space (center of end room, slightly above floor)
        /// </summary>
        public Vector3 GetExitPosition() => endRoom != null
            ? endRoom.GetCenter() + Vector3.up * 0.5f
            : Vector3.zero;

        /// <summary>
        /// Get the critical path (shortest route from start to end).
        /// Returns null if validation hasn't run or path doesn't exist.
        /// </summary>
        public List<DungeonRoom> GetCriticalPath() => lastValidation?.CriticalPath;

        /// <summary>
        /// Check if the dungeon passed validation (has valid start-to-end path)
        /// </summary>
        public bool IsDungeonValid() => lastValidation != null && lastValidation.IsValid;

        private void OnDrawGizmos()
        {
            if (!showMarkers || dungeonMarkers == null || dungeonMarkers.Count == 0)
                return;

            foreach (var marker in dungeonMarkers)
            {
                Gizmos.color = GetMarkerColor(marker.Type);

                // Spawn and exit get larger solid spheres for visibility
                if (marker.Type == MarkerType.SpawnPoint)
                {
                    Gizmos.DrawSphere(marker.Position, markerGizmoSize * 3f);
                }
                else if (marker.Type == MarkerType.ExitPoint)
                {
                    Gizmos.DrawSphere(marker.Position, markerGizmoSize * 3f);
                }
                else
                {
                    Gizmos.DrawWireSphere(marker.Position, markerGizmoSize);
                }

                if (marker.Type == MarkerType.DoorwayLeft ||
                    marker.Type == MarkerType.DoorwayRight ||
                    marker.Type == MarkerType.RoomWallMid)
                {
                    Vector3 forward = marker.Rotation * Vector3.forward;
                    Gizmos.DrawRay(marker.Position, forward * markerGizmoSize * 2f);
                }
            }

            // Draw critical path as connected line
            if (lastValidation?.CriticalPath != null && lastValidation.CriticalPath.Count >= 2)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.8f); // Bright green
                for (int i = 0; i < lastValidation.CriticalPath.Count - 1; i++)
                {
                    Vector3 a = lastValidation.CriticalPath[i].GetCenter() + Vector3.up * 1.5f;
                    Vector3 b = lastValidation.CriticalPath[i + 1].GetCenter() + Vector3.up * 1.5f;
                    Gizmos.DrawLine(a, b);
                }
            }
        }

        private Color GetMarkerColor(MarkerType type)
        {
            return type switch
            {
                MarkerType.SpawnPoint => Color.green,
                MarkerType.ExitPoint => new Color(1f, 0f, 0f, 1f), // Bright red
                MarkerType.DoorwayLeft or MarkerType.DoorwayRight or MarkerType.DoorwayTop => Color.yellow,
                MarkerType.RoomFloorCenter => new Color(0.3f, 0.8f, 0.3f),
                MarkerType.RoomCeilingCenter => Color.cyan,
                MarkerType.RoomCorner => Color.magenta,
                MarkerType.RoomWallMid => Color.blue,
                MarkerType.CorridorFloor or MarkerType.CorridorIntersection => new Color(0.8f, 0.4f, 0.4f),
                MarkerType.StairBottom or MarkerType.StairTop or MarkerType.StairMid => new Color(1f, 0.5f, 0f),
                MarkerType.LightPoint => Color.white,
                _ => Color.gray
            };
        }
    }
}
