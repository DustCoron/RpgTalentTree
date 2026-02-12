using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Generates room walls using ProBuilder cubes with doorway support.
    /// Layout: 4 corner pillars + 4 wall sides split by doorways = gap-free geometry.
    /// </summary>
    public class WallGenerator
    {
        private Material wallMaterial;
        private float wallHeight;
        private float wallThickness;
        private float doorwayHeight;

        public WallGenerator(Material wallMaterial, float wallHeight, float wallThickness)
        {
            this.wallMaterial = wallMaterial;
            this.wallHeight = wallHeight;
            this.wallThickness = wallThickness;
            this.doorwayHeight = wallHeight * 0.8f;
        }

        /// <summary>
        /// Create all walls around the room with corner pillars for gap-free corners
        /// </summary>
        public void CreateWalls(GameObject parent, DungeonRoom room)
        {
            float sx = room.Size.x;
            float sz = room.Size.z;
            float t = wallThickness;
            float h = wallHeight;

            // Corner pillars fill gaps where wall sides meet
            CreateCube(parent, "Corner_SW", new Vector3(-t / 2f, h / 2f, -t / 2f), new Vector3(t, h, t));
            CreateCube(parent, "Corner_SE", new Vector3(sx + t / 2f, h / 2f, -t / 2f), new Vector3(t, h, t));
            CreateCube(parent, "Corner_NW", new Vector3(-t / 2f, h / 2f, sz + t / 2f), new Vector3(t, h, t));
            CreateCube(parent, "Corner_NE", new Vector3(sx + t / 2f, h / 2f, sz + t / 2f), new Vector3(t, h, t));

            // Wall panels between corners, split by doorways
            // North/South walls run along X, East/West along Z
            BuildWallSide(parent, room, Doorway.WallSide.North, 0f, sx, sz + t / 2f, true);
            BuildWallSide(parent, room, Doorway.WallSide.South, 0f, sx, -t / 2f, true);
            BuildWallSide(parent, room, Doorway.WallSide.East, 0f, sz, sx + t / 2f, false);
            BuildWallSide(parent, room, Doorway.WallSide.West, 0f, sz, -t / 2f, false);
        }

        /// <summary>
        /// Build one wall side, splitting around doorways.
        /// wallStart/wallEnd: range along the wall in room-local coords.
        /// perpCenter: center position perpendicular to the wall face.
        /// isHorizontal: true for North/South (along X), false for East/West (along Z).
        /// </summary>
        private void BuildWallSide(GameObject parent, DungeonRoom room, Doorway.WallSide side,
            float wallStart, float wallEnd, float perpCenter, bool isHorizontal)
        {
            var doorways = room.Doorways.FindAll(d => d.Wall == side);

            if (doorways.Count == 0)
            {
                // Full solid wall, no doorways
                CreateWallPanel(parent, $"Wall_{side}", isHorizontal,
                    wallStart, wallEnd, 0f, wallHeight, perpCenter);
                return;
            }

            // Sort doorways along wall direction
            doorways.Sort((a, b) =>
            {
                float posA = isHorizontal ? a.Position.x : a.Position.z;
                float posB = isHorizontal ? b.Position.x : b.Position.z;
                return posA.CompareTo(posB);
            });

            float currentPos = wallStart;
            int idx = 0;

            foreach (var doorway in doorways)
            {
                float doorPos = isHorizontal ? doorway.Position.x : doorway.Position.z;
                float halfDoor = doorway.Width / 2f;
                float doorStart = Mathf.Max(wallStart, doorPos - halfDoor);
                float doorEnd = Mathf.Min(wallEnd, doorPos + halfDoor);

                // Solid wall segment before doorway opening
                if (doorStart > currentPos + 0.01f)
                {
                    CreateWallPanel(parent, $"Wall_{side}_{idx++}", isHorizontal,
                        currentPos, doorStart, 0f, wallHeight, perpCenter);
                }

                // Lintel above doorway opening
                if (doorwayHeight < wallHeight - 0.01f)
                {
                    CreateWallPanel(parent, $"Lintel_{side}_{idx++}", isHorizontal,
                        doorStart, doorEnd, doorwayHeight, wallHeight, perpCenter);
                }

                currentPos = doorEnd;
            }

            // Solid wall segment after last doorway
            if (currentPos < wallEnd - 0.01f)
            {
                CreateWallPanel(parent, $"Wall_{side}_{idx}", isHorizontal,
                    currentPos, wallEnd, 0f, wallHeight, perpCenter);
            }
        }

        /// <summary>
        /// Create a wall panel as a ProBuilder cube.
        /// startAlong/endAlong: range along the wall direction.
        /// startY/endY: vertical range. perpCenter: position on the perpendicular axis.
        /// </summary>
        private void CreateWallPanel(GameObject parent, string name, bool isHorizontal,
            float startAlong, float endAlong, float startY, float endY, float perpCenter)
        {
            float length = endAlong - startAlong;
            float height = endY - startY;
            if (length < 0.01f || height < 0.01f) return;

            float centerAlong = (startAlong + endAlong) / 2f;
            float centerY = (startY + endY) / 2f;

            Vector3 center, size;
            if (isHorizontal)
            {
                // North/South wall: extends along X axis
                center = new Vector3(centerAlong, centerY, perpCenter);
                size = new Vector3(length, height, wallThickness);
            }
            else
            {
                // East/West wall: extends along Z axis
                center = new Vector3(perpCenter, centerY, centerAlong);
                size = new Vector3(wallThickness, height, length);
            }

            CreateCube(parent, name, center, size);
        }

        private void CreateCube(GameObject parent, string name, Vector3 localPos, Vector3 size)
        {
            var mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            mesh.gameObject.name = name;
            mesh.gameObject.transform.SetParent(parent.transform);
            mesh.gameObject.transform.localPosition = localPos;

            if (wallMaterial != null)
            {
                var renderer = mesh.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.sharedMaterial = wallMaterial;
            }

            mesh.ToMesh();
            mesh.Refresh();
        }
    }
}
