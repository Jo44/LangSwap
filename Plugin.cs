using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using LangSwap.input;
using LangSwap.translation;
using LangSwap.ui;
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
    private readonly ComboDetector comboDetector;
    private readonly TooltipHook tooltipHook;
    private bool isLanguageSwapped = false;
    private bool previousComboPressed = false;

    // Constructor
    public Plugin(IDataManager dataManager, IGameInteropProvider gameInterop, ISigScanner sigScanner, IGameGui gameGui)
    {
        // Load configuration
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // Initialize windows
        ConfigWindow = new ConfigWindow(this, Log);
        WindowSystem.AddWindow(ConfigWindow);

        // Initialize combo detector
        comboDetector = new ComboDetector(Configuration, KeyState, Log);

        // Initialize translation system
        var excelProvider = new ExcelProvider(Configuration, dataManager, Log);
        var translationCache = new TranslationCache(excelProvider, Log);
        tooltipHook = new TooltipHook(Configuration, gameInterop, sigScanner, translationCache, Log, gameGui);
        tooltipHook.Enable();

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
        // Evaluate combo state
        bool comboPressed = comboDetector.IsComboPressed();

        // Toggle the swap state
        if (comboPressed && !previousComboPressed)
        {
            ToggleLanguageSwap();
        }

        // Update previous state
        previousComboPressed = comboPressed;
    }

    // Toggle language swap
    private void ToggleLanguageSwap()
    {
        if (isLanguageSwapped)
        {
            // Currently swapped -> restore
            RestoreLanguage();
        }
        else
        {
            // Currently not swapped -> swap
            SwapLanguage();
        }
    }

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

        // Dispose tooltip hook
        tooltipHook?.Dispose();

        // Unregister command
        CommandManager.RemoveHandler(CommandName);
    }

    // Get client language
    private void GetClientLanguage()
    {
        // Map client language to configuration
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

    // Swap language
    private void SwapLanguage()
    {
        // Check if already swapped
        if (isLanguageSwapped)
        {
            Log.Debug("Language already swapped, ignoring");
            return;
        }

        // Check if target language is the same as client language
        if (Configuration.TargetLanguage == Configuration.ClientLanguage)
        {
            ChatGui?.Print("[LangSwap] Target language is the same as client language");
            Log.Warning("Target language is the same as client language; swap aborted.");
            return;
        }

        // Activate tooltip swap
        tooltipHook?.SwapLanguage();

        // Update state
        isLanguageSwapped = true;

        // Notify user
        ChatGui?.Print($"[LangSwap] Swapped to {Enum.GetName(typeof(LanguageEnum), Configuration.TargetLanguage)}");
        Log.Information($"Language swapped to {Enum.GetName(typeof(LanguageEnum), Configuration.TargetLanguage)} ({Configuration.TargetLanguage})");
    }

    // Restore language
    private void RestoreLanguage()
    {
        // Check if already restored
        if (!isLanguageSwapped)
        {
            Log.Debug("Language not swapped, ignoring restore");
            return;
        }

        // Deactivate tooltip swap
        tooltipHook?.RestoreLanguage();

        // Update state
        isLanguageSwapped = false;

        // Notify user
        ChatGui?.Print($"[LangSwap] Restored to {Enum.GetName(typeof(LanguageEnum), Configuration.ClientLanguage)}");
        Log.Information($"Language restored to {Enum.GetName(typeof(LanguageEnum), Configuration.ClientLanguage)} ({Configuration.ClientLanguage})");
    }

    // Command handler
    private void OnCommand(string command, string args) => ConfigWindow.Toggle();

    // Toggle config UI
    public void ToggleConfigUi() => ConfigWindow.Toggle();
}
