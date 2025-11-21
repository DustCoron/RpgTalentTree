using System.Collections.Generic;
using UnityEngine;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Represents a doorway in a room wall
    /// </summary>
    public struct Doorway
    {
        public enum WallSide { North, South, East, West }

        public WallSide Wall { get; set; }
        public Vector3 Position { get; set; }
        public float Width { get; set; }

        public Doorway(WallSide wall, Vector3 position, float width)
        {
            Wall = wall;
            Position = position;
            Width = width;
        }
    }

    /// <summary>
    /// Room type for gameplay logic (CodeRespawn pattern)
    /// </summary>
    public enum RoomType
    {
        Normal,
        Spawn,
        Boss,
        Treasure,
        Secret
    }

    /// <summary>
    /// Represents a single room in the dungeon
    /// Enhanced with CodeRespawn patterns: unique ID, connectivity, room types
    /// </summary>
    public class DungeonRoom
    {
        private static int nextId = 0;

        public int Id { get; private set; }
        public Vector3Int Position { get; set; }
        public Vector3Int Size { get; set; }
        public GameObject RoomObject { get; set; }
        public List<Doorway> Doorways { get; set; }
        public float FloorHeight { get; set; }

        // Room connectivity (CodeRespawn pattern)
        public List<int> ConnectedRoomIds { get; private set; } = new List<int>();
        public RoomType Type { get; set; } = RoomType.Normal;

        // Metadata for custom data
        public Dictionary<string, object> Metadata { get; private set; } = new Dictionary<string, object>();

        public DungeonRoom(Vector3Int position, Vector3Int size, float floorHeight = 0f)
        {
            Id = nextId++;
            Position = position;
            Size = size;
            FloorHeight = floorHeight;
            Doorways = new List<Doorway>();
        }

        public void ConnectTo(DungeonRoom other)
        {
            if (!ConnectedRoomIds.Contains(other.Id))
                ConnectedRoomIds.Add(other.Id);
            if (!other.ConnectedRoomIds.Contains(Id))
                other.ConnectedRoomIds.Add(Id);
        }

        public static void ResetIdCounter() => nextId = 0;

        /// <summary>
        /// Get the center position of the room in world space (at floor level)
        /// </summary>
        public Vector3 GetCenter()
        {
            return new Vector3(
                Position.x + Size.x / 2f,
                FloorHeight,
                Position.z + Size.z / 2f
            );
        }

        /// <summary>
        /// Get the floor position at a specific local XZ coordinate
        /// </summary>
        public Vector3 GetFloorPosition(float localX, float localZ)
        {
            return new Vector3(
                Position.x + localX,
                FloorHeight,
                Position.z + localZ
            );
        }

        /// <summary>
        /// Check if this room overlaps with another room (with padding)
        /// </summary>
        public bool Overlaps(DungeonRoom other, int padding = 1)
        {
            return Position.x < other.Position.x + other.Size.x + padding &&
                   Position.x + Size.x + padding > other.Position.x &&
                   Position.z < other.Position.z + other.Size.z + padding &&
                   Position.z + Size.z + padding > other.Position.z;
        }

        /// <summary>
        /// Add a doorway to the room at a specific world position
        /// </summary>
        public void AddDoorway(Vector3 worldPosition, float width)
        {
            // Convert world position to local position
            Vector3 localPos = worldPosition - Position;

            // Determine which wall the doorway is on based on proximity
            Doorway.WallSide wall = DetermineWallSide(localPos);

            Doorways.Add(new Doorway(wall, localPos, width));
        }

        /// <summary>
        /// Determine which wall a position is closest to
        /// </summary>
        private Doorway.WallSide DetermineWallSide(Vector3 localPos)
        {
            float distToNorth = Mathf.Abs(localPos.z - Size.z);
            float distToSouth = Mathf.Abs(localPos.z);
            float distToEast = Mathf.Abs(localPos.x - Size.x);
            float distToWest = Mathf.Abs(localPos.x);

            float minDist = Mathf.Min(distToNorth, distToSouth, distToEast, distToWest);

            if (minDist == distToNorth) return Doorway.WallSide.North;
            if (minDist == distToSouth) return Doorway.WallSide.South;
            if (minDist == distToEast) return Doorway.WallSide.East;
            return Doorway.WallSide.West;
        }
    }
}
