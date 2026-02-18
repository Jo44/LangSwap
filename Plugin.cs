using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
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

// ----------------------------
// Plugin Main Class
// ----------------------------
public sealed class Plugin : IDalamudPlugin
{
    // Plugin services
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IKeyState KeyState { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    // Constants
    private const string CommandName = "/langswap";
    private const int DeferredFrames = 2;
    private const byte LogTagColor = 45;
    private const byte MessageColor = 57;
    private const int ToggleCooldownMs = 500;

    // Core components
    private readonly Configuration _config;
    private readonly ConfigWindow _configWindow;
    private readonly ExcelProvider _excelProvider;
    private readonly HookManager _hookManager;
    private readonly ShortcutDetector _shortcutDetector;
    private readonly TranslationCache _translationCache;
    private readonly WindowSystem _windowSystem = new("LangSwap");

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
    public Plugin(IDataManager dataManager, IGameGui gameGui, IGameInteropProvider gameInterop, ISigScanner sigScanner)
    {
        // Load configuration
        _config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // Detect client language
        DetectClientLanguage();

        // Initialize core components
        _shortcutDetector = new ShortcutDetector(_config, KeyState, Log);
        _excelProvider = new(_config, dataManager, Log);
        _translationCache = new(_excelProvider, Log);
        _hookManager = new HookManager(_config, gameGui, gameInterop, sigScanner, _translationCache, Log);
        _configWindow = new ConfigWindow(_config, this, _translationCache, Log);

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
        Log.Information("=== LangSwap plugin loaded ===");
        Log.Debug("Configuration loaded :");
        Log.Debug($"ClientLanguage = {_config.ClientLanguage}");
        Log.Debug($"TargetLanguage = {_config.TargetLanguage}");
        Log.Debug($"PrimaryKey = {_config.PrimaryKey}");
        Log.Debug($"Ctrl = {_config.Ctrl}");
        Log.Debug($"Alt = {_config.Alt}");
        Log.Debug($"Shift = {_config.Shift}");
        Log.Debug($"Castbars = {_config.Castbars}");
        Log.Debug($"ActionDetails = {_config.ActionDetails}");
        Log.Debug($"ItemDetails = {_config.ItemDetails}");
    }

    // ----------------------------
    // Main Draw / Swap Logic
    // ----------------------------
    private void OnDraw()
    {
        // Check disposed
        if (_disposed) return;

        // Check current shortcut state
        bool shortcutPressed = _shortcutDetector.IsPressed();

        // Toggle language swap
        if (shortcutPressed && !_previousShortcutPressed)
        {
            ToggleLanguageSwap();
        }

        // Update previous shortcut state
        _previousShortcutPressed = shortcutPressed;
    }

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
            if (_isSwapEnabled)
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
        catch (Exception ex)
        {
            Log.Error(ex, "Error while toggling language swap");
        }
        finally
        {
            // Reset swapping flag
            _isSwapping = false;
        }
    }

    // ----------------------------
    // Swap / Restore
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
            Log.Warning(error);
            return;
        }

        // Perform the swap
        _hookManager.SwapLanguage();
        _isSwapEnabled = true;

        // Log swap informations
        string info = $"Swapped to {Enum.GetName(typeof(LanguageEnum), _config.TargetLanguage)}";
        ChatLog(info);
        Log.Information(info);
    }

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
        Log.Information(info);
    }

    // ----------------------------
    // Configuration
    // ----------------------------
    private void DetectClientLanguage()
    {
        // Detect client language from Dalamud
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
        if ((int)lang > 3)
            Log.Warning($"Unrecognized client language: {lang}, defaulting to English");

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
        if (string.IsNullOrEmpty(message)) return;

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
            Log.Error(ex, "Failed to print chat log");
        }
    }

    // ----------------------------
    // Dispose
    // ----------------------------
    public void Dispose()
    {
        _disposed = true;

        // Cancel any deferred swap
        Framework.Update -= DeferredSwap;

        // Restore language
        if (_isSwapEnabled)
            RestoreLanguage();

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
        Log.Information("=== LangSwap plugin unloaded ===");
    }
}
