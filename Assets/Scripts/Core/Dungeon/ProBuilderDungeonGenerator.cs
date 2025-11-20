using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Procedural dungeon generator using ProBuilder API for mesh generation
    /// Generates rooms and corridors without using prefabs
    /// </summary>
    public class ProBuilderDungeonGenerator : MonoBehaviour
    {
        [Header("Dungeon Settings")]
        [SerializeField] private int roomCount = 10;
        [SerializeField] private Vector2Int minRoomSize = new Vector2Int(4, 4);
        [SerializeField] private Vector2Int maxRoomSize = new Vector2Int(10, 10);
        [SerializeField] private float wallHeight = 3f;
        [SerializeField] private float wallThickness = 0.2f;
        [SerializeField] private int corridorWidth = 2;
        [SerializeField] private int maxAttempts = 100;
        [SerializeField] private int gridSpread = 50;

        [Header("Materials")]
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material corridorMaterial;
        [SerializeField] private Material stairMaterial;

        [Header("Multi-Level Settings")]
        [SerializeField] private bool enableMultiLevel = false;
        [SerializeField] private float minFloorHeight = 0f;
        [SerializeField] private float maxFloorHeight = 6f;
        [SerializeField] private float floorHeightStep = 3f;
        [SerializeField] private bool enableStairs = true;
        [SerializeField] private float stepHeight = 0.2f;
        [SerializeField] private float stepDepth = 0.5f;

        [Header("Generation")]
        [SerializeField] private bool generateOnStart = false;
        [SerializeField] private int seed = 0;

        private List<DungeonRoom> rooms = new List<DungeonRoom>();
        private GameObject dungeonParent;
        private System.Random random;
        private RoomGenerator roomGenerator;
        private StairsGenerator stairsGenerator;
        private CorridorGenerator corridorGenerator;

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
            GenerateCorridorsAndDoorways();
            CreateRoomMeshes();
            Debug.Log($"Dungeon generated with {rooms.Count} rooms");
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
            corridorGenerator = new CorridorGenerator(corridorFloorMat, wallMaterial, floorMaterial, wallHeight, corridorWidth);
        }

        /// <summary>
        /// Generate all rooms with random placement
        /// </summary>
        private void GenerateRooms()
        {
            int attempts = 0;

            while (rooms.Count < roomCount && attempts < maxAttempts)
            {
                attempts++;

                // Generate random room size
                int width = random.Next(minRoomSize.x, maxRoomSize.x + 1);
                int depth = random.Next(minRoomSize.y, maxRoomSize.y + 1);

                // Generate random position
                int x = random.Next(-gridSpread, gridSpread);
                int z = random.Next(-gridSpread, gridSpread);

                Vector3Int position = new Vector3Int(x, 0, z);
                Vector3Int size = new Vector3Int(width, (int)wallHeight, depth);

                // Generate random floor height if multi-level is enabled
                float floorHeight = 0f;
                if (enableMultiLevel)
                {
                    int heightLevels = Mathf.FloorToInt((maxFloorHeight - minFloorHeight) / floorHeightStep) + 1;
                    int randomLevel = random.Next(0, heightLevels);
                    floorHeight = minFloorHeight + (randomLevel * floorHeightStep);
                }

                DungeonRoom newRoom = new DungeonRoom(position, size, floorHeight);

                // Check for overlaps
                bool overlaps = false;
                foreach (var existingRoom in rooms)
                {
                    if (newRoom.Overlaps(existingRoom, 2))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    rooms.Add(newRoom);
                }
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
        /// Generate corridors connecting rooms and add doorways
        /// Uses minimum spanning tree to ensure all rooms are connected
        /// </summary>
        private void GenerateCorridorsAndDoorways()
        {
            if (rooms.Count < 2)
                return;

            // Track which rooms are connected
            bool[] connected = new bool[rooms.Count];
            connected[0] = true;

            int connectedCount = 1;
            int corridorIndex = 0;

            // Minimum Spanning Tree approach: connect closest unconnected room to the connected set
            while (connectedCount < rooms.Count)
            {
                float minDistance = float.MaxValue;
                int bestConnectedRoom = -1;
                int bestUnconnectedRoom = -1;

                // Find the closest pair between connected and unconnected rooms
                for (int i = 0; i < rooms.Count; i++)
                {
                    if (!connected[i]) continue;

                    for (int j = 0; j < rooms.Count; j++)
                    {
                        if (connected[j]) continue;

                        float distance = Vector3.Distance(rooms[i].GetCenter(), rooms[j].GetCenter());
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            bestConnectedRoom = i;
                            bestUnconnectedRoom = j;
                        }
                    }
                }

                // Connect the best pair
                if (bestUnconnectedRoom >= 0)
                {
                    ConnectRooms(rooms[bestConnectedRoom], rooms[bestUnconnectedRoom], corridorIndex++);
                    connected[bestUnconnectedRoom] = true;
                    connectedCount++;
                }
            }

            // Add extra connections for loops (10% chance per pair of nearby rooms)
            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    float distance = Vector3.Distance(rooms[i].GetCenter(), rooms[j].GetCenter());

                    // Only consider nearby rooms for extra connections
                    if (distance < (gridSpread * 0.5f) && random.NextDouble() < 0.1)
                    {
                        ConnectRooms(rooms[i], rooms[j], corridorIndex++);
                    }
                }
            }

            Debug.Log($"Created {corridorIndex} corridor connections");
        }

        /// <summary>
        /// Connect two rooms with an L-shaped corridor (with optional stairs)
        /// </summary>
        private void ConnectRooms(DungeonRoom roomA, DungeonRoom roomB, int corridorIndex)
        {
            Vector3 startPos = roomA.GetCenter();
            Vector3 endPos = roomB.GetCenter();

            float heightDiff = endPos.y - startPos.y;
            bool needsStairs = enableStairs && Mathf.Abs(heightDiff) > 0.1f;

            if (needsStairs)
            {
                // Multi-level connection with stairs
                ConnectRoomsWithStairs(roomA, roomB, corridorIndex, startPos, endPos);
            }
            else
            {
                // Same-level connection
                ConnectRoomsSameLevel(roomA, roomB, corridorIndex, startPos, endPos);
            }
        }

        /// <summary>
        /// Connect two rooms on the same level
        /// </summary>
        private void ConnectRoomsSameLevel(DungeonRoom roomA, DungeonRoom roomB, int corridorIndex, Vector3 startPos, Vector3 endPos)
        {
            // Create L-shaped corridor (horizontal then vertical)
            Vector3 cornerPos = new Vector3(endPos.x, startPos.y, startPos.z);

            // Add doorways where corridors meet rooms
            Vector3 doorwayA = FindRoomBoundaryIntersection(roomA, startPos, cornerPos);
            roomA.AddDoorway(doorwayA, corridorWidth);

            Vector3 doorwayB = FindRoomBoundaryIntersection(roomB, endPos, cornerPos);
            roomB.AddDoorway(doorwayB, corridorWidth);

            // Create corridors
            CreateCorridor(startPos, cornerPos, corridorIndex, "Horizontal");
            CreateCorridor(cornerPos, endPos, corridorIndex, "Vertical");
        }

        /// <summary>
        /// Connect two rooms at different heights with stairs
        /// </summary>
        private void ConnectRoomsWithStairs(DungeonRoom roomA, DungeonRoom roomB, int corridorIndex, Vector3 startPos, Vector3 endPos)
        {
            // Create horizontal corridor at lower level
            Vector3 midPoint = new Vector3((startPos.x + endPos.x) / 2f, startPos.y, (startPos.z + endPos.z) / 2f);

            // Find doorway for roomA
            Vector3 doorwayA = FindRoomBoundaryIntersection(roomA, startPos, midPoint);
            roomA.AddDoorway(doorwayA, corridorWidth);

            // Create corridor from roomA to stairs start
            Vector3 stairsStart = new Vector3(midPoint.x, startPos.y, midPoint.z);
            CreateCorridor(startPos, stairsStart, corridorIndex, "ToStairs");

            // Create stairs from lower to higher level
            Vector3 stairsEnd = new Vector3(midPoint.x, endPos.y, midPoint.z);
            if (stairsGenerator != null)
            {
                stairsGenerator.CreateStairs(stairsStart, stairsEnd, dungeonParent.transform, corridorIndex);
            }

            // Find doorway for roomB
            Vector3 doorwayB = FindRoomBoundaryIntersection(roomB, endPos, stairsEnd);
            roomB.AddDoorway(doorwayB, corridorWidth);

            // Create corridor from stairs end to roomB
            CreateCorridor(stairsEnd, endPos, corridorIndex, "FromStairs");
        }

        /// <summary>
        /// Find where a line from inside the room intersects with the room boundary
        /// </summary>
        private Vector3 FindRoomBoundaryIntersection(DungeonRoom room, Vector3 insidePoint, Vector3 outsidePoint)
        {
            Vector3 direction = (outsidePoint - insidePoint).normalized;
            Vector3 currentPoint = insidePoint;

            // Step along the line until we hit the room boundary
            float step = 0.1f;
            float maxDistance = Vector3.Distance(insidePoint, outsidePoint);

            for (float dist = 0; dist < maxDistance; dist += step)
            {
                currentPoint = insidePoint + direction * dist;

                // Check if current point is outside the room bounds
                if (!IsPointInRoom(room, currentPoint))
                {
                    // Step back slightly to get the boundary point
                    return insidePoint + direction * (dist - step);
                }
            }

            // Fallback: return the outside point
            return outsidePoint;
        }

        /// <summary>
        /// Check if a point is inside a room's bounds
        /// </summary>
        private bool IsPointInRoom(DungeonRoom room, Vector3 point)
        {
            return point.x >= room.Position.x &&
                   point.x <= room.Position.x + room.Size.x &&
                   point.z >= room.Position.z &&
                   point.z <= room.Position.z + room.Size.z;
        }

        /// <summary>
        /// Create a corridor segment between two points using CorridorGenerator
        /// </summary>
        private void CreateCorridor(Vector3 start, Vector3 end, int corridorIndex, string direction)
        {
            corridorGenerator.CreateCorridor(start, end, dungeonParent.transform, corridorIndex, direction);
        }

        private void OnValidate()
        {
            // Clamp values
            roomCount = Mathf.Max(1, roomCount);
            minRoomSize.x = Mathf.Max(2, minRoomSize.x);
            minRoomSize.y = Mathf.Max(2, minRoomSize.y);
            maxRoomSize.x = Mathf.Max(minRoomSize.x, maxRoomSize.x);
            maxRoomSize.y = Mathf.Max(minRoomSize.y, maxRoomSize.y);
            wallHeight = Mathf.Max(1f, wallHeight);
            corridorWidth = Mathf.Max(1, corridorWidth);

            // Clamp multi-level settings
            maxFloorHeight = Mathf.Max(minFloorHeight, maxFloorHeight);
            floorHeightStep = Mathf.Max(0.5f, floorHeightStep);
            stepHeight = Mathf.Clamp(stepHeight, 0.1f, 0.5f);
            stepDepth = Mathf.Clamp(stepDepth, 0.3f, 1f);
        }
    }
}
