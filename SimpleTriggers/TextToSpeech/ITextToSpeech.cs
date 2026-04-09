
using System;
using SimpleTriggers.TextToSpeech;

public interface ITextToSpeech : IDisposable
{
    void Speak(string message, bool extra=false);
    void SetVoice(string voice);
    void SetVolume(float volume);
    void SetSpeed(float speed);
    void SetLanguage(string lang);
    bool IsInitialized();
    AudioPlayer AudioPlayer { get; }
}