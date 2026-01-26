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
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInterop { get; private set; } = null!;
    [PluginService] internal static IKeyState KeyState { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    // References
    public required Configuration Configuration { get; init; }
    public required ConfigWindow ConfigWindow { get; init; }
    public readonly WindowSystem WindowSystem = new("LangSwap");
    public required ComboDetector comboDetector;
    public required ExcelProvider excelProvider;
    public required TranslationCache translationCache;
    public required TooltipHook tooltipHook;
    private const string CommandName = "/langswap";
    private bool previousComboPressed = false;
    private bool isLanguageSwapped = false;

    // Constructor
    public Plugin()
    {
        try
        {
            // Load configuration
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            // Initialize client language
            InitClientLanguage();

            // Initialize config window
            ConfigWindow = new ConfigWindow(this, Log);
            WindowSystem.AddWindow(ConfigWindow);

            // Initialize components
            comboDetector = new ComboDetector(Configuration, KeyState, Log);
            excelProvider = new ExcelProvider(DataManager, Log);
            translationCache = new TranslationCache(excelProvider, Log);
            tooltipHook = new TooltipHook(Configuration, GameGui, GameInterop, translationCache, Log);

            // Enable tooltip hook
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

            // Log plugin load
            Log.Information($"=== LangSwap plugin loaded ===");
            Log.Debug($"Configuration loaded : ClientLanguage = {Configuration.ClientLanguage}, TargetLanguage = {Configuration.TargetLanguage}, PrimaryKey = {Configuration.PrimaryKey}, UseCtrl = {Configuration.UseCtrl}, UseAlt = {Configuration.UseAlt}, UseShift = {Configuration.UseShift}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception during plugin initialization");
        }
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
            // Notify user
            ChatGui.Print("[LangSwap] Target language is the same as client language");
            Log.Warning("Target language is the same as client language; swap aborted.");
            return;
        }
        isLanguageSwapped = true;

        // TODO : on est sur de vouloir faire ca ?
        // Update tooltip hook with new language
        tooltipHook.SetLanguageSwapped(true);

        // Notify user
        ChatGui.Print($"[LangSwap] Swap to {Enum.GetName(typeof(LanguageEnum), Configuration.TargetLanguage)}");
        Log.Information($"Language swapped to {Enum.GetName(typeof(LanguageEnum), Configuration.TargetLanguage)} ({Configuration.TargetLanguage})");
    }

    // Restore language
    private void RestoreLanguage()
    {
        if (!isLanguageSwapped) return;
        isLanguageSwapped = false;

        // TODO : on est sur de vouloir faire ca ?
        // Update tooltip hook to restore original language
        tooltipHook.SetLanguageSwapped(false);

        // Notify user
        ChatGui.Print($"[LangSwap] Restored to {Enum.GetName(typeof(LanguageEnum), Configuration.ClientLanguage)}");
        Log.Information($"Language restored to {Enum.GetName(typeof(LanguageEnum), Configuration.ClientLanguage)} ({Configuration.ClientLanguage})");
    }

    // Initialize client language
    private void InitClientLanguage()
    {
        ClientLanguage language = ClientState.ClientLanguage;
        switch (language)
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
                Log.Warning($"Unrecognized client language: {language} ==> english default");
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
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.Draw -= OnDraw;
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;

        // Unregister command
        CommandManager.RemoveHandler(CommandName);

        // Dispose tooltip hook
        tooltipHook?.Dispose();

        // Cleanup windows
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
    }

}
