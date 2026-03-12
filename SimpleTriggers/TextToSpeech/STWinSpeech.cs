using System;
using System.Speech.Synthesis;

public class STWinSpeech : ITextToSpeech
{
    private SpeechSynthesizer synth {get; init;}
    public STWinSpeech()
    {
        synth = new SpeechSynthesizer();
        synth.SetOutputToDefaultAudioDevice();
    }

    public void SetVoice(string voice)
    { }

    public void SetVolume(float volume)
    {
        synth.Volume = (int)Math.Clamp(volume, 0, 100);
    }

    public void SetSpeed(float speed)
    { }
    
    public void Speak(string message, bool extra)
    {
        synth.SpeakAsync(message);
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