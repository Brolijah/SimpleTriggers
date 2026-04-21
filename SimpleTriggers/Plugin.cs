using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using SimpleTriggers.Windows;
using SimpleTriggers.TextToSpeech;
using SimpleTriggers.Logger;
using System;

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
    private const string CommandToggle = "/sttoggle";
    private const string CommandSpeak = "/stspeak";
    private const string CommandStop = "/ststop";
    internal readonly string DefaultCategoryName = "Default";
    internal uint MaxLogHistoryCeiling = 10000; // Hard coded limit. Who says? Me says.
    internal bool doLogChatHistory = false; // transient value, must be enabled by the user
    internal bool doIncludeChatTypeInfo = false; // includes the ChatType information next to chat messages
    public Configuration Configuration { get; init; }
    
    public readonly WindowSystem WindowSystem = new("Simple Triggers");
    private MainWindow MainWindow { get; init; }
    private ChatListener ChatListener { get; init; }
    private ITextToSpeech? TextToSpeech { get; set; }
    private AudioPlayer AudioPlayer { get; set; }
    internal Queue<string> ChatLog { get; init; }
    
    public Plugin()
    {
        STLog.SetLogger(Log);
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        if(Configuration.MaxLogHistory > MaxLogHistoryCeiling) { Configuration.MaxLogHistory = MaxLogHistoryCeiling; }
        ChatLog = new Queue<string>((int)MaxLogHistoryCeiling);
        ChatListener = new ChatListener(this, ChatGui);
        if(!Enum.IsDefined(Configuration.AudioBackend)) Configuration.AudioBackend = AudioOutputType.DirectSound;
        AudioPlayer = new AudioPlayer(Configuration.AudioOutputDevice, Configuration.AudioBackend) { BlendStreams=Configuration.BlendAudioStreams };
        SwapTTSBackend();

        MainWindow = new MainWindow(this, GetInformationalVersion());
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandPrefixA, new CommandInfo(OnCommand)
        {
            DisplayOrder=2, HelpMessage = "Opens the Simple Triggers window."
        });

        CommandManager.AddHandler(CommandPrefixB, new CommandInfo(OnCommand)
        {
            DisplayOrder=1, HelpMessage = "Opens the Simple Triggers window."
        });

        CommandManager.AddHandler(CommandToggle, new CommandInfo(OnCommandToggle)
        {
            DisplayOrder=3, HelpMessage = "Toggles the entire trigger system on or off."
        });

        CommandManager.AddHandler(CommandSpeak, new CommandInfo(OnCommandSpeak)
        {
            DisplayOrder=4, HelpMessage = "Reads aloud the phrase using the configured TTS."
        });
        
        CommandManager.AddHandler(CommandStop, new CommandInfo(OnCommandStop)
        {
            DisplayOrder=5, HelpMessage = "Stops the current audio playback and removes any queued streams."
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
        AudioPlayer.Dispose();

        CommandManager.RemoveHandler(CommandPrefixA);
        CommandManager.RemoveHandler(CommandPrefixB);
        CommandManager.RemoveHandler(CommandToggle);
        CommandManager.RemoveHandler(CommandSpeak);
        CommandManager.RemoveHandler(CommandStop);
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
            case "toggle":
                Configuration.EnableTriggers = !Configuration.EnableTriggers;
                Configuration.Save();
                PrintChatMsg(string.Format("All triggers are {0}.", Configuration.EnableTriggers ? "enabled" : "disabled"));
            break;
            case "speak":
                var msg = args.Split(" ", 2);
                if(msg.Length > 1) SpeakTTS(msg[1]);
            break;
            case "stop":
                AudioPlayer.StopPlayback(true);
            break;

            default:
                MainWindow.Toggle();
            break;
        }
    }

    private void OnCommandToggle(string command, string args)
    {
        Configuration.EnableTriggers = !Configuration.EnableTriggers;
        Configuration.Save();
        PrintChatMsg(string.Format("All triggers are {0}.", Configuration.EnableTriggers ? "enabled" : "disabled"));
    }

    private void OnCommandSpeak(string command, string args)
    {
        SpeakTTS(args);
    }

    private void OnCommandStop(string command, string args)
    {
        AudioPlayer.StopPlayback(true);
    }

    internal void SwapTTSBackend()
    {
        TextToSpeech?.Dispose();
        TextToSpeech = null;

        switch (Configuration.TTSProvider)
        {
            case TextToSpeechType.Kokoro:
                TextToSpeech = new STKokoro(PluginInterface.AssemblyLocation.Directory?.FullName!, PluginInterface.GetPluginConfigDirectory(), AudioPlayer);
                TextToSpeech.SetVoice(KokoroVoiceHelper.ToString(Configuration.Kokoro.Voice));
                TextToSpeech.SetSpeed(Configuration.Kokoro.Speed);
                TextToSpeech.SetVolume(Configuration.Kokoro.Volume);
                break;
            case TextToSpeechType.WindowsSystem:
                TextToSpeech = new STWinSpeech(AudioPlayer);
                TextToSpeech.SetVoice(Configuration.WinSpeech.Voice);
                TextToSpeech.SetSpeed(Configuration.WinSpeech.Speed);
                TextToSpeech.SetVolume(Configuration.WinSpeech.Volume);
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
                case TextToSpeechType.Kokoro:
                    Task.Run(() => TextToSpeech?.Speak(message, Configuration.Kokoro.UseEspeak));
                    break;
                case TextToSpeechType.WindowsSystem:
                    Task.Run(() => TextToSpeech?.Speak(message));
                    break;
                default:
                    break;
            }
        }
    }

    internal void StopAudioPlayback(bool clearQueue = false) => AudioPlayer.StopPlayback(clearQueue);
    internal void SetAudioBackend(AudioOutputType type) => AudioPlayer.InitializeAudioBackend(type,null);
    internal void SetAudioOutputDevice(string id) => AudioPlayer.SetOutputDevice(id);
    internal void SetAudioBlending(bool blend) => AudioPlayer.BlendStreams = blend;
    internal void SetTTSVoice(string voice) => TextToSpeech?.SetVoice(voice);
    internal void SetTTSVolume(float volume) => TextToSpeech?.SetVolume(volume);
    internal void SetTTSSpeed(float speed) => TextToSpeech?.SetSpeed(speed);
    internal void SetTTSLanguage(string lang) => TextToSpeech?.SetLanguage(lang);
    internal bool CanSpeak() => TextToSpeech?.IsInitialized() ?? false;

    public void ToggleConfigUi() => MainWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
