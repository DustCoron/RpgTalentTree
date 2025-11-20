using UnityEngine;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Represents a single room in the dungeon
    /// </summary>
    public class DungeonRoom
    {
        public Vector3Int Position { get; set; }
        public Vector3Int Size { get; set; }
        public GameObject RoomObject { get; set; }

        public DungeonRoom(Vector3Int position, Vector3Int size)
        {
            Position = position;
            Size = size;
        }

        /// <summary>
        /// Get the center position of the room in world space
        /// </summary>
        public Vector3 GetCenter()
        {
            return new Vector3(
                Position.x + Size.x / 2f,
                Position.y,
                Position.z + Size.z / 2f
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
    }
}
