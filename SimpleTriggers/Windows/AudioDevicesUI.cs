using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading;
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
    private volatile static bool failed = false;
    private volatile static bool scanning = false;
    private static ImmutableList<AudioDeviceInfo> DeviceCache = [];
    private static string DefaultDeviceName = "";

    public static void RefreshDeviceList()
    {
        failed = false;
        if(!scanning)
        {
            scanning = true;
            new Thread(() => {
            try {
                using(var enumerator = new MMDeviceEnumerator())
                {
                    DefaultDeviceName = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console).FriendlyName;
                    var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                    var tempCache = new List<AudioDeviceInfo>(devices.Count);
                    foreach(var d in devices)
                    {
                        tempCache.Add(new AudioDeviceInfo { Name = d.FriendlyName, ID = d.ID });
                    }
                    DeviceCache = tempCache.ToImmutableList();
                }
            } catch (Exception e) {
                STLog.Log.Error(e, "Exception caught:");
                failed = true;
            } finally { scanning = false; }}) { IsBackground = true }.Start();
        }
    }

    public static void DrawAudioDeviceBox(Plugin plugin)
    {
        if(failed)
        {
            ImGui.TextColoredWrapped(new Vector4(1.0f, 1.0f, 0, 1.0f), "An error occurred trying to fetch the audio device list.\nCheck /xllog for more details.");
            return;
        }

        if(DeviceCache.Count > 0)
        {
            ImGui.SetNextItemWidth(400 * ImGuiHelpers.GlobalScale);
            using (var box = ImRaii.Combo("Output Device", DeviceCache.FirstOrDefault(d => d.ID.Equals(plugin.Configuration.AudioOutputDevice))?.Name ?? (DefaultDeviceName + " (Default)")))
            {
                if (box)
                {
                    foreach(var dc in DeviceCache)
                    {
                        if(ImGui.Selectable(dc.Name))
                        {
                            plugin.SetTTSOutputDevice(plugin.Configuration.AudioOutputDevice = dc.ID);
                            plugin.Configuration.Save();
                        }
                    }
                }
            }
        } else { // Either it isn't populated yet or the user doesn't have any devices??
            ImGui.TextColoredWrapped(new Vector4(1.0f, 1.0f, 0, 1.0f), "No audio devices were found for your system!!");
            if(ImGui.Button("Scan again?"))
            {
                RefreshDeviceList();
            }
        }
    }
}