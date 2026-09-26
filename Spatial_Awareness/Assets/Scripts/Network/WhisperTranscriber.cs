using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class WhisperTranscriber : MonoBehaviour
{
    [Tooltip("Your OpenAI API key")]
    public string apiKey = "";

    private const string WhisperUrl = "https://api.openai.com/v1/audio/transcriptions";

    public void Transcribe(byte[] wavBytes, Action<string> onResult)
    {
        if (wavBytes == null || wavBytes.Length == 0)
        {
            onResult?.Invoke(null);
            return;
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("[WhisperTranscriber] API key is not set!");
            onResult?.Invoke(null);
            return;
        }

        StartCoroutine(SendRequest(wavBytes, onResult));
    }

    private IEnumerator SendRequest(byte[] wavBytes, Action<string> onResult)
    {
        var form = new WWWForm();
        form.AddBinaryData("file", wavBytes, "recording.wav", "audio/wav");
        form.AddField("model", "whisper-1");

        var request = UnityWebRequest.Post(WhisperUrl, form);
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[WhisperTranscriber] Error: {request.error}\n{request.downloadHandler.text}");
            onResult?.Invoke(null);
        }
        else
        {
            var response = JsonUtility.FromJson<WhisperResponse>(request.downloadHandler.text);
            Debug.Log($"[WhisperTranscriber] Transcription: {response.text}");
            onResult?.Invoke(response.text);
        }
    }

    [Serializable]
    private class WhisperResponse
    {
        public string text;
    }
}
