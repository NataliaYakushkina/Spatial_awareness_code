using UnityEngine;
using SpatialAwareness.SceneGraph;

namespace SpatialAwareness.Core
{
    /// <summary>
    /// Attach to a GameObject with a BoxCollider (isTrigger = true)
    /// that covers the floor area of a room.
    /// When the player enters, SceneGraphManager.currentRoom is updated.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class RoomZone : MonoBehaviour
    {
        [Tooltip("Room name matching SceneNode.room values (e.g. Room_01, Room_02, Corridor)")]
        public string roomName;

        [Tooltip("Tag on the player GameObject (default: Player)")]
        public string playerTag = "Player";

        private void Start()
        {
            // Ensure the collider is set as trigger
            var col = GetComponent<BoxCollider>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"[RoomZone] BoxCollider on '{gameObject.name}' was not a trigger — fixed automatically.");
            }

            if (string.IsNullOrEmpty(roomName))
            {
                Debug.LogWarning($"[RoomZone] '{gameObject.name}' has no roomName set!", this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Update player room tracking
            if (other.CompareTag(playerTag))
            {
                if (SceneGraphManager.Instance != null)
                {
                    SceneGraphManager.Instance.currentRoom = roomName;
                    Debug.Log($"[RoomZone] Player entered: {roomName}");
                }
                return;
            }

            // Update SceneNode.room for any object with a SceneNode (e.g., robot vacuum)
            var node = other.GetComponent<SceneNode>();
            if (node != null)
            {
                node.room = roomName;
                Debug.Log($"[RoomZone] {node.id} entered: {roomName}");
            }
        }
    }
}
