using System;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
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

    public void Dispose()
    {
        chatGui.ChatMessage -= OnChatMessage;
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        // Ignore messages sent from the plugin
        var msgStr = message.ToString();
        if(msgStr.StartsWith($"[{plugin.Name}]")) { return; }

        foreach(var trig in plugin.Configuration.Triggers)
        {
            var expression = trig.expression;
            if(msgStr.Contains(expression, StringComparison.CurrentCultureIgnoreCase))
            {
                if(trig.doResponseTTS && (trig.response.Length > 0))
                {
                    plugin.TestTTS(trig.response);
                }
                
                if(trig.doPlaySound && trig.soundFx > 0)
                {
                    PlaySound.Play(SoundsExtensions.FromIdx(trig.soundFx));
                }

                if(trig.doPostInChat && (trig.response.Length > 0))
                {
                    chatGui.Print(trig.response, $"{plugin.Name}", 529);
                }

            }
        }

        while(plugin.ChatLog.Count >= plugin.Configuration.MaxLogHistory)
        {
            plugin.ChatLog.Dequeue();
        }
        plugin.ChatLog.Enqueue(message);
    }
}