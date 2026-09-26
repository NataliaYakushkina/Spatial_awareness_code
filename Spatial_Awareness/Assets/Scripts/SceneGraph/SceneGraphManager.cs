using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SpatialAwareness.SceneGraph
{
    public class SceneGraphManager : MonoBehaviour
    {
        public static SceneGraphManager Instance { get; private set; }

        private List<SceneNode> nodes = new List<SceneNode>();

        [Header("Player Room Tracking")]
        [Tooltip("Updated automatically by RoomZone triggers")]
        public string currentRoom = "";

        public float updateInterval = 0.5f;
        private float timer;

        // Room connectivity map — defines which rooms connect to which
        private static readonly Dictionary<string, List<string>> roomConnections = new()
        {
            { "Room_01", new List<string> { "Corridor" } },
            { "Room_02", new List<string> { "Corridor" } },
            { "Room_03", new List<string> { "Corridor" } },
            { "Corridor", new List<string> { "Room_01", "Room_02", "Room_03" } }
        };

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void RegisterNode(SceneNode node)
        {
            if (!nodes.Contains(node))
            {
                nodes.Add(node);
                //Debug.Log($"[SceneGraph] Registered: ({node.id})");
            }
        }

        public void UnregisterNode(SceneNode node)
        {
            if (nodes.Contains(node))
            {
                nodes.Remove(node);
            }
        }

        // ── Query Methods (used by FunctionDispatcher for LLM tool calls) ──

        /// <summary>
        /// Find a single node by its unique ID. Returns null if not found.
        /// Used by: get_direction_to, highlight_object, get_path_to_object,
        ///          show_path_to_user, check_affordance
        /// </summary>
        public SceneNode GetNodeById(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return null;
            return nodes.FirstOrDefault(n =>
                string.Equals(n.id?.Trim(), nodeId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Find all nodes of a specific semantic type (e.g. "table", "chair").
        /// Used by: find_objects tool
        /// </summary>
        public List<SceneNode> GetNodesByType(string objectType)
        {
            return nodes.Where(n =>
                !n.CompareTag("Player") &&
                string.Equals(n.objectType, objectType, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        /// <summary>
        /// Find all nodes in a specific room.
        /// Used by: get_objects_in_room tool
        /// </summary>
        public List<SceneNode> GetNodesInRoom(string roomName)
        {
            return nodes.Where(n =>
                !n.CompareTag("Player") &&
                string.Equals(n.room, roomName, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        /// <summary>
        /// Find all nodes within a radius of a position, sorted by distance (closest first).
        /// Used by: get_nearby_objects tool
        /// </summary>
        public List<SceneNode> GetNearbyNodes(Vector3 position, float radius)
        {
            return nodes
                .Where(n => !n.CompareTag("Player") && Vector3.Distance(position, n.transform.position) <= radius)
                .OrderBy(n => Vector3.Distance(position, n.transform.position))
                .ToList();
        }

        /// <summary>
        /// Build the static room context sent to the LLM system prompt. IN THE BEGINNING
        /// Contains WHAT exists (rooms, objects, types) but NOT coordinates or distances.
        /// The LLM must use tools to get spatial data.
        /// </summary>
        public RoomContext BuildRoomContext()
        {
            var context = new RoomContext();
            context.currentRoom = currentRoom;

            var allRooms = nodes
                .Select(n => n.room)
                .Where(r => !string.IsNullOrEmpty(r))
                .Distinct()
                .ToList();

            context.connectedRooms = roomConnections.TryGetValue(currentRoom, out var connections)
                ? connections
                : new List<string>();

            context.rooms = new List<RoomInfo>();
            foreach (string roomName in allRooms)
            {
                var roomInfo = new RoomInfo();
                roomInfo.name = roomName;
                roomInfo.objects = new List<ObjectInfo>();

                foreach (var node in GetNodesInRoom(roomName))
                {
                    roomInfo.objects.Add(new ObjectInfo
                    {
                        id = node.id,
                        type = node.objectType,
                        affordances = node.affordances
                    });
                }

                context.rooms.Add(roomInfo);
            }

            context.availableTypes = nodes
                .Where(n => !n.CompareTag("Player") && !string.IsNullOrEmpty(n.objectType))
                .Select(n => n.objectType)
                .Distinct()
                .ToList();

            return context;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= updateInterval)
            {
                UpdateSpatialRelations();
                timer = 0;
            }

            
        }

        private void UpdateSpatialRelations()
        {
            // Simple O(N^2) loop for prototype
            for (int i = 0; i < nodes.Count; i++)
            {
                var source = nodes[i];
                source.activeRelations.Clear(); // Fresh update

                for (int j = 0; j < nodes.Count; j++)
                {
                    if (i == j) continue; // Skip self

                    var target = nodes[j];
                    
                    // Calc relation
                    SpatialConfig relType = SpatialReasoning.GetSpatialRelation(source.transform, target.transform);

                    if (relType != SpatialConfig.Far && relType != SpatialConfig.None)
                    {
                        source.activeRelations.Add(new Relation 
                        { 
                            targetNode = target, 
                            type = relType 
                        });
                    }
                }
            }
        }

        public string GetSnapshot()
        {
            var data = new SceneGraphData();
            data.timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            foreach (var node in nodes)
            {
                var nodeData = new NodeData
                {
                    id = node.id,
                    objectType = node.objectType,
                    description = node.Description,
                    room = node.room,
                    affordances = node.affordances,
                    position = new PositionData 
                    { 
                        x = node.transform.position.x, 
                        y = node.transform.position.y, 
                        z = node.transform.position.z 
                    },
                    relations = new List<RelationData>()
                };

                foreach (var rel in node.activeRelations)
                {
                    if (rel.targetNode != null)
                    {
                        nodeData.relations.Add(new RelationData
                        {
                            targetId = rel.targetNode.id,
                            relationType = rel.type.ToString()
                        });
                    }
                }

                data.nodes.Add(nodeData);
            }

            // Serialize with nice formatting and Enum-to-String conversion
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new StringEnumConverter());
            settings.Formatting = Formatting.Indented;

            return JsonConvert.SerializeObject(data, settings);
        }

        public void SaveSnapshot()
        {
            string json = GetSnapshot();
            
            // Generate filename: snapshot_YYYY-MM-DD_HH-mm-ss.json
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"snapshot_{timestamp}.json";
            
            // Path: ProjectRoot/JSONSnapshots/
            // Application.dataPath gives .../Spatial_Awareness/Assets
            // We want .../Spatial_Awareness/JSONSnapshots
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string folder = Path.Combine(projectRoot, "JSONSnapshots");
            
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string fullPath = Path.Combine(folder, filename);
            File.WriteAllText(fullPath, json);

            Debug.Log($"[SceneGraph] Snapshot saved to: {fullPath}");
        }
    }

    [System.Serializable]
    public class SceneGraphData
    {
        public string timestamp;
        public List<NodeData> nodes = new List<NodeData>();
    }

    [System.Serializable]
    public class NodeData
    {
        public string id;
        public string objectType;
        public string description;
        public string room;
        public List<string> affordances;
        public PositionData position;
        public List<RelationData> relations;
    }

    [System.Serializable]
    public class PositionData
    {
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    public class RelationData
    {
        public string targetId;
        public string relationType;
    }

    // ── Room Context DTOs (sent to LLM system prompt) ──

    [System.Serializable]
    public class RoomContext
    {
        public string currentRoom;
        public List<string> connectedRooms;
        public List<string> availableTypes;
        public List<RoomInfo> rooms;
    }

    [System.Serializable]
    public class RoomInfo
    {
        public string name;
        public List<ObjectInfo> objects;
    }

    [System.Serializable]
    public class ObjectInfo
    {
        public string id;
        public string type;
        public List<string> affordances;
    }
}
