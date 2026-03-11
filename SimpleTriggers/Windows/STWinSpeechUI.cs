using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface;

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
        }

        /* TODO: Kokoro equivalent, needs to be reworked for Windows Speech System
        ImGui.SetNextItemWidth(160);
        using (var box = ImRaii.Combo("##KokoroVoiceBox", KokoroVoiceHelper.ToName(plugin.Configuration.TTSKokoroVoice), ImGuiComboFlags.HeightLarge))
        {
            if(box)
            {
                for(var i = 0; i < Enum.GetNames<KokoroVoiceKind>().Length; ++i)
                {
                    if(ImGui.Selectable(KokoroVoiceHelper.ToName((KokoroVoiceKind)i)))
                    {
                        plugin.Configuration.TTSKokoroVoice = (KokoroVoiceKind)i;
                        plugin.Configuration.Save();
                        plugin.SetTTSVoice(KokoroVoiceHelper.ToString((KokoroVoiceKind)i));
                    }
                }
            }
        }

        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        if(ImGui.Button($"{FontAwesomeIcon.Play.ToIconString()}"))
        {
            plugin.SpeakTTS("This is a test of the Kokoro voice.");
        }
        ImGui.PopFont();
        ImGui.SameLine();
        ImGui.Text("Test Voice");
        */

        // Volume and Speed
        ImGui.SetNextItemWidth(192);
        ImGui.SliderFloat("Voice Speed", ref plugin.Configuration.TTSSpeed,0.5f, 1.5f,"%.1fx");
        if(ImGui.IsItemDeactivatedAfterEdit())
        {
            plugin.SetTTSSpeed(plugin.Configuration.TTSSpeed);
            plugin.Configuration.Save();
        }

        ImGui.SetNextItemWidth(192);
        ImGui.SliderFloat("Voice Volume", ref plugin.Configuration.TTSVolume,1.0f, 100.0f,"%.0f%%");
        if(ImGui.IsItemDeactivatedAfterEdit())
        {
            plugin.SetTTSVolume(plugin.Configuration.TTSVolume);
            plugin.Configuration.Save();
        }
    }

}
