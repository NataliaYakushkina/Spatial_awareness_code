using UnityEngine;
using UnityEngine.AI;
using SpatialAwareness.SceneGraph;

namespace SpatialAwareness.Core
{
    /// <summary>
    /// A robot vacuum that autonomously patrols between rooms using NavMesh.
    /// It picks a random room waypoint, walks there, waits, then picks the next.
    /// Its SceneNode.room is updated automatically by RoomZone triggers.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(SceneNode))]
    public class RobotVacuum : MonoBehaviour
    {
        [Header("Patrol Settings")]
        [Tooltip("Waypoints to patrol between (place one per room)")]
        public Transform[] waypoints;

        [Tooltip("How long the vacuum stays at each waypoint before moving on")]
        public float waitTime = 5f;

        [Tooltip("NavMeshAgent movement speed")]
        public float moveSpeed = 2f;

        private NavMeshAgent agent;
        private int currentWaypointIndex;
        private float waitTimer;
        private bool isWaiting;

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed = moveSpeed;

            if (waypoints == null || waypoints.Length == 0)
            {
                Debug.LogError("[RobotVacuum] No waypoints assigned!", this);
                enabled = false;
                return;
            }

            // Start by going to a random waypoint
            currentWaypointIndex = Random.Range(0, waypoints.Length);
            GoToCurrentWaypoint();
        }

        private void Update()
        {
            if (isWaiting)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    isWaiting = false;
                    PickNextWaypoint();
                    GoToCurrentWaypoint();
                }
                return;
            }

            // Check if the agent has arrived
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                isWaiting = true;
                waitTimer = waitTime;
                Debug.Log($"[RobotVacuum] Arrived at waypoint {currentWaypointIndex}, waiting {waitTime}s");
            }
        }

        private void PickNextWaypoint()
        {
            // Pick a different random waypoint
            int next;
            do
            {
                next = Random.Range(0, waypoints.Length);
            } while (next == currentWaypointIndex && waypoints.Length > 1);

            currentWaypointIndex = next;
        }

        private void GoToCurrentWaypoint()
        {
            var target = waypoints[currentWaypointIndex];
            agent.SetDestination(target.position);
            Debug.Log($"[RobotVacuum] Heading to waypoint {currentWaypointIndex} ({target.name})");
        }
    }
}
