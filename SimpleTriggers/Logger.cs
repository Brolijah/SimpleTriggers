using System.Diagnostics.CodeAnalysis;
using Dalamud.Plugin.Services;

namespace SimpleTriggers.Logger;

public static class STLog
{
    [NotNull] public static IPluginLog? Log = null!;

    public static void SetLogger(IPluginLog pluginLog) { Log = pluginLog; }
}
