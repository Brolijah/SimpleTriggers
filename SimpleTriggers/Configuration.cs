using Dalamud.Configuration;
using System;
using SimpleTriggers.TextToSpeech;
using SimpleTriggers.Triggers;
using System.Collections.Generic;

namespace SimpleTriggers;

public class KokoroConfig
{
    public KokoroVoiceKind Voice = KokoroVoiceKind.af_heart; // 0
    public float Speed = 1.0f; // [0, 2] // technically supports higher, but 2.0x is already incomprehensible
    public float Volume = 100.0f; // [0, 100] // gets scaled to [0,1] internally
    public string Language = "en-us";
    public bool UseEspeak = false;
    public KokoroConfig() {}
}

public class WinSpeechConfig
{
    public string Voice = "";
    public int Speed = 0; // [-5, +5]
    public int Volume = 100; // [0, 100]
    public WinSpeechConfig() {}
}


[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public bool EnableTriggers = true;
    public uint MaxLogHistory = 500;
    public string AudioOutputDevice = "";
    public bool AllowAudioBoost = false; // Lets the user boost the volume above a normally safe amount
    public bool ChannelReadAllTypes = true;
    public SortedSet<int> ChannelTypeFilter = [];
    public TextToSpeechType TTSProvider = TextToSpeechType.None;
    public KokoroConfig Kokoro { get; set; } = new();
    public WinSpeechConfig WinSpeech { get; set; } = new();
    public TriggerTree TriggerTree { get; set; } = new();

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
