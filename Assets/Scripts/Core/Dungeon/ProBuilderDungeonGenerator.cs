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

        [Header("Generation")]
        [SerializeField] private bool generateOnStart = false;
        [SerializeField] private int seed = 0;

        private List<DungeonRoom> rooms = new List<DungeonRoom>();
        private GameObject dungeonParent;
        private System.Random random;
        private RoomGenerator roomGenerator;

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

                DungeonRoom newRoom = new DungeonRoom(position, size);

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
        /// </summary>
        private void GenerateCorridorsAndDoorways()
        {
            for (int i = 0; i < rooms.Count - 1; i++)
            {
                ConnectRooms(rooms[i], rooms[i + 1], i);
            }

            // Optionally connect first and last room to create a loop
            if (rooms.Count > 2)
            {
                ConnectRooms(rooms[rooms.Count - 1], rooms[0], rooms.Count - 1);
            }
        }

        /// <summary>
        /// Connect two rooms with an L-shaped corridor
        /// </summary>
        private void ConnectRooms(DungeonRoom roomA, DungeonRoom roomB, int corridorIndex)
        {
            Vector3 startPos = roomA.GetCenter();
            Vector3 endPos = roomB.GetCenter();

            // Create L-shaped corridor (horizontal then vertical)
            Vector3 cornerPos = new Vector3(endPos.x, startPos.y, startPos.z);

            // Add doorways where corridors meet rooms
            // Doorway at roomA exit (horizontal corridor start)
            Vector3 doorwayA = FindRoomBoundaryIntersection(roomA, startPos, cornerPos);
            roomA.AddDoorway(doorwayA, corridorWidth);

            // Doorway at roomB entrance (vertical corridor end)
            Vector3 doorwayB = FindRoomBoundaryIntersection(roomB, endPos, cornerPos);
            roomB.AddDoorway(doorwayB, corridorWidth);

            // Horizontal corridor
            CreateCorridor(startPos, cornerPos, corridorIndex, "Horizontal");

            // Vertical corridor
            CreateCorridor(cornerPos, endPos, corridorIndex, "Vertical");
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
        /// Create a corridor segment between two points
        /// </summary>
        private void CreateCorridor(Vector3 start, Vector3 end, int corridorIndex, string direction)
        {
            if (Vector3.Distance(start, end) < 0.1f)
                return;

            GameObject corridorObj = new GameObject($"Corridor_{corridorIndex}_{direction}");
            corridorObj.transform.SetParent(dungeonParent.transform);

            Vector3 dir = (end - start).normalized;
            float distance = Vector3.Distance(start, end);

            // Calculate corridor dimensions
            Vector3 localStart = start;
            Vector3 localEnd = end;

            // Create floor
            GameObject floorObj = new GameObject("Floor");
            floorObj.transform.SetParent(corridorObj.transform);
            floorObj.transform.position = localStart;

            ProBuilderMesh pbMesh = floorObj.AddComponent<ProBuilderMesh>();

            // Determine corridor orientation and create appropriate polygon
            Vector3[] polygonPoints;
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
            {
                // Horizontal corridor
                float halfWidth = corridorWidth / 2f;
                polygonPoints = new Vector3[] {
                    new Vector3(0, 0, -halfWidth),
                    new Vector3(distance, 0, -halfWidth),
                    new Vector3(distance, 0, halfWidth),
                    new Vector3(0, 0, halfWidth)
                };
            }
            else
            {
                // Vertical corridor
                float halfWidth = corridorWidth / 2f;
                polygonPoints = new Vector3[] {
                    new Vector3(-halfWidth, 0, 0),
                    new Vector3(halfWidth, 0, 0),
                    new Vector3(halfWidth, 0, distance),
                    new Vector3(-halfWidth, 0, distance)
                };
            }

            pbMesh.CreateShapeFromPolygon(polygonPoints, 0f, false);

            // Apply material
            if (corridorMaterial != null || floorMaterial != null)
            {
                var renderer = floorObj.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = corridorMaterial != null ? corridorMaterial : floorMaterial;
                }
            }

            pbMesh.ToMesh();
            pbMesh.Refresh();

            // Create corridor walls (optional - you can add this if needed)
            CreateCorridorWalls(corridorObj, polygonPoints, wallHeight);
        }

        /// <summary>
        /// Create walls for a corridor
        /// </summary>
        private void CreateCorridorWalls(GameObject parent, Vector3[] floorPoints, float height)
        {
            // Left wall
            CreateCorridorWallSegment(parent, "Wall_Left",
                floorPoints[0], floorPoints[3], height);

            // Right wall
            CreateCorridorWallSegment(parent, "Wall_Right",
                floorPoints[1], floorPoints[2], height);
        }

        /// <summary>
        /// Create a single corridor wall segment
        /// </summary>
        private void CreateCorridorWallSegment(GameObject parent, string name, Vector3 start, Vector3 end, float height)
        {
            GameObject wallObj = new GameObject(name);
            wallObj.transform.SetParent(parent.transform);
            wallObj.transform.localPosition = Vector3.zero;

            ProBuilderMesh pbMesh = wallObj.AddComponent<ProBuilderMesh>();

            // Create wall polygon
            pbMesh.CreateShapeFromPolygon(
                new Vector3[] { start, end },
                height,
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
        }
    }
}
