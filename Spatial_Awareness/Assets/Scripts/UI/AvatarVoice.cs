using System;
using UnityEngine;
using Newtonsoft.Json.Linq;

/// <summary>
/// Speaks the avatar's reply. Subscribes to the WebSocket "final_response"
/// message, decodes the base64 PCM produced by the Python TTS step, and plays
/// it. The message arrives on a background thread, so the AudioClip is built
/// and played on the main thread inside Update().
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AvatarVoice : MonoBehaviour
{
    // OpenAI TTS "pcm" output is always 24 kHz, 16-bit signed, mono, little-endian.
    private const int SampleRate = 24000;

    private AudioSource audioSource;
    private volatile byte[] pendingPcm;   // written on WS thread, read on main thread

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        if (WebSocketClient.Instance != null)
            WebSocketClient.Instance.OnFinalResponseReceived += HandleFinalResponse;
        else
            Debug.LogWarning("[AvatarVoice] WebSocketClient not found; no voice output.");
    }

    void OnDestroy()
    {
        if (WebSocketClient.Instance != null)
            WebSocketClient.Instance.OnFinalResponseReceived -= HandleFinalResponse;
    }

    // Runs on the WebSocket receive thread — do NOT call the Unity API here.
    private void HandleFinalResponse(JObject message)
    {
        string b64 = message["audio"]?.ToString();
        if (string.IsNullOrEmpty(b64)) return;
        try { pendingPcm = Convert.FromBase64String(b64); }
        catch (Exception e) { Debug.LogError($"[AvatarVoice] Bad audio payload: {e.Message}"); }
    }

    void Update()
    {
        if (pendingPcm == null) return;
        byte[] pcm = pendingPcm;
        pendingPcm = null;
        Speak(pcm);
    }

    private void Speak(byte[] pcm)
    {
        int sampleCount = pcm.Length / 2;          // 2 bytes per 16-bit sample
        if (sampleCount == 0) return;

        var samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            short s = (short)(pcm[2 * i] | (pcm[2 * i + 1] << 8));  // little-endian
            samples[i] = s / 32768f;
        }

        var clip = AudioClip.Create("AvatarVoice", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }
}
