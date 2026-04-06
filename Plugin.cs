using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using LangSwap.hook;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.windows;
using System;

namespace LangSwap;

// ----------------------------
// Plugin : LangSwap
// ----------------------------
public sealed class Plugin : IDalamudPlugin
{
    // Log
    private const string Class = "[Plugin.cs]";

    // Services
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IDalamudPluginInterface DalamudPluginInterface { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInterop { get; private set; } = null!;
    [PluginService] internal static IKeyState KeyState { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;

    // Core components
    private readonly Configuration config = null!;
    private readonly ExcelProvider excelProvider = null!;
    private readonly HookManager hookManager = null!;
    private readonly TranslationCache translationCache = null!;
    private readonly Utilities utilities = null!;

    // UI windows
    private readonly ConfigWindow configWindow = null!;
    private readonly CustomizeWindow customizeWindow = null!;
    private readonly DebugWindow debugWindow = null!;
    private readonly WindowSystem windowSystem = new("LangSwap");

    // Toggle state
    private int deferredFrameCount = 0;
    private bool disposed = false;
    private bool isSwapping = false;
    private bool isSwapEnabled = false;
    private bool previousShortcutPressed = false;
    private DateTime lastToggle = DateTime.MinValue;

    // ----------------------------
    // Initialization
    // ----------------------------
    public Plugin()
    {
        try
        {
            // Log initialization
            Log.Information($"{Class} === LangSwap : Initialization ===");

            // Load configuration
            config = DalamudPluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            // Initialize core components
            utilities = new(ClientState, config, GameGui, KeyState, Log);
            excelProvider = new(config, DataManager, Log);
            translationCache = new(excelProvider);
            hookManager = new(AddonLifecycle, config, Framework, GameInterop, ObjectTable, SigScanner, TargetManager, translationCache, utilities, Log);

            // Detect client language
            utilities.DetectClientLanguage();

            // Log configuration
            Log.Information($"{Class} === LangSwap : Configuration ===");
            Log.Information($"{Class} - Client Language = {config.ClientLanguage}");
            Log.Information($"{Class} - Target Language = {config.TargetLanguage}");
            Log.Information($"{Class} - Auto Startup = {config.AutoStartup}");
            Log.Information($"{Class} - Shortcut Enabled = {config.ShortcutEnabled}");
            Log.Information($"{Class} - Primary Key = {config.PrimaryKey}");
            Log.Information($"{Class} - Ctrl = {config.Ctrl}");
            Log.Information($"{Class} - Alt = {config.Alt}");
            Log.Information($"{Class} - Shift = {config.Shift}");
            Log.Information($"{Class} - Allies CastBars - Target = {config.AlliesCastBarsTarget}");
            Log.Information($"{Class} - Allies CastBars - Focus = {config.AlliesCastBarsFocus}");
            Log.Information($"{Class} - Allies CastBars - Party List = {config.AlliesCastBarsPartyList}");
            Log.Information($"{Class} - Enemies CastBars - Target = {config.EnemiesCastBarsTarget}");
            Log.Information($"{Class} - Enemies CastBars - Focus = {config.EnemiesCastBarsFocus}");
            Log.Information($"{Class} - Enemies CastBars - Enmity List = {config.EnemiesCastBarsEnmityList}");
            Log.Information($"{Class} - Tooltip - Action = {config.ActionTooltip}");
            Log.Information($"{Class} - Tooltip - Item = {config.ItemTooltip}");

            // Log obfuscated translations
            Log.Information($"{Class} === LangSwap : Obfuscated Translations ===");

            // Get remote
            utilities.GetRemoteObfuscatedTranslations();

            // Log remote / scanned / local
            utilities.LogObfuscatedTranslations("Remote", config.RemoteObfuscatedTranslations);
            utilities.LogObfuscatedTranslations("Scanned", config.ScannedObfuscatedTranslations);
            utilities.LogObfuscatedTranslations("Local", config.LocalObfuscatedTranslations);

            // Log alternative translations
            Log.Information($"{Class} === LangSwap : Alternative Translations ===");
            utilities.LogAlternativeTranslations("Alternative", config.AlternativeTranslations);

            // Initialize windows
            Log.Information($"{Class} === LangSwap : Windows ===");
            configWindow = new(config, this, translationCache, Log);
            customizeWindow = new(config, Log);
            debugWindow = new(config, excelProvider, Log);

            // Register windows
            windowSystem.AddWindow(configWindow);
            windowSystem.AddWindow(customizeWindow);
            windowSystem.AddWindow(debugWindow);

            // Register UI callbacks
            DalamudPluginInterface.UiBuilder.Draw += windowSystem.Draw;
            DalamudPluginInterface.UiBuilder.Draw += OnDraw;
            DalamudPluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

            // Register command
            CommandManager.AddHandler("/langswap", new CommandInfo(OnCommand) { HelpMessage = "Opens the LangSwap configuration window" });

            // Enable hooks
            Log.Information($"{Class} === LangSwap : Hooks ===");
            hookManager.EnableAll();

            // Log loaded
            Log.Information($"{Class} === LangSwap : Loaded ===");

            // Auto startup swap if enabled
            if (config.AutoStartup) ToggleLanguageSwap();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} === Failed to initialize LangSwap ===");
            Dispose();
        }
    }

    // ----------------------------
    // On draw
    // ----------------------------
    private void OnDraw()
    {
        // Check disposed
        if (disposed) return;

        // Check current shortcut state
        bool shortcutPressed = utilities.IsShortcutPressed();

        // Toggle language swap
        if (shortcutPressed && !previousShortcutPressed) ToggleLanguageSwap();

        // Update previous shortcut state
        previousShortcutPressed = shortcutPressed;
    }

    // ----------------------------
    // Toogle language swap
    // ----------------------------
    private void ToggleLanguageSwap()
    {
        // Check disposed or already swapping
        if (disposed || isSwapping) return;

        // Anti-spam cooldown
        if ((DateTime.Now - lastToggle).TotalMilliseconds < 500) return;
        lastToggle = DateTime.Now;

        // Register for deferred swap
        deferredFrameCount = 0;
        isSwapping = true;
        Framework.Update += DeferredSwap;
    }

    // ----------------------------
    // Deferred swap
    // ----------------------------
    private void DeferredSwap(IFramework framework)
    {
        // Check disposed
        if (disposed) return;

        // Wait for deferred frames
        if (++deferredFrameCount < 2) return;

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
    // Swap language
    // ----------------------------
    private void SwapLanguage()
    {
        // Check if already swapped
        if (isSwapEnabled) return;

        // Update hooks
        hookManager.UpdateHooks();

        // Perform the swap
        hookManager.SwapLanguage();
        isSwapEnabled = true;

        // Log swap information
        string info = $"Swapped to {Enum.GetName(config.TargetLanguage)}";
        Log.Information($"{Class} - {info}");

        // Notify swap
        ChatLog(info);
    }

    // ----------------------------
    // Restore language
    // ----------------------------
    private void RestoreLanguage()
    {
        // Check if already restored
        if (!isSwapEnabled) return;

        // Perform the restore
        hookManager.RestoreLanguage();
        isSwapEnabled = false;

        // Log restore information
        string info = $"Restored to {Enum.GetName(config.ClientLanguage)}";
        Log.Information($"{Class} - {info}");

        // Notify restore
        ChatLog(info);
    }

    // ----------------------------
    // Chat logging
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
                new UIForegroundPayload(45),
                new TextPayload("[LangSwap] "),
                new UIForegroundPayload(0),
                new UIForegroundPayload(57),
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
    // Apply new target language
    // ----------------------------
    public void ApplyNewTargetLanguage()
    {
        try
        {
            // Notify swap modification
            if (isSwapEnabled) ChatLog($"Swapped to {Enum.GetName(config.TargetLanguage)}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Failed to apply new target language");
        }
    }

    // ----------------------------
    // Apply new UI component
    // ----------------------------
    public void ApplyNewUIComponents()
    {
        try
        {
            // Get current swap state
            bool wasSwapEnabled = isSwapEnabled;

            // Temporarily restore language if enabled
            if (wasSwapEnabled)
            {
                hookManager.RestoreLanguage();
                isSwapEnabled = false;
            }

            // Update hooks with new configuration
            hookManager.UpdateHooks();

            // Re-apply swap if it was previously enabled
            if (wasSwapEnabled)
            {
                hookManager.SwapLanguage();
                isSwapEnabled = true;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Failed to apply new UI component");
        }
    }

    // ----------------------------
    // Swap state
    // ----------------------------
    public bool IsSwapEnabled() => isSwapEnabled;

    // ----------------------------
    // Toggle translation
    // ----------------------------
    public void ToggleTranslation() => ToggleLanguageSwap();

    // ----------------------------
    // Command handler
    // ----------------------------
    private void OnCommand(string command, string args) => configWindow.Toggle();

    // ----------------------------
    // Toggle config UI
    // ----------------------------
    public void ToggleConfigUI() => configWindow.Toggle();

    // ----------------------------
    // Toggle customize UI
    // ----------------------------
    public void ToggleCustomizeUI() => customizeWindow.Toggle();

    // ----------------------------
    // Toggle debug UI
    // ----------------------------
    public void ToggleDebugUI() => debugWindow.Toggle();

    // ----------------------------
    // Dispose
    // ----------------------------
    public void Dispose()
    {
        // Log unloading
        Log.Information($"{Class} === LangSwap : Unloading ===");

        // Set disposed flag
        disposed = true;

        // Cancel any deferred swap
        Framework.Update -= DeferredSwap;

        // Restore language if swapped
        if (isSwapEnabled) RestoreLanguage();

        // Dispose hook manager
        hookManager.Dispose();

        // Remove command
        CommandManager.RemoveHandler("/langswap");

        // Unregister UI callbacks
        DalamudPluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        DalamudPluginInterface.UiBuilder.Draw -= OnDraw;
        DalamudPluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;

        // Dispose windows
        debugWindow.Dispose();
        customizeWindow.Dispose();
        configWindow.Dispose();
        windowSystem.RemoveAllWindows();

        // Log unloaded
        Log.Information($"{Class} === LangSwap : Unloaded ===");
    }

}