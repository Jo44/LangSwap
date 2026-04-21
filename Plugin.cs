using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using LangSwap.hook.manager;
using LangSwap.translation;
using LangSwap.translation.@base;
using LangSwap.translation.model;
using LangSwap.windows;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace LangSwap;

// ----------------------------
// Plugin : LangSwap
//
// @author Jo44
// @version 1.7 (21/04/2026)
// @since 01/01/2026
// ----------------------------
public sealed class Plugin : IDalamudPlugin
{
    // Log
    private static readonly string Class = $"[{nameof(Plugin)}]";

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
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    
    // Core components
    private readonly Configuration config = null!;
    private readonly ExcelProvider excelProvider = null!;
    private readonly HookManager hookManager = null!;
    private readonly TranslationCache translationCache = null!;

    // UI windows
    private readonly ConfigWindow configWindow = null!;
    private readonly CustomizeWindow customizeWindow = null!;
    private readonly AdvancedWindow advancedWindow = null!;
    private readonly WindowSystem windowSystem = new("LangSwap");

    // Toggle states
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
            excelProvider = new(config);
            translationCache = new(config, excelProvider);
            hookManager = new(config, translationCache);

            // Detect client language
            DetectClientLanguage();

            // Log configuration
            Log.Information($"{Class} === LangSwap : Configuration ===");
            Log.Information($"{Class} - Client Language = {config.ClientLanguage}");
            Log.Information($"{Class} - Target Language = {config.TargetLanguage}");
            Log.Information($"{Class} - Auto Swap Language = {config.AutoSwapLanguage}");
            Log.Information($"{Class} - Auto Send Scanned Data = {config.AutoSendScannedData}");
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
            Log.Information($"{Class} - Enemies CastBars - Hate List = {config.EnemiesCastBarsHateList}");
            Log.Information($"{Class} - Tooltip - Action = {config.ActionTooltip}");
            Log.Information($"{Class} - Tooltip - Item = {config.ItemTooltip}");

            // Log obfuscated translations
            Log.Information($"{Class} === LangSwap : Obfuscated Translations ===");

            // Get remote
            GetRemoteObfuscatedTranslations();

            // Log remote / scanned / local
            LogObfuscatedTranslations("Remote", config.RemoteObfuscatedTranslations);
            LogObfuscatedTranslations("Scanned", config.ScannedObfuscatedTranslations);
            LogObfuscatedTranslations("Local", config.LocalObfuscatedTranslations);

            // Log alternative translations
            Log.Information($"{Class} === LangSwap : Alternative Translations ===");
            LogAlternativeTranslations("Alternative", config.AlternativeTranslations);

            // Initialize windows
            Log.Information($"{Class} === LangSwap : Windows ===");
            configWindow = new(config, this, translationCache);
            customizeWindow = new(config);
            advancedWindow = new(config, excelProvider);

            // Register windows
            windowSystem.AddWindow(configWindow);
            windowSystem.AddWindow(customizeWindow);
            windowSystem.AddWindow(advancedWindow);

            // Register UI callbacks
            DalamudPluginInterface.UiBuilder.Draw += windowSystem.Draw;
            DalamudPluginInterface.UiBuilder.Draw += OnDraw;
            DalamudPluginInterface.UiBuilder.OpenConfigUi += ToggleConfigWindow;

            // Register command
            CommandManager.AddHandler("/langswap", new CommandInfo(OnCommand) { HelpMessage = "Opens the configuration window" });

            // Enable hooks
            Log.Information($"{Class} === LangSwap : Hooks ===");
            hookManager.EnableAll();

            // Log loaded
            Log.Information($"{Class} === LangSwap : Loaded ===");

            // Auto swap language if enabled
            if (config.AutoSwapLanguage) ToggleLanguageSwap();
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
        bool shortcutPressed = IsShortcutPressed();

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

        // Set swapping flag
        isSwapping = true;

        // Register for deferred swap
        deferredFrameCount = 0;
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

        // Unregister deferred swap
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
    // Detect client language
    // ----------------------------
    private void DetectClientLanguage()
    {
        // Get client language
        ClientLanguage lang = ClientState.ClientLanguage;

        // Convert to Language enum
        config.ClientLanguage = ClientLangToLang(lang);

        // Save configuration
        config.Save();

        // Log unrecognized language
        if ((int)lang < 0 || (int)lang > 3) Log.Warning($"{Class} - Unrecognized client language: {lang}, defaulting to English");
    }

    // ----------------------------
    // Convert ClientLanguage to Language
    // ----------------------------
    private static Language ClientLangToLang(ClientLanguage lang)
    {
        // Map ClientLanguage to Language
        return lang switch
        {
            ClientLanguage.Japanese => Language.Japanese,
            ClientLanguage.English  => Language.English,
            ClientLanguage.German   => Language.German,
            ClientLanguage.French   => Language.French,
            _                       => Language.English
        };
    }

    // ----------------------------
    // Get remote obfuscated translations
    // ----------------------------
    private void GetRemoteObfuscatedTranslations()
    {
        try
        {
            // Validate URL
            if (string.IsNullOrWhiteSpace(config.RemoteUrl)) return;

            // Download remote CSV
            using HttpClient httpClient = new();
            string csv = httpClient.GetStringAsync(config.RemoteUrl).GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(csv))
            {
                Log.Warning($"{Class} - Remote obfuscated translations CSV is empty");
                return;
            }

            // Import CSV content into a temporary list
            List<ObfuscatedTranslation> importedTranslations = [];
            if (!AdvancedWindow.ImportObfuscatedTranslationsCSV(csv, importedTranslations, out string status))
            {
                Log.Warning($"{Class} - Failed to import remote obfuscated translations CSV: {status}");
                return;
            }

            // Replace current remote translations and persist
            config.RemoteObfuscatedTranslations.Clear();
            config.RemoteObfuscatedTranslations.AddRange(importedTranslations);
            config.Save();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"{Class} - Failed to download remote obfuscated translations CSV");
        }
    }

    // ----------------------------
    // Log obfuscated translations
    // ----------------------------
    private static void LogObfuscatedTranslations(string listName, List<ObfuscatedTranslation> translations)
    {
        // Count translations
        int count = translations?.Count ?? 0;
        Log.Information($"{Class} - {listName} ({count})");

        // Check for empty list
        if (translations == null || translations.Count == 0) return;

        // Log each translation
        foreach (ObfuscatedTranslation translation in translations)
        {
            Log.Debug($"{Class} - Action ID = {translation.ActionID}, Obfuscated = {translation.ObfuscatedName}, Language = {translation.LanguageID}, Spell = {translation.DeobfuscatedName}");
        }
    }

    // ----------------------------
    // Log alternative translations
    // ----------------------------
    private static void LogAlternativeTranslations(string listName, List<AlternativeTranslation> translations)
    {
        // Count translations
        int count = translations?.Count ?? 0;
        Log.Information($"{Class} - {listName} ({count})");

        // Check for empty list
        if (translations == null || translations.Count == 0) return;

        // Log each translation
        foreach (AlternativeTranslation translation in translations)
        {
            Log.Debug($"{Class} - Spell = {translation.SpellName}, Alternative = {translation.AlternativeName}");
        }
    }

    // ----------------------------
    // Check if the shortcut is currently pressed
    // ----------------------------
    private bool IsShortcutPressed()
    {
        // Check for null key state
        if (KeyState is null) return false;

        // Check if shortcut is enabled
        if (!config.ShortcutEnabled) return false;

        // Get primary key state
        bool primary = config.PrimaryKey < 0 || IsKeyDown(config.PrimaryKey);

        // Get modifier keys states
        bool ctrl = !config.Ctrl || IsKeyDown((int)VirtualKey.LCONTROL) || IsKeyDown((int)VirtualKey.RCONTROL) || IsKeyDown((int)VirtualKey.CONTROL);
        bool alt = !config.Alt || IsKeyDown((int)VirtualKey.LMENU) || IsKeyDown((int)VirtualKey.RMENU) || IsKeyDown((int)VirtualKey.MENU);
        bool shift = !config.Shift || IsKeyDown((int)VirtualKey.LSHIFT) || IsKeyDown((int)VirtualKey.RSHIFT) || IsKeyDown((int)VirtualKey.SHIFT);

        // Always return false if no keys are configured
        if (config.PrimaryKey == 0 && !config.Ctrl && !config.Alt && !config.Shift) return false;

        // Final evaluation
        return primary && ctrl && alt && shift;
    }

    // ----------------------------
    // Check if a virtual key is currently down
    // ----------------------------
    private static bool IsKeyDown(int vkCode)
    {
        try
        {
            // Get the underlying type of the VirtualKey enum
            Type underlying = Enum.GetUnderlyingType(typeof(VirtualKey));

            // Convert the integer vkCode to the underlying type
            VirtualKey converted = (VirtualKey)Convert.ChangeType(vkCode, underlying);

            // Check if the converted value is a defined VirtualKey
            if (Enum.IsDefined(converted))
            {
                // Get the VirtualKey
                VirtualKey vk = (VirtualKey)Enum.ToObject(typeof(VirtualKey), converted);

                // Return true if the key is down
                if (KeyState.IsVirtualKeyValid(vk)) return KeyState.GetRawValue(vk) != 0;
            }

            // Return false
            return false;
        }
        catch (Exception ex)
        {
            Log.Warning($"{Class} - IsKeyDown exception for vk = {vkCode} : {ex.Message}");
            return false;
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
    // Command handler
    // ----------------------------
    private void OnCommand(string command, string args) => configWindow.Toggle();

    // ----------------------------
    // Swap state
    // ----------------------------
    public bool IsSwapEnabled() => isSwapEnabled;

    // ----------------------------
    // Toggle translation
    // ----------------------------
    public void ToggleTranslation() => ToggleLanguageSwap();

    // ----------------------------
    // Toggle config window
    // ----------------------------
    public void ToggleConfigWindow() => configWindow.Toggle();

    // ----------------------------
    // Toggle customize window
    // ----------------------------
    public void ToggleCustomizeWindow() => customizeWindow.Toggle();

    // ----------------------------
    // Toggle advanced window
    // ----------------------------
    public void ToggleAdvancedWindow() => advancedWindow.Toggle();

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
        DalamudPluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigWindow;

        // Dispose windows
        advancedWindow.Dispose();
        customizeWindow.Dispose();
        configWindow.Dispose();
        windowSystem.RemoveAllWindows();

        // Log unloaded
        Log.Information($"{Class} === LangSwap : Unloaded ===");
    }

}