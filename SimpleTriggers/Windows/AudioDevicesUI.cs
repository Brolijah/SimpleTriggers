using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Immutable;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using SimpleTriggers.Logger;
using SimpleTriggers.TextToSpeech;
using Dalamud.Interface;
using SimpleTriggers.Gui;

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

    // See here for more info:
    // https://github.com/naudio/NAudio/blob/master/Docs/EnumerateOutputDevices.md
    public static void RefreshDeviceList()
    {
        failed = false;
        if(!scanning)
        {
            scanning = true;
            new Thread(() => {
                try {
                    using var enumerator = new MMDeviceEnumerator();
                    DefaultDeviceName = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console).FriendlyName;
                    var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                    var tempCache = new List<AudioDeviceInfo>(devices.Count);
                    foreach (var d in devices)
                    {
                        tempCache.Add(new AudioDeviceInfo { Name = d.FriendlyName, ID = d.ID[(d.ID.LastIndexOf('.') + 1)..] });
                    }
                    DeviceCache = tempCache.ToImmutableList();
                } catch (Exception e) {
                    STLog.Log.Warning("Something went wrong scanning audio hardware (WASAPI). Will try again using the backup.");
                    STLog.Log.Warning(e, "Exception caught:");
                    failed = true;
                }

                // This is the fallback if the above fails.
                if(failed)
                {
                    try {
                        var devices = DirectSoundOut.Devices;
                        var firstDevice = devices.FirstOrDefault();
                        if(firstDevice is not null) DefaultDeviceName = firstDevice.Description.Length < 32 ? firstDevice.Description : firstDevice.Description[..32] + "...";
                        var tempCache = new List<AudioDeviceInfo>(devices.Count());
                        foreach(var dev in DirectSoundOut.Devices)
                        {
                            var name = dev.Description.Length < 32 ? dev.Description : dev.Description[..32] + "...";
                            tempCache.Add(new AudioDeviceInfo { Name = name, ID = dev.Guid.ToString("B") });
                        }
                        DeviceCache = tempCache.ToImmutableList();
                        failed = false;
                    } catch(Exception e)
                    {
                        STLog.Log.Warning("Fallback audio hardware scan failed (DirectSound)!!");
                        STLog.Log.Warning(e, "Exception caught:");
                        failed = true; // for real this time
                    }
                }
                scanning = false;
            }){ IsBackground = true }.Start();
        }
    }

    public static void DrawAudioSettings(Plugin plugin)
    {
        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        using(var box = ImRaii.Combo("Audio Backend", plugin.Configuration.AudioBackend.ToString()))
        {
            if(box)
            {
                foreach(var type in Enum.GetValues<AudioOutputType>())
                {
                    if(ImGui.Selectable(type.ToString()))
                    {
                        plugin.SetTTSAudioBackend(plugin.Configuration.AudioBackend = type);
                        plugin.Configuration.Save();
                    }
                }
            }
        }
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.Text(FontAwesomeIcon.ExclamationCircle.ToIconString());
        ImGui.PopFont();
        ImGuiCustom.HoverTooltip(
            "Recommended to use DirectSound or Wasapi.\n"+
            "WaveOut should be considered a backup if you encounter issues\n"+
            "using the others. WaveOut will only use the default audio device."
        );

        if(failed)
        {
            ImGui.TextColoredWrapped(new Vector4(1.0f, 1.0f, 0, 1.0f), "An error occurred trying to fetch the audio device list.\nCheck /xllog for more details.");
            return;
        }

        if(plugin.Configuration.AudioBackend != AudioOutputType.WaveOut)
        {
            if(DeviceCache.Count > 0)
            {
                ImGui.SetNextItemWidth(400 * ImGuiHelpers.GlobalScale);
                using var box = ImRaii.Combo("Output Device", DeviceCache.FirstOrDefault(d => d.ID.Contains(plugin.Configuration.AudioOutputDevice, StringComparison.CurrentCultureIgnoreCase))?.Name ?? (DefaultDeviceName + " (Default)"));
                if (box)
                {
                    foreach (var dc in DeviceCache)
                    {
                        if (ImGui.Selectable(dc.Name))
                        {
                            plugin.SetTTSOutputDevice(plugin.Configuration.AudioOutputDevice = dc.ID);
                            plugin.Configuration.Save();
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
}