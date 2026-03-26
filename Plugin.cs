using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using LangSwap.hook;
using LangSwap.input;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.Windows;
using Serilog;
using System;

namespace LangSwap;

// ----------------------------
// Plugin Main Class
// ----------------------------
public sealed class Plugin : IDalamudPlugin
{
    // Log
    private const string Class = "[Plugin.cs]";

    // Plugin services
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInterop { get; private set; } = null!;
    [PluginService] internal static IKeyState KeyState { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;

    // Core components
    private readonly Configuration config = null!;
    private readonly ConfigWindow configWindow = null!;
    private readonly ExcelProvider excelProvider = null!;
    private readonly HookManager hookManager = null!;
    private readonly ShortcutDetector shortcutDetector = null!;
    private readonly TranslationCache translationCache = null!;
    private readonly Utilities utilities = null!;
    private readonly WindowSystem windowSystem = new("LangSwap");

    // Global constants
    private const string CommandName = "/langswap";
    private const int DeferredFrames = 2;
    private const byte LogTagColor = 45;
    private const byte MessageColor = 57;
    private const int ToggleCooldownMs = 500;

    // Toggle state
    private int deferredFrameCount = 0;
    private bool disposed = false;
    private bool isSwapEnabled = false;
    private bool isSwapping = false;
    private DateTime lastToggle = DateTime.MinValue;
    private bool previousShortcutPressed = false;

    // ----------------------------
    // Initialization
    // ----------------------------
    public Plugin()
    {
        try
        {
            // Log plugin initialization
            Log.Information($"{Class} === LangSwap plugin initialization ===");

            // Load configuration
            config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            // Detect client language
            DetectClientLanguage();

            // Initialize core components
            utilities = new(config, GameGui, Log);
            shortcutDetector = new(config, KeyState, Log);
            excelProvider = new(config, DataManager, Log);
            translationCache = new(excelProvider);
            hookManager = new(AddonLifecycle, config, Framework, GameInterop, ObjectTable, SigScanner, TargetManager, translationCache, utilities, Log);
            configWindow = new(config, this, translationCache, Log);

            // Register window
            windowSystem.AddWindow(configWindow);

            // Register command
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) { HelpMessage = "Opens the LangSwap configuration window" });

            // Enable hooks
            hookManager.EnableAll();

            // Register UI callbacks
            PluginInterface.UiBuilder.Draw += windowSystem.Draw;
            PluginInterface.UiBuilder.Draw += OnDraw;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

            // Log plugin informations
            Log.Information($"{Class} === LangSwap plugin configuration ===");
            Log.Debug($"{Class} - Client Language = {config.ClientLanguage}");
            Log.Debug($"{Class} - Target Language = {config.TargetLanguage}");
            Log.Debug($"{Class} - Auto Startup = {config.AutoStartup}");
            Log.Debug($"{Class} - Shortcut Enabled = {config.ShortcutEnabled}");
            Log.Debug($"{Class} - Primary Key = {config.PrimaryKey}");
            Log.Debug($"{Class} - Ctrl = {config.Ctrl}");
            Log.Debug($"{Class} - Alt = {config.Alt}");
            Log.Debug($"{Class} - Shift = {config.Shift}");
            Log.Debug($"{Class} - Tooltip - Action = {config.ActionTooltip}");
            Log.Debug($"{Class} - Tooltip - Item = {config.ItemTooltip}");
            Log.Debug($"{Class} - Allies CastBars - Target = {config.AlliesCastBarsTarget}");
            Log.Debug($"{Class} - Allies CastBars - Focus = {config.AlliesCastBarsFocus}");
            Log.Debug($"{Class} - Allies CastBars - Party List = {config.AlliesCastBarsPartyList}");
            Log.Debug($"{Class} - Enemies CastBars - Target = {config.EnemiesCastBarsTarget}");
            Log.Debug($"{Class} - Enemies CastBars - Focus = {config.EnemiesCastBarsFocus}");
            Log.Debug($"{Class} - Enemies CastBars - Enmity List = {config.EnemiesCastBarsEnmityList}");
            Log.Information($"{Class} === LangSwap plugin loaded ===");

            // Auto startup swap if enabled
            if (config.AutoStartup) ToggleLanguageSwap();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} === Failed to initialize LangSwap plugin ===");
            Dispose();
        }
    }

    // ----------------------------
    // Swap Logic
    // ----------------------------
    private void OnDraw()
    {
        // Check disposed
        if (disposed) return;

        // Check current shortcut state
        bool shortcutPressed = shortcutDetector.IsPressed();

        // Toggle language swap
        if (shortcutPressed && !previousShortcutPressed) ToggleLanguageSwap();

        // Update previous shortcut state
        previousShortcutPressed = shortcutPressed;
    }

    // ----------------------------
    // Toogle Language Swap
    // ----------------------------
    private void ToggleLanguageSwap()
    {
        // Check disposed or already swapping
        if (disposed || isSwapping) return;

        // Anti-spam cooldown
        if ((DateTime.Now - lastToggle).TotalMilliseconds < ToggleCooldownMs) return;
        lastToggle = DateTime.Now;

        // Register for deferred swap
        deferredFrameCount = 0;
        isSwapping = true;
        Framework.Update += DeferredSwap;
    }

    // ----------------------------
    // Deferred Swap Handler
    // ----------------------------
    private void DeferredSwap(IFramework framework)
    {
        // Check disposed
        if (disposed) return;

        // Wait for deferred frames
        if (++deferredFrameCount < DeferredFrames) return;

        // Unregister immediately
        Framework.Update -= DeferredSwap;

        // Perform the swap or restore
        try
        {
            // Currently swapped -> restore
            if (isSwapEnabled) RestoreLanguage();
            // Currently not swapped -> swap
            else SwapLanguage();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Error while toggling language swap");
        }
        finally
        {
            // Reset swapping flag
            isSwapping = false;
        }
    }

    // ----------------------------
    // Swap Language
    // ----------------------------
    private void SwapLanguage()
    {
        // Check if already swapped
        if (isSwapEnabled) return;

        // Check if target language is different from client language
        if (config.TargetLanguage == config.ClientLanguage)
        {
            string error = "Target language is the same as client language, swap aborted";
            ChatLog(error);
            Log.Warning($"{Class} - {error}");
            return;
        }

        // Update hooks
        hookManager.UpdateHooks();

        // Perform the swap
        hookManager.SwapLanguage();
        isSwapEnabled = true;

        // Log swap informations
        string info = $"Swapped to {Enum.GetName(config.TargetLanguage)}";
        ChatLog(info);
        Log.Information($"{Class} - {info}");
    }

    // ----------------------------
    // Restore Language
    // ----------------------------
    private void RestoreLanguage()
    {
        // Check if already restored
        if (!isSwapEnabled) return;

        // Perform the restore
        hookManager.RestoreLanguage();
        isSwapEnabled = false;

        // Log restore informations
        string info = $"Restored to {Enum.GetName(config.ClientLanguage)}";
        ChatLog(info);
        Log.Information($"{Class} - {info}");
    }

    // ----------------------------
    // Configuration
    // ----------------------------
    private void DetectClientLanguage()
    {
        // Detect client language
        ClientLanguage lang = ClientState.ClientLanguage;

        // Map client language to configuration
        config.ClientLanguage = lang switch
        {
            ClientLanguage.Japanese => LanguageEnum.Japanese,
            ClientLanguage.English => LanguageEnum.English,
            ClientLanguage.German => LanguageEnum.German,
            ClientLanguage.French => LanguageEnum.French,
            _ => LanguageEnum.English
        };

        // Log unrecognized language
        if ((int)lang > 3) Log.Warning($"{Class} - Unrecognized client language: {lang}, defaulting to English");

        // Save configuration
        config.Save();
    }

    // ----------------------------
    // Swap State
    // ----------------------------
    public bool IsSwapEnabled() => isSwapEnabled;

    // ----------------------------
    // Toggle Config UI
    // ----------------------------
    public void ToggleConfigUi() => configWindow.Toggle();

    // ----------------------------
    // Command Handler
    // ----------------------------
    private void OnCommand(string command, string args) => configWindow.Toggle();

    // ----------------------------
    // Chat Logging
    // ----------------------------
    private static void ChatLog(string message)
    {
        // Check for empty message
        if (string.IsNullOrWhiteSpace(message)) return;

        try
        {
            // Build log message
            SeString log = new(new Payload[]
            {
                new UIForegroundPayload(LogTagColor),
                new TextPayload("[LangSwap] "),
                new UIForegroundPayload(0),
                new UIForegroundPayload(MessageColor),
                new TextPayload(message),
                new UIForegroundPayload(0)
            });

            // Print to chat
            ChatGui?.Print(log);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Failed to print chat log");
        }
    }

    // ----------------------------
    // Dispose
    // ----------------------------
    public void Dispose()
    {
        // Set disposed flag
        disposed = true;

        // Cancel any deferred swap
        Framework.Update -= DeferredSwap;

        // Restore language
        if (isSwapEnabled) RestoreLanguage();

        // Remove command
        CommandManager.RemoveHandler(CommandName);

        // Dispose hook manager
        hookManager.Dispose();

        // Unregister UI callbacks
        PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        PluginInterface.UiBuilder.Draw -= OnDraw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

        // Dispose window
        windowSystem.RemoveAllWindows();
        configWindow.Dispose();
        
        // Log plugin informations
        Log.Information($"{Class} === LangSwap plugin unloaded ===");
    }

}