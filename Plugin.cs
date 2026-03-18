using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using LangSwap.input;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.ui;
using LangSwap.Windows;
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
    private readonly Configuration _config = null!;
    private readonly ConfigWindow _configWindow = null!;
    private readonly ExcelProvider _excelProvider = null!;
    private readonly HookManager _hookManager = null!;
    private readonly ShortcutDetector _shortcutDetector = null!;
    private readonly TranslationCache _translationCache = null!;
    private readonly Utilities _utilities = null!;
    private readonly WindowSystem _windowSystem = new("LangSwap");

    // Global constants
    private const string CommandName = "/langswap";
    private const int DeferredFrames = 2;
    private const byte LogTagColor = 45;
    private const byte MessageColor = 57;
    private const int ToggleCooldownMs = 500;

    // Toggle state
    private int _deferredFrameCount = 0;
    private bool _disposed = false;
    private bool _isSwapEnabled = false;
    private bool _isSwapping = false;
    private DateTime _lastToggle = DateTime.MinValue;
    private bool _previousShortcutPressed = false;

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
            _config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            // Detect client language
            DetectClientLanguage();

            // Initialize core components
            _shortcutDetector = new(_config, KeyState, Log);
            _excelProvider = new(_config, DataManager, Log);
            _translationCache = new(_excelProvider);
            _utilities = new(_config, GameGui, Log);
            _hookManager = new(AddonLifecycle, _config, Framework, GameInterop, ObjectTable, SigScanner, TargetManager, _translationCache, _utilities, Log);
            _configWindow = new(_config, _hookManager, this, _translationCache, Log);

            // Register window
            _windowSystem.AddWindow(_configWindow);

            // Register command
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) { HelpMessage = "Opens the LangSwap configuration window" });

            // Enable hooks
            _hookManager.EnableAll();

            // Register UI callbacks
            PluginInterface.UiBuilder.Draw += _windowSystem.Draw;
            PluginInterface.UiBuilder.Draw += OnDraw;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

            // Log plugin informations
            Log.Debug($"{Class} - Configuration :");
            Log.Debug($"{Class} - Client Language = {_config.ClientLanguage}");
            Log.Debug($"{Class} - Target Language = {_config.TargetLanguage}");
            Log.Debug($"{Class} - Primary Key = {_config.PrimaryKey}");
            Log.Debug($"{Class} - Ctrl = {_config.Ctrl}");
            Log.Debug($"{Class} - Alt = {_config.Alt}");
            Log.Debug($"{Class} - Shift = {_config.Shift}");
            Log.Debug($"{Class} - Action Tooltip = {_config.ActionTooltip}");
            Log.Debug($"{Class} - Item Tooltip = {_config.ItemTooltip}");
            Log.Debug($"{Class} - Allies CastBars = {_config.AlliesCastBars}");
            Log.Debug($"{Class} - Enemies CastBars = {_config.EnemiesCastBars}");
            Log.Information($"{Class} === LangSwap plugin loaded ===");
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
        if (_disposed) return;

        // Check current shortcut state
        bool shortcutPressed = _shortcutDetector.IsPressed();

        // Toggle language swap
        if (shortcutPressed && !_previousShortcutPressed) ToggleLanguageSwap();

        // Update previous shortcut state
        _previousShortcutPressed = shortcutPressed;
    }

    // ----------------------------
    // Toogle Language Swap
    // ----------------------------
    private void ToggleLanguageSwap()
    {
        // Check disposed or already swapping
        if (_disposed || _isSwapping) return;

        // Anti-spam cooldown
        if ((DateTime.Now - _lastToggle).TotalMilliseconds < ToggleCooldownMs) return;
        _lastToggle = DateTime.Now;

        // Register for deferred swap
        _deferredFrameCount = 0;
        _isSwapping = true;
        Framework.Update += DeferredSwap;
    }

    // ----------------------------
    // Deferred Swap Handler
    // ----------------------------
    private void DeferredSwap(IFramework framework)
    {
        // Check disposed
        if (_disposed) return;

        // Wait for deferred frames
        if (++_deferredFrameCount < DeferredFrames) return;

        // Unregister immediately
        Framework.Update -= DeferredSwap;

        // Perform the swap or restore
        try
        {
            // Currently swapped -> restore
            if (_isSwapEnabled) RestoreLanguage();
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
            _isSwapping = false;
        }
    }

    // ----------------------------
    // Swap Language
    // ----------------------------
    private void SwapLanguage()
    {
        // Check if already swapped
        if (_isSwapEnabled) return;

        // Check if target language is different from client language
        if (_config.TargetLanguage == _config.ClientLanguage)
        {
            string error = "Target language is the same as client language, swap aborted";
            ChatLog(error);
            Log.Warning($"{Class} - {error}");
            return;
        }

        // Perform the swap
        _hookManager.SwapLanguage();
        _isSwapEnabled = true;

        // Log swap informations
        string info = $"Swapped to {Enum.GetName(typeof(LanguageEnum), _config.TargetLanguage)}";
        ChatLog(info);
        Log.Information($"{Class} - {info}");
    }

    // ----------------------------
    // Restore Language
    // ----------------------------
    private void RestoreLanguage()
    {
        // Check if already restored
        if (!_isSwapEnabled) return;

        // Perform the restore
        _hookManager.RestoreLanguage();
        _isSwapEnabled = false;

        // Log restore informations
        string info = $"Restored to {Enum.GetName(typeof(LanguageEnum), _config.ClientLanguage)}";
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
        _config.ClientLanguage = lang switch
        {
            ClientLanguage.Japanese => 0,
            ClientLanguage.English => 1,
            ClientLanguage.German => 2,
            ClientLanguage.French => 3,
            _ => 1
        };

        // Log unrecognized language
        if ((int)lang > 3) Log.Warning($"{Class} - Unrecognized client language: {lang}, defaulting to English");

        // Save configuration
        _config.Save();
    }

    // ----------------------------
    // Swap State
    // ----------------------------
    public bool IsSwapEnabled() => _isSwapEnabled;

    // ----------------------------
    // Toggle Config UI
    // ----------------------------
    public void ToggleConfigUi() => _configWindow.Toggle();

    // ----------------------------
    // Command Handler
    // ----------------------------
    private void OnCommand(string command, string args) => _configWindow.Toggle();

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
        _disposed = true;

        // Cancel any deferred swap
        Framework.Update -= DeferredSwap;

        // Restore language
        if (_isSwapEnabled) RestoreLanguage();

        // Remove command
        CommandManager.RemoveHandler(CommandName);

        // Dispose hook manager
        _hookManager.Dispose();

        // Unregister UI callbacks
        PluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        PluginInterface.UiBuilder.Draw -= OnDraw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

        // Dispose windows
        _windowSystem.RemoveAllWindows();
        _configWindow.Dispose();

        // Log plugin informations
        Log.Information($"{Class} === LangSwap plugin unloaded ===");
    }

}