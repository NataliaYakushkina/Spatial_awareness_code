using UnityEngine;

public class VoiceRecorder : MonoBehaviour
{
    private AudioClip recording;
    private string micDevice;
    private bool isRecording;
    private const int SampleRate = 16000;
    private const int MaxRecordSeconds = 30;

    public bool IsRecording => isRecording;

    void Start()
    {
        if (Microphone.devices.Length > 0)
            micDevice = Microphone.devices[0];
        else
            Debug.LogError("[VoiceRecorder] No microphone found!");
    }

    public void StartRecording()
    {
        if (micDevice == null || isRecording) return;
        recording = Microphone.Start(micDevice, false, MaxRecordSeconds, SampleRate);
        isRecording = true;
        Debug.Log("[VoiceRecorder] Recording started...");
    }

    public void StopRecording()
    {
        if (!isRecording) return;
        int position = Microphone.GetPosition(micDevice);
        Microphone.End(micDevice);
        isRecording = false;

        if (position <= 0)
        {
            Debug.LogWarning("[VoiceRecorder] No audio recorded. Hold the button a bit longer.");
            recording = null;
            return;
        }

        // Trim clip to actual recorded length
        float[] samples = new float[position * recording.channels];
        recording.GetData(samples, 0);
        var trimmed = AudioClip.Create("Recording", position, recording.channels, SampleRate, false);
        trimmed.SetData(samples, 0);
        recording = trimmed;

        Debug.Log($"[VoiceRecorder] Recording stopped. {position} samples captured.");
    }

    public byte[] GetWavBytes()
    {
        if (recording == null) return null;

        float[] samples = new float[recording.samples * recording.channels];
        recording.GetData(samples, 0);

        // Convert to 16-bit PCM WAV
        int sampleCount = samples.Length;
        int byteRate = SampleRate * recording.channels * 2;
        int dataSize = sampleCount * 2;
        int fileSize = 44 + dataSize;

        byte[] wav = new byte[fileSize];
        int pos = 0;

        // RIFF header
        WriteString(wav, ref pos, "RIFF");
        WriteInt32(wav, ref pos, fileSize - 8);
        WriteString(wav, ref pos, "WAVE");

        // fmt chunk
        WriteString(wav, ref pos, "fmt ");
        WriteInt32(wav, ref pos, 16);
        WriteInt16(wav, ref pos, 1); // PCM
        WriteInt16(wav, ref pos, (short)recording.channels);
        WriteInt32(wav, ref pos, SampleRate);
        WriteInt32(wav, ref pos, byteRate);
        WriteInt16(wav, ref pos, (short)(recording.channels * 2));
        WriteInt16(wav, ref pos, 16); // bits per sample

        // data chunk
        WriteString(wav, ref pos, "data");
        WriteInt32(wav, ref pos, dataSize);

        for (int i = 0; i < sampleCount; i++)
        {
            short val = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767);
            wav[pos++] = (byte)(val & 0xFF);
            wav[pos++] = (byte)((val >> 8) & 0xFF);
        }

        return wav;
    }

    private void WriteString(byte[] buffer, ref int pos, string value)
    {
        foreach (char c in value)
            buffer[pos++] = (byte)c;
    }

    private void WriteInt32(byte[] buffer, ref int pos, int value)
    {
        buffer[pos++] = (byte)(value & 0xFF);
        buffer[pos++] = (byte)((value >> 8) & 0xFF);
        buffer[pos++] = (byte)((value >> 16) & 0xFF);
        buffer[pos++] = (byte)((value >> 24) & 0xFF);
    }

    private void WriteInt16(byte[] buffer, ref int pos, short value)
    {
        buffer[pos++] = (byte)(value & 0xFF);
        buffer[pos++] = (byte)((value >> 8) & 0xFF);
    }
}
