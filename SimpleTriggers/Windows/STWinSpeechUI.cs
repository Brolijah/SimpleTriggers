using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface;
using System.Speech.Synthesis;
using System.Speech.AudioFormat;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Components;

namespace SimpleTriggers.Windows;

public static class STWinSpeechUI
{
    public static void DrawWinSpeechSettings(Plugin plugin)
    {
        if(!OSHelper.IsWindows())
        {
            using(var style = ImRaii.PushColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 0, 1.0f)))
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Text($"{FontAwesomeIcon.ExclamationTriangle.ToIconString()}");
                ImGui.PopFont();
                ImGui.SameLine();
                ImGui.Text("This TTS option is not supported on your OS!!");
            }
            return; // We don't want the below to run if this isn't windows.
        }

        ImGui.SetNextItemWidth(160 * ImGuiHelpers.GlobalScale);
        using (var box = ImRaii.Combo("##WinSpeechVoiceBox", plugin.Configuration.WinSpeechVoice, ImGuiComboFlags.HeightLarge))
        {
            var synth = new SpeechSynthesizer();
            if(box)
            {
                foreach(InstalledVoice voice in synth.GetInstalledVoices())
                {
                    var info = voice.VoiceInfo;
                    if(ImGui.Selectable(info.Name))
                    {
                        plugin.Configuration.WinSpeechVoice = info.Name;
                        plugin.Configuration.Save();
                        plugin.SetTTSVoice(info.Name);
                    }
                }
            }
        }

        ImGui.SameLine();
        if(ImGuiComponents.IconButton(FontAwesomeIcon.Play))
        {
            plugin.SpeakTTS("This is a test of the Windows System voice.");
        }
        ImGui.SameLine();
        ImGui.Text("Test Voice");

        // Volume and Speed
        ImGui.SetNextItemWidth(192 * ImGuiHelpers.GlobalScale);
        ImGui.SliderFloat("Voice Speed", ref plugin.Configuration.TTSSpeed,0.5f, 1.5f,"%.1fx");
        if(ImGui.IsItemDeactivatedAfterEdit())
        {
            plugin.SetTTSSpeed(plugin.Configuration.TTSSpeed);
            plugin.Configuration.Save();
        }

        ImGui.SetNextItemWidth(192 * ImGuiHelpers.GlobalScale);
        ImGui.SliderFloat("Voice Volume", ref plugin.Configuration.TTSVolume,1.0f, 100.0f,"%.0f%%");
        if(ImGui.IsItemDeactivatedAfterEdit())
        {
            plugin.SetTTSVolume(plugin.Configuration.TTSVolume);
            plugin.Configuration.Save();
        }
    }

}
