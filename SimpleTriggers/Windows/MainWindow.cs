using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Interface;
using SimpleTriggers.Gui;
using SimpleTriggers.SeFunctions;
using SimpleTriggers.TextToSpeech;

namespace SimpleTriggers.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private ImGuiTextFilter chatFilter;
    private ImGuiTextFilter trigFilter;

    private int selectedTriggerIndex = -1;
    private int selectedChatIndex = -1;
    private TriggerEntry? activeTrigger {get; set;}

    // We give this window a hidden ID using ##.
    // The user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin, string version)
        : base($"{plugin.Name} v{version}##WindowSTrigger", ImGuiWindowFlags.NoScrollbar)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 480),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
        activeTrigger = null;
    }

    public void Dispose() { }

    public override void Draw()
    {
        using (var tabbar = ImRaii.TabBar("STTabBar##WindowTabBar"))
        {
            if(tabbar)
            {
                using ( var tab = ImRaii.TabItem("Triggers##TriggerTab"))
                {
                    if(tab) {
                        DrawTriggersTab();
                    } else { // if we're not on this tab, deselect any trigger entry
                        selectedTriggerIndex = -1;
                        activeTrigger = null;
                    }
                }

                using ( var tab = ImRaii.TabItem("Chat History##ChatTab"))
                {
                    if(tab) {
                        DrawChatLogTab();
                    } else { // if we're not on this tab, deselect any chat message
                        selectedChatIndex = -1;
                    }   
                }

                using ( var tab = ImRaii.TabItem("Settings##SettingsTab"))
                {
                    if(tab) {
                        DrawSettingsTab();
                    }
                }
            }
        }
    } // Draw()

    private void DrawTriggersTab()
    {
        bool updateConfig = false;
        // Creates a reference to the actively selected trigger or a new blank one.
        TriggerEntry trigRef = activeTrigger ?? new TriggerEntry();
        // This is a copy of the above trigger for editing. Changes are only saved when interacted/hit Enter.
        TriggerEntry editing = new TriggerEntry(trigRef);

        // Text box for the expression to match
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Text to Match:   ");
        ImGui.SameLine();
        // We must not allow the expression to be blank
        if(ImGui.InputText("##ExpressionTextBox", ref editing.expression, 128, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            if(editing.expression.Length != 0) { trigRef.expression = editing.expression; }
            updateConfig = true;
        }
        // Text box for the response to give back to the player
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Response Text: ");
        ImGui.SameLine();
        if(ImGui.InputText("##ResponseTextBox", ref editing.response, 128, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            trigRef.response = editing.response;
            updateConfig = true;
        }
        // Checkbox for Sending Chat Message
        if(ImGui.Checkbox("Send Message?", ref editing.doPostInChat))
        {
            trigRef.doPostInChat = editing.doPostInChat;
            updateConfig = true;
        }
        // Checkbox for playing Text-to-Speech
        if(ImGui.Checkbox("Text to Speech?", ref editing.doResponseTTS))
        {
            trigRef.doResponseTTS = editing.doResponseTTS;
            updateConfig = true;
        }
        // If the above is toggled on, renders a button to test the Response TTS
        if(editing.doResponseTTS)
        {
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if(ImGui.Button($"{FontAwesomeIcon.Play.ToIconString()}##TestPlayTTS"))
            {
                plugin.SpeakTTS(editing.response);
            }
            ImGui.PopFont();
        }
        // Checkbox for Playing Sound FX
        if(ImGui.Checkbox("Play Sound?", ref editing.doPlaySound))
        {
            trigRef.doPlaySound = editing.doPlaySound;
            updateConfig = true;
        }
        // If the above is toggled on, renders a drop-down selection for SoundFX and a Test button
        if(editing.doPlaySound)
        {
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if(ImGui.Button($"{FontAwesomeIcon.Play.ToIconString()}##TestPlaySFX"))
            {
                PlaySound.Play(SoundsExtensions.FromIdx(editing.soundFx));
            }
            ImGui.PopFont();

            ImGui.SameLine();
            ImGui.SetNextItemWidth(135);
            if(ImGui.BeginCombo("##SoundFXComboBox", SoundsExtensions.ToName(SoundsExtensions.FromIdx(editing.soundFx)), ImGuiComboFlags.HeightRegular))
            {
                for(int i = 0; i < 16; ++i)
                {
                    if(ImGui.Selectable($"Sound{i+1:00}"))
                    {
                        trigRef.soundFx = editing.soundFx = i+1;
                        updateConfig = true;
                    }
                }
                ImGui.EndCombo();
            }
        }

        if(ImGui.Button("Add New"))
        {
            AddTrigger(trigRef);
            selectedTriggerIndex = plugin.Configuration.Triggers.Count-1;
            activeTrigger = plugin.Configuration.Triggers.ElementAt(selectedTriggerIndex);
            if(activeTrigger.expression.Length == 0) { activeTrigger.expression = "New Trigger"; }
        }

        ImGui.SameLine();
        if(ImGui.Button("Remove") && selectedTriggerIndex != -1)
        {
            RemoveTrigger(selectedTriggerIndex);
            selectedTriggerIndex = -1;
            activeTrigger = null;
        }

        // Shift trigger up and down buttons
        if(selectedTriggerIndex != -1)
        {
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if(ImGui.Button($"{FontAwesomeIcon.ArrowUp.ToIconString()}"))
            {
                if(selectedTriggerIndex > 0)
                {
                    SwapTriggers(selectedTriggerIndex, --selectedTriggerIndex);
                    activeTrigger = plugin.Configuration.Triggers.ElementAt(selectedTriggerIndex);
                }
            }
            ImGui.SameLine();
            if(ImGui.Button($"{FontAwesomeIcon.ArrowDown.ToIconString()}"))
            {
                if(selectedTriggerIndex < plugin.Configuration.Triggers.Count-1)
                {
                    SwapTriggers(selectedTriggerIndex, ++selectedTriggerIndex);
                    activeTrigger = plugin.Configuration.Triggers.ElementAt(selectedTriggerIndex);
                }
            }
            ImGui.PopFont();
        }

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Filter: ");
        ImGui.SameLine();
        trigFilter.Draw("##TriggerFilter", 180);
        ImGui.SameLine();
        if(ImGui.Button("\uE04C")) { trigFilter.Clear(); }

        ImGui.SameLine(ImGui.GetWindowWidth()-125);
        if(ImGui.Button("Clear All Triggers")
            && ImGui.GetIO().KeyShift)
        {
            ClearAllTriggers();
        }
        ImGuiCustom.HoverTooltip("Hold SHIFT to Clear All");

        ImGui.Separator();
        using (var child = ImRaii.Child("TriggerBoxWithScrollbar", Vector2.Zero, true))
        {
            var removeIndex = -1;
            var id = 0;
            foreach(var trigger in plugin.Configuration.Triggers)
            {
                var expression = trigger.expression;
                ImGui.PushID(id);
                if(trigFilter.PassFilter(expression))
                {
                    if(ImGui.Checkbox("", ref trigger.enabled))
                    {
                        updateConfig = true;
                    }
                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding();
                    if(ImGui.Selectable(expression, selectedTriggerIndex==id)) {
                        selectedTriggerIndex = id;
                        activeTrigger = trigger;
                    }

                    if(ImGui.BeginPopupContextItem("SaveMsgToTriggerPopup"))
                    {
                        selectedTriggerIndex = id;
                        if(ImGui.MenuItem("Remove Trigger"))
                        {
                            removeIndex = id;
                            selectedTriggerIndex = -1;
                            activeTrigger = null;
                        }
                        ImGui.EndPopup();
                    }
                }

                ImGui.PopID();
                id++;
            } // foreach TriggerTexts

            if(removeIndex != -1) { RemoveTrigger(removeIndex); }
        }

        if(updateConfig)
        {
            plugin.Configuration.Save();
        }

        if(ImGui.IsItemHovered() && ImGui.IsMouseClicked(0))
        {
            selectedTriggerIndex = -1;
            activeTrigger = null;
        }
    } // DrawTriggersTab

    private void DrawChatLogTab()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Filter: ");
        ImGui.SameLine();
        chatFilter.Draw("##ChatFilter", 180);
        ImGui.SameLine();
        if(ImGui.Button("\uE04C")) { chatFilter.Clear(); }
        ImGui.SameLine();
        if(ImGui.Button("Clear Log")) { ClearLog(); selectedChatIndex = -1; }
        ImGui.Separator();

        using (var child = ImRaii.Child("ChatBoxWithScrollBar", Vector2.Zero, true))
        {
            var id = plugin.ChatLog.Count;
            foreach(var ses in plugin.ChatLog.Reverse())
            {
                ImGui.PushID(id);

                if(chatFilter.PassFilter(ses))
                {
                    if(ImGui.Selectable(ses, selectedChatIndex==id, ImGuiSelectableFlags.DontClosePopups)) // Left-Click action
                    {
                        selectedChatIndex = id;
                    }

                    if(ImGui.BeginPopupContextItem("SaveMsgToTriggerPopup"))
                    {
                        selectedChatIndex = id;
                        if(ImGui.MenuItem("Save to Triggers?"))
                        {
                            SaveChatMessageToTriggers(ses);
                            selectedChatIndex = -1;
                        }

                        ImGui.EndPopup();
                    }
                }

                ImGui.PopID();
                id--;
            } // foreach chatLog
        }

        // This deselects any chat items if we click in the open space of the chat box
        if(ImGui.IsItemHovered() && ImGui.IsMouseClicked(0))
        {
            selectedChatIndex = -1;
        }
    } // DrawChatLogTab

    private void DrawSettingsTab()
    {
        // General Options
        if(ImGui.CollapsingHeader("General", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if(ImGui.Checkbox("Enable All Triggers?", ref plugin.Configuration.EnableTriggers))
            {
                plugin.Configuration.Save();
            }

            if(ImGui.Checkbox("Log Chat History?", ref plugin.doLogChatHistory) && !plugin.doLogChatHistory)
            {
                ClearLog(); // clear when unchecked
            }

            ImGui.Text($"How many entries should the chat history keep saved? (Max {plugin.MaxLogHistoryCeiling})");
            ImGui.SetNextItemWidth(120);
            ImGui.DragScalar<uint>("Max Log History##MaxHistoryLength",
                ref plugin.Configuration.MaxLogHistory,0.2f,0, plugin.MaxLogHistoryCeiling,default,ImGuiSliderFlags.AlwaysClamp);
            if(ImGui.IsItemDeactivatedAfterEdit())
            {
                plugin.Configuration.Save();
            }

            ImGui.NewLine();
        }
        
        // TTS Backend
        if(ImGui.CollapsingHeader("Text-to-Speech", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.SetNextItemWidth(140);
            using (var box = ImRaii.Combo("Text-to-Speech Provider", TTSProviders.ToName(plugin.Configuration.TTSProvider))) 
            {
                if(box)
                {
                    for(var i = 0; i < Enum.GetNames<TextToSpeechType>().Length; ++i)
                    {
                        if(ImGui.Selectable(TTSProviders.ToName((TextToSpeechType)i)))
                        {
                            plugin.Configuration.TTSProvider = (TextToSpeechType)i;
                            plugin.Configuration.Save();
                            plugin.SwapTTSBackend((TextToSpeechType)i);
                        }
                    }
                }
            }

            // Voice Options
            ImGui.Indent();
            if(plugin.Configuration.TTSProvider == TextToSpeechType.Kokoro)
            {
                STKokoroUI.DrawKokoroSettings(plugin);
            }

            if(plugin.Configuration.TTSProvider == TextToSpeechType.WindowsSystem)
            {
                STWinSpeechUI.DrawWinSpeechSettings(plugin);
            }
            ImGui.Unindent();
        }
    }

    void ClearLog()
    {
        plugin.ChatLog.Clear();
    }
    
    void ClearAllTriggers()
    {
        plugin.Configuration.Triggers.Clear();
        plugin.Configuration.Save();
    }

    void SaveChatMessageToTriggers(String str)
    {
        var trigger = new TriggerEntry();
        trigger.expression = str;
        plugin.Configuration.Triggers.Add(trigger);
        plugin.Configuration.Save();
    }

    void AddTrigger(TriggerEntry trigger)
    {
        var trig = new TriggerEntry(trigger);
        plugin.Configuration.Triggers.Add(trig);
        plugin.Configuration.Save();
    }

    void RemoveTrigger(int idx)
    {
        plugin.Configuration.Triggers.RemoveAt(idx);
        plugin.Configuration.Save();
    }

    void SwapTriggers(int idx1, int idx2)
    {
        var triggers = plugin.Configuration.Triggers;
        var temp = new TriggerEntry(triggers.ElementAt(idx1));
        triggers[idx1] = triggers[idx2];
        triggers[idx2] = temp;
    }
}
