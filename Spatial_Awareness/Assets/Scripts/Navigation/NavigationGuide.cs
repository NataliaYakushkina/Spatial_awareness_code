using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SpatialAwareness.SceneGraph;

namespace SpatialAwareness.Navigation
{
    public class NavigationGuide : MonoBehaviour
    {
        private LineRenderer pathLine;

        [Header("Path auto-clear")]
        [Tooltip("Delete the path (and stop the counter) this many seconds after it is drawn. 0 = never.")]
        [SerializeField] private float pathDuration = 120f;
        private Coroutine autoClearRoutine;

        [Header("Walked-distance counter")]
        [Tooltip("Show a live 'meters walked' label while guiding the user.")]
        [SerializeField] private bool showWalkedCounter = true;
        [Tooltip("Ignore per-frame movement below this (metres) so jitter/gravity doesn't inflate the total.")]
        [SerializeField] private float moveThreshold = 0.01f;

        /// <summary>Metres the player has walked since the current navigation started.</summary>
        public float DistanceWalked { get; private set; }

        private bool tracking;
        private Vector3 lastFlatPos;

        private void Start()
        {
            // Visual path line
            pathLine = gameObject.AddComponent<LineRenderer>();
            pathLine.startWidth = 0.1f;
            pathLine.endWidth = 0.1f;
            pathLine.material = new Material(Shader.Find("Sprites/Default"));
            pathLine.startColor = Color.green;
            pathLine.endColor = Color.green;
            pathLine.positionCount = 0;
        }

        /// <summary>
        /// Navigate to a specific world position.
        /// </summary>
        public void ShowTheWay(Vector3 targetPosition)
        {
            // Find the closest point on NavMesh to the target
            NavMeshHit hit;
            Vector3 destination = targetPosition;

            if (NavMesh.SamplePosition(targetPosition, out hit, 5.0f, NavMesh.AllAreas))
            {
                destination = hit.position;
                Debug.Log($"[Navigation] Target: {targetPosition}, navigating to nearest NavMesh point: {destination}");
            }
            else
            {
                Debug.LogWarning($"[Navigation] Target {targetPosition} is too far from NavMesh!");
                return;
            }

            // Start path from the player (camera) position, not this GameObject's position
            Vector3 startPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            NavMeshHit startHit;
            if (NavMesh.SamplePosition(startPos, out startHit, 5.0f, NavMesh.AllAreas))
                startPos = startHit.position;

            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(startPos, destination, NavMesh.AllAreas, path))
            {
                DrawPath(path);
                StartWalkedCounter();
                RestartAutoClear();
                Debug.Log($"[Navigation] Path found with {path.corners.Length} waypoints.");
            }
            else
            {
                Debug.LogWarning($"[Navigation] No valid path to {destination}.");
            }
        }

        /// <summary>
        /// Navigate to a SceneNode by its unique ID (e.g., "table_01").
        /// Used by FunctionDispatcher for show_path_to_user tool.
        /// </summary>
        public bool ShowTheWayById(string objectId)
        {
            if (SceneGraphManager.Instance == null)
            {
                Debug.LogWarning("[Navigation] SceneGraphManager not found!");
                return false;
            }

            var node = SceneGraphManager.Instance.GetNodeById(objectId);
            if (node != null)
            {
                ShowTheWay(node.transform.position);
                return true;
            }

            Debug.LogWarning($"[Navigation] No object with ID '{objectId}' found.");
            return false;
        }

        /// <summary>
        /// Draw the calculated path as a visible line.
        /// </summary>
        private void DrawPath(NavMeshPath path)
        {
            pathLine.positionCount = path.corners.Length;
            pathLine.SetPositions(path.corners);
        }

        // Path line stays visible until StopNavigation() is called
        // No need to update in Update() since player doesn't move

        /// <summary>
        /// Stop navigation and clear the path line.
        /// </summary>
        public void StopNavigation()
        {
            pathLine.positionCount = 0;
            tracking = false;
            if (autoClearRoutine != null) { StopCoroutine(autoClearRoutine); autoClearRoutine = null; }
        }

        // Restart the auto-clear timer whenever a new path is drawn (0 minutes = never clear).
        private void RestartAutoClear()
        {
            if (autoClearRoutine != null) StopCoroutine(autoClearRoutine);
            autoClearRoutine = pathDuration > 0f
                ? StartCoroutine(AutoClearPath(pathDuration))
                : null;
        }

        private IEnumerator AutoClearPath(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            StopNavigation();
        }

        // --- Walked-distance odometer -------------------------------------------------

        private void StartWalkedCounter()
        {
            DistanceWalked = 0f;
            lastFlatPos = FlatPlayerPos();
            tracking = true;
        }

        private void Update()
        {
            if (!tracking) return;

            Vector3 cur = FlatPlayerPos();
            float step = Vector3.Distance(cur, lastFlatPos);
            if (step >= moveThreshold)
            {
                DistanceWalked += step;
                lastFlatPos = cur;
            }
        }

        // Horizontal (XZ) position of the player, so height changes don't count as walking.
        private Vector3 FlatPlayerPos()
        {
            Vector3 p = Camera.main != null ? Camera.main.transform.position : transform.position;
            p.y = 0f;
            return p;
        }

        private void OnGUI()
        {
            if (!showWalkedCounter || !tracking) return;

            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter
            };
            float w = 220f, h = 40f;
            var rect = new Rect((Screen.width - w) / 2f, 20f, w, h);
            GUI.Box(rect, $"Walked: {DistanceWalked:0.0} m", style);
        }
    }
}
