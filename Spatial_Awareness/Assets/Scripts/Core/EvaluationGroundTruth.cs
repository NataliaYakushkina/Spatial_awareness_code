using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json.Linq;
using SpatialAwareness.SceneGraph;

/// <summary>
/// RQ1 evaluation helper. Exports the deterministic ground truth that Unity
/// computes for the player's CURRENT pose, so each query can be graded against
/// an exact answer key instead of by eye.
///
/// Workflow (manual run): walk to a stop, set <see cref="stopLabel"/> in the
/// Inspector, press F5 to write &lt;stopLabel&gt;_gt.json. F9 prints the current
/// pose to the Console so the stop can be recorded/reproduced.
///
/// The ground truth is computed from Camera.main.transform and the same
/// SpatialReasoning routines the live tools use, so the exported facts match
/// exactly what get_direction_to / find_objects / get_nearby_objects return.
/// </summary>
public class EvaluationGroundTruth : MonoBehaviour
{
    [Header("Current stop")]
    [Tooltip("Filename label for the exported ground truth (e.g. Stop1_Room_01).")]
    public string stopLabel = "Stop1";

    [Header("Output")]
    [Tooltip("Folder under the project root (next to JSONSnapshots).")]
    public string outputFolder = "RQ1_GroundTruth";

    [Header("Hotkeys")]
    public KeyCode exportKey = KeyCode.F5;
    public KeyCode capturePoseKey = KeyCode.F9;

    void Update()
    {
        if (Input.GetKeyDown(exportKey)) ExportGroundTruth();
        if (Input.GetKeyDown(capturePoseKey)) CapturePose();
    }

    [ContextMenu("Export Ground Truth (current pose)")]
    public void ExportGroundTruth()
    {
        if (Camera.main == null) { Debug.LogWarning("[EvalGT] No Camera.main found."); return; }

        Transform user = Camera.main.transform;
        Vector3 userPos = user.position;

        var nodes = Object.FindObjectsByType<SceneNode>(FindObjectsSortMode.None);

        var objList = new List<KeyValuePair<float, JObject>>();
        var roomContents = new JObject();
        var typeCountsPerRoom = new JObject();

        foreach (var node in nodes)
        {
            if (node.CompareTag("Player")) continue;

            Vector3 pos = node.transform.position;
            float dist = SpatialReasoning.GetDistanceTo(userPos, pos);
            string dir = SpatialReasoning.GetRelativeDirectionText(user, pos);
            bool vis = SpatialReasoning.CheckVisibility(userPos, pos);

            objList.Add(new KeyValuePair<float, JObject>(dist, new JObject
            {
                ["objectId"] = node.id,
                ["type"] = node.objectType,
                ["room"] = node.room,
                ["distance"] = System.Math.Round(dist, 1),
                ["relativeDirection"] = dir,
                ["isVisible"] = vis
            }));

            string room = string.IsNullOrEmpty(node.room) ? "(none)" : node.room;

            if (roomContents[room] == null) roomContents[room] = new JArray();
            ((JArray)roomContents[room]).Add(new JObject
            {
                ["objectId"] = node.id,
                ["type"] = node.objectType
            });

            if (typeCountsPerRoom[room] == null) typeCountsPerRoom[room] = new JObject();
            var counts = (JObject)typeCountsPerRoom[room];
            string t = string.IsNullOrEmpty(node.objectType) ? "(none)" : node.objectType;
            counts[t] = (counts[t]?.ToObject<int>() ?? 0) + 1;
        }

        objList.Sort((a, b) => a.Key.CompareTo(b.Key));

        var objectsArr = new JArray();
        var nearest5 = new JArray();
        for (int i = 0; i < objList.Count; i++)
        {
            objectsArr.Add(objList[i].Value);
            if (i < 5) nearest5.Add(objList[i].Value.DeepClone());
        }

        var data = new JObject
        {
            ["stop"] = stopLabel,
            ["trackedRoom"] = SceneGraphManager.Instance != null ? SceneGraphManager.Instance.currentRoom : "",
            ["userPosition"] = new JObject { ["x"] = userPos.x, ["y"] = userPos.y, ["z"] = userPos.z },
            ["yaw"] = System.Math.Round(user.eulerAngles.y, 1),
            ["facing"] = AngleToCompass(user.forward),
            ["nearest5"] = nearest5,
            ["objects"] = objectsArr,           // every object, sorted by distance
            ["roomContents"] = roomContents,    // per-room id + type lists
            ["typeCountsPerRoom"] = typeCountsPerRoom
        };

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string folder = Path.Combine(projectRoot, outputFolder);
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        string safe = string.IsNullOrEmpty(stopLabel) ? "stop" : stopLabel.Replace(" ", "_");
        string path = Path.Combine(folder, $"{safe}_gt.json");
        File.WriteAllText(path, data.ToString(Newtonsoft.Json.Formatting.Indented));

        Debug.Log($"[EvalGT] Ground truth for '{stopLabel}' ({objList.Count} objects) -> {path}");
    }

    [ContextMenu("Capture current pose (log to Console)")]
    public void CapturePose()
    {
        if (Camera.main == null) { Debug.LogWarning("[EvalGT] No Camera.main found."); return; }
        Transform user = Camera.main.transform;
        string room = SceneGraphManager.Instance != null ? SceneGraphManager.Instance.currentRoom : "?";
        Debug.Log($"[EvalGT] POSE  position=({user.position.x:F2}, {user.position.y:F2}, {user.position.z:F2})  " +
                  $"yaw={user.eulerAngles.y:F1}  facing={AngleToCompass(user.forward)}  room={room}");
    }

    // Mirrors the compass logic in FunctionDispatcher.GetUserContext.
    private static string AngleToCompass(Vector3 forward)
    {
        float angle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        if (angle >= 337.5f || angle < 22.5f) return "north";
        if (angle < 67.5f) return "north-east";
        if (angle < 112.5f) return "east";
        if (angle < 157.5f) return "south-east";
        if (angle < 202.5f) return "south";
        if (angle < 247.5f) return "south-west";
        if (angle < 292.5f) return "west";
        return "north-west";
    }
}
