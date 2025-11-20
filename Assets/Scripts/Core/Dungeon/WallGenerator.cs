using UnityEngine;
using UnityEngine.ProBuilder;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Handles generation of room walls using ProBuilder API
    /// Supports doorway cuts for room-corridor connections
    /// </summary>
    public class WallGenerator
    {
        private Material wallMaterial;
        private float wallHeight;
        private float wallThickness;

        public WallGenerator(Material wallMaterial, float wallHeight, float wallThickness)
        {
            this.wallMaterial = wallMaterial;
            this.wallHeight = wallHeight;
            this.wallThickness = wallThickness;
        }

        /// <summary>
        /// Create all four walls around the room with doorway cuts
        /// </summary>
        public void CreateWalls(GameObject parent, DungeonRoom room)
        {
            // Create four walls with doorways
            CreateWallWithDoorways(parent, room, Doorway.WallSide.North,
                new Vector3(0, 0, room.Size.z),
                new Vector3(room.Size.x, 0, room.Size.z + wallThickness));

            CreateWallWithDoorways(parent, room, Doorway.WallSide.South,
                new Vector3(0, 0, -wallThickness),
                new Vector3(room.Size.x, 0, 0));

            CreateWallWithDoorways(parent, room, Doorway.WallSide.East,
                new Vector3(room.Size.x, 0, 0),
                new Vector3(room.Size.x + wallThickness, 0, room.Size.z));

            CreateWallWithDoorways(parent, room, Doorway.WallSide.West,
                new Vector3(-wallThickness, 0, 0),
                new Vector3(0, 0, room.Size.z));
        }

        /// <summary>
        /// Create a wall with doorway cuts
        /// </summary>
        private void CreateWallWithDoorways(GameObject parent, DungeonRoom room, Doorway.WallSide wallSide, Vector3 corner1, Vector3 corner2)
        {
            // Get all doorways for this wall
            var doorways = room.Doorways.FindAll(d => d.Wall == wallSide);

            if (doorways.Count == 0)
            {
                // No doorways, create full wall
                CreateWall(parent, $"Wall_{wallSide}", corner1, corner2, wallHeight);
                return;
            }

            // Sort doorways along the wall
            doorways.Sort((a, b) =>
            {
                float posA = GetDoorwayPositionAlongWall(a, wallSide);
                float posB = GetDoorwayPositionAlongWall(b, wallSide);
                return posA.CompareTo(posB);
            });

            // Determine wall direction and length
            bool isHorizontal = wallSide == Doorway.WallSide.North || wallSide == Doorway.WallSide.South;
            float wallLength = isHorizontal ? Mathf.Abs(corner2.x - corner1.x) : Mathf.Abs(corner2.z - corner1.z);

            // Create wall segments between doorways
            float currentPos = 0f;
            int segmentIndex = 0;

            foreach (var doorway in doorways)
            {
                float doorwayPos = GetDoorwayPositionAlongWall(doorway, wallSide);
                float doorwayHalfWidth = doorway.Width / 2f;
                float doorwayStart = Mathf.Max(0, doorwayPos - doorwayHalfWidth);
                float doorwayEnd = Mathf.Min(wallLength, doorwayPos + doorwayHalfWidth);

                // Create wall segment before doorway
                if (doorwayStart > currentPos + 0.1f)
                {
                    CreateWallSegment(parent, wallSide, corner1, corner2, currentPos, doorwayStart, segmentIndex++);
                }

                currentPos = doorwayEnd;
            }

            // Create final wall segment after last doorway
            if (currentPos < wallLength - 0.1f)
            {
                CreateWallSegment(parent, wallSide, corner1, corner2, currentPos, wallLength, segmentIndex);
            }
        }

        /// <summary>
        /// Get doorway position along the wall (0 to wall length)
        /// </summary>
        private float GetDoorwayPositionAlongWall(Doorway doorway, Doorway.WallSide wallSide)
        {
            switch (wallSide)
            {
                case Doorway.WallSide.North:
                case Doorway.WallSide.South:
                    return doorway.Position.x;
                case Doorway.WallSide.East:
                case Doorway.WallSide.West:
                    return doorway.Position.z;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Create a wall segment between two positions along the wall
        /// </summary>
        private void CreateWallSegment(GameObject parent, Doorway.WallSide wallSide, Vector3 corner1, Vector3 corner2, float startPos, float endPos, int segmentIndex)
        {
            bool isHorizontal = wallSide == Doorway.WallSide.North || wallSide == Doorway.WallSide.South;

            Vector3 segmentCorner1, segmentCorner2;

            if (isHorizontal)
            {
                segmentCorner1 = new Vector3(corner1.x + startPos, corner1.y, corner1.z);
                segmentCorner2 = new Vector3(corner1.x + endPos, corner2.y, corner2.z);
            }
            else
            {
                segmentCorner1 = new Vector3(corner1.x, corner1.y, corner1.z + startPos);
                segmentCorner2 = new Vector3(corner2.x, corner2.y, corner1.z + endPos);
            }

            CreateWall(parent, $"Wall_{wallSide}_Segment{segmentIndex}", segmentCorner1, segmentCorner2, wallHeight);
        }

        /// <summary>
        /// Create a single wall segment using ProBuilder
        /// </summary>
        private void CreateWall(GameObject parent, string name, Vector3 corner1, Vector3 corner2, float height)
        {
            GameObject wallObj = new GameObject(name);
            wallObj.transform.SetParent(parent.transform);
            wallObj.transform.localPosition = Vector3.zero;

            // Define base rectangle vertices
            Vector3 v0 = corner1;
            Vector3 v1 = new Vector3(corner2.x, corner1.y, corner1.z);
            Vector3 v2 = corner2;
            Vector3 v3 = new Vector3(corner1.x, corner2.y, corner2.z);

            // Create all 8 vertices (4 bottom + 4 top)
            Vector3[] vertices = new Vector3[]
            {
                // Bottom vertices (0-3)
                v0,
                v1,
                v2,
                v3,
                // Top vertices (4-7)
                v0 + Vector3.up * height,
                v1 + Vector3.up * height,
                v2 + Vector3.up * height,
                v3 + Vector3.up * height
            };

            // Define faces (6 faces, each as 2 triangles)
            Face[] faces = new Face[]
            {
                // Bottom face (facing down)
                new Face(new int[] { 0, 2, 1, 0, 3, 2 }),
                // Top face (facing up)
                new Face(new int[] { 4, 5, 6, 4, 6, 7 }),
                // Front face
                new Face(new int[] { 0, 1, 5, 0, 5, 4 }),
                // Right face
                new Face(new int[] { 1, 2, 6, 1, 6, 5 }),
                // Back face
                new Face(new int[] { 2, 3, 7, 2, 7, 6 }),
                // Left face
                new Face(new int[] { 3, 0, 4, 3, 4, 7 })
            };

            ProBuilderMesh pbMesh = ProBuilderMesh.Create(vertices, faces);
            pbMesh.gameObject.transform.SetParent(wallObj.transform);
            pbMesh.gameObject.transform.localPosition = Vector3.zero;
            pbMesh.gameObject.name = name + "_Mesh";

            // Apply material
            if (wallMaterial != null)
            {
                var renderer = pbMesh.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = wallMaterial;
                }
            }

            pbMesh.ToMesh();
            pbMesh.Refresh();
        }
    }
}
