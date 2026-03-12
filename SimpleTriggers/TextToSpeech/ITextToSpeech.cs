
using System;

public interface ITextToSpeech : IDisposable
{
    void Speak(string message, bool extra=false);
    void SetVoice(string voice);
    void SetVolume(float volume);
    void SetSpeed(float speed);
    bool IsInitialized();
}