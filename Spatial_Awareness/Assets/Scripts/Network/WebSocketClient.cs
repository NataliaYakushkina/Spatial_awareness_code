using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class WebSocketClient : MonoBehaviour
{
    public static WebSocketClient Instance { get; private set; }

    [SerializeField] private string serverUrl = "ws://localhost:8765";

    private ClientWebSocket socket;
    private CancellationTokenSource cts;

    public bool IsConnected => socket?.State == WebSocketState.Open;

    public event Action<JObject> OnToolCallReceived;
    public event Action<JObject> OnFinalResponseReceived;
    public event Action OnDisconnected;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    async void Start()
    {
        await Connect();
    }

    public async Task Connect()
    {
        socket = new ClientWebSocket();
        cts = new CancellationTokenSource();

        try
        {
            await socket.ConnectAsync(new Uri(serverUrl), cts.Token);
            Debug.Log($"[WebSocket] Connected to {serverUrl}");
            _ = ReceiveLoop();
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebSocket] Connection failed: {e.Message}");
        }
    }

    public async Task SendMessage(string jsonMessage)
    {
        if (!IsConnected) { Debug.LogWarning("[WebSocket] Not connected"); return; }

        var bytes = Encoding.UTF8.GetBytes(jsonMessage);
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
    }

    private async Task ReceiveLoop()
    {
        var buffer = new byte[8192];

        try
        {
            while (socket.State == WebSocketState.Open)
            {
                // Accumulate fragments until the full message is received
                var messageBuilder = new StringBuilder();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                var json = messageBuilder.ToString();
                var message = JObject.Parse(json);
                var msgType = message["type"]?.ToString();

                switch (msgType)
                {
                    case "tool_call":
                        OnToolCallReceived?.Invoke(message);
                        break;
                    case "final_response":
                        OnFinalResponseReceived?.Invoke(message);
                        break;
                    default:
                        Debug.LogWarning($"[WebSocket] Unknown message type: {msgType}");
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebSocket] Receive error: {e.Message}");
        }

        Debug.Log("[WebSocket] Disconnected");
        OnDisconnected?.Invoke();
    }

    public async Task Disconnect()
    {
        if (socket != null && socket.State == WebSocketState.Open)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
        cts?.Cancel();
    }

    void OnDestroy()
    {
        Disconnect().ConfigureAwait(false);
    }
}
