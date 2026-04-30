using System;
using Newtonsoft.Json;
using SimpleTriggers.Logger;
using SimpleTriggers.Triggers;

namespace SimpleTriggers.Windows;

public static class TriggerPreset
{
    public static string buffer = "";

    // On Success, returns the name of the Category
    // On Failure, returns null
    public static string? Import(string enc, Plugin plugin)
    {
        TriggerCategory? import = null;
        try
        {
            import = JsonConvert.DeserializeObject<TriggerCategory>(enc);
        } catch (Exception e)
        {
            STLog.Log.Error(e, "Error importing JSON string.");
            return null;
        }

        if(import is not null)
        {
            // Doesn't exist yet, just insert
            if(plugin.Configuration.TriggerTree.GetIndexOfCategory(import.Name) == -1)
            {
                plugin.Configuration.TriggerTree.Add(import);
            } else // Category exists, we need to copy the triggers
            {
                var tc = plugin.Configuration.TriggerTree[import.Name]!;
                foreach(var te in import.Triggers)
                {
                    tc.Add(te);
                }
            }
            buffer = "";
        }

        return import?.Name;
    }

    public static string Export(TriggerCategory tc)
    {
        return JsonConvert.SerializeObject(tc);
    }

}
