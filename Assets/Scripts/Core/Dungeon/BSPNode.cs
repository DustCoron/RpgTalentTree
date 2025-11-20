using UnityEngine;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Binary Space Partitioning node for dungeon generation
    /// Each node represents a rectangular partition of space
    /// </summary>
    public class BSPNode
    {
        public Rect Bounds { get; private set; }
        public BSPNode LeftChild { get; private set; }
        public BSPNode RightChild { get; private set; }
        public DungeonRoom Room { get; set; }

        public BSPNode(Rect bounds)
        {
            Bounds = bounds;
        }

        /// <summary>
        /// Check if this is a leaf node (has no children)
        /// </summary>
        public bool IsLeaf()
        {
            return LeftChild == null && RightChild == null;
        }

        /// <summary>
        /// Recursively split this node into smaller partitions
        /// </summary>
        public bool Split(int minSize, System.Random random)
        {
            // Already split
            if (!IsLeaf())
                return false;

            // Determine split orientation based on aspect ratio
            bool splitHorizontally;
            if (Bounds.width > Bounds.height && Bounds.width / Bounds.height >= 1.25f)
            {
                splitHorizontally = false; // Wide rectangle - split vertically
            }
            else if (Bounds.height > Bounds.width && Bounds.height / Bounds.width >= 1.25f)
            {
                splitHorizontally = true; // Tall rectangle - split horizontally
            }
            else
            {
                splitHorizontally = random.Next(0, 2) == 0; // Square-ish - random
            }

            // Calculate maximum split position
            int maxSize = (splitHorizontally ? (int)Bounds.height : (int)Bounds.width) - minSize;

            // Can't split if too small
            if (maxSize <= minSize)
                return false;

            // Choose random split position
            int splitPos = random.Next(minSize, maxSize);

            // Create child nodes
            if (splitHorizontally)
            {
                LeftChild = new BSPNode(new Rect(Bounds.x, Bounds.y, Bounds.width, splitPos));
                RightChild = new BSPNode(new Rect(Bounds.x, Bounds.y + splitPos, Bounds.width, Bounds.height - splitPos));
            }
            else
            {
                LeftChild = new BSPNode(new Rect(Bounds.x, Bounds.y, splitPos, Bounds.height));
                RightChild = new BSPNode(new Rect(Bounds.x + splitPos, Bounds.y, Bounds.width - splitPos, Bounds.height));
            }

            return true;
        }

        /// <summary>
        /// Create a room within this partition's bounds
        /// </summary>
        public void CreateRoom(System.Random random, Vector2Int minRoomSize, Vector2Int maxRoomSize, float floorHeight)
        {
            if (!IsLeaf())
                return;

            // Calculate room size within partition bounds
            int roomWidth = random.Next(
                Mathf.Min(minRoomSize.x, (int)Bounds.width - 2),
                Mathf.Min(maxRoomSize.x, (int)Bounds.width - 1) + 1
            );

            int roomDepth = random.Next(
                Mathf.Min(minRoomSize.y, (int)Bounds.height - 2),
                Mathf.Min(maxRoomSize.y, (int)Bounds.height - 1) + 1
            );

            // Random position within partition (with 1 unit padding)
            int roomX = (int)Bounds.x + random.Next(1, (int)Bounds.width - roomWidth);
            int roomZ = (int)Bounds.y + random.Next(1, (int)Bounds.height - roomDepth);

            Vector3Int position = new Vector3Int(roomX, 0, roomZ);
            Vector3Int size = new Vector3Int(roomWidth, 3, roomDepth); // Height will be set by wallHeight

            Room = new DungeonRoom(position, size, floorHeight);
        }

        /// <summary>
        /// Get all leaf nodes (containing rooms) from this node's subtree
        /// </summary>
        public void GetLeaves(System.Collections.Generic.List<BSPNode> leaves)
        {
            if (IsLeaf())
            {
                leaves.Add(this);
            }
            else
            {
                if (LeftChild != null)
                    LeftChild.GetLeaves(leaves);
                if (RightChild != null)
                    RightChild.GetLeaves(leaves);
            }
        }
    }
}
