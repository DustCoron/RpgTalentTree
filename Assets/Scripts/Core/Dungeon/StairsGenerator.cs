using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.Shapes;

namespace RpgTalentTree.Core.Dungeon
{
    /// <summary>
    /// Generates stairs between different floor heights using ProBuilder API
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
        /// Create stairs between two points with different heights
        /// </summary>
        public GameObject CreateStairs(Vector3 startPos, Vector3 endPos, Transform parent, int stairsIndex)
        {
            float heightDiff = endPos.y - startPos.y;

            // If no height difference, don't create stairs
            if (Mathf.Abs(heightDiff) < 0.1f)
            {
                return null;
            }

            GameObject stairsObject = new GameObject($"Stairs_{stairsIndex}");
            stairsObject.transform.SetParent(parent);
            stairsObject.transform.position = startPos;

            // Calculate number of steps needed
            int stepCount = Mathf.CeilToInt(Mathf.Abs(heightDiff) / stepHeight);
            stepCount = Mathf.Max(1, stepCount);

            // Recalculate actual step height to fit perfectly
            float actualStepHeight = heightDiff / stepCount;

            // Calculate horizontal direction
            Vector3 horizontalDir = new Vector3(endPos.x - startPos.x, 0, endPos.z - startPos.z);
            float horizontalDistance = horizontalDir.magnitude;
            horizontalDir.Normalize();

            // Calculate actual step depth based on horizontal distance
            float actualStepDepth = horizontalDistance / stepCount;

            // Determine if stairs go up or down
            bool goingUp = heightDiff > 0;

            // Create each step
            for (int i = 0; i < stepCount; i++)
            {
                CreateStep(stairsObject, i, horizontalDir, actualStepHeight, actualStepDepth, goingUp);
            }

            return stairsObject;
        }

        /// <summary>
        /// Create a single stair step using ProBuilder
        /// </summary>
        private void CreateStep(GameObject parent, int stepIndex, Vector3 direction, float stepHeight, float stepDepth, bool goingUp)
        {
            GameObject stepObj = new GameObject($"Step_{stepIndex}");
            stepObj.transform.SetParent(parent.transform);

            // Calculate step position
            float heightOffset = stepIndex * stepHeight;
            float depthOffset = stepIndex * stepDepth;
            Vector3 stepPosition = new Vector3(
                direction.x * depthOffset,
                heightOffset,
                direction.z * depthOffset
            );
            stepObj.transform.localPosition = stepPosition;

            // Create ProBuilder mesh for the step
            ProBuilderMesh pbMesh = stepObj.AddComponent<ProBuilderMesh>();

            // Calculate step dimensions
            // The step should be tall enough to reach the next step
            float stepTotalHeight = goingUp ? (stepHeight * (pbMesh.transform.parent.childCount - stepIndex)) : stepHeight;

            // Create step as a box
            Vector3 stepSize = new Vector3(stepWidth, stepTotalHeight, stepDepth);
            pbMesh.CreateShapeFromPolygon(ShapeGenerator.GeneratePlane(PivotLocation.FirstCorner, stepSize.x, stepSize.z, 0, 0, Axis.Up), stepTotalHeight, false);

            // Adjust for proper orientation
            AdjustStepOrientation(pbMesh, direction, stepSize);

            // Apply material
            if (stairMaterial != null)
            {
                var renderer = stepObj.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = stairMaterial;
                }
            }

