using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace SimpleTriggers;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    //public bool IsConfigWindowMovable { get; set; } = true;
    //public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

    public List<TriggerEntry> Triggers { get; set; } = [];

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
