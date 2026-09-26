using UnityEngine;

namespace SpatialAwareness.SceneGraph
{
    public static class SpatialReasoning
    {
        // Thresholds - these could be moved to a settings object later
        public static float NEAR_THRESHOLD = 2.0f;
        public static float ON_TOP_MAX_DIST = 0.5f;

        /// <summary>
        /// Determines the relationship between source and target relative to the source's orientation.
        /// </summary>
        public static SpatialConfig GetSpatialRelation(Transform source, Transform target)
        {
            Vector3 direction = target.position - source.position;
            float distance = direction.magnitude;

            // 1. Proximity Check
            if (distance < 0.1f) return SpatialConfig.Inside; // Simplistic collision check
            
            // 2. On Top Check (Vertical delta positive and small horizontal distance)
            // Simplified: checks if target is above source within a cylinder
            if (target.position.y > source.position.y && 
                Mathf.Abs(target.position.y - source.position.y) < 1.0f &&
                Vector3.Distance(new Vector3(source.position.x, 0, source.position.z), 
                                 new Vector3(target.position.x, 0, target.position.z)) < ON_TOP_MAX_DIST)
            {
                return SpatialConfig.OnTop;
            }

            if (distance < NEAR_THRESHOLD)
            {
                // We can have "Near" combined with direction, but for now let's return direction if distinct, else Near
                // or just prioritize direction? Let's check direction first.
            }

            // 3. Directional Checks (Relative to Source)
            // Transform direction to local space of source
            Vector3 localDir = source.InverseTransformPoint(target.position);
            
            // Forward/Back
            if (localDir.z > 0.5f) return SpatialConfig.InFrontOf;
            if (localDir.z < -0.5f) return SpatialConfig.Behind;

            // Left/Right
            if (localDir.x > 0.5f) return SpatialConfig.RightOf;
            if (localDir.x < -0.5f) return SpatialConfig.LeftOf;

            // Fallback to distance
            if (distance < NEAR_THRESHOLD) return SpatialConfig.Near;

            return SpatialConfig.Far; // or None?
        }

        /// <summary>
        /// Natural-language direction from user to a target position.
        /// Returns: "ahead", "behind you", "to your left",
        /// "ahead and to your right", "right next to you", etc.
        /// Used by FunctionDispatcher for LLM tool calls.
        /// </summary>
        public static string GetRelativeDirectionText(Transform userTransform, Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - userTransform.position;
            toTarget.y = 0;

            if (toTarget.magnitude < 0.5f)
                return "right next to you";

            Vector3 forward = userTransform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = userTransform.right;
            right.y = 0;
            right.Normalize();

            float forwardDot = Vector3.Dot(toTarget.normalized, forward);
            float rightDot = Vector3.Dot(toTarget.normalized, right);

            string fbComponent = "";
            if (forwardDot > 0.5f) //
                fbComponent = "ahead";
            else if (forwardDot < -0.5f)
                fbComponent = "behind you";

            string lrComponent = "";
            if (rightDot > 0.5f) // more than 30 degrees to the right
                lrComponent = "to your right";
            else if (rightDot < -0.5f) // more than 30 degrees to the left
                lrComponent = "to your left";

            if (!string.IsNullOrEmpty(fbComponent) && !string.IsNullOrEmpty(lrComponent))
                return $"{fbComponent} and {lrComponent}";
            if (!string.IsNullOrEmpty(fbComponent))
                return fbComponent;
            if (!string.IsNullOrEmpty(lrComponent))
                return lrComponent;

            return "nearby";
        }

        /// <summary>
        /// Check line-of-sight visibility from one position to another.
        /// Returns true if nothing blocks the view.
        /// Used by FunctionDispatcher for find_objects and get_direction_to tools.
        /// </summary>
        public static bool CheckVisibility(Vector3 fromPosition, Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - fromPosition;
            float distance = direction.magnitude;

            if (Physics.Raycast(fromPosition, direction.normalized, out RaycastHit hit, distance))
            {
                // Hit something — visible only if the hit point is near the target
                float distToTarget = Vector3.Distance(hit.point, targetPosition);
                return distToTarget < 1.0f;
            }

            return true;
        }

        /// <summary>
        /// Calculate the distance between two positions.
        /// Used by FunctionDispatcher for distance calculations in tool calls.
        /// </summary>
        public static float GetDistanceTo(Vector3 from, Vector3 to)
        {
            return Vector3.Distance(from, to);
        }
    }
}
