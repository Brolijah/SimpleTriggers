using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Game.Text.SeStringHandling;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using SimpleTriggers.Windows;
using SimpleTriggers.TextToSpeech;
using System.Threading.Tasks;
using System;

namespace SimpleTriggers;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;

    public string Name => "Simple Triggers";
    private const string CommandPrefixA = "/simpletriggers";
    private const string CommandPrefixB = "/strig";
    public uint MaxLogHistoryCeiling = 10000; // Hard coded limit. Who says? Me says.

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("Simple Triggers");
    //private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private ChatListener ChatListener { get; init; }
    private STKokoro StKokoro { get; init; }
    internal Queue<SeString> ChatLog { get; init; }
    
    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        // TODO: Configuration validation
        if(Configuration.MaxLogHistory > MaxLogHistoryCeiling) { Configuration.MaxLogHistory = MaxLogHistoryCeiling; }
        ChatListener = new ChatListener(this, ChatGui);
        ChatLog = [];

        StKokoro = new STKokoro(PluginInterface.AssemblyLocation.Directory?.FullName!);
        StKokoro.SetVoice(KokoroVoiceHelper.ToString(Configuration.TTSKokoroVoice));

        SwapTTSBackend(Configuration.TTSProvider);
        // You might normally want to embed resources and load them from the manifest stream
        //var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";

        //ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, version);

        //WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandPrefixA, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Simple Triggers window."
        });

        CommandManager.AddHandler(CommandPrefixB, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Simple Triggers window."
        });

        // Tell the UI system that we want our windows to be drawn through the window system
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;

        // This adds a button to the plugin installer entry of this plugin which allows
        // toggling the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        // Adds another button doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [SamplePlugin] ===A cool log message from Sample Plugin===
        //Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
    }

    public void Dispose()
    {
        // Unregister all actions to not leak anything during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        
        WindowSystem.RemoveAllWindows();

        //ConfigWindow.Dispose();
        MainWindow.Dispose();
        ChatListener.Dispose();
        StKokoro.Dispose();

        CommandManager.RemoveHandler(CommandPrefixA);
        CommandManager.RemoveHandler(CommandPrefixB);
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        var argArr = args.Trim().Split(" ");
        switch (argArr[0])
        {
            case "enable":
                Configuration.EnableTriggers = true;
                Configuration.Save();
            break;
            case "disable":
                Configuration.EnableTriggers = false;
                Configuration.Save();
            break;

            default:
                MainWindow.Toggle();
            break;
        }
    }

    internal void SwapTTSBackend(TextToSpeechType ttst)
    {
        switch (this.Configuration.TTSProvider)
        {
            case TextToSpeechType.eSpeakNG:
                //tts_espeak.Start();
                break;
            // TODO: The others...
        }
    }

    internal void SwapKokoroVoice(string voice)
    {
        StKokoro.SetVoice(voice);
    }
    
    internal void SpeakTTS(string message)
    {
        if(message.Length > 0)
        {
            switch (this.Configuration.TTSProvider)
            {
                case TextToSpeechType.eSpeakNG:
                    Task.Run(() => Process.Start("/usr/bin/espeak-ng",$"\"{message}\""));
                    break;
                case TextToSpeechType.flite:
                    Task.Run(() => Process.Start("/usr/bin/flite", $"-t \"{message}\""));
                    break;
                case TextToSpeechType.Kokoro:
                    StKokoro.Speak(message);
                    break;
                default:
                    break;
            }
        }
    }

    public void ToggleConfigUi() => MainWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
