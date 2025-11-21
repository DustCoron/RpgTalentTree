using System.Collections.Generic;
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
    /// Enhanced with CodeRespawn patterns: metadata, hierarchy, and unique IDs
    /// </summary>
    public class DungeonMarker
    {
        private static int nextId = 0;

        public int Id { get; private set; }
        public Vector3 Position;
        public Quaternion Rotation;
        public MarkerType Type;
        public DungeonRoom Room;
        public float Scale = 1f;

        // Hierarchical markers (CodeRespawn pattern)
        public DungeonMarker Parent { get; set; }
        public List<DungeonMarker> Children { get; private set; } = new List<DungeonMarker>();

        // Metadata for custom data (CodeRespawn pattern)
        public Dictionary<string, object> Metadata { get; private set; } = new Dictionary<string, object>();

        public DungeonMarker(MarkerType type, Vector3 position, Quaternion rotation, DungeonRoom room = null, float scale = 1f)
        {
            Id = nextId++;
            Type = type;
            Position = position;
            Rotation = rotation;
            Room = room;
            Scale = scale;
        }

        public void AddChild(DungeonMarker child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public void SetMetadata(string key, object value) => Metadata[key] = value;
        public T GetMetadata<T>(string key, T defaultValue = default) =>
            Metadata.TryGetValue(key, out var val) ? (T)val : defaultValue;

        public static DungeonMarker CreateWithNormal(MarkerType type, Vector3 position, Vector3 normal, DungeonRoom room = null)
        {
            Quaternion rotation = Quaternion.LookRotation(normal);
            return new DungeonMarker(type, position, rotation, room);
        }

        public static DungeonMarker CreateSimple(MarkerType type, Vector3 position, DungeonRoom room = null)
        {
            return new DungeonMarker(type, position, Quaternion.identity, room);
        }

        public static void ResetIdCounter() => nextId = 0;
    }

    /// <summary>
    /// Spatial partitioning for efficient marker queries (CodeRespawn pattern)
    /// </summary>
    public class MarkerSpatialIndex
    {
        private Dictionary<Vector3Int, List<DungeonMarker>> buckets = new Dictionary<Vector3Int, List<DungeonMarker>>();
        private float cellSize;

        public MarkerSpatialIndex(float cellSize = 5f)
        {
            this.cellSize = cellSize;
        }

        private Vector3Int GetCell(Vector3 position)
        {
            return new Vector3Int(
                Mathf.FloorToInt(position.x / cellSize),
                Mathf.FloorToInt(position.y / cellSize),
                Mathf.FloorToInt(position.z / cellSize)
            );
        }

        public void Add(DungeonMarker marker)
        {
            var cell = GetCell(marker.Position);
            if (!buckets.ContainsKey(cell))
                buckets[cell] = new List<DungeonMarker>();
            buckets[cell].Add(marker);
        }

        public List<DungeonMarker> GetMarkersInRadius(Vector3 center, float radius)
        {
            var result = new List<DungeonMarker>();
            int cellRadius = Mathf.CeilToInt(radius / cellSize);
            var centerCell = GetCell(center);

            for (int x = -cellRadius; x <= cellRadius; x++)
            for (int y = -cellRadius; y <= cellRadius; y++)
            for (int z = -cellRadius; z <= cellRadius; z++)
            {
                var cell = new Vector3Int(centerCell.x + x, centerCell.y + y, centerCell.z + z);
                if (buckets.TryGetValue(cell, out var markers))
                {
                    foreach (var m in markers)
                        if (Vector3.Distance(m.Position, center) <= radius)
                            result.Add(m);
                }
            }
            return result;
        }

        public List<DungeonMarker> GetMarkersOfType(MarkerType type)
        {
            var result = new List<DungeonMarker>();
            foreach (var bucket in buckets.Values)
                foreach (var m in bucket)
                    if (m.Type == type)
                        result.Add(m);
            return result;
        }

        public void Clear()
        {
            buckets.Clear();
        }
    }
}
