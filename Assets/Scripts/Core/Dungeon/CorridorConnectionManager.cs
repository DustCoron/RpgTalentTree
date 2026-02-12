using System.Collections.Generic;
using UnityEngine;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Manages corridor connections between dungeon rooms.
    /// Handles BSP spanning tree connections, connectivity verification,
    /// and extra loop connections for maze effect.
    /// </summary>
    public class CorridorConnectionManager
    {
        private readonly List<DungeonRoom> rooms;
        private readonly CorridorGenerator corridorGenerator;
        private readonly StairsGenerator stairsGenerator;
        private readonly System.Random random;

        private readonly int maxCorridorsPerRoom;
        private readonly float extraConnectionChance;
        private readonly int corridorWidth;
        private readonly bool useHardCorners;
        private readonly int segmentsPerUnit;
        private readonly bool enableStairs;

        private Transform parent;

        public CorridorConnectionManager(
            List<DungeonRoom> rooms,
            CorridorGenerator corridorGenerator,
            StairsGenerator stairsGenerator,
            System.Random random,
            int maxCorridorsPerRoom,
            float extraConnectionChance,
            int corridorWidth,
            bool useHardCorners,
            int segmentsPerUnit,
            bool enableStairs)
        {
            this.rooms = rooms;
            this.corridorGenerator = corridorGenerator;
            this.stairsGenerator = stairsGenerator;
            this.random = random;
            this.maxCorridorsPerRoom = maxCorridorsPerRoom;
            this.extraConnectionChance = extraConnectionChance;
            this.corridorWidth = corridorWidth;
            this.useHardCorners = useHardCorners;
            this.segmentsPerUnit = segmentsPerUnit;
            this.enableStairs = enableStairs;
        }

        /// <summary>
        /// Generate all corridors using a three-phase system:
        /// 1. BSP spanning tree (mandatory)
        /// 2. Connectivity verification (fix isolates)
        /// 3. Extra connections (maze loops)
        /// </summary>
        public void GenerateCorridorsAndDoorways(BSPNode bspRoot, Transform parentTransform)
        {
            if (rooms.Count < 2 || bspRoot == null)
                return;

            parent = parentTransform;
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

            if (node.LeftChild != null)
                ConnectBSPNodes(node.LeftChild, ref corridorIndex);
            if (node.RightChild != null)
                ConnectBSPNodes(node.RightChild, ref corridorIndex);

            if (node.LeftChild != null && node.RightChild != null)
            {
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
        /// Connect two rooms with a corridor (with optional stairs).
        /// mandatory=true for BSP spanning tree connections (ignores maxCorridorsPerRoom).
        /// </summary>
        private void ConnectRooms(DungeonRoom roomA, DungeonRoom roomB, int corridorIndex, bool mandatory)
        {
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
        private void ConnectRoomsSameLevel(DungeonRoom roomA, DungeonRoom roomB, int corridorIndex,
            Vector3 startPos, Vector3 endPos, bool mandatory = false)
        {
            if (!CanAddCorridor(roomA, mandatory) || !CanAddCorridor(roomB, mandatory))
            {
                if (!mandatory) return;
                Debug.Log($"Skipping corridor {corridorIndex}: no available walls");
                return;
            }

            var connectionA = roomA.GetBestConnectionPoint(roomB);
            var connectionB = roomB.GetBestConnectionPoint(roomA);

            if (!connectionA.HasValue || !connectionB.HasValue)
            {
                Debug.Log($"Skipping corridor {corridorIndex}: no available walls");
                return;
            }

            var (wallA, exitA, dirA) = connectionA.Value;
            var (wallB, exitB, dirB) = connectionB.Value;

            roomA.ReserveWall(wallA);
            roomB.ReserveWall(wallB);
            roomA.AddDoorway(exitA, corridorWidth);
            roomB.AddDoorway(exitB, corridorWidth);
            roomA.ConnectTo(roomB);

            if (useHardCorners)
            {
                corridorGenerator.CreateLShapedCorridor(exitA, dirA, exitB, dirB,
                    parent, corridorIndex, segmentsPerUnit);
            }
            else
            {
                corridorGenerator.CreateSplineCorridor(exitA, dirA, exitB, dirB,
                    parent, corridorIndex, segmentsPerUnit * 4);
            }
        }

        /// <summary>
        /// Connect two rooms at different heights with stairs
        /// </summary>
        private void ConnectRoomsWithStairs(DungeonRoom roomA, DungeonRoom roomB, int corridorIndex,
            Vector3 startPos, Vector3 endPos, bool mandatory = false)
        {
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

            Vector3 stairsStart = new Vector3((exitA.x + exitB.x) / 2f, exitA.y, (exitA.z + exitB.z) / 2f);
            Vector3 stairsEnd = new Vector3(stairsStart.x, exitB.y, stairsStart.z);
            Vector3 toStairsA = (stairsStart - exitA).normalized;
            Vector3 toStairsB = (stairsEnd - exitB).normalized;

            if (useHardCorners)
            {
                corridorGenerator.CreateLShapedCorridor(exitA, dirA, stairsStart, -toStairsA,
                    parent, corridorIndex, segmentsPerUnit);
                if (stairsGenerator != null)
                    stairsGenerator.CreateStairs(stairsStart, stairsEnd, parent, corridorIndex);
                corridorGenerator.CreateLShapedCorridor(stairsEnd, toStairsB, exitB, dirB,
                    parent, corridorIndex, segmentsPerUnit);
            }
            else
            {
                corridorGenerator.CreateSplineCorridor(exitA, dirA, stairsStart, -toStairsA,
                    parent, corridorIndex, segmentsPerUnit * 2);
                if (stairsGenerator != null)
                    stairsGenerator.CreateStairs(stairsStart, stairsEnd, parent, corridorIndex);
                corridorGenerator.CreateSplineCorridor(stairsEnd, toStairsB, exitB, dirB,
                    parent, corridorIndex, segmentsPerUnit * 2);
            }
        }

        /// <summary>
        /// Check if room can accept more corridors.
        /// mandatory=true bypasses maxCorridorsPerRoom (still needs a free wall).
        /// </summary>
        private bool CanAddCorridor(DungeonRoom room, bool mandatory = false)
        {
            if (room.GetAvailableWallCount() == 0) return false;
            if (!mandatory && room.ConnectedRoomIds.Count >= maxCorridorsPerRoom) return false;
            return true;
        }

        /// <summary>
        /// BFS connectivity check - find and fix any disconnected rooms.
        /// Guarantees every room is reachable from every other room.
        /// </summary>
        private void EnsureFullConnectivity(ref int corridorIndex)
        {
            if (rooms.Count < 2) return;

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

            List<DungeonRoom> unreachable = rooms.FindAll(r => !visited.Contains(r.Id));
            while (unreachable.Count > 0)
            {
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

                ConnectRooms(bestUnreachable, bestReachable, corridorIndex++, true);

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
        /// </summary>
        private void AddExtraConnections(ref int corridorIndex)
        {
            if (extraConnectionChance <= 0f || rooms.Count < 3) return;

            List<(DungeonRoom a, DungeonRoom b, float dist)> pairs =
                new List<(DungeonRoom, DungeonRoom, float)>();

            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    float dist = Vector3.Distance(rooms[i].GetCenter(), rooms[j].GetCenter());
                    pairs.Add((rooms[i], rooms[j], dist));
                }
            }
            pairs.Sort((a, b) => a.dist.CompareTo(b.dist));

            foreach (var (roomA, roomB, dist) in pairs)
            {
                if (roomA.ConnectedRoomIds.Contains(roomB.Id)) continue;
                if (!CanAddCorridor(roomA) || !CanAddCorridor(roomB)) continue;
                if (random.NextDouble() > extraConnectionChance) continue;

                ConnectRooms(roomA, roomB, corridorIndex++, false);
            }
        }
    }
}
