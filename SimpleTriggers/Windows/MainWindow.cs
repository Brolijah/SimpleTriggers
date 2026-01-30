using System;
using System.Linq;
using System.Numerics;
using SimpleTriggers.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Game.Player;
using Newtonsoft.Json.Linq;
using Dalamud.Interface;
using SimpleTriggers.SeFunctions;

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
        using (var tabbar = ImRaii.TabBar("STTabBar", ImGuiTabBarFlags.None))
        {
            if(ImGui.BeginTabItem("Triggers", ImGuiTabItemFlags.None))
            {
                // Creates a reference to the actively selected trigger or a new blank one.
                // This is to help protect malformed expression text (ie empty)
                TriggerEntry trigRef = activeTrigger ?? new TriggerEntry();
                TriggerEntry editing = new TriggerEntry(trigRef);
                // Text box for the expression to match
                ImGui.Text("Text to Match:   ");
                ImGui.SameLine();
                // We must not allow the expression to be blank
                if(ImGui.InputText("##ExpressionTextBox", ref editing.expression, 128, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    if(editing.expression.Length != 0) { trigRef.expression = editing.expression; }
                }
                // Text box for the response to give back to the player
                ImGui.Text("Response Text: ");
                ImGui.SameLine();
                if(ImGui.InputText("##ResponseTextBox", ref editing.response, 128, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    trigRef.response = editing.response;
                }

                // Checkbox for Sending Chat Message
                if(ImGui.Checkbox("Send Message?", ref editing.doPostInChat))
                {
                    trigRef.doPostInChat = editing.doPostInChat;
                }
                //ImGui.SameLine();
                // Checkbox for Playing Sound FX
                if(ImGui.Checkbox("Play Sound?", ref editing.doPlaySound))
                {
                    trigRef.doPlaySound = editing.doPlaySound;
                }

                if(editing.doPlaySound)
                {
                    ImGui.SameLine();
                    ImGui.PushFont(UiBuilder.IconFont);
                    if(ImGui.Button(FontAwesomeIcon.Play.ToIconString()))
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
                            if(ImGui.Selectable($"Sound{i+1:00}##sound{i+1}"))
                            {
                                editing.soundFx = i+1;
                                trigRef.soundFx = editing.soundFx;
                            }
                        }
                        ImGui.EndCombo();
                    }
                }
                
                // TODO : Text-to-Speech
                /* ImGui.SameLine();
                if(ImGui.Checkbox("Text to Speech?", ref trigRef.doPostInChat))
                { } */

                if(ImGui.Button("Clear All Triggers")
                    && ImGui.GetIO().KeyShift)
                {
                    ClearAllTriggers();
                }
                ImGuiCustom.HoverTooltip("Hold SHIFT to Clear All");
                ImGui.SameLine();
                if(ImGui.Button("Add New"))
                {
                    AddTrigger(trigRef);
                }

                ImGui.Text("Search: ");
                ImGui.SameLine();
                trigFilter.Draw("##TriggerFilter", 180);
                ImGui.SameLine();
                if(ImGui.Button("\uE04C")) { trigFilter.Clear(); }
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
                            if(ImGui.Selectable(expression, selectedTriggerIndex==id)) {
                                selectedTriggerIndex = id;
                                activeTrigger = trigger;
                            }

                            if(ImGui.BeginPopupContextItem("SaveMsgToTriggerPopup"))
                            {
                                selectedTriggerIndex = id;
                                if(ImGui.MenuItem("Delete Trigger"))
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
                ImGui.EndTabItem();

                if(ImGui.IsItemHovered() && ImGui.IsMouseClicked(0))
                {
                    selectedTriggerIndex = -1;
                    activeTrigger = null;
                }

            } else { 
                selectedTriggerIndex = -1;
                activeTrigger = null;

            } // Triggers tab

            if(ImGui.BeginTabItem("Chat History", ImGuiTabItemFlags.None))
            {
                if(ImGui.Button("Clear Log")) { ClearLog(); selectedChatIndex = -1; }
                ImGui.SameLine();
                chatFilter.Draw("##ChatFilter", 180);
                ImGui.SameLine();
                if(ImGui.Button("\uE04C")) { chatFilter.Clear(); }
                ImGui.Separator();

                using (var child = ImRaii.Child("ChatBoxWithScrollBar", Vector2.Zero, true))
                {
                    var id = plugin.chatLog.Count;
                    foreach(var ses in plugin.chatLog.Reverse())
                    {
                        ImGui.PushID(id);

                        if(chatFilter.PassFilter(ses.TextValue))
                        {
                            if(ImGui.Selectable(ses.TextValue, selectedChatIndex==id, ImGuiSelectableFlags.DontClosePopups)) // Left-Click action
                            {
                                selectedChatIndex = id;
                            }

                            if(ImGui.BeginPopupContextItem("SaveMsgToTriggerPopup"))
                            {
                                selectedChatIndex = id;
                                if(ImGui.MenuItem("Save to Triggers?"))
                                {
                                    SaveChatMessageToTriggers(ses.TextValue);
                                    selectedChatIndex = -1;
                                }

                                ImGui.EndPopup();
                            }
                        }

                        ImGui.PopID();
                        id--;
                    } // foreach chatLog
                }

                ImGui.EndTabItem();

                if(ImGui.IsItemHovered() && ImGui.IsMouseClicked(0))
                {
                    selectedChatIndex = -1;
                }
            } else { 
                selectedChatIndex = -1; 
            } // Chat History tab

        } // TabBar
    } // Draw()

    void ClearLog()
    {
        plugin.chatLog.Clear();
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
        plugin.Configuration.Triggers.Add(trigger);
        plugin.Configuration.Save();
    }

    void RemoveTrigger(int idx)
    {
        plugin.Configuration.Triggers.RemoveAt(idx);
        plugin.Configuration.Save();
    }
}
