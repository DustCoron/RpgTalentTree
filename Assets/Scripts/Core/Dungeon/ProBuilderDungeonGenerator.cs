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

        // Track corridor segments for crossroad detection
        private struct CorridorSegment
        {
            public Vector3 Start;
            public Vector3 End;
            public bool IsHorizontalX; // true if corridor runs along X axis
            public bool IsHorizontalZ; // true if corridor runs along Z axis

            public CorridorSegment(Vector3 start, Vector3 end)
            {
                Start = start;
                End = end;
                Vector3 dir = end - start;
                IsHorizontalX = Mathf.Abs(dir.x) > Mathf.Abs(dir.z);
                IsHorizontalZ = Mathf.Abs(dir.z) > Mathf.Abs(dir.x);
            }
        }

        private List<CorridorSegment> corridorSegments = new List<CorridorSegment>();
        private List<Vector3> crossroadPositions = new List<Vector3>();
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
            GenerateCorridorsAndDoorways();
            CreateRoomMeshes();
            GenerateMarkers();
            AddDecorations();
            OptimizeMeshes();
            Debug.Log($"Dungeon generated with {rooms.Count} rooms, {dungeonMarkers.Count} markers");
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
            corridorSegments.Clear();
            crossroadPositions.Clear();
            dungeonMarkers.Clear();
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
            // Get best exit points from each room (always facing outward)
            var (wallA, exitA, dirA) = roomA.GetBestConnectionPoint(roomB);
            var (wallB, exitB, dirB) = roomB.GetBestConnectionPoint(roomA);

            // Add doorways at the exit points
            roomA.AddDoorway(exitA, corridorWidth);
            roomB.AddDoorway(exitB, corridorWidth);

            // Track room connectivity
            roomA.ConnectTo(roomB);

            // Determine if walls are on same axis
            bool wallAIsNS = (wallA == Doorway.WallSide.North || wallA == Doorway.WallSide.South);
            bool wallBIsNS = (wallB == Doorway.WallSide.North || wallB == Doorway.WallSide.South);

            if (wallAIsNS == wallBIsNS)
            {
                // Both walls on same axis - may need simple L or straight corridor
                if (wallAIsNS)
                {
                    // Both on Z axis
                    if (Mathf.Abs(exitA.x - exitB.x) < corridorWidth)
                    {
                        // Aligned - straight corridor
                        CreateCorridor(exitA, exitB, corridorIndex, "Straight");
                    }
                    else
                    {
                        // L-shape: go Z first, then X
                        float midZ = (exitA.z + exitB.z) / 2f;
                        Vector3 corner1 = new Vector3(exitA.x, exitA.y, midZ);
                        Vector3 corner2 = new Vector3(exitB.x, exitA.y, midZ);
                        CreateCorridor(exitA, corner1, corridorIndex, "Segment1");
                        CreateCorridor(corner1, corner2, corridorIndex, "Segment2");
                        CreateCorridor(corner2, exitB, corridorIndex, "Segment3");
                        CreateCorridorCorner(corner1, corridorIndex);
                        CreateCorridorCorner(corner2, corridorIndex);
                    }
                }
                else
                {
                    // Both on X axis
                    if (Mathf.Abs(exitA.z - exitB.z) < corridorWidth)
                    {
                        // Aligned - straight corridor
                        CreateCorridor(exitA, exitB, corridorIndex, "Straight");
                    }
                    else
                    {
                        // L-shape: go X first, then Z
                        float midX = (exitA.x + exitB.x) / 2f;
                        Vector3 corner1 = new Vector3(midX, exitA.y, exitA.z);
                        Vector3 corner2 = new Vector3(midX, exitA.y, exitB.z);
                        CreateCorridor(exitA, corner1, corridorIndex, "Segment1");
                        CreateCorridor(corner1, corner2, corridorIndex, "Segment2");
                        CreateCorridor(corner2, exitB, corridorIndex, "Segment3");
                        CreateCorridorCorner(corner1, corridorIndex);
                        CreateCorridorCorner(corner2, corridorIndex);
                    }
                }
            }
            else
            {
                // Walls on perpendicular axes - simple L-shape
                Vector3 corner;
                if (wallAIsNS)
                {
                    // A exits on Z, B exits on X -> corner at (exitA.x, y, exitB.z)
                    corner = new Vector3(exitA.x, exitA.y, exitB.z);
                }
                else
                {
                    // A exits on X, B exits on Z -> corner at (exitB.x, y, exitA.z)
                    corner = new Vector3(exitB.x, exitA.y, exitA.z);
                }
                CreateCorridor(exitA, corner, corridorIndex, "ToCorner");
                CreateCorridor(corner, exitB, corridorIndex, "FromCorner");
                CreateCorridorCorner(corner, corridorIndex);
            }
        }

        /// <summary>
        /// Connect two rooms at different heights with stairs
        /// </summary>
        private void ConnectRoomsWithStairs(DungeonRoom roomA, DungeonRoom roomB, int corridorIndex, Vector3 startPos, Vector3 endPos)
        {
            // Get best exit points from each room (always facing outward)
            var (wallA, exitA, dirA) = roomA.GetBestConnectionPoint(roomB);
            var (wallB, exitB, dirB) = roomB.GetBestConnectionPoint(roomA);

            // Add doorways at the exit points
            roomA.AddDoorway(exitA, corridorWidth);
            roomB.AddDoorway(exitB, corridorWidth);

            // Track room connectivity
            roomA.ConnectTo(roomB);

            // Calculate stairs position - midpoint between exits
            Vector3 stairsStart = new Vector3((exitA.x + exitB.x) / 2f, exitA.y, (exitA.z + exitB.z) / 2f);
            Vector3 stairsEnd = new Vector3(stairsStart.x, exitB.y, stairsStart.z);

            // Create corridor from roomA exit to stairs (going outward)
            CreateCorridor(exitA, stairsStart, corridorIndex, "ToStairs");

            // Create corner at stairs start if corridor changes direction
            if (Mathf.Abs(exitA.x - stairsStart.x) > 0.1f && Mathf.Abs(exitA.z - stairsStart.z) > 0.1f)
            {
                CreateCorridorCorner(stairsStart, corridorIndex);
            }

            // Create stairs from lower to higher level
            if (stairsGenerator != null)
            {
                stairsGenerator.CreateStairs(stairsStart, stairsEnd, dungeonParent.transform, corridorIndex);
            }

            // Create corridor from stairs end to roomB exit (going outward from B)
            CreateCorridor(stairsEnd, exitB, corridorIndex, "FromStairs");

            // Create corner at stairs end if corridor changes direction
            if (Mathf.Abs(stairsEnd.x - exitB.x) > 0.1f && Mathf.Abs(stairsEnd.z - exitB.z) > 0.1f)
            {
                CreateCorridorCorner(stairsEnd, corridorIndex);
            }
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
        /// Create a corridor segment between two points, detecting and creating crossroads if needed
        /// </summary>
        private void CreateCorridor(Vector3 start, Vector3 end, int corridorIndex, string direction)
        {
            // Check for intersections with existing corridors
            Vector3? intersectionPoint = FindCorridorIntersection(start, end);

            if (intersectionPoint.HasValue)
            {
                Vector3 crossroad = intersectionPoint.Value;

                // Check if we already have a crossroad at this position
                bool crossroadExists = false;
                foreach (var existingCrossroad in crossroadPositions)
                {
                    if (Vector3.Distance(existingCrossroad, crossroad) < corridorWidth * 0.5f)
                    {
                        crossroadExists = true;
                        crossroad = existingCrossroad; // Use existing position
                        break;
                    }
                }

                // Create corridor segments from start to crossroad and crossroad to end
                if (Vector3.Distance(start, crossroad) > 0.5f)
                {
                    corridorGenerator.CreateCorridor(start, crossroad, dungeonParent.transform, corridorIndex, direction + "_ToCrossroad");
                    corridorSegments.Add(new CorridorSegment(start, crossroad));
                }

                if (Vector3.Distance(crossroad, end) > 0.5f)
                {
                    corridorGenerator.CreateCorridor(crossroad, end, dungeonParent.transform, corridorIndex, direction + "_FromCrossroad");
                    corridorSegments.Add(new CorridorSegment(crossroad, end));
                }

                // Create crossroad piece if it doesn't exist yet
                if (!crossroadExists)
                {
                    CreateCrossroad(crossroad, corridorIndex);
                    crossroadPositions.Add(crossroad);
                }
            }
            else
            {
                // No intersection, create normal corridor
                corridorGenerator.CreateCorridor(start, end, dungeonParent.transform, corridorIndex, direction);
                corridorSegments.Add(new CorridorSegment(start, end));
            }
        }

        /// <summary>
        /// Find intersection point between a new corridor and existing corridors
        /// </summary>
        private Vector3? FindCorridorIntersection(Vector3 start, Vector3 end)
        {
            CorridorSegment newSegment = new CorridorSegment(start, end);
            float halfWidth = corridorWidth / 2f;

            foreach (var existing in corridorSegments)
            {
                // Check if corridors are perpendicular (one horizontal X, one horizontal Z)
                if (newSegment.IsHorizontalX && existing.IsHorizontalZ)
                {
                    // New corridor runs along X, existing runs along Z
                    float intersectX = existing.Start.x;
                    float intersectZ = newSegment.Start.z;
                    float intersectY = Mathf.Max(newSegment.Start.y, existing.Start.y); // Use higher Y

                    // Check if intersection point is within both corridor bounds
                    if (IsPointOnSegment(new Vector3(intersectX, intersectY, intersectZ), newSegment, halfWidth) &&
                        IsPointOnSegment(new Vector3(intersectX, intersectY, intersectZ), existing, halfWidth))
                    {
                        return new Vector3(intersectX, intersectY, intersectZ);
                    }
                }
                else if (newSegment.IsHorizontalZ && existing.IsHorizontalX)
                {
                    // New corridor runs along Z, existing runs along X
                    float intersectX = newSegment.Start.x;
                    float intersectZ = existing.Start.z;
                    float intersectY = Mathf.Max(newSegment.Start.y, existing.Start.y);

                    if (IsPointOnSegment(new Vector3(intersectX, intersectY, intersectZ), newSegment, halfWidth) &&
                        IsPointOnSegment(new Vector3(intersectX, intersectY, intersectZ), existing, halfWidth))
                    {
                        return new Vector3(intersectX, intersectY, intersectZ);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Check if a point lies on a corridor segment (with tolerance)
        /// </summary>
        private bool IsPointOnSegment(Vector3 point, CorridorSegment segment, float tolerance)
        {
            Vector3 min = Vector3.Min(segment.Start, segment.End);
            Vector3 max = Vector3.Max(segment.Start, segment.End);

            return point.x >= min.x - tolerance && point.x <= max.x + tolerance &&
                   point.z >= min.z - tolerance && point.z <= max.z + tolerance &&
                   Mathf.Abs(point.y - segment.Start.y) < 0.1f; // Same floor level
        }

        /// <summary>
        /// Create a crossroad piece where corridors intersect
        /// </summary>
        private void CreateCrossroad(Vector3 position, int corridorIndex)
        {
            // Create a larger square piece for the crossroad
            GameObject crossroadObj = new GameObject($"Crossroad_{corridorIndex}");
            crossroadObj.transform.SetParent(dungeonParent.transform);
            crossroadObj.transform.position = position;

            float crossroadSize = corridorWidth * 1.5f; // Make crossroad slightly larger
            float halfSize = crossroadSize / 2f;

            // Create floor for crossroad using ProBuilder cube
            ProBuilderMesh floorMesh = ShapeGenerator.GenerateCube(
                PivotLocation.Center,
                new Vector3(crossroadSize, 0.1f, crossroadSize)
            );

            GameObject floorObj = floorMesh.gameObject;
            floorObj.name = "CrossroadFloor";
            floorObj.transform.SetParent(crossroadObj.transform);
            floorObj.transform.localPosition = new Vector3(0, 0.05f, 0);

            // Apply material
            var renderer = floorMesh.GetComponent<MeshRenderer>();
            if (renderer != null && corridorMaterial != null)
            {
                renderer.sharedMaterial = corridorMaterial;
            }

            floorMesh.ToMesh();
            floorMesh.Refresh();

            Debug.Log($"Created crossroad at {position}");
        }

        /// <summary>
        /// Create a corner piece at corridor junction
        /// </summary>
        private void CreateCorridorCorner(Vector3 cornerPosition, int corridorIndex)
        {
            corridorGenerator.CreateCorridorCorner(cornerPosition, dungeonParent.transform, corridorIndex);
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

            // Corridor intersection markers
            foreach (var crossroadPos in crossroadPositions)
            {
                dungeonMarkers.Add(DungeonMarker.CreateSimple(MarkerType.CorridorIntersection, crossroadPos));
            }

            // Corridor floor markers (along corridor segments)
            foreach (var segment in corridorSegments)
            {
                Vector3 midPoint = (segment.Start + segment.End) / 2f;
                dungeonMarkers.Add(DungeonMarker.CreateSimple(MarkerType.CorridorFloor, midPoint));
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
