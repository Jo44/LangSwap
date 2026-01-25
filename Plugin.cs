using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LangSwap.Windows;
using Dalamud.Game.ClientState.Keys;
using System;

namespace LangSwap;

// Plugin main
public sealed class Plugin : IDalamudPlugin
{
    // Services
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IKeyState KeyState { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    // References
    private const string CommandName = "/langswap";
    public Configuration Configuration { get; init; }
    public readonly WindowSystem WindowSystem = new("LangSwap");
    private ConfigWindow ConfigWindow { get; init; }
    private byte? originalLanguage = null;
    private bool isLanguageSwapped = false;
    private bool previousComboPressed = false;

    // Constructor
    public Plugin()
    {
        // Load configuration
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // Initialize windows
        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

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
        Log.Debug($"Configuration loaded : TargetLanguage = {Configuration.TargetLanguage}, PrimaryKey = {Configuration.PrimaryKey}, UseCtrl = {Configuration.UseCtrl}, UseAlt = {Configuration.UseAlt}, UseShift = {Configuration.UseShift}");
    }

    // OnDraw callback
    private void OnDraw()
    {
        // Check primary key
        bool primaryOk = Configuration.PrimaryKey < 0 || IsKeyDown(Configuration.PrimaryKey);

        // Check modifier keys
        bool ctrlOk  = !Configuration.UseCtrl  || IsKeyDown((int)VirtualKey.LCONTROL) || IsKeyDown((int)VirtualKey.RCONTROL) || IsKeyDown((int)VirtualKey.CONTROL);
        bool altOk   = !Configuration.UseAlt   || IsKeyDown((int)VirtualKey.LMENU)    || IsKeyDown((int)VirtualKey.RMENU)    || IsKeyDown((int)VirtualKey.MENU);
        bool shiftOk = !Configuration.UseShift || IsKeyDown((int)VirtualKey.LSHIFT)   || IsKeyDown((int)VirtualKey.RSHIFT)   || IsKeyDown((int)VirtualKey.SHIFT);

        // Determine if combo is held
        bool comboHeld = primaryOk && ctrlOk && altOk && shiftOk;

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

    // Get current language
    private byte GetCurrentLanguage()
    {
        // TODO: Implement language retrieval logic
        return 0;
    }

    // Set language
    private void SetLanguage(byte language)
    {
        // TODO: Implement language setting logic
    }

    // Dispose
    public void Dispose()
    {
        // Restore language if swapped
        if (isLanguageSwapped && originalLanguage.HasValue) RestoreLanguage();

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

    // Command handler
    private void OnCommand(string command, string args) => ConfigWindow.Toggle();

    // Toggle config UI
    public void ToggleConfigUi() => ConfigWindow.Toggle();

    // Check key down
    private static bool IsKeyDown(int vkCode)
    {
        if (KeyState is null) return false;
        try
        {
            // Get underlying type of VirtualKey enum
            Type underlying = Enum.GetUnderlyingType(typeof(VirtualKey));
            object converted;
            try
            {
                converted = Convert.ChangeType(vkCode, underlying);
            }
            catch
            {
                var rawFallback = KeyState.GetRawValue(vkCode);
                return rawFallback != 0;
            }

            // Check if converted value is defined in VirtualKey enum
            if (Enum.IsDefined(typeof(VirtualKey), converted))
            {
                var vk = (VirtualKey)Enum.ToObject(typeof(VirtualKey), converted);
                if (KeyState.IsVirtualKeyValid(vk))
                {
                    var raw = KeyState.GetRawValue(vk);
                    return raw != 0;
                }
            }

            // Final fallback to int overload
            var rawInt = KeyState.GetRawValue(vkCode);
            return rawInt != 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // Swap language
    private void SwapLanguage()
    {
        if (isLanguageSwapped) return;
        originalLanguage = GetCurrentLanguage();
        SetLanguage(Configuration.TargetLanguage);
        isLanguageSwapped = true;
        Log.Information($"Language swapped to {Configuration.TargetLanguage}");
    }

    // Restore language
    private void RestoreLanguage()
    {
        if (!isLanguageSwapped || !originalLanguage.HasValue) return;
        SetLanguage(originalLanguage.Value);
        isLanguageSwapped = false;
        originalLanguage = null;
        Log.Information("Language restored to original");
    }

}
