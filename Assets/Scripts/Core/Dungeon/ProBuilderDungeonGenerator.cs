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
        [SerializeField] private int dungeonWidth = 80;
        [SerializeField] private int dungeonDepth = 80;
        [SerializeField] private int bspDepth = 4;
        [SerializeField] private int minPartitionSize = 12;
        [SerializeField] private Vector2Int minRoomSize = new Vector2Int(4, 4);
        [SerializeField] private Vector2Int maxRoomSize = new Vector2Int(10, 10);
        [SerializeField] private float wallHeight = 3f;
        [SerializeField] private float wallThickness = 0.2f;
        [SerializeField] private int corridorWidth = 2;

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
        private BSPNode bspRoot;

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

            foreach (var leaf in leaves)
            {
                // Generate random floor height if multi-level is enabled
                float floorHeight = 0f;
                if (enableMultiLevel)
                {
                    int heightLevels = Mathf.FloorToInt((maxFloorHeight - minFloorHeight) / floorHeightStep) + 1;
                    int randomLevel = random.Next(0, heightLevels);
                    floorHeight = minFloorHeight + (randomLevel * floorHeightStep);
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
            ConnectBSPNodes(bspRoot, ref corridorIndex);

            Debug.Log($"Created {corridorIndex} corridor connections using BSP tree");
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
                    ConnectRooms(leftRoom, rightRoom, corridorIndex++);
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

            // Find doorway positions on room boundaries
            Vector3 doorwayA = FindRoomBoundaryIntersection(roomA, startPos, cornerPos);
            roomA.AddDoorway(doorwayA, corridorWidth);

            Vector3 doorwayB = FindRoomBoundaryIntersection(roomB, endPos, cornerPos);
            roomB.AddDoorway(doorwayB, corridorWidth);

            // Calculate corridor corner position
            Vector3 corridorCorner = new Vector3(doorwayB.x, doorwayA.y, doorwayA.z);

            // Create corridors from doorway to doorway
            CreateCorridor(doorwayA, corridorCorner, corridorIndex, "Horizontal");
            CreateCorridor(corridorCorner, doorwayB, corridorIndex, "Vertical");
        }

        /// <summary>
        /// Connect two rooms at different heights with stairs
        /// </summary>
        private void ConnectRoomsWithStairs(DungeonRoom roomA, DungeonRoom roomB, int corridorIndex, Vector3 startPos, Vector3 endPos)
        {
            // Calculate midpoint for stairs location
            Vector3 midPoint = new Vector3((startPos.x + endPos.x) / 2f, startPos.y, (startPos.z + endPos.z) / 2f);
            Vector3 stairsStart = new Vector3(midPoint.x, startPos.y, midPoint.z);
            Vector3 stairsEnd = new Vector3(midPoint.x, endPos.y, midPoint.z);

            // Find doorway positions for both rooms
            Vector3 doorwayA = FindRoomBoundaryIntersection(roomA, startPos, stairsStart);
            roomA.AddDoorway(doorwayA, corridorWidth);

            Vector3 doorwayB = FindRoomBoundaryIntersection(roomB, endPos, stairsEnd);
            roomB.AddDoorway(doorwayB, corridorWidth);

            // Create corridor from roomA doorway to stairs start
            CreateCorridor(doorwayA, stairsStart, corridorIndex, "ToStairs");

            // Create stairs from lower to higher level
            if (stairsGenerator != null)
            {
                stairsGenerator.CreateStairs(stairsStart, stairsEnd, dungeonParent.transform, corridorIndex);
            }

            // Create corridor from stairs end to roomB doorway
            CreateCorridor(stairsEnd, doorwayB, corridorIndex, "FromStairs");
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
            stepHeight = Mathf.Clamp(stepHeight, 0.1f, 0.5f);
            stepDepth = Mathf.Clamp(stepDepth, 0.3f, 1f);
        }
    }
}
