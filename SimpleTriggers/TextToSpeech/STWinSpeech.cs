using System;
using System.IO;
using System.Speech.Synthesis;
using System.Threading;
using SimpleTriggers.Logger;

namespace SimpleTriggers.TextToSpeech;

public class STWinSpeech : ITextToSpeech
{
    private readonly AudioPlayer audioPlayer;
    private SpeechSynthesizer synth {get; init;}
    private Lock synthLock = new();
    public STWinSpeech(AudioPlayer player)
    {
        audioPlayer = player;
        audioPlayer.SetSourceWaveFormat(24000, 1);
        synth = new SpeechSynthesizer();
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
        lock(synthLock)
        {
            try {
                using var stream = new MemoryStream();
                synth.SetOutputToWaveStream(stream);
                synth.Speak(message);
                
                var data = new byte[stream.Length - 44]; // will hold raw PCM stream without header
                stream.Position = 44;
                stream.Read(data, 0, data.Length);
                audioPlayer.Enqueue(data);
            } catch (Exception e)
            {
                STLog.Log.Error(e, "STWinSpeech.Speak(): Exception caught:");
            }   
        }
    }

    public bool IsInitialized()
    {
        return true;
    }

    public void Dispose()
    {
        synth.Dispose();
    }
}