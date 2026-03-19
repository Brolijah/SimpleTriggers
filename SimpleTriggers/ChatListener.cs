using System;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using SimpleTriggers.SeFunctions;

namespace SimpleTriggers;

internal class ChatListener : IDisposable
{
    private readonly Plugin plugin;
    private readonly IChatGui chatGui;

    internal ChatListener(Plugin plugin, IChatGui chatGui)
    {
        this.plugin = plugin;
        this.chatGui = chatGui;
        chatGui.ChatMessage += OnChatMessage;
    }

    private unsafe string SanitizeString(string text)
    {
        var utfStr = new Utf8String(text);
        utfStr.SanitizeString(
            AllowedEntities.UppercaseLetters | AllowedEntities.LowercaseLetters |
            AllowedEntities.Numbers | AllowedEntities.SpecialCharacters | AllowedEntities.CJK);
        return utfStr.ToString().Trim();
    }

    public void Dispose()
    {
        chatGui.ChatMessage -= OnChatMessage;
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        // Ignore messages sent from the plugin
        var msgStr = SanitizeString(message.ToString());
        if(msgStr.StartsWith($"[{plugin.Name}]")) { return; }
        
        if(plugin.Configuration.EnableTriggers)
        {
            foreach(var category in plugin.Configuration.TriggerTree)
            {
                if(category.enabled)
                {
                    foreach(var trig in category.Triggers)
                    {
                        if(trig.enabled)
                        {
                            var expression = trig.expression;
                            if(msgStr.Contains(expression, StringComparison.CurrentCultureIgnoreCase))
                            {
                                if(trig.doResponseTTS && (trig.response.Length > 0))
                                {
                                    plugin.SpeakTTS(trig.response);
                                }
                                
                                if(trig.doPlaySound && trig.soundFx > 0)
                                {
                                    PlaySound.Play(SoundsExtensions.FromIdx(trig.soundFx));
                                }

                                if(trig.doPostInChat && (trig.response.Length > 0))
                                {
                                    plugin.PrintChatMsg(trig.response);
                                }

                            }
                        }
                    }
                }
                
            }
            
        }

        if(plugin.doLogChatHistory)
        {
            while(plugin.ChatLog.Count >= plugin.Configuration.MaxLogHistory)
            {
                plugin.ChatLog.Dequeue();
            }
            plugin.ChatLog.Enqueue(msgStr);
        }
    }
}