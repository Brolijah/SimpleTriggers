using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
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
    internal Queue<string> ChatLog { get; init; }
    public Configuration Configuration { get; init; }
    
    public readonly WindowSystem WindowSystem = new("Simple Triggers");
    private MainWindow MainWindow { get; init; }
    private ChatListener ChatListener { get; init; }
    private ITextToSpeech? TextToSpeech { get; set; }
    private AudioPlayer AudioPlayer { get; set; }
    private readonly ConcurrentQueue<string> SpeakQueue = new();
    private readonly Lock speakLock = new();
    private CancellationTokenSource cts = new ();
    private bool ttsInProgress = false;
    
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
                SetTriggerState(true);
            break;
            case "off":
            case "disable":
                SetTriggerState(false);
            break;
            case "toggle":
                SetTriggerState(!Configuration.EnableTriggers);
            break;
            case "speak":
                var msg = args.Split(" ", 2);
                if(msg.Length > 1) SpeakTTS(msg[1]);
            break;
            case "stop":
                StopAudioPlayback(true);
            break;

            default:
                MainWindow.Toggle();
            break;
        }
    }

    private void OnCommandToggle(string command, string args)
    {
        SetTriggerState(!Configuration.EnableTriggers);
    }

    private void OnCommandSpeak(string command, string args)
    {
        SpeakTTS(args);
    }

    private void OnCommandStop(string command, string args)
    {
        StopAudioPlayback(true);
    }

    internal void SetTriggerState(bool state)
    {
        Configuration.EnableTriggers = state;
        PrintChatMsg(string.Format("All triggers are {0}.", Configuration.EnableTriggers ? "enabled" : "disabled"));
        Configuration.Save();
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
            case TextToSpeechType.DecTalk:
                TextToSpeech = new DecTalk(PluginInterface.GetPluginConfigDirectory(), AudioPlayer);
                TextToSpeech.SetVoice(DecTalkVoiceHelper.ToString(Configuration.DecTalk.Voice));
                TextToSpeech.SetSpeed(Configuration.DecTalk.Speed);
                TextToSpeech.SetVolume(Configuration.DecTalk.Volume);
                break;
        }
    }

    internal void PrintChatMsg(string message)
    {
        ChatGui.Print(message, $"{Name}", 529);
    }
    
    internal void SpeakTTS(string message)
    {
        if(cts.IsCancellationRequested)
        {
            cts.Dispose();
            cts = new();
        }

        if(message.Length > 0)
        {
            SpeakQueue.Enqueue(message);
            lock(speakLock)
            {
                if(ttsInProgress) return;
                ttsInProgress = true;
            }
            Task.Run(ProcessSpeakQueue);
        }
    }

    private async void ProcessSpeakQueue()
    {
        while(SpeakQueue.TryDequeue(out var msg) && !cts.IsCancellationRequested)
        {
            var speakTask = Task.Run(() =>
            {
                switch (Configuration.TTSProvider)
                {
                    case TextToSpeechType.Kokoro:
                        TextToSpeech?.Speak(msg, Configuration.Kokoro.UseEspeak);
                        break;
                    case TextToSpeechType.WindowsSystem:
                    case TextToSpeechType.DecTalk:
                        TextToSpeech?.Speak(msg);
                        break;
                    default:
                        break;
                }
            });
            var timeoutTask = Task.Delay(5000);
            var finished = await Task.WhenAny(speakTask, timeoutTask);
            if(finished == timeoutTask && !cts.IsCancellationRequested)
            {
                STLog.Log.Error("\n"+
                    "  A speaker thread timed out trying to play a TTS message. If you're seeing this, then\n"+
                    "  you should restart this plugin or your game. Restarting the plugin may mean your game\n"+
                    "  will continue to function as normal, but there's probably a zombie thread running in\n"+
                    "  the background that did not exit gracefully. Most likely DECtalk misbehaving.\n"+
                    "  If you don't want to restart the plugin, swapping TTS providers may also work.");
                StopAudioPlayback(true);
                break;
            }
        }

        if(cts.IsCancellationRequested)
        {
            STLog.Log.Information("ProcessSpeakQueue(): Cancellation Requested!");
            ttsInProgress = false;
            return;
        }

        lock(speakLock)
        {
            ttsInProgress = false;
            if(!SpeakQueue.IsEmpty)
            {
                ttsInProgress = true;
                Task.Run(ProcessSpeakQueue);
            }
        }
    }

    internal void StopAudioPlayback(bool clearQueue = false) { cts.Cancel(); SpeakQueue.Clear(); AudioPlayer.StopPlayback(clearQueue); }
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
