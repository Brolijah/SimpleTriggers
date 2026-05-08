using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface;
using SimpleTriggers.TextToSpeech;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Components;

namespace SimpleTriggers.Windows;

public static class DecTalkUI
{
    public static void DrawDecTalkSettings(Plugin plugin)
    {
        ImGui.SetNextItemWidth(160 * ImGuiHelpers.GlobalScale);
        using (var box = ImRaii.Combo("##DecTalkVoiceBox", DecTalkVoiceHelper.ToString(plugin.Configuration.DecTalk.Voice), ImGuiComboFlags.HeightLarge))
        {
            if(box)
            {
                for(var i = 0; i < Enum.GetNames<DecTalkVoice>().Length; ++i)
                {
                    if(ImGui.Selectable(DecTalkVoiceHelper.ToString((DecTalkVoice)i)))
                    {
                        plugin.SetTTSVoice(DecTalkVoiceHelper.ToString(plugin.Configuration.DecTalk.Voice = (DecTalkVoice)i));
                        plugin.Configuration.Save();
                    }
                }
            }
        }

        ImGui.SameLine();
        if(ImGuiComponents.IconButton(FontAwesomeIcon.Play))
        {
            plugin.SpeakTTS("This is a test of the DECTalk voice.");
        }
        ImGui.SameLine();
        ImGui.Text("Test Voice");

        // Volume and Speed
        ImGui.SetNextItemWidth(192 * ImGuiHelpers.GlobalScale);
        ImGui.SliderInt("Voice Speed", ref plugin.Configuration.DecTalk.Speed, 50, 500, "%d");
        if(ImGui.IsItemDeactivatedAfterEdit())
        {
            plugin.Configuration.DecTalk.Speed = Math.Clamp(plugin.Configuration.DecTalk.Speed, 50, 500);
            plugin.SetTTSSpeed(plugin.Configuration.DecTalk.Speed);
            plugin.Configuration.Save();
        }

        ImGui.SetNextItemWidth(192 * ImGuiHelpers.GlobalScale);
        if(plugin.Configuration.AllowAudioBoost) // Danger Zone, Use Wisely
        {
            ImGui.DragScalar("Voice Volume", ref plugin.Configuration.DecTalk.Volume,1f,1,3000,"%d%%",ImGuiSliderFlags.AlwaysClamp);
        } else { // Normal Range
            ImGui.SliderInt("Voice Volume", ref plugin.Configuration.DecTalk.Volume,1, 200,"%d%%", ImGuiSliderFlags.NoInput);
        }
        if(ImGui.IsItemDeactivatedAfterEdit())
        {
            plugin.Configuration.DecTalk.Volume = Math.Clamp(plugin.Configuration.DecTalk.Volume, 1, plugin.Configuration.AllowAudioBoost ? 3000 : 200);
            plugin.SetTTSVolume(plugin.Configuration.DecTalk.Volume);
            plugin.Configuration.Save();
        }
        if(ImGui.Checkbox("Allow boosting over 200%", ref plugin.Configuration.AllowAudioBoost))
        {
            plugin.Configuration.DecTalk.Volume = Math.Clamp(plugin.Configuration.DecTalk.Volume, 1, 200);
            plugin.SetTTSVolume(plugin.Configuration.DecTalk.Volume);
            plugin.Configuration.Save();
        }

        // Information Text
        if(!plugin.CanSpeak())
        {
            ImGui.Text("Loading library...");
        }
    }

}
