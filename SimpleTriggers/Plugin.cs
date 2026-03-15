using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using System.Collections.Generic;
using System.Reflection;
using SimpleTriggers.Windows;
using SimpleTriggers.TextToSpeech;
using System.Threading.Tasks;
using SimpleTriggers.Logger;

namespace SimpleTriggers;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log {get; private set; } = null!;

    public string Name => "Simple Triggers";
    private const string CommandPrefixA = "/simpletriggers";
    private const string CommandPrefixB = "/strig";
    internal readonly string DefaultCategoryName = "Default";
    internal uint MaxLogHistoryCeiling = 10000; // Hard coded limit. Who says? Me says.
    internal bool doLogChatHistory = false; // transient value, must be enabled by the user
    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("Simple Triggers");
    private MainWindow MainWindow { get; init; }
    private ChatListener ChatListener { get; init; }
    private ITextToSpeech? TextToSpeech {get; set;}
    internal Queue<string> ChatLog { get; init; }
    
    public Plugin()
    {
        STLog.SetLogger(Log);
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        if(Configuration.MaxLogHistory > MaxLogHistoryCeiling) { Configuration.MaxLogHistory = MaxLogHistoryCeiling; }
        ChatListener = new ChatListener(this, ChatGui);
        ChatLog = [];

        SwapTTSBackend(Configuration.TTSProvider);
        var version = GetInformationalVersion();

        MainWindow = new MainWindow(this, version);
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
    }

    public string GetInformationalVersion()
    {
        var ifv = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return ifv?.InformationalVersion.ToString() ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
    }

    public void Dispose()
    {
        // Unregister all actions to not leak anything during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        
        WindowSystem.RemoveAllWindows();

        MainWindow.Dispose();
        ChatListener.Dispose();
        TextToSpeech?.Dispose();

        CommandManager.RemoveHandler(CommandPrefixA);
        CommandManager.RemoveHandler(CommandPrefixB);
    }

    private void OnCommand(string command, string args)
    {
        var argArr = args.Trim().Split(" ");
        switch (argArr[0])
        {
            case "on":
            case "enable":
                Configuration.EnableTriggers = true;
                Configuration.Save();
                PrintChatMsg("All triggers are enabled.");
            break;
            case "off":
            case "disable":
                Configuration.EnableTriggers = false;
                Configuration.Save();
                PrintChatMsg("All triggers are disabled.");
            break;

            default:
                MainWindow.Toggle();
            break;
        }
    }

    internal void SwapTTSBackend(TextToSpeechType ttst)
    {
        TextToSpeech?.Dispose();
        TextToSpeech = null;

        switch (Configuration.TTSProvider)
        {
            case TextToSpeechType.Kokoro:
                TextToSpeech = new STKokoro(PluginInterface.AssemblyLocation.Directory?.FullName!, PluginInterface.GetPluginConfigDirectory());
                TextToSpeech.SetVoice(KokoroVoiceHelper.ToString(Configuration.TTSKokoroVoice));
                TextToSpeech.SetSpeed(Configuration.TTSSpeed);
                TextToSpeech.SetVolume(Configuration.TTSVolume);
                break;
            case TextToSpeechType.WindowsSystem:
                TextToSpeech = new STWinSpeech();
                TextToSpeech.SetVoice(Configuration.WinSpeechVoice);
                TextToSpeech.SetSpeed(Configuration.TTSSpeed);
                TextToSpeech.SetVolume(Configuration.TTSVolume);
                break;
        }
    }

    internal void PrintChatMsg(string message)
    {
        ChatGui.Print(message, $"{Name}", 529);
    }
    
    internal void SpeakTTS(string message)
    {
        if(message.Length > 0)
        {
            switch (Configuration.TTSProvider)
            {
                /* case TextToSpeechType.eSpeakNG:
                    Task.Run(() => Process.Start("/usr/bin/espeak-ng",$"\"{message}\""));
                    break;
                case TextToSpeechType.flite:
                    Task.Run(() => Process.Start("/usr/bin/flite", $"-t \"{message}\""));
                    break;
                */
                case TextToSpeechType.Kokoro:
                    Task.Run(() => TextToSpeech?.Speak(message, Configuration.KokoroUseEspeak));
                    break;
                case TextToSpeechType.WindowsSystem:
                    Task.Run(() => TextToSpeech?.Speak(message));
                    break;
                default:
                    break;
            }
        }
    }

    internal void SetTTSVoice(string voice) => TextToSpeech?.SetVoice(voice);
    internal void SetTTSVolume(float volume) => TextToSpeech?.SetVolume(volume);
    internal void SetTTSSpeed(float speed) => TextToSpeech?.SetSpeed(speed);
    internal bool CanSpeak() => TextToSpeech?.IsInitialized() ?? false;

    public void ToggleConfigUi() => MainWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
