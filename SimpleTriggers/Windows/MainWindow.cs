using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Interface;
using SimpleTriggers.Gui;
using SimpleTriggers.SeFunctions;
using SimpleTriggers.TextToSpeech;
using SimpleTriggers.Triggers;

namespace SimpleTriggers.Windows;

internal struct SelectionState
{
    public int chatIndex = -1; // used to track the selected index in Chat History
    public int trigSubIndex = -1; // used to track selected index in TriggerCategory
    public int trigListIndex = -1; // used to track selected node in the trigger box list
    public TriggerEntry? activeTrigger = null; // Currently selected trigger
    public TriggerCategory? activeCategory = null; // Currently selected category -- should never be null if activeTrigger is not null

    public SelectionState() {}
    public void Reset()
    {
        chatIndex = -1;
        trigSubIndex = -1;
        trigListIndex = -1;
        activeTrigger = null;
        activeCategory = null;
    }
}

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private ImGuiTextFilter chatFilter;
    private SelectionState state;

    public MainWindow(Plugin plugin, string version)
        : base($"{plugin.Name} v{version}##WindowSTrigger", ImGuiWindowFlags.NoScrollbar)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 480),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
        this.state = new();
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
                        state.Reset();
                    }
                }

                using ( var tab = ImRaii.TabItem("Chat History##ChatTab"))
                {
                    if(tab) {
                        DrawChatLogTab();
                    } else { // if we're not on this tab, deselect any chat message
                        state.chatIndex = -1;
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
        string editingCatName = state.activeCategory?.Name ?? plugin.DefaultCategoryName;
        TriggerEntry trigRef = state.activeTrigger ?? new TriggerEntry();
        // This is a copy of the above trigger for editing. Changes are only saved when interacted/hit Enter.
        TriggerEntry editing = new TriggerEntry(trigRef);

        // Text box for the expression to match
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Text to Match:    ");
        ImGui.SameLine();
        // We must not allow the expression to be blank
        if(ImGui.InputText("##ExpressionTextBox", ref editing.expression, 128, ImGuiInputTextFlags.EnterReturnsTrue)
           || ImGui.IsItemDeactivatedAfterEdit())
        {
            if(editing.expression.Length != 0)
            {
                trigRef.expression = editing.expression;
                if(state.activeTrigger is null)
                {
                    AddTrigger(trigRef, plugin.DefaultCategoryName);
                    RefreshSelectionState(editingCatName, true);
                }
            }
            updateConfig = true;
        }
        // Text box for the response to give back to the player
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Response Text:  ");
        ImGui.SameLine();
        if(ImGui.InputText("##ResponseTextBox", ref editing.response, 128, ImGuiInputTextFlags.EnterReturnsTrue)
           || ImGui.IsItemDeactivatedAfterEdit())
        {
            trigRef.response = editing.response;
            updateConfig = true;
        }
        // Text box for Category name.
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Category Name:");
        if(state.activeCategory is not null)
        {
            ImGui.SameLine();
            if(ImGui.InputText("##CategoryTextBox", ref editingCatName, 128, ImGuiInputTextFlags.EnterReturnsTrue)
            || ImGui.IsItemDeactivatedAfterEdit())
            {
                // Let's assume we're trying to reassign the category for the current trigger
                if(state.activeTrigger is not null)
                {
                    // Remove the trigger from the current category
                    RemoveTrigger(state.trigSubIndex, state.activeCategory.Name);
                    // Insert trigger into new category (existing or will create a new one)
                    AddTrigger(state.activeTrigger, editingCatName);
                    RefreshSelectionState(editingCatName, true);
                }
                state.activeCategory.Name = editingCatName;
                updateConfig = true;
            }
        }

        // Checkbox for Sending Chat Message
        if(ImGui.Checkbox("Send Message?", ref editing.doPostInChat))
        {
            trigRef.doPostInChat = editing.doPostInChat;
            updateConfig = true;
        }
        // Checkbox for playing Text-to-Speech
        if(ImGui.Checkbox("Text-to-Speech?", ref editing.doResponseTTS))
        {
            trigRef.doResponseTTS = editing.doResponseTTS;
            updateConfig = true;
        }
        // If the above is toggled on, renders a button to test the Response TTS
        if(editing.doResponseTTS)
        {
            ImGui.SameLine();
            if(ImGuiComponents.IconButton(FontAwesomeIcon.Play))
            {
                plugin.SpeakTTS(editing.response);
            }
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
            if(ImGuiComponents.IconButton(FontAwesomeIcon.Play))
            {
                PlaySound.Play(SoundsExtensions.FromIdx(editing.soundFx));
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(135 * ImGuiHelpers.GlobalScale);
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

        // Import Button
        if(ImGuiComponents.IconButtonWithText(FontAwesomeIcon.FileImport, "Import"))
        {
            ImGui.OpenPopup("Import");
        }
        using (var popup = ImRaii.ContextPopup("Import"))
        {
            if (popup)
            {
                bool doImport = false;
                ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
                if(ImGui.InputText("##ImportTriggerField", ref TriggerPreset.buffer, 8192, ImGuiInputTextFlags.EnterReturnsTrue)) doImport = true;
                ImGui.SameLine(); if (ImGui.Button("OK")) doImport = true;
                if(doImport)
                {
                    var name = TriggerPreset.Import(TriggerPreset.buffer, plugin);
                    if(name is not null) RefreshSelectionState(name, true, true);
                    plugin.Configuration.Save();
                    ImGui.CloseCurrentPopup();
                }
            }
        }

        // Export Button
        ImGui.SameLine();
        if(ImGuiComponents.IconButtonWithText(FontAwesomeIcon.FileExport, "Export"))
        {
            string export;
            if(state.activeCategory is not null)
            {
                // export just the selected trigger
                if(state.activeTrigger is not null)
                {
                    export = TriggerPreset.Export(new TriggerCategory(state.activeCategory.Name, [state.activeTrigger]));
                } else // export whole cateogry
                {
                    export = TriggerPreset.Export(state.activeCategory);
                }
                ImGui.SetClipboardText(export);
            }
        }

        // Add/Copy Trigger
        (var addIcon, var addText) = (state.trigSubIndex != -1) ? (FontAwesomeIcon.Copy, "Copy") : (FontAwesomeIcon.Plus, " Add");
        if(ImGuiComponents.IconButtonWithText(addIcon, addText, new Vector2(60 * ImGuiHelpers.GlobalScale, 0)))
        {
            AddTrigger(trigRef, state.activeCategory?.Name ?? plugin.DefaultCategoryName);
            RefreshSelectionState(state.activeCategory?.Name ?? plugin.DefaultCategoryName, true);
        }

        // Remove Trigger/Category
        ImGui.SameLine();
        if(ImGuiComponents.IconButtonWithText(FontAwesomeIcon.TrashAlt, "Remove"))// && state.trigSubIndex != -1)
        {
            if(state.activeCategory is not null)
            {
                // Remove a single trigger
                if(state.activeTrigger is not null)
                {
                    var catname = state.activeCategory!.Name;
                    RemoveTrigger(state.trigSubIndex, catname);
                    state.Reset();
                } else {
                    // Remove whole category
                    // Use popup modal to confirm removing the whole category
                    ImGui.OpenPopup("Remove Category?");
                }
            }
        }
        using(var popup = ImRaii.PopupModal("Remove Category?", ImGuiWindowFlags.NoResize))
        {
            if(popup)
            {
                ImGui.Text("Are you sure you want to remove this category \nand all the triggers contained within?\nThis action cannot be undone.");
                if(ImGui.Button("OK"))
                {
                    plugin.Configuration.TriggerTree.Remove(state.activeCategory!.Name);
                    plugin.Configuration.Save();
                    state.Reset();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if(ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
            }
        }

        // Shift trigger up and down buttons
        if(state.trigListIndex != -1)
        {
            // Shift Up button
            ImGui.SameLine();
            if(ImGuiComponents.IconButton(FontAwesomeIcon.ArrowUp))
            {
                // Reordering Triggers inside a Category
                if(state.activeTrigger is not null)
                {
                    if(state.trigSubIndex > 0)
                    {
                        state.activeCategory!.SwapTriggers(state.trigSubIndex, --state.trigSubIndex);
                        state.activeTrigger = state.activeCategory.Triggers.ElementAt(state.trigSubIndex);
                        --state.trigListIndex;
                    }
                } else if(state.activeCategory is not null) // Reordering Categories inside the Tree
                {
                    var index = plugin.Configuration.TriggerTree.GetIndexOfCategory(state.activeCategory.Name);
                    if(index > 0)
                    {
                        plugin.Configuration.TriggerTree.SwapCategories(index, --index);
                        state.activeCategory = plugin.Configuration.TriggerTree.ElementAt(index);
                        RefreshSelectionState(state.activeCategory.Name, state.activeCategory.opened, true);
                    }
                }
                updateConfig = true;
            }
            // Shift Down button
            ImGui.SameLine();
            if(ImGuiComponents.IconButton(FontAwesomeIcon.ArrowDown))
            {
                // Reordering Triggers inside a Category
                if(state.activeTrigger is not null)
                {
                    if(state.trigSubIndex < state.activeCategory!.Triggers.Count-1)
                    {
                        state.activeCategory.SwapTriggers(state.trigSubIndex, ++state.trigSubIndex);
                        state.activeTrigger = state.activeCategory.Triggers.ElementAt(state.trigSubIndex);
                        ++state.trigListIndex;
                    }
                } else if(state.activeCategory is not null) // Reordering Categories inside the Tree
                {
                    var index = plugin.Configuration.TriggerTree.GetIndexOfCategory(state.activeCategory);
                    if(index < plugin.Configuration.TriggerTree.Count-1)
                    {
                        plugin.Configuration.TriggerTree.SwapCategories(index, ++index);
                        state.activeCategory = plugin.Configuration.TriggerTree.ElementAt(index);
                        RefreshSelectionState(state.activeCategory.Name, state.activeCategory.opened, true);
                    }
                }
                updateConfig = true;
            }
        }

        ImGui.SameLine(ImGui.GetWindowWidth()-(125*ImGuiHelpers.GlobalScale));
        if(ImGui.Button("Clear All Triggers")
            && ImGui.GetIO().KeyShift)
        {
            ClearAllTriggers();
        }
        ImGuiCustom.HoverTooltip("Hold SHIFT to Clear All");

        // The below is the visual for the trigger tree structure
        ImGui.Separator();
        using (var child = ImRaii.Child("TriggerBoxWithScrollbar", Vector2.Zero, true))
        {
            if(child)
            {
                var idx = 0;
                foreach(var category in plugin.Configuration.TriggerTree)
                {
                    using var pushedCatId = ImRaii.PushId(category.Name);
                    if(ImGui.Checkbox($"##CheckBox{category.Name}{pushedCatId}", ref category.enabled))
                    {
                        updateConfig = true;
                    }
                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding();
                    ImGui.SetNextItemOpen(category.opened);
                    using var style = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, 30 * ImGuiHelpers.GlobalScale);
                    var treeFlags = ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.OpenOnArrow;
                    treeFlags |= state.trigListIndex==idx ? ImGuiTreeNodeFlags.Selected : 0;
                    using(var tree = ImRaii.TreeNode($"{category.Name}##TreeNode{category.Name}{pushedCatId}", treeFlags))
                    {
                        // if the category itself was selected
                        if(ImGui.IsItemClicked())
                        {
                            state.trigSubIndex = -1;
                            state.trigListIndex = idx;
                            state.activeTrigger = null;
                            state.activeCategory = category;
                        }

                        if(tree)
                        {
                            var subIdx = -1;
                            category.opened = true;
                            foreach(var trigger in category.Triggers)
                            {
                                ++idx; ++subIdx;
                                using var pushedTrigId = ImRaii.PushId($"{trigger.expression}{subIdx}");
                                if(ImGui.Checkbox($"##{trigger.expression}{pushedTrigId}", ref trigger.enabled))
                                {
                                    updateConfig = true;
                                }
                                ImGui.SameLine();
                                ImGui.AlignTextToFramePadding();
                                if(ImGui.Selectable($"{trigger.expression}##{trigger.expression}{pushedTrigId}", state.trigListIndex==idx))
                                {
                                    state.trigListIndex = idx;
                                    state.trigSubIndex = subIdx;
                                    state.activeTrigger = trigger;
                                    state.activeCategory = category;
                                }

                                using(var pop = ImRaii.ContextPopupItem($"##RemoveTriggerPopup"))
                                {
                                    if(pop)
                                    {
                                        state.trigListIndex = idx;
                                        state.trigSubIndex = subIdx;
                                        state.activeCategory = category;
                                        if(ImGui.MenuItem("Remove Trigger"))
                                        {
                                            RemoveTrigger(subIdx, category.Name);
                                            state.Reset();
                                            break; // exit the loop because the list is now invalidated
                                        }
                                    }
                                }
                            }
                        } else { category.opened = false; }

                    }
                    ++idx;
                }

                // Background text in the window to instruct users on creating their first triggers
                if(idx==0)
                {
                    using var color = ImRaii.PushColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 0.8f));
                    ImGui.Text("Looks like you have no triggers yet. To get started, click \"Add New.\"\n"+
                               "Or, you can Save a chat message from the \"Chat History\" tab.");
                }
            }
        }

        if(updateConfig)
        {
            plugin.Configuration.Save();
        }

        // checks if we click anywhere else and resets the selection state
        if(ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            state.Reset();
        }
    } // DrawTriggersTab

    private void DrawChatLogTab()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Filter: ");
        ImGui.SameLine();
        chatFilter.Draw("##ChatFilter", 180);
        ImGui.SameLine();
        if(ImGuiComponents.IconButton(FontAwesomeIcon.Times)) { chatFilter.Clear(); }
        ImGui.SameLine();
        if(ImGui.Button("Clear Log")) { ClearLog(); state.chatIndex = -1; }
        ImGui.Separator();

        using (var child = ImRaii.Child("ChatBoxWithScrollBar", Vector2.Zero, true))
        {
            if(child)
            {    
                var index = plugin.ChatLog.Count;
                foreach(var ses in plugin.ChatLog.Reverse())
                {
                    using var id = ImRaii.PushId($"{ses.ToString()}{index}");

                    if(chatFilter.PassFilter(ses))
                    {
                        if(ImGui.Selectable(ses, state.chatIndex==index, ImGuiSelectableFlags.DontClosePopups)) // Left-Click action
                        {
                            state.chatIndex = index;
                        }

                        //if(ImGui.BeginPopupContextItem("SaveMsgToTriggerPopup"))
                        using(var pop = ImRaii.ContextPopupItem($"##SaveMsgToTriggerPopup"))
                        {
                            if(pop)
                            {
                                state.chatIndex = index;
                                if(ImGui.MenuItem("Save to Triggers?"))
                                {
                                    AddTrigger(new TriggerEntry(ses), plugin.DefaultCategoryName);
                                    state.chatIndex = -1;
                                }
                            }
                        }
                    }
                    index--;
                } // foreach chatLog
            }
        }

        // This deselects any chat items if we click in the open space of the chat box
        if(ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            state.chatIndex = -1;
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
            ImGui.SetNextItemWidth(120 * ImGuiHelpers.GlobalScale);
            ImGui.DragScalar<uint>("Max Log History##MaxHistoryLength",
                ref plugin.Configuration.MaxLogHistory,0.2f,0, plugin.MaxLogHistoryCeiling,default,ImGuiSliderFlags.AlwaysClamp);
            if(plugin.Configuration.MaxLogHistory > plugin.MaxLogHistoryCeiling)
                plugin.Configuration.MaxLogHistory = Math.Clamp(plugin.Configuration.MaxLogHistory, 1, plugin.MaxLogHistoryCeiling);
            if(ImGui.IsItemDeactivatedAfterEdit())
            {
                plugin.Configuration.Save();
            }

            ImGui.NewLine();
        }
        
        // TTS Backend
        if(ImGui.CollapsingHeader("Text-to-Speech", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.SetNextItemWidth(140 * ImGuiHelpers.GlobalScale);
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
        plugin.Configuration.TriggerTree.Clear();
        plugin.Configuration.Save();
    }

    void AddTrigger(TriggerEntry trigger, string categoryName)
    {
        var trig = new TriggerEntry(trigger);
        if(trig.expression.Length == 0) trig.expression = "New Trigger";
        var category = plugin.Configuration.TriggerTree[categoryName];
        if(category is not null)
        {
            category.Triggers.Add(trig);
        } else
        {
            plugin.Configuration.TriggerTree.Add(new TriggerCategory(categoryName, [trig]));
        }
        plugin.Configuration.Save();
    }

    void RemoveTrigger(int idx, string categoryName)
    {
        var category = plugin.Configuration.TriggerTree[categoryName];
        if(category is not null)
        {
            category.Triggers.RemoveAt(idx);
            if(category.Triggers.Count == 0)
            {
                plugin.Configuration.TriggerTree.Remove(categoryName);
            }
            plugin.Configuration.Save();
        }
    }

    void RefreshSelectionState(string categoryName, bool openActiveCategory, bool stopAtCategory=false)
    {
        var tempIdx = 0;
        state.activeCategory = plugin.Configuration.TriggerTree[categoryName];
        if(state.activeCategory is not null)
        {
            foreach(var c in plugin.Configuration.TriggerTree)
            {
                tempIdx++;
                if(c.Name == categoryName)
                {
                    if(stopAtCategory) { state.trigSubIndex = -1; }
                    else               { state.trigSubIndex = c.Triggers.Count-1; }
                    state.activeCategory = c;
                    break;
                } else if(!c.opened) { continue; }
                foreach(var t in c.Triggers)
                {
                    tempIdx++;
                }
            }
            state.trigListIndex = tempIdx + state.trigSubIndex;
            //Log.Debug($"activeCategory.Name == \"{state.activeCategory.Name}\" ;; trigListIndex == {state.trigListIndex} ;; trigSubIndex == {state.trigSubIndex}");
            state.activeTrigger = stopAtCategory ? null : state.activeCategory.Triggers.ElementAt(state.trigSubIndex);
            state.activeCategory.opened = openActiveCategory;
        }
    }
}
