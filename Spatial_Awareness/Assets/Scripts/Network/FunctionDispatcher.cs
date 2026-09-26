using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Newtonsoft.Json.Linq;
using SpatialAwareness.SceneGraph;
using SpatialAwareness.Navigation;

public class FunctionDispatcher : MonoBehaviour
{
    public static FunctionDispatcher Instance { get; private set; }

    [SerializeField] private Transform userTransform;
    private NavigationGuide navigationGuide;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (userTransform == null)
            userTransform = Camera.main?.transform;
    }

    public string Execute(string functionName, JObject arguments)
    {
        switch (functionName)
        {
            case "find_objects": return FindObjects(arguments);
            case "get_objects_in_room": return GetObjectsInRoom(arguments);
            case "get_nearby_objects": return GetNearbyObjects(arguments);
            case "get_user_context": return GetUserContext();
            case "get_direction_to": return GetDirectionTo(arguments);
            case "highlight_object": return HighlightObject(arguments);
            case "get_path_to_object": return GetPathToObject(arguments);
            case "show_path_to_user": return ShowPathToUser(arguments);
            case "check_affordance": return CheckAffordance(arguments);
            case "get_description": return GetDescription(arguments);
            default: return $"{{\"error\": \"Unknown function: {functionName}\"}}";
        }
    }

    private string FindObjects(JObject args)
    {
        string objectType = args["object_type"]?.ToString();
        var nodes = SceneGraphManager.Instance.GetNodesByType(objectType);
        var results = new JArray();

        foreach (var node in nodes)
        {
            var pos = node.transform.position;
            float dist = SpatialReasoning.GetDistanceTo(userTransform.position, pos);
            string dir = SpatialReasoning.GetRelativeDirectionText(userTransform, pos);
            bool visible = SpatialReasoning.CheckVisibility(userTransform.position, pos);

            results.Add(new JObject
            {
                ["objectId"] = node.id,
                ["type"] = node.objectType,
                ["room"] = node.room,
                ["distance"] = System.Math.Round(dist, 1),
                ["relativeDirection"] = dir,
                ["isVisible"] = visible
            });
        }

        return results.ToString();
    }

    private string GetObjectsInRoom(JObject args)
    {
        string roomName = args["room_name"]?.ToString();
        var nodes = SceneGraphManager.Instance.GetNodesInRoom(roomName);
        var results = new JArray();

        foreach (var node in nodes)
        {
            results.Add(new JObject
            {
                ["objectId"] = node.id,
                ["type"] = node.objectType
            });
        }

        return results.ToString();
    }

    private string GetNearbyObjects(JObject args)
    {
        float radius = args["radius"]?.ToObject<float>() ?? 5.0f;
        var nodes = SceneGraphManager.Instance.GetNearbyNodes(userTransform.position, radius);
        var results = new JArray();

        foreach (var node in nodes)
        {
            float dist = SpatialReasoning.GetDistanceTo(userTransform.position, node.transform.position);
            results.Add(new JObject
            {
                ["objectId"] = node.id,
                ["type"] = node.objectType,
                ["distance"] = System.Math.Round(dist, 1),
                ["room"] = node.room
            });
        }

        return results.ToString();
    }

    private string GetUserContext()
    {
        var pos = userTransform.position;
        var fwd = userTransform.forward;
        var nearby = SceneGraphManager.Instance.GetNearbyNodes(pos, 3f);

        var nearbyArr = new JArray();
        foreach (var node in nearby)
        {
            if (nearbyArr.Count >= 3) break;
            float dist = SpatialReasoning.GetDistanceTo(pos, node.transform.position);
            string dir = SpatialReasoning.GetRelativeDirectionText(userTransform, node.transform.position);
            nearbyArr.Add(new JObject
            {
                ["objectId"] = node.id,
                ["type"] = node.objectType,
                ["distance"] = System.Math.Round(dist, 1),
                ["direction"] = dir
            });
        }

        // Convert facing direction to compass text
        float angle = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        string facing;
        if (angle >= 337.5f || angle < 22.5f) facing = "north";
        else if (angle < 67.5f) facing = "north-east";
        else if (angle < 112.5f) facing = "east";
        else if (angle < 157.5f) facing = "south-east";
        else if (angle < 202.5f) facing = "south";
        else if (angle < 247.5f) facing = "south-west";
        else if (angle < 292.5f) facing = "west";
        else facing = "north-west";

        return new JObject
        {
            ["currentRoom"] = SceneGraphManager.Instance.currentRoom,
            ["facing"] = facing,
            ["nearestObjects"] = nearbyArr
        }.ToString();
    }

    private string GetDirectionTo(JObject args)
    {
        string objectId = args["object_id"]?.ToString();
        var node = SceneGraphManager.Instance.GetNodeById(objectId);

        if (node == null)
            return $"{{\"error\": \"Object '{objectId}' not found\"}}";

        var targetPos = node.transform.position;
        float dist = SpatialReasoning.GetDistanceTo(userTransform.position, targetPos);
        string dir = SpatialReasoning.GetRelativeDirectionText(userTransform, targetPos);
        bool visible = SpatialReasoning.CheckVisibility(userTransform.position, targetPos);

        return new JObject
        {
            ["objectId"] = objectId,
            ["distance"] = System.Math.Round(dist, 1),
            ["relativeDirection"] = dir,
            ["isVisible"] = visible,
            ["room"] = node.room
        }.ToString();
    }

    private string HighlightObject(JObject args)
    {
        string objectId = args["object_id"]?.ToString();
        bool success = ObjectHighlighter.Instance.HighlightObject(objectId);
        return new JObject { ["success"] = success }.ToString();
    }

    private string GetPathToObject(JObject args)
    {
        string objectId = args["object_id"]?.ToString();
        var node = SceneGraphManager.Instance.GetNodeById(objectId);

        if (node == null)
            return $"{{\"error\": \"Object '{objectId}' not found\"}}";

        // Snap start and target to nearest NavMesh point
        NavMeshHit hit;
        Vector3 start = userTransform.position;
        if (NavMesh.SamplePosition(userTransform.position, out hit, 5.0f, NavMesh.AllAreas))
            start = hit.position;

        Vector3 destination = node.transform.position;
        if (NavMesh.SamplePosition(node.transform.position, out hit, 5.0f, NavMesh.AllAreas))
            destination = hit.position;

        var path = new NavMeshPath();
        NavMesh.CalculatePath(start, destination, NavMesh.AllAreas, path);

        if (path.status != NavMeshPathStatus.PathComplete)
            return new JObject { ["reachable"] = false, ["reason"] = "No complete path found on NavMesh" }.ToString();

        // Build turn-by-turn text directions
        float totalDist = 0f;
        var steps = new JArray();
        Vector3 currentFacing = userTransform.forward;

        for (int i = 1; i < path.corners.Length; i++)
        {
            float segDist = Vector3.Distance(path.corners[i - 1], path.corners[i]);
            totalDist += segDist;

            Vector3 segDir = (path.corners[i] - path.corners[i - 1]).normalized;
            string turn = GetTurnDirection(currentFacing, segDir);
            steps.Add($"{turn} and walk {System.Math.Round(segDist, 1)}m");
            currentFacing = segDir;
        }

        return new JObject
        {
            ["reachable"] = true,
            ["totalDistance"] = System.Math.Round(totalDist, 1),
            ["directions"] = steps,
            ["destinationRoom"] = node.room
        }.ToString();
    }

    private string GetTurnDirection(Vector3 currentFacing, Vector3 newDirection)
    {
        float angle = Vector3.SignedAngle(currentFacing, newDirection, Vector3.up);

        if (angle > 135f) return "Turn around";
        if (angle > 25f) return "Turn right";
        if (angle < -135f) return "Turn around";
        if (angle < -25f) return "Turn left";
        return "Go straight";
    }

    private string ShowPathToUser(JObject args)
    {
        string objectId = args["object_id"]?.ToString();

        if (navigationGuide == null)
            navigationGuide = FindFirstObjectByType<NavigationGuide>();

        if (navigationGuide == null)
            return "{\"error\": \"NavigationGuide not found\"}";

        bool success = navigationGuide.ShowTheWayById(objectId);
        return new JObject { ["success"] = success }.ToString();
    }

    private string CheckAffordance(JObject args)
    {
        string objectId = args["object_id"]?.ToString();
        string action = args["action"]?.ToString();
        var node = SceneGraphManager.Instance.GetNodeById(objectId);

        if (node == null)
            return $"{{\"error\": \"Object '{objectId}' not found\"}}";

        bool possible = node.affordances != null && node.affordances.Contains(action);
        string reason = possible
            ? $"Yes, you can {action} the {node.objectType}."
            : $"No, you cannot {action} the {node.objectType}.";

        return new JObject
        {
            ["possible"] = possible,
            ["reason"] = reason
        }.ToString();
    }

    private string GetDescription(JObject args)
    {
        string objectId = args["object_id"]?.ToString();
        var node = SceneGraphManager.Instance.GetNodeById(objectId);

        if (node == null)
            return $"{{\"error\": \"Object '{objectId}' not found\"}}";

        return new JObject
        {
            ["objectId"] = objectId,
            ["type"] = node.objectType,
            ["description"] = node.Description
        }.ToString();
    }
}
