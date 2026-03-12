using System;
using System.Speech.Synthesis;
using Serilog;

public class STWinSpeech : ITextToSpeech
{
    private SpeechSynthesizer synth {get; init;}
    public STWinSpeech()
    {
        synth = new SpeechSynthesizer();
        synth.SetOutputToDefaultAudioDevice();
    }

    public void SetVoice(string voice)
    {
        try
        {
            synth.SelectVoice(voice);
        } catch (Exception)
        {
            Log.Warning($"[Simple Triggers]: STWinSpeech.SetVoice(): Voice \"{voice}\" not found.");
        }
    }

    public void SetVolume(float volume)
    {
        synth.Volume = (int)Math.Clamp(volume, 0, 100);
    }

    public void SetSpeed(float speed)
    { }
    
    public void Speak(string message, bool extra)
    {
        try
        {
            synth.SpeakAsync(message);
        } catch (Exception e)
        {
            Log.Error($"[Simple Triggers]: STWinSpeech.Speak(): Exception caught: {e.Message}");
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