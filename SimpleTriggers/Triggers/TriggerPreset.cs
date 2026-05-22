using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SimpleTriggers.Logger;

namespace SimpleTriggers.Triggers;

public static class TriggerPreset
{
    private static readonly List<string> defaultValues =
    [
        ",\"enabled\":true", // seen in TriggerCategory and TriggerEntry
        ",\"opened\":true", // seen in TriggerCategory
        // The below are only seen in TriggerEntry
        ",\"doPostInChat\":false",
        ",\"doResponseTTS\":false",
        ",\"doPlaySound\":false",
        ",\"doPopup\":false",
        ",\"popupStyle\":0",
        ",\"soundFx\":0", // older versions had this as default, it's "None"
        ",\"soundFx\":1",
    ];

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
        }

        return import?.Name;
    }

    public static string Export(TriggerCategory tc)
    {
        var str = JsonConvert.SerializeObject(tc);
        foreach(var dv in defaultValues)
        {
            str = str.Replace(dv, "");
        }
        // some final cleanup, puts line breaks in for readability
        str = str.Replace("\":[{", "\":[\n{");
        return str.Replace("},", "},\n");
    }

}
