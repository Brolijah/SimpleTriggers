using System;

namespace SimpleTriggers.TextToSpeech;

public interface IUnixTTSCmd
{
    public string FileName { get; }
    public bool Start();
    public void Stop();
    public void Speak(string message);

}
