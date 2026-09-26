using UnityEngine;

public class DebugChatUI : MonoBehaviour
{
    [Header("Display")]
    [Tooltip("Show the text chat box (input field, Send button, and the agent's answer). Uncheck before Play for a pure voice run: move around, hold right mouse to talk, and listen to the spoken reply — only the recording indicator appears on screen.")]
    [SerializeField] private bool showChatBox = true;

    private string inputText = "";
    private string responseText = "";
    private bool chatOpen = false;
    private SpatialQueryManager sqm;
    private VoiceRecorder voiceRecorder;
    private WhisperTranscriber whisperTranscriber;
    private bool isTranscribing;

    public static bool IsChatActive { get; private set; }

    void Start()
    {
        sqm = FindFirstObjectByType<SpatialQueryManager>();
        voiceRecorder = GetComponent<VoiceRecorder>();
        whisperTranscriber = GetComponent<WhisperTranscriber>();
        WebSocketClient.Instance.OnFinalResponseReceived += msg =>
        {
            responseText = msg["text"]?.ToString() ?? "";
        };
    }

    void Update()
    {
        // Enter opens the text chat box (only when the chat box is enabled)
        if (showChatBox && Input.GetKeyDown(KeyCode.Return) && !chatOpen)
        {
            chatOpen = true;
            IsChatActive = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetKeyDown(KeyCode.Escape) && chatOpen)
        {
            chatOpen = false;
            IsChatActive = false;
            responseText = "";
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Hold Right Mouse Button to record voice — works while walking, with no
        // need to open the chat box first, so you can move and talk at the same time.
        if (Input.GetMouseButtonDown(1) && !isTranscribing)
        {
            voiceRecorder.StartRecording();
        }

        if (Input.GetMouseButtonUp(1) && voiceRecorder.IsRecording)
        {
            voiceRecorder.StopRecording();
            byte[] wav = voiceRecorder.GetWavBytes();
            if (wav != null)
            {
                isTranscribing = true;
                responseText = "Transcribing...";
                whisperTranscriber.Transcribe(wav, OnTranscriptionResult);
            }
        }
    }

    private void OnTranscriptionResult(string text)
    {
        isTranscribing = false;
        if (!string.IsNullOrEmpty(text))
        {
            responseText = "Thinking...";
            sqm.SendQuery(text);
        }
        else
        {
            responseText = "Could not transcribe audio. Try again.";
        }
    }

    void OnGUI()
    {
        bool recording = voiceRecorder != null && voiceRecorder.IsRecording;

        // Draw nothing unless the chat box is open or we are recording (push-to-talk)
        if (!chatOpen && !recording) return;

        float w = Screen.width * 0.75f;
        float h = 70f;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height - 100f;

        // Text chat box (answer + input + Send) — only when enabled and opened with Enter
        if (showChatBox && chatOpen)
        {
            // Agent's answer above the input
            if (!string.IsNullOrEmpty(responseText))
            {
                var style = new GUIStyle(GUI.skin.box);
                style.wordWrap = true;
                style.fontSize = 22;
                style.alignment = TextAnchor.UpperLeft;
                style.padding = new RectOffset(8, 8, 8, 8);

                float responseHeight = style.CalcHeight(new GUIContent(responseText), w) + 16f;
                GUI.Box(new Rect(x, y - responseHeight - 10f, w, responseHeight), responseText, style);
            }

            // Input field
            var inputStyle = new GUIStyle(GUI.skin.textField);
            inputStyle.fontSize = 22;
            GUI.SetNextControlName("ChatInput");
            inputText = GUI.TextField(new Rect(x, y, w - 70f, h), inputText, inputStyle);
            GUI.FocusControl("ChatInput");

            // Send button or Enter key
            var buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 22;
            bool send = GUI.Button(new Rect(x + w - 65f, y, 65f, h), "Send", buttonStyle);
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && !string.IsNullOrEmpty(inputText))
                send = true;

            if (send && !string.IsNullOrEmpty(inputText))
            {
                responseText = "Thinking...";
                sqm.SendQuery(inputText);
                inputText = "";
            }
        }

        // Voice recording indicator — shown whether or not the chat box is open
        if (recording)
        {
            var recStyle = new GUIStyle(GUI.skin.box);
            recStyle.fontSize = 22;
            recStyle.alignment = TextAnchor.MiddleCenter;
            recStyle.normal.textColor = Color.red;
            GUI.Box(new Rect(x, y - 50f, w, 40f), "Recording... (release right mouse button to stop)", recStyle);
        }
    }
}
