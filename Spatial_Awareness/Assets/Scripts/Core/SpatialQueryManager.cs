using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpatialAwareness.SceneGraph;

public class SpatialQueryManager : MonoBehaviour
{
    private bool contextSent = false;

    void OnEnable()
    {
        if (WebSocketClient.Instance == null)
        {
            Debug.LogWarning("[SpatialQuery] WebSocketClient not ready yet, retrying in Start.");
            return;
        }
        WebSocketClient.Instance.OnToolCallReceived += HandleToolCall;
        WebSocketClient.Instance.OnFinalResponseReceived += HandleFinalResponse;
    }

    void Start()
    {
        // Retry subscription if WebSocketClient wasn't ready in OnEnable
        if (WebSocketClient.Instance != null)
        {
            WebSocketClient.Instance.OnToolCallReceived -= HandleToolCall;
            WebSocketClient.Instance.OnFinalResponseReceived -= HandleFinalResponse;
            WebSocketClient.Instance.OnToolCallReceived += HandleToolCall;
            WebSocketClient.Instance.OnFinalResponseReceived += HandleFinalResponse;
        }
    }

    void OnDisable()
    {
        if (WebSocketClient.Instance != null)
        {
            WebSocketClient.Instance.OnToolCallReceived -= HandleToolCall;
            WebSocketClient.Instance.OnFinalResponseReceived -= HandleFinalResponse;
        }
    }

    public async void SendQuery(string userText) //Builds JSON message, attaches scene context on first call, sends to Python
    {
        if (!WebSocketClient.Instance.IsConnected)
        {
            Debug.LogWarning("[SpatialQuery] Not connected to Python server");
            return;
        }

        var message = new JObject { ["type"] = "query", ["text"] = userText };

        // First query: include scene context
        if (!contextSent)
        {
            var context = SceneGraphManager.Instance.BuildRoomContext();
            var contextJson = JObject.Parse(JsonConvert.SerializeObject(context));
            message["context"] = contextJson;
            contextSent = true;
        }

        // Attach live spatial snapshot with every query
        message["spatial_context"] = BuildSpatialSnapshot();

        Debug.Log($"[SpatialQuery] Sending: {userText}");
        await WebSocketClient.Instance.SendMessage(message.ToString());
    }

    private JObject BuildSpatialSnapshot()
    {
        var cam = Camera.main.transform;
        var nearby = SceneGraphManager.Instance.GetNearbyNodes(cam.position, 5f);

        string userRoom = SceneGraphManager.Instance.currentRoom;

        var nearbyArr = new JArray();
        foreach (var node in nearby)
        {
            if (!string.Equals(node.room, userRoom, System.StringComparison.OrdinalIgnoreCase))
                continue;
            if (nearbyArr.Count >= 5) break;
            float dist = SpatialReasoning.GetDistanceTo(cam.position, node.transform.position);
            string dir = SpatialReasoning.GetRelativeDirectionText(cam, node.transform.position);
            nearbyArr.Add(new JObject
            {
                ["id"] = node.id,
                ["type"] = node.objectType,
                ["distance"] = System.Math.Round(dist, 1),
                ["direction"] = dir
            });
        }

        return new JObject
        {
            ["room"] = SceneGraphManager.Instance.currentRoom,
            ["nearest"] = nearbyArr
        };
    }

    private async void HandleToolCall(JObject message) //Receives tool call from Python → runs FunctionDispatcher.Execute() → sends result back
    {
        string callId = message["call_id"]?.ToString();
        string functionName = message["function"]?.ToString();
        JObject arguments = message["arguments"] as JObject ?? new JObject();

        Debug.Log($"[SpatialQuery] Tool call: {functionName}({arguments})");

        string result = FunctionDispatcher.Instance.Execute(functionName, arguments);

        Debug.Log($"[SpatialQuery] Tool result: {result.Substring(0, Mathf.Min(result.Length, 300))}");

        var response = new JObject
        {
            ["type"] = "tool_result",
            ["call_id"] = callId,
            ["result"] = result
        };

        await WebSocketClient.Instance.SendMessage(response.ToString());
    }

    private void HandleFinalResponse(JObject message) //Receives LLM's final text answer → logs it
    {
        string text = message["text"]?.ToString();
        Debug.Log($"[SpatialQuery] Response: {text}");
    }
}
