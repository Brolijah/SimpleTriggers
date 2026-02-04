using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using SimpleTriggers.TextToSpeech;

namespace SimpleTriggers;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public bool EnableTriggers = true;
    public uint MaxLogHistory = 500;
    public TextToSpeechType TTSProvider = TextToSpeechType.None;
    public KokoroVoiceKind TTSKokoroVoice = 0;
    public List<TriggerEntry> Triggers { get; set; } = [];

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
