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
        using (var box = ImRaii.Combo("##WinSpeechVoiceBox", plugin.Configuration.WinSpeech.Voice, ImGuiComboFlags.HeightLarge))
        {
            var synth = new SpeechSynthesizer();
            if(box)
            {
                foreach(InstalledVoice voice in synth.GetInstalledVoices())
                {
                    var info = voice.VoiceInfo;
                    if(ImGui.Selectable(info.Name))
                    {
                        plugin.Configuration.WinSpeech.Voice = info.Name;
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
        ImGui.SliderInt("Voice Speed", ref plugin.Configuration.WinSpeech.Speed,-5, 5, "%+d", ImGuiSliderFlags.NoInput);
        if(ImGui.IsItemDeactivatedAfterEdit())
        {
            plugin.SetTTSSpeed(plugin.Configuration.WinSpeech.Speed);
            plugin.Configuration.Save();
        }

        ImGui.SetNextItemWidth(192 * ImGuiHelpers.GlobalScale);
        if(plugin.Configuration.AllowAudioBoost) // Danger Zone, Use Wisely
        {
            ImGui.DragScalar("Voice Volume", ref plugin.Configuration.WinSpeech.Volume,1f,1,3000,"%d%%",ImGuiSliderFlags.AlwaysClamp);
        } else { // Normal Range
            ImGui.SliderInt("Voice Volume", ref plugin.Configuration.WinSpeech.Volume,1, 200,"%d%%", ImGuiSliderFlags.NoInput);
        }
        if(ImGui.IsItemDeactivatedAfterEdit())
        {
            plugin.Configuration.WinSpeech.Volume = Math.Clamp(plugin.Configuration.WinSpeech.Volume, 1, plugin.Configuration.AllowAudioBoost ? 3000 : 200);
            plugin.SetTTSVolume(plugin.Configuration.WinSpeech.Volume);
            plugin.Configuration.Save();
        }
        if(ImGui.Checkbox("Allow boosting over 200%", ref plugin.Configuration.AllowAudioBoost))
        {
            plugin.Configuration.WinSpeech.Volume = Math.Clamp(plugin.Configuration.WinSpeech.Volume, 1, 200);
            plugin.SetTTSVolume(plugin.Configuration.WinSpeech.Volume);
            plugin.Configuration.Save();
        }
    }
}
