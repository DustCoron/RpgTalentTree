using UnityEngine;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Types of markers that can be placed during dungeon generation
    /// These markers are used by the theme system to spawn decorations
    /// </summary>
    public enum MarkerType
    {
        // Doorway markers
        DoorwayLeft,          // Left side of doorway entrance
        DoorwayRight,         // Right side of doorway entrance
        DoorwayTop,           // Above doorway (for hanging decorations)

        // Room markers
        RoomFloorCenter,      // Center of room floor
        RoomCeilingCenter,    // Center of room ceiling
        RoomCorner,           // Corner of room
        RoomWallMid,          // Mid-point of a wall segment

        // Corridor markers
        CorridorFloor,        // Along corridor floor (for props)
        CorridorWall,         // Along corridor walls
        CorridorIntersection, // Crossroad intersection point

        // Stair markers
        StairBottom,          // Bottom of staircase
        StairTop,             // Top of staircase
        StairMid,             // Middle of staircase (for lighting)

        // Special markers
        SpawnPoint,           // Potential player/enemy spawn
        TreasureSpot,         // Potential treasure location
        LightPoint            // Suggested light placement
    }

    /// <summary>
    /// Marker placed during dungeon generation for decoration and gameplay elements
    /// </summary>
    public class DungeonMarker
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public MarkerType Type;
        public DungeonRoom Room;  // Reference to parent room (if applicable)
        public float Scale = 1f;  // Suggested scale for spawned objects

        public DungeonMarker(MarkerType type, Vector3 position, Quaternion rotation, DungeonRoom room = null, float scale = 1f)
        {
            Type = type;
            Position = position;
            Rotation = rotation;
            Room = room;
            Scale = scale;
        }

        /// <summary>
        /// Create a marker with forward direction calculated from normal
        /// </summary>
        public static DungeonMarker CreateWithNormal(MarkerType type, Vector3 position, Vector3 normal, DungeonRoom room = null)
        {
            Quaternion rotation = Quaternion.LookRotation(normal);
            return new DungeonMarker(type, position, rotation, room);
        }

        /// <summary>
        /// Create a marker with default identity rotation
        /// </summary>
        public static DungeonMarker CreateSimple(MarkerType type, Vector3 position, DungeonRoom room = null)
        {
            return new DungeonMarker(type, position, Quaternion.identity, room);
        }
    }
}
