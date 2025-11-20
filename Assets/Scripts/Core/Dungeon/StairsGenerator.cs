using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Generates stairs between different floor heights using ProBuilder's built-in shape generators
    /// </summary>
    public class StairsGenerator
    {
        private Material stairMaterial;
        private float stepHeight;
        private float stepDepth;
        private float stepWidth;

        public StairsGenerator(Material stairMaterial, float stepHeight = 0.2f, float stepDepth = 0.5f, float stepWidth = 2f)
        {
            this.stairMaterial = stairMaterial;
            this.stepHeight = stepHeight;
            this.stepDepth = stepDepth;
            this.stepWidth = stepWidth;
        }

        /// <summary>
        /// Create stairs between two points with different heights using ProBuilder's built-in stair generator
        /// </summary>
        public GameObject CreateStairs(Vector3 startPos, Vector3 endPos, Transform parent, int stairsIndex)
        {
            float heightDiff = endPos.y - startPos.y;

            // If no height difference, don't create stairs
            if (Mathf.Abs(heightDiff) < 0.1f)
            {
                return null;
            }

            // Calculate number of steps needed
            int stepCount = Mathf.CeilToInt(Mathf.Abs(heightDiff) / stepHeight);
            stepCount = Mathf.Max(3, stepCount); // Minimum 3 steps for ProBuilder

            // Calculate horizontal direction
            Vector3 horizontalDir = new Vector3(endPos.x - startPos.x, 0, endPos.z - startPos.z);
            float horizontalDistance = horizontalDir.magnitude;
            horizontalDir.Normalize();

            // Calculate stair size (width, height, depth)
            Vector3 stairSize = new Vector3(
                stepWidth,                    // Width of stairs
                Mathf.Abs(heightDiff),       // Total height
                horizontalDistance           // Total depth
            );

            // Use ProBuilder's built-in stair generator
            ProBuilderMesh stairMesh = ShapeGenerator.GenerateStair(
                PivotLocation.FirstCorner,   // Pivot at bottom-front corner
                stairSize,                    // Size of entire staircase
                stepCount,                    // Number of steps
                true                         // Build sides
            );

            // Configure the stairs object
            GameObject stairsObject = stairMesh.gameObject;
            stairsObject.name = $"Stairs_{stairsIndex}";
            stairsObject.transform.SetParent(parent);
            stairsObject.transform.position = startPos;

            // Calculate rotation to align with direction
            float angle = Mathf.Atan2(horizontalDir.x, horizontalDir.z) * Mathf.Rad2Deg;
            stairsObject.transform.rotation = Quaternion.Euler(0, angle, 0);

            // Apply material
            if (stairMaterial != null)
            {
                var renderer = stairMesh.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = stairMaterial;
                }
            }

            // Finalize the mesh
            stairMesh.ToMesh();
            stairMesh.Refresh();

            return stairsObject;
        }


        /// <summary>
        /// Calculate the number of steps needed for a given height difference
        /// </summary>
        public int CalculateStepCount(float heightDifference)
        {
            return Mathf.CeilToInt(Mathf.Abs(heightDifference) / stepHeight);
        }

        /// <summary>
        /// Calculate the horizontal distance needed for stairs with given height difference
        /// </summary>
        public float CalculateRequiredDistance(float heightDifference)
        {
            int steps = CalculateStepCount(heightDifference);
            return steps * stepDepth;
        }

        /// <summary>
        /// Check if stairs are needed between two positions
        /// </summary>
        public bool NeedsStairs(Vector3 startPos, Vector3 endPos, float threshold = 0.1f)
        {
            return Mathf.Abs(endPos.y - startPos.y) > threshold;
        }
    }
}
