using System;
using System.IO;
using System.Speech.Synthesis;
using SimpleTriggers.Logger;

namespace SimpleTriggers.TextToSpeech;

public class STWinSpeech : ITextToSpeech
{
    private readonly AudioPlayer audioPlayer;
    private SpeechSynthesizer synth {get; init;}
    private MemoryStream? stream;
    public STWinSpeech(AudioPlayer player)
    {
        audioPlayer = player;
        synth = new SpeechSynthesizer();
        synth.SpeakCompleted += OnSpeakCompleted;
    }

    public void SetVoice(string voice)
    {
        try
        {
            if(voice.Length != 0) synth.SelectVoice(voice);
        } catch (Exception e)
        {
            STLog.Log.Warning(e, $"Voice \"{voice}\" not found.");
        }
    }

    public void SetVolume(float volume)
    {
        audioPlayer.SetVolume(volume);
    }

    public void SetSpeed(float speed)
    {
        synth.Rate = (int)speed;
    }

    public void SetLanguage(string lang)
    { } // Language is controlled by the user's Windows Settings

    public void Speak(string message, bool extra)
    {
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
            STLog.Log.Error(e.Error, "STWinSpeech.OnSpeakCompleted():");
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
        audioPlayer.Enqueue(data);
    }

    public bool IsInitialized()
    {
        return true;
    }

    public void Dispose()
    {
        synth.SpeakCompleted -= OnSpeakCompleted;
        synth.Dispose();
        stream?.Dispose();
    }
}