using System;
using System.IO;
using System.Runtime.InteropServices;
using SimpleTriggers.Logger;

namespace SimpleTriggers.TextToSpeech;

public class DecTalk : ITextToSpeech {
    private readonly nint handle;
    private readonly AudioPlayer audioPlayer;

    public DecTalk(string dtResPath, AudioPlayer player)
    {
        audioPlayer = player;
        audioPlayer.SetSourceWaveFormat(11025, 1);
        // sha256: a3dd5cedf74abc0c5bc1c6aef34ec96287ed5fe256b72d4df1ce52db9146e586
        DecTalkImports.SetupResolver(Path.Join(dtResPath, "dectalk/dtalk_us.dll"));
        var dictionaryPath = Path.Join(dtResPath, "dectalk/dtalk_us.dic");
        AssertCall(
            DecTalkImports.TextToSpeechStartupExFonix(ref this.handle, -1, 0, nint.Zero, 0, dictionaryPath),
            "TextToSpeechStartup");
        AssertCall(
            DecTalkImports.TextToSpeechOpenInMemory(this.handle, DecTalkImports.WaveFormat.WAVE_FORMAT_1M16),
            "TextToSpeechOpenInMemory");
    }

    public void Dispose()
    {
        try {
            Reset(true);
            AssertCall(DecTalkImports.TextToSpeechShutdown(this.handle),"TextToSpeechShutdown");
            DecTalkImports.Free();
        } catch(Exception e)
        {
            STLog.Log.Error(e, "DecTalk.Dispose(): Exception caught:");
        }
        GC.SuppressFinalize(this);
    }

    public void Speak(string text, bool extra)
    {
        try {
            // If something bad happens here... pray
            unsafe {
                var syncText = text + " [:sync]";
                // For some reason punctuation locks it up, needs deeper testing
                syncText = syncText.Replace(".", "");
                syncText = syncText.Replace(",", "");
                syncText = syncText.Replace("!", "");
                syncText = syncText.Replace("?", "");

                var buffSize = 1024 * 1024 * 2; // 2MB
                var buffer = new DecTalkImports.TTS_BUFFER();
                buffer.Data = Marshal.AllocHGlobal(buffSize);
                buffer.MaximumBufferLength = buffSize;
                AssertCall(DecTalkImports.TextToSpeechAddBuffer(this.handle, &buffer), "TextToSpeechAddBuffer");
                AssertCall(DecTalkImports.TextToSpeechSpeak(this.handle, syncText, DecTalkImports.SpeechFlags.Force), "TextToSpeechSpeak");
                AssertCall(DecTalkImports.TextToSpeechSync(this.handle), "TextToSpeechSync");

                var data = new byte[buffSize];
                Marshal.Copy(buffer.Data, data, 0, buffSize);
                audioPlayer.Enqueue(data);

                Marshal.FreeHGlobal(buffer.Data);
                Reset(false);
            }
        } catch (Exception e)
        {
            STLog.Log.Error(e, "DecTalk.Speak(): Exception caught:");
        }
    }

    public void SetVoice(string voice)
    {
        if(!Enum.TryParse(voice, true, out DecTalkVoice dv))
        {
            STLog.Log.Warning($"Could not find voice {voice}. Falling back to default.");
            dv = DecTalkVoice.PAUL;
        }
        try {
            AssertCall(DecTalkImports.TextToSpeechSetSpeaker(this.handle, dv), "TextToSpeechSetSpeaker");
        } catch (Exception e)
        {
            STLog.Log.Error(e, "Exception caught:");
        }
    }
    
    public void SetVolume(float volume)
    {
        audioPlayer.SetVolume(volume);
    }

    public void SetSpeed(float speed)
    {
        try {
            AssertCall(DecTalkImports.TextToSpeechSetRate(this.handle, (uint)speed), "TextToSpeechSetRate");
        } catch (Exception e)
        {
            STLog.Log.Error(e, "Exception caught:");
        }
    }

    public void SetLanguage(string lang)
    { } // Unused, DecTalk only supports English

    public bool IsInitialized()
    {
        return handle != 0;
    }

    public void Reset(bool reset) => AssertCall(DecTalkImports.TextToSpeechReset(this.handle, reset), "TextToSpeechReset");

    public void GetStatus(out uint[] statuses)
    {
        statuses = [0, 0, 0];
        DecTalkImports.StatusId[] identifiers = [
            DecTalkImports.StatusId.INPUT_CHARACTER_COUNT, DecTalkImports.StatusId.STATUS_SPEAKING, DecTalkImports.StatusId.WAVE_OUT_DEVICE_ID
        ];
        AssertCall(
            DecTalkImports.TextToSpeechGetStatus(this.handle, identifiers, statuses, 3),
            "TextToSpeechGetStatus"
        );
    }

    private static void AssertCall(uint value, string method) {
        if (value != 0) throw new Exception($"Calling {method} returned error code {value}");
    }
}