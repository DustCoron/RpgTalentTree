using System.Collections.Generic;
using UnityEngine;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Validates dungeon navigability and sets up start/end points.
    /// Ensures the dungeon is always solvable as a maze (guaranteed path from entrance to exit).
    /// Also sets up MeshColliders on all geometry so player physics works.
    /// </summary>
    public static class DungeonValidator
    {
        /// <summary>
        /// Result of a full dungeon validation pass
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid;
            public DungeonRoom StartRoom;
            public DungeonRoom EndRoom;
            public List<DungeonRoom> CriticalPath;
            public int CriticalPathLength;
            public int TotalRooms;
            public int ReachableRooms;
            public int UnreachableRooms;
            public int CollidersAdded;
            public List<string> Warnings = new List<string>();
        }

        /// <summary>
        /// Run full validation: assign start/end, verify path, check connectivity, setup colliders.
        /// This is the main entry point - call after dungeon generation is complete.
        /// </summary>
        public static ValidationResult Validate(List<DungeonRoom> rooms, Transform dungeonParent, int corridorWidth)
        {
            var result = new ValidationResult
            {
                TotalRooms = rooms.Count,
                IsValid = false
            };

            if (rooms.Count < 2)
            {
                result.Warnings.Add("Not enough rooms for start/end assignment");
                return result;
            }

            // Phase 1: Verify full connectivity
            var reachable = GetReachableRooms(rooms[0], rooms);
            result.ReachableRooms = reachable.Count;
            result.UnreachableRooms = rooms.Count - reachable.Count;

            if (result.UnreachableRooms > 0)
            {
                result.Warnings.Add($"{result.UnreachableRooms} rooms are unreachable from the main network");
            }

            // Phase 2: Assign start and end rooms (farthest pair by graph distance)
            AssignStartAndEnd(rooms, out DungeonRoom startRoom, out DungeonRoom endRoom);
            result.StartRoom = startRoom;
            result.EndRoom = endRoom;

            // Phase 3: Find and verify the critical path (start -> end)
            var path = FindPath(startRoom, endRoom, rooms);
            if (path != null && path.Count >= 2)
            {
                result.CriticalPath = path;
                result.CriticalPathLength = path.Count;
                result.IsValid = true;
            }
            else
            {
                result.Warnings.Add("CRITICAL: No path found between start and end rooms");
                result.IsValid = false;
            }

            // Phase 4: Validate corridor walkability (check each connection has valid geometry)
            ValidateCorridorWalkability(rooms, corridorWidth, result);

            // Phase 5: Setup MeshColliders on all geometry
            result.CollidersAdded = SetupColliders(dungeonParent);

            return result;
        }

        // ===================== Start/End Assignment =====================

        /// <summary>
        /// Assign start and end rooms using double-BFS for maximum graph distance.
        /// This makes the player traverse the most of the dungeon to reach the exit.
        /// Start room gets RoomType.Spawn, end room gets RoomType.Boss.
        /// </summary>
        public static void AssignStartAndEnd(List<DungeonRoom> rooms,
            out DungeonRoom startRoom, out DungeonRoom endRoom)
        {
            // Double BFS: finds the two rooms with maximum graph distance (diameter)
            // 1) BFS from any room to find the farthest room -> candidate start
            // 2) BFS from candidate start to find the farthest room -> candidate end
            startRoom = BFSFarthest(rooms[0], rooms);
            endRoom = BFSFarthest(startRoom, rooms);

            // If same room (degenerate case), pick any two different rooms
            if (startRoom.Id == endRoom.Id && rooms.Count >= 2)
            {
                startRoom = rooms[0];
                endRoom = rooms[rooms.Count - 1];
            }

            // Mark room types
            startRoom.Type = RoomType.Spawn;
            endRoom.Type = RoomType.Boss;
        }

        /// <summary>
        /// BFS from a start room, return the last room visited (farthest by hop count)
        /// </summary>
        private static DungeonRoom BFSFarthest(DungeonRoom start, List<DungeonRoom> allRooms)
        {
            var visited = new HashSet<int>();
            var queue = new Queue<DungeonRoom>();
            visited.Add(start.Id);
            queue.Enqueue(start);
            DungeonRoom farthest = start;

            while (queue.Count > 0)
            {
                farthest = queue.Dequeue();
                foreach (int connectedId in farthest.ConnectedRoomIds)
                {
                    if (visited.Contains(connectedId)) continue;
                    visited.Add(connectedId);
                    var room = allRooms.Find(r => r.Id == connectedId);
                    if (room != null)
                        queue.Enqueue(room);
                }
            }

            return farthest;
        }

        // ===================== Path Finding =====================

        /// <summary>
        /// BFS pathfinding from start to end room. Returns the ordered list of rooms in the path,
        /// or null if no path exists.
        /// </summary>
        public static List<DungeonRoom> FindPath(DungeonRoom start, DungeonRoom end, List<DungeonRoom> allRooms)
        {
            if (start == null || end == null) return null;
            if (start.Id == end.Id) return new List<DungeonRoom> { start };

            var parent = new Dictionary<int, int>(); // childId -> parentId
            var visited = new HashSet<int>();
            var queue = new Queue<int>();

            visited.Add(start.Id);
            parent[start.Id] = -1;
            queue.Enqueue(start.Id);

            while (queue.Count > 0)
            {
                int currentId = queue.Dequeue();
                var current = allRooms.Find(r => r.Id == currentId);
                if (current == null) continue;

                foreach (int connectedId in current.ConnectedRoomIds)
                {
                    if (visited.Contains(connectedId)) continue;
                    visited.Add(connectedId);
                    parent[connectedId] = currentId;

                    if (connectedId == end.Id)
                    {
                        // Reconstruct path from end back to start
                        var path = new List<DungeonRoom>();
                        int id = end.Id;
                        while (id != -1)
                        {
                            var room = allRooms.Find(r => r.Id == id);
                            if (room != null) path.Add(room);
                            id = parent.ContainsKey(id) ? parent[id] : -1;
                        }
                        path.Reverse();
                        return path;
                    }

                    queue.Enqueue(connectedId);
                }
            }

            return null; // No path found
        }

        // ===================== Connectivity Check =====================

        /// <summary>
        /// Get all rooms reachable from a starting room via BFS
        /// </summary>
        public static HashSet<int> GetReachableRooms(DungeonRoom start, List<DungeonRoom> allRooms)
        {
            var visited = new HashSet<int>();
            var queue = new Queue<DungeonRoom>();
            visited.Add(start.Id);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (int connectedId in current.ConnectedRoomIds)
                {
                    if (visited.Contains(connectedId)) continue;
                    visited.Add(connectedId);
                    var room = allRooms.Find(r => r.Id == connectedId);
                    if (room != null) queue.Enqueue(room);
                }
            }

            return visited;
        }

        // ===================== Walkability Validation =====================

        /// <summary>
        /// Validate that each room connection has proper doorways and reasonable corridor geometry.
        /// Checks doorway count, corridor width, and connection symmetry.
        /// </summary>
        private static void ValidateCorridorWalkability(List<DungeonRoom> rooms, int corridorWidth,
            ValidationResult result)
        {
            foreach (var room in rooms)
            {
                // Check each connected room has symmetric connection
                foreach (int connectedId in room.ConnectedRoomIds)
                {
                    var connected = rooms.Find(r => r.Id == connectedId);
                    if (connected == null)
                    {
                        result.Warnings.Add($"Room {room.Id} references non-existent room {connectedId}");
                        continue;
                    }

                    if (!connected.ConnectedRoomIds.Contains(room.Id))
                    {
                        result.Warnings.Add($"Asymmetric connection: Room {room.Id} -> {connectedId} but not reverse");
                    }
                }

                // Check doorway count matches connections
                if (room.Doorways.Count < room.ConnectedRoomIds.Count)
                {
                    result.Warnings.Add($"Room {room.Id}: has {room.Doorways.Count} doorways but {room.ConnectedRoomIds.Count} connections");
                }

                // Check room is large enough for player to enter
                if (room.Size.x < corridorWidth || room.Size.z < corridorWidth)
                {
                    result.Warnings.Add($"Room {room.Id}: size ({room.Size.x}x{room.Size.z}) is smaller than corridor width ({corridorWidth})");
                }
            }
        }

        // ===================== Collision Setup =====================

        /// <summary>
        /// Add MeshCollider to all mesh objects in the dungeon that don't already have one.
        /// This ensures the player's CharacterController/Rigidbody can collide with dungeon geometry.
        /// Returns the number of colliders added.
        /// </summary>
        public static int SetupColliders(Transform dungeonParent)
        {
            int added = 0;
            var meshFilters = dungeonParent.GetComponentsInChildren<MeshFilter>();

            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;

                // Skip if already has any collider
                if (mf.GetComponent<Collider>() != null) continue;

                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                added++;
            }

            return added;
        }

        /// <summary>
        /// Log a formatted validation report to the console
        /// </summary>
        public static void LogReport(ValidationResult result)
        {
            string status = result.IsValid ? "VALID" : "INVALID";
            Debug.Log($"=== Dungeon Validation: {status} ===");
            Debug.Log($"Rooms: {result.TotalRooms} total, {result.ReachableRooms} reachable, {result.UnreachableRooms} unreachable");

            if (result.StartRoom != null)
                Debug.Log($"Start: Room {result.StartRoom.Id} ({result.StartRoom.Scale}) at {result.StartRoom.GetCenter()}");
            if (result.EndRoom != null)
                Debug.Log($"End: Room {result.EndRoom.Id} ({result.EndRoom.Scale}) at {result.EndRoom.GetCenter()}");
            if (result.CriticalPath != null)
                Debug.Log($"Critical path: {result.CriticalPathLength} rooms");

            Debug.Log($"Colliders added: {result.CollidersAdded}");

            foreach (var warning in result.Warnings)
            {
                Debug.LogWarning($"Dungeon: {warning}");
            }
        }
    }
}
