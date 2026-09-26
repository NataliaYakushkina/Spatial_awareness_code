using System.Collections.Generic;
using UnityEngine;

namespace SpatialAwareness.SceneGraph
{
    public class SceneNode : MonoBehaviour
    {
        [Header("Semantic Attributes")]
        public string id;
        public string objectType;        // "table", "chair", "lamp" — semantic category
        public TextAsset descriptionFile; // .md file with rich description for LLM
        public string Description => descriptionFile != null ? descriptionFile.text : "";
        public string room;              // "Room_1", "Room_2", "Room_3", "Corridor"
        public List<string> affordances; // ["sittable", "viewable"] — what can user do with it

        [Header("Spatial State")]
        public List<Relation> activeRelations = new List<Relation>();

        private void Start()
        {
            // Manual Mode: Ensure ID is set in Inspector
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[SceneNode] Object '{gameObject.name}' has no ID assigned!", this);
            }

            // Register with Manager
            if (SceneGraphManager.Instance != null)
            {
                SceneGraphManager.Instance.RegisterNode(this);
            }
            else
            {
                Debug.LogWarning("SceneGraphManager not found!");
            }
        }

        private void OnDestroy()
        {
            if (SceneGraphManager.Instance != null)
            {
                SceneGraphManager.Instance.UnregisterNode(this);
            }
        }
    }
}
