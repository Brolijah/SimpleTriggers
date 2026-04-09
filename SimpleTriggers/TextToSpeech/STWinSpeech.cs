using System;
using System.IO;
using System.Speech.Synthesis;
using SimpleTriggers.Logger;
using SimpleTriggers.TextToSpeech;

public class STWinSpeech : ITextToSpeech
{
    public AudioPlayer AudioPlayer { get; }
    private SpeechSynthesizer synth {get; init;}
    private MemoryStream? stream;
    public STWinSpeech()
    {
        AudioPlayer = new AudioPlayer();
        synth = new SpeechSynthesizer();
        synth.SpeakCompleted += OnSpeakCompleted;
    }

    public void SetVoice(string voice)
    {
        try
        {
            synth.SelectVoice(voice);
        } catch (Exception e)
        {
            STLog.Log.Warning(e, $"Voice \"{voice}\" not found.");
        }
    }

    public void SetVolume(float volume)
    {
        AudioPlayer.SetVolume(volume);
    }

    public void SetSpeed(float speed)
    {
        synth.Rate = (int)speed;
    }

    public void SetLanguage(string lang)
    { } // Language is controlled by the user's Windows Settings

    public void Speak(string message, bool extra)
    {
        AudioPlayer.StopPlayback(true);
        try
        {
            synth.SpeakAsyncCancelAll();

            stream?.Dispose();
            stream = new MemoryStream();
            
            synth.SetOutputToWaveStream(stream);
            synth.SpeakAsync(message);
        } catch (Exception e)
        {
            STLog.Log.Error(e, "STWinSpeech.Speak(): Exception caught:");
        }
    }

    private void OnSpeakCompleted(object? sender, SpeakCompletedEventArgs e)
    {
        if(e.Error is not null)
        {
            STLog.Log.Error($"STWinSpeech.OnSpeakCompleted(): Error: {e.Error.Message}");
            return;
        }
        if(e.Cancelled || stream == null) // don't care, abort
        {
            return;
        }
        // Queues the stream into the AudioPlayer
        var data = new byte[stream.Length-44]; // will hold raw PCM stream without header
        stream.Position = 44;
        stream.Read(data, 0, data.Length);
        AudioPlayer.Enqueue(data);
    }

    public bool IsInitialized()
    {
        return true;
    }

    public void Dispose()
    {
        AudioPlayer.Dispose();
        synth.SpeakCompleted -= OnSpeakCompleted;
        synth.Dispose();
        stream?.Dispose();
    }
}