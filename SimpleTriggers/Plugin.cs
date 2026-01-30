using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SimpleTriggers.Windows;
using System.Collections.Generic;
using Dalamud.Game.Text.SeStringHandling;
using System.Reflection;

namespace SimpleTriggers;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    //[PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    //[PluginService] internal static IClientState ClientState { get; private set; } = null!;
    //[PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    //[PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    //[PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;

    public string Name => "Simple Triggers";
    private const string CommandPrefixA = "/simpletriggers";
    private const string CommandPrefixB = "/strig";


    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("Simple Triggers");
    //private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private ChatListener ChatListener { get; init; }
    internal Queue<SeString> chatLog { get; init; }
    internal int logMaxSize = 512;
    
    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        ChatListener = new ChatListener(this, ChatGui);
        chatLog = [];

        // You might normally want to embed resources and load them from the manifest stream
        //var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";

        //ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, version);

        //WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandPrefixA, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        CommandManager.AddHandler(CommandPrefixB, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
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

        CommandManager.RemoveHandler(CommandPrefixA);
        CommandManager.RemoveHandler(CommandPrefixB);
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        if (args.Length == 0)
        {
            MainWindow.Toggle();
        } else
        {
            // TODO: Parse arguments
        }
    }
    
    public void ToggleConfigUi() => MainWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
