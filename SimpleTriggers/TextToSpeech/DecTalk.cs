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
        var dllPath = Path.Join(dtResPath, "dectalk/dtalk_us.dll");
        if(!DecTalkImports.IsLoaded(dllPath))
        {
            // sha256: a3dd5cedf74abc0c5bc1c6aef34ec96287ed5fe256b72d4df1ce52db9146e586
            DecTalkImports.SetupResolver(dllPath);
        } else { STLog.Log.Warning("DECTalk DLL already loaded."); }

        var dictionaryPath = Path.Join(dtResPath, "dectalk/dtalk_us.dic");
        AssertCall(
            DecTalkImports.TextToSpeechStartupExFonix(ref this.handle, DecTalkImports.WaveMapper, 0,
                nint.Zero, 0,
                dictionaryPath
            ),"TextToSpeechStartup");
        AssertCall(
            DecTalkImports.TextToSpeechOpenInMemory(this.handle, DecTalkImports.WaveFormat.WAVE_FORMAT_1M16),
            "TextToSpeechOpenInMemory");
    }

    public void Dispose()
    {
        try {
            Reset(true);
            AssertCall(DecTalkImports.TextToSpeechShutdown(this.handle),"TextToSpeechShutdown");
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
                var buffSize = 1024 * 1024 * 8; // no shot a trigger will be this big, but I'd rather it be too big than lock up
                var buffer = new DecTalkImports.TTS_BUFFER();
                buffer.Data = Marshal.AllocHGlobal(buffSize);
                buffer.MaximumBufferLength = buffSize;
                AssertCall(DecTalkImports.TextToSpeechAddBuffer(this.handle, &buffer), "TextToSpeechAddBuffer");
                AssertCall(DecTalkImports.TextToSpeechSpeak(this.handle, syncText, DecTalkImports.SpeechFlags.Force),"TextToSpeechSpeak");
                AssertCall(DecTalkImports.TextToSpeechSync(this.handle),"TextToSpeechSync");

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
            AssertCall(DecTalkImports.TextToSpeechSetSpeaker(this.handle, dv),"TextToSpeechSetSpeaker");
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
            AssertCall(DecTalkImports.TextToSpeechSetRate(this.handle, (uint)speed),"TextToSpeechSetRate");
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

    public void Reset(bool reset) => AssertCall(DecTalkImports.TextToSpeechReset(this.handle, reset),"TextToSpeechReset");

    // Unused currently
    public bool IsBusy() {
        uint[] identifiers = [DecTalkImports.StatusSpeaking];
        uint[] statuses = [0];

        AssertCall(
            DecTalkImports.TextToSpeechGetStatus(this.handle, identifiers, statuses, 1),
            "TextToSpeechGetStatus"
        );

        return statuses[0] != 0;
    }

    private static void AssertCall(uint value, string method) {
        if (value != 0) throw new Exception($"Calling {method} returned error code {value}");
    }
}