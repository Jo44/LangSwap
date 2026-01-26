using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using LangSwap.input;
using LangSwap.translation;
using LangSwap.Windows;
using System;

namespace LangSwap;

// Plugin main
public sealed class Plugin : IDalamudPlugin
{
    // Services
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IKeyState KeyState { get; private set; } = null!;
    [PluginService] internal static IChatGui? ChatGui { get; private set; }

    // References
    private const string CommandName = "/langswap";
    public Configuration Configuration { get; init; }
    public readonly WindowSystem WindowSystem = new("LangSwap");
    private ConfigWindow ConfigWindow { get; init; }
    private bool isLanguageSwapped = false;
    private bool previousComboPressed = false;

    private readonly ComboDetector comboDetector;

    // Constructor
    public Plugin()
    {
        // Load configuration
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // Initialize windows
        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        // Initialize combo detector
        comboDetector = new ComboDetector(KeyState, Configuration, Log);

        // Register command
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the LangSwap configuration window"
        });

        // Register UI callbacks
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.Draw += OnDraw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        // Get client language
        GetClientLanguage();

        // Log plugin load
        Log.Information($"=== LangSwap plugin loaded ===");
        Log.Debug($"Configuration loaded : ClientLanguage = {Configuration.ClientLanguage}, TargetLanguage = {Configuration.TargetLanguage}, PrimaryKey = {Configuration.PrimaryKey}, UseCtrl = {Configuration.UseCtrl}, UseAlt = {Configuration.UseAlt}, UseShift = {Configuration.UseShift}");
    }

    // OnDraw callback
    private void OnDraw()
    {
        // Check combo shortcut
        bool comboHeld = comboDetector.IsComboHeld();

        // Handle language swap/restore
        if (comboHeld && !previousComboPressed)
        {
            if (!isLanguageSwapped) SwapLanguage();
        }
        else if (!comboHeld && previousComboPressed)
        {
            if (isLanguageSwapped) RestoreLanguage();
        }

        // Update previous state
        previousComboPressed = comboHeld;
    }

    // Swap language
    private void SwapLanguage()
    {
        if (isLanguageSwapped) return;
        // Check if target language is the same as client language
        if (Configuration.TargetLanguage == Configuration.ClientLanguage)
        {
            ChatGui?.Print("[LangSwap] Target language is the same as client language");
            Log.Warning("Target language is the same as client language; swap aborted.");
            return;
        }
        SetTargetLanguage();
        isLanguageSwapped = true;
        ChatGui?.Print($"[LangSwap] Swap to {Enum.GetName(typeof(LanguageEnum), Configuration.TargetLanguage)}");
        Log.Information($"Language swapped to {Enum.GetName(typeof(LanguageEnum), Configuration.TargetLanguage)} ({Configuration.TargetLanguage})");
    }

    // Restore language
    private void RestoreLanguage()
    {
        if (!isLanguageSwapped) return;
        SetClientLanguage();
        isLanguageSwapped = false;
        ChatGui?.Print($"[LangSwap] Restored to {Enum.GetName(typeof(LanguageEnum), Configuration.ClientLanguage)}");
        Log.Information($"Language restored to {Enum.GetName(typeof(LanguageEnum), Configuration.ClientLanguage)} ({Configuration.ClientLanguage})");
    }

    // Set client language
    private void SetClientLanguage()
    {
        // TODO: Implement language retrieval logic
    }

    // Set target language
    private void SetTargetLanguage()
    {
        // TODO: Implement language setting logic
    }

    // Get client language
    private void GetClientLanguage()
    {
        ClientLanguage clientLanguage = ClientState.ClientLanguage;
        switch (clientLanguage)
        {
            case ClientLanguage.Japanese:
                Configuration.ClientLanguage = 0;
                break;
            case ClientLanguage.English:
                Configuration.ClientLanguage = 1;
                break;
            case ClientLanguage.German:
                Configuration.ClientLanguage = 2;
                break;
            case ClientLanguage.French:
                Configuration.ClientLanguage = 3;
                break;
            default:
                Log.Warning($"Unrecognized client language: {clientLanguage} ==> english default");
                Configuration.ClientLanguage = 1;
                break;
        }
        Configuration.Save();
    }

    // Command handler
    private void OnCommand(string command, string args) => ConfigWindow.Toggle();

    // Toggle config UI
    public void ToggleConfigUi() => ConfigWindow.Toggle();

    // Dispose
    public void Dispose()
    {
        // Restore language if swapped
        if (isLanguageSwapped) RestoreLanguage();

        // Unregister UI callbacks
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.Draw -= OnDraw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

        // Dispose windows
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();

        // Unregister command
        CommandManager.RemoveHandler(CommandName);
    }

}
