using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using NAudio.CoreAudioApi;
using SimpleTriggers.Logger;

namespace SimpleTriggers.Windows;

public class AudioDeviceInfo
{
    public required string Name { get; set; }
    public required string ID { get; set; }
}

static public class AudioDevicesUI
{
    private static bool failed = false;
    private static List<AudioDeviceInfo> DeviceCache = [];

    public static void RefreshDeviceList()
    {
        failed = false;
        DeviceCache = new();
        try
        {
            using(var enumerator = new MMDeviceEnumerator())
            {
                var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                foreach(var d in devices)
                {
                    DeviceCache.Add(new AudioDeviceInfo { Name = d.FriendlyName, ID = d.ID });
                }
            }
        } catch (Exception e) {
            STLog.Log.Error(e, "Exception caught:");
            failed = true;
        }
    }

    public static void DrawAudioDeviceBox(Plugin plugin)
    {
        ImGui.SetNextItemWidth(400 * ImGuiHelpers.GlobalScale);
        using (var box = ImRaii.Combo("Output Device", DeviceCache.FirstOrDefault(d => d.ID.Equals(plugin.Configuration.AudioOutputDevice))?.Name ?? ""))
        {
            if(ImGui.IsWindowAppearing())
            {
                RefreshDeviceList();
            }
            
            if (box)
            {
                for(var i = 0; i < DeviceCache.Count-1; ++i)
                {
                    if(ImGui.Selectable(DeviceCache[i].Name))
                    {
                        plugin.Configuration.AudioOutputDevice = DeviceCache[i].ID;
                        plugin.SetTTSOutputDevice(i);
                        plugin.Configuration.Save();
                    }
                }
            }
        }

        if(failed)
        {
            ImGui.TextColoredWrapped(new Vector4(1.0f, 1.0f, 0, 1.0f), "An error occurred trying to fetch the audio device list.\nCheck /xllog for more details.");
            return;
        }

        // Either it isn't populated yet or the user doesn't have any devices??
        if(DeviceCache.Count == 0)
        {
            ImGui.TextColoredWrapped(new Vector4(1.0f, 1.0f, 0, 1.0f), "No audio devices were found for your system!!");
        }
    }
}