using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LangSwap.Windows;
using Dalamud.Game.ClientState.Keys;

namespace LangSwap;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IKeyState KeyState { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/langswap";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("LangSwap");
    private ConfigWindow ConfigWindow { get; init; }

    private byte? originalLanguage = null;
    private bool isLanguageSwapped = false;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the LangSwap configuration window"
        });

        // Tell the UI system that we want our windows to be drawn through the window system
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.Draw += OnDraw;

        // This adds a button to the plugin installer entry of this plugin which allows
        // toggling the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        Log.Information($"===LangSwap plugin loaded===");
    }

    public void Dispose()
    {
        // Restore original language if swapped
        if (isLanguageSwapped && originalLanguage.HasValue)
        {
            RestoreLanguage();
        }

        // Unregister all actions to not leak anything during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.Draw -= OnDraw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our config ui
        ConfigWindow.Toggle();
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();

    private void OnDraw()
    {

        // Check if Ctrl+Alt is being held
        bool ctrlPressed = KeyState[VirtualKey.CONTROL];
        bool altPressed = KeyState[VirtualKey.MENU]; // ALT key is MENU in VirtualKey enum

        if (ctrlPressed && altPressed)
        {
            if (!isLanguageSwapped)
            {
                SwapLanguage();
            }
        }
        else
        {
            if (isLanguageSwapped)
            {
                RestoreLanguage();
            }
        }
    }

    private void SwapLanguage()
    {
        if (isLanguageSwapped)
            return;

        // Get current language and store it
        // Note: This will need to be implemented based on Dalamud's API for language settings
        // For now, this is a placeholder structure
        originalLanguage = GetCurrentLanguage();
        
        // Set target language
        SetLanguage(Configuration.TargetLanguage);
        
        isLanguageSwapped = true;
        Log.Debug($"Language swapped to {Configuration.TargetLanguage}");
    }

    private void RestoreLanguage()
    {
        if (!isLanguageSwapped || !originalLanguage.HasValue)
            return;

        SetLanguage(originalLanguage.Value);
        isLanguageSwapped = false;
        originalLanguage = null;
        Log.Debug("Language restored to original");
    }

    private byte GetCurrentLanguage()
    {
        // TODO: Implement getting current language from game settings
        // This will require accessing the game's configuration or Dalamud's language API
        return 0; // Placeholder
    }

    private void SetLanguage(byte language)
    {
        // TODO: Implement setting language for HUD and tooltips
        // This will require accessing the game's language settings API
        // May need to use Dalamud's GameConfig or similar service
    }
}