            pbMesh.ToMesh();
            pbMesh.Refresh();
        }

        /// <summary>
        /// Adjust step orientation based on direction
        /// </summary>
        private void AdjustStepOrientation(ProBuilderMesh pbMesh, Vector3 direction, Vector3 stepSize)
        {
            // Calculate rotation angle based on direction
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            pbMesh.transform.localRotation = Quaternion.Euler(0, angle, 0);

            // Center the step
            pbMesh.transform.localPosition += new Vector3(-stepSize.x / 2f, 0, 0);
        }

        /// <summary>
        /// Create stairs with railing (more complex version)
        /// </summary>
        public GameObject CreateStairsWithRailing(Vector3 startPos, Vector3 endPos, Transform parent, int stairsIndex, float railingHeight = 1f)
        {
            GameObject stairsObj = CreateStairs(startPos, endPos, parent, stairsIndex);

            if (stairsObj == null)
                return null;

            // Add railings on both sides
            CreateRailing(stairsObj, true, startPos, endPos, railingHeight);
            CreateRailing(stairsObj, false, startPos, endPos, railingHeight);

            return stairsObj;
        }

        /// <summary>
        /// Create a railing along the stairs
        /// </summary>
        private void CreateRailing(GameObject stairsParent, bool leftSide, Vector3 startPos, Vector3 endPos, float railingHeight)
        {
            GameObject railingObj = new GameObject(leftSide ? "Railing_Left" : "Railing_Right");
            railingObj.transform.SetParent(stairsParent.transform);
            railingObj.transform.localPosition = Vector3.zero;

            // Calculate railing positions
            Vector3 horizontalDir = new Vector3(endPos.x - startPos.x, 0, endPos.z - startPos.z).normalized;
            Vector3 perpendicular = new Vector3(-horizontalDir.z, 0, horizontalDir.x);

            float sideOffset = (stepWidth / 2f) * (leftSide ? -1f : 1f);
            Vector3 railingStart = perpendicular * sideOffset;
            Vector3 railingEnd = railingStart + new Vector3(endPos.x - startPos.x, endPos.y - startPos.y, endPos.z - startPos.z);

            // Create railing post at start
            CreateRailingPost(railingObj, railingStart, railingHeight);

            // Create railing post at end
            CreateRailingPost(railingObj, railingEnd, railingHeight);

            // Create railing bar connecting posts
            CreateRailingBar(railingObj, railingStart + Vector3.up * railingHeight, railingEnd + Vector3.up * railingHeight);
        }

        /// <summary>
        /// Create a railing post
        /// </summary>
        private void CreateRailingPost(GameObject parent, Vector3 position, float height)
        {
            GameObject postObj = new GameObject("Post");
            postObj.transform.SetParent(parent.transform);
            postObj.transform.localPosition = position;

            ProBuilderMesh pbMesh = postObj.AddComponent<ProBuilderMesh>();

            // Create thin vertical post
            float postThickness = 0.1f;
            pbMesh.CreateShapeFromPolygon(
                new Vector3[] {
                    new Vector3(-postThickness / 2, 0, -postThickness / 2),
                    new Vector3(postThickness / 2, 0, -postThickness / 2),
                    new Vector3(postThickness / 2, 0, postThickness / 2),
                    new Vector3(-postThickness / 2, 0, postThickness / 2)
                },
                height,
                false
            );

            // Apply material
            if (stairMaterial != null)
            {
                var renderer = postObj.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = stairMaterial;
                }
            }

            pbMesh.ToMesh();
            pbMesh.Refresh();
        }

        /// <summary>
        /// Create a horizontal railing bar
        /// </summary>
        private void CreateRailingBar(GameObject parent, Vector3 start, Vector3 end)
        {
            GameObject barObj = new GameObject("Bar");
            barObj.transform.SetParent(parent.transform);
            barObj.transform.localPosition = start;

            ProBuilderMesh pbMesh = barObj.AddComponent<ProBuilderMesh>();

            // Calculate bar direction and length
            Vector3 direction = (end - start);
            float length = direction.magnitude;
            direction.Normalize();

            float barThickness = 0.05f;

            // Create horizontal bar
            pbMesh.CreateShapeFromPolygon(
                new Vector3[] {
                    new Vector3(-barThickness / 2, -barThickness / 2, 0),
                    new Vector3(barThickness / 2, -barThickness / 2, 0),
                    new Vector3(barThickness / 2, barThickness / 2, 0),
                    new Vector3(-barThickness / 2, barThickness / 2, 0)
                },
                length,
                false
            );

            // Rotate to align with direction
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Asin(direction.y) * Mathf.Rad2Deg;
            pbMesh.transform.localRotation = Quaternion.Euler(-pitch, angle, 0);

            // Apply material
            if (stairMaterial != null)
            {
                var renderer = barObj.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = stairMaterial;
                }
            }

            pbMesh.ToMesh();
            pbMesh.Refresh();
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
