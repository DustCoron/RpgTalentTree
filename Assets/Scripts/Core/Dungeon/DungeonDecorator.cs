using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Handles dungeon decoration: marker generation, decorative elements, and mesh optimization.
    /// Extracted from ProBuilderDungeonGenerator for cleaner separation of concerns.
    /// </summary>
    public class DungeonDecorator
    {
        private readonly List<DungeonRoom> rooms;
        private readonly CorridorGenerator corridorGenerator;
        private readonly float wallHeight;
        private readonly float wallThickness;
        private readonly int corridorWidth;
        private readonly Material floorMaterial;
        private readonly Material wallMaterial;
        private readonly bool addPillars;
        private readonly bool addTorchHolders;
        private readonly bool addCeilingDecorations;
        private readonly bool combineMeshes;

        public DungeonDecorator(
            List<DungeonRoom> rooms,
            CorridorGenerator corridorGenerator,
            float wallHeight,
            float wallThickness,
            int corridorWidth,
            Material floorMaterial,
            Material wallMaterial,
            bool addPillars,
            bool addTorchHolders,
            bool addCeilingDecorations,
            bool combineMeshes)
        {
            this.rooms = rooms;
            this.corridorGenerator = corridorGenerator;
            this.wallHeight = wallHeight;
            this.wallThickness = wallThickness;
            this.corridorWidth = corridorWidth;
            this.floorMaterial = floorMaterial;
            this.wallMaterial = wallMaterial;
            this.addPillars = addPillars;
            this.addTorchHolders = addTorchHolders;
            this.addCeilingDecorations = addCeilingDecorations;
            this.combineMeshes = combineMeshes;
        }

        // ===================== Marker System =====================

        /// <summary>
        /// Generate markers throughout the dungeon for decoration and gameplay element placement
        /// </summary>
        public List<DungeonMarker> GenerateMarkers()
        {
            var markers = new List<DungeonMarker>();

            foreach (var room in rooms)
            {
                // Room floor center marker
                Vector3 floorCenter = new Vector3(
                    room.Position.x + room.Size.x / 2f,
                    room.FloorHeight,
                    room.Position.z + room.Size.z / 2f
                );
                markers.Add(DungeonMarker.CreateSimple(MarkerType.RoomFloorCenter, floorCenter, room));

                // Room ceiling center marker
                Vector3 ceilingCenter = floorCenter + Vector3.up * wallHeight;
                markers.Add(DungeonMarker.CreateSimple(MarkerType.RoomCeilingCenter, ceilingCenter, room));

                // Room corner markers
                Vector3[] corners =
                {
                    new Vector3(room.Position.x, room.FloorHeight, room.Position.z),
                    new Vector3(room.Position.x + room.Size.x, room.FloorHeight, room.Position.z),
                    new Vector3(room.Position.x + room.Size.x, room.FloorHeight, room.Position.z + room.Size.z),
                    new Vector3(room.Position.x, room.FloorHeight, room.Position.z + room.Size.z)
                };

                foreach (var corner in corners)
                {
                    markers.Add(DungeonMarker.CreateSimple(MarkerType.RoomCorner, corner, room));
                }

                // Doorway markers
                foreach (var doorway in room.Doorways)
                {
                    Vector3 doorwayPos = doorway.Position;
                    Vector3 doorwayNormal = GetDoorwayNormal(doorway.Wall);

                    Vector3 perpendicular = Vector3.Cross(doorwayNormal, Vector3.up);
                    Vector3 leftPos = doorwayPos - perpendicular * (doorway.Width / 2f);
                    Vector3 rightPos = doorwayPos + perpendicular * (doorway.Width / 2f);

                    markers.Add(DungeonMarker.CreateWithNormal(MarkerType.DoorwayLeft, leftPos, doorwayNormal, room));
                    markers.Add(DungeonMarker.CreateWithNormal(MarkerType.DoorwayRight, rightPos, doorwayNormal, room));

                    Vector3 topPos = doorwayPos + Vector3.up * wallHeight * 0.7f;
                    markers.Add(DungeonMarker.CreateWithNormal(MarkerType.DoorwayTop, topPos, doorwayNormal, room));
                }

                // Wall midpoint markers
                EmitWallMarkers(room, markers);
            }

            // Corridor markers from generated paths
            if (corridorGenerator != null)
            {
                foreach (var path in corridorGenerator.GetCorridorPaths())
                {
                    for (int i = 0; i < path.Points.Count - 1; i++)
                    {
                        Vector3 midPoint = (path.Points[i] + path.Points[i + 1]) / 2f;
                        markers.Add(DungeonMarker.CreateSimple(MarkerType.CorridorFloor, midPoint));
                    }
                }

                foreach (var junction in corridorGenerator.GetJunctionPoints())
                {
                    markers.Add(DungeonMarker.CreateSimple(MarkerType.CorridorIntersection, junction));
                }
            }

            Debug.Log($"Generated {markers.Count} markers for decoration");
            return markers;
        }

        private Vector3 GetDoorwayNormal(Doorway.WallSide wallSide)
        {
            return wallSide switch
            {
                Doorway.WallSide.North => Vector3.forward,
                Doorway.WallSide.South => Vector3.back,
                Doorway.WallSide.East => Vector3.right,
                Doorway.WallSide.West => Vector3.left,
                _ => Vector3.forward
            };
        }

        private void EmitWallMarkers(DungeonRoom room, List<DungeonMarker> markers)
        {
            float midY = room.FloorHeight + wallHeight / 2f;

            // North wall
            markers.Add(DungeonMarker.CreateWithNormal(MarkerType.RoomWallMid,
                new Vector3(room.Position.x + room.Size.x / 2f, midY, room.Position.z + room.Size.z),
                Vector3.back, room));

            // South wall
            markers.Add(DungeonMarker.CreateWithNormal(MarkerType.RoomWallMid,
                new Vector3(room.Position.x + room.Size.x / 2f, midY, room.Position.z),
                Vector3.forward, room));

            // East wall
            markers.Add(DungeonMarker.CreateWithNormal(MarkerType.RoomWallMid,
                new Vector3(room.Position.x + room.Size.x, midY, room.Position.z + room.Size.z / 2f),
                Vector3.left, room));

            // West wall
            markers.Add(DungeonMarker.CreateWithNormal(MarkerType.RoomWallMid,
                new Vector3(room.Position.x, midY, room.Position.z + room.Size.z / 2f),
                Vector3.right, room));
        }

        // ===================== Decorations =====================

        /// <summary>
        /// Add decorative elements to all rooms
        /// </summary>
        public void AddDecorations(Transform dungeonParent)
        {
            if (!addPillars && !addTorchHolders && !addCeilingDecorations)
                return;

            foreach (var room in rooms)
            {
                GameObject roomObj = dungeonParent.Find($"Room_{rooms.IndexOf(room)}")?.gameObject;
                if (roomObj == null) continue;

                if (addPillars)
                    AddRoomPillars(roomObj, room);

                if (addTorchHolders)
                    AddWallTorchHolders(roomObj, room);

                if (addCeilingDecorations)
                    AddCeilingDecoration(roomObj, room);
            }
        }

        private void AddRoomPillars(GameObject roomObj, DungeonRoom room)
        {
            float pillarRadius = 0.2f;
            float pillarHeight = wallHeight * 0.9f;

            Vector3[] corners =
            {
                new Vector3(0.5f, 0, 0.5f),
                new Vector3(room.Size.x - 0.5f, 0, 0.5f),
                new Vector3(room.Size.x - 0.5f, 0, room.Size.z - 0.5f),
                new Vector3(0.5f, 0, room.Size.z - 0.5f)
            };

            for (int i = 0; i < corners.Length; i++)
            {
                ProBuilderMesh pillarMesh = ShapeGenerator.GenerateCylinder(
                    PivotLocation.Center, 8, pillarRadius, pillarHeight, 0, -1);

                GameObject pillarObj = pillarMesh.gameObject;
                pillarObj.name = $"Pillar_Corner_{i}";
                pillarObj.transform.SetParent(roomObj.transform);
                pillarObj.transform.localPosition = corners[i] + new Vector3(0, pillarHeight / 2f, 0);

                if (wallMaterial != null)
                {
                    var renderer = pillarMesh.GetComponent<MeshRenderer>();
                    if (renderer != null) renderer.sharedMaterial = wallMaterial;
                }

                pillarMesh.ToMesh();
                pillarMesh.Refresh();
            }
        }

        private void AddWallTorchHolders(GameObject roomObj, DungeonRoom room)
        {
            float torchHeight = wallHeight * 0.6f;
            float torchRadius = 0.15f;
            float torchThickness = 0.05f;
            float wallOffset = 0.2f;

            Vector3[] torchPositions =
            {
                new Vector3(room.Size.x / 2f, torchHeight, wallOffset),
                new Vector3(room.Size.x / 2f, torchHeight, room.Size.z - wallOffset),
                new Vector3(wallOffset, torchHeight, room.Size.z / 2f),
                new Vector3(room.Size.x - wallOffset, torchHeight, room.Size.z / 2f)
            };

            for (int i = 0; i < torchPositions.Length; i++)
            {
                ProBuilderMesh torchMesh = ShapeGenerator.GeneratePipe(
                    PivotLocation.Center, torchRadius, 0.1f, torchThickness, 12, 1);

                GameObject torchObj = torchMesh.gameObject;
                torchObj.name = $"TorchHolder_{i}";
                torchObj.transform.SetParent(roomObj.transform);
                torchObj.transform.localPosition = torchPositions[i];

                if (wallMaterial != null)
                {
                    var renderer = torchMesh.GetComponent<MeshRenderer>();
                    if (renderer != null) renderer.sharedMaterial = wallMaterial;
                }

                torchMesh.ToMesh();
                torchMesh.Refresh();
            }
        }

        private void AddCeilingDecoration(GameObject roomObj, DungeonRoom room)
        {
            float decorationRadius = Mathf.Min(room.Size.x, room.Size.z) * 0.15f;
            Vector3 centerPosition = new Vector3(room.Size.x / 2f, wallHeight - 0.3f, room.Size.z / 2f);

            ProBuilderMesh decorationMesh = ShapeGenerator.GenerateIcosahedron(
                PivotLocation.Center, decorationRadius, 0, true, true);

            GameObject decorationObj = decorationMesh.gameObject;
            decorationObj.name = "CeilingDecoration";
            decorationObj.transform.SetParent(roomObj.transform);
            decorationObj.transform.localPosition = centerPosition;

            if (wallMaterial != null)
            {
                var renderer = decorationMesh.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.sharedMaterial = wallMaterial;
            }

            decorationMesh.ToMesh();
            decorationMesh.Refresh();
        }

        // ===================== Mesh Optimization =====================

        /// <summary>
        /// Optimize dungeon by combining meshes using ProBuilder's CombineMeshes
        /// </summary>
        public void OptimizeMeshes(Transform dungeonParent)
        {
            if (!combineMeshes) return;

            var allMeshes = dungeonParent.GetComponentsInChildren<ProBuilderMesh>().ToList();
            if (allMeshes.Count == 0) return;

            Debug.Log($"Optimizing dungeon: Combining {allMeshes.Count} meshes...");

            GameObject targetObj = new GameObject("CombinedDungeonMesh");
            targetObj.transform.SetParent(dungeonParent);
            targetObj.transform.localPosition = Vector3.zero;
            ProBuilderMesh targetMesh = targetObj.AddComponent<ProBuilderMesh>();

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
                        Object.Destroy(targetObj);
                    else
                        Object.DestroyImmediate(targetObj);
                }
            }
        }
    }
}
