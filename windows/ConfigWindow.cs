using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.translation.@base;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace LangSwap.windows;

// ----------------------------
// Config Window
// ----------------------------
public class ConfigWindow : Window, IDisposable
{
    // Log
    private const string Class = "[ConfigWindow.cs]";

    // Core components
    private readonly Configuration config;
    private readonly Plugin plugin;
    private readonly TranslationCache translationCache;
    private readonly IPluginLog log;

    // Primary key options
    private readonly List<KeyValuePair<string, int>> keys = [new("None", -1)];

    // ----------------------------
    // Constructor
    // ----------------------------
    public ConfigWindow(Configuration config, Plugin plugin, TranslationCache translationCache, IPluginLog log) : base("LangSwap - Configuration###LangSwapConfig")
    {
        // Initialize core components
        this.config = config;
        this.plugin = plugin;
        this.translationCache = translationCache;
        this.log = log;

        // Window settings
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 410),
            MaximumSize = new Vector2(420, float.MaxValue)
        };

        // Initialize primary keys
        Utilities.InitPrimaryKeys(keys);
    }

    // ----------------------------
    // Draw
    // ----------------------------
    public override void Draw()
    {
        // Buttons size
        const float buttonWidth = 110f;
        const float buttonRightPadding = 15f;
        float buttonX = ImGui.GetWindowContentRegionMax().X - buttonWidth - buttonRightPadding;

        // Current state
        DrawCurrentState();

        // Toggle button
        DrawToggleButton(buttonX, buttonWidth);

        // Target language
        DrawTargetLanguage();

        // Customize button
        DrawCustomizeButton(buttonX, buttonWidth);

        // Auto Startup
        DrawAutoStartup();

        // Clear cache
        DrawClearCacheButton(buttonX, buttonWidth);
        DrawClearCachePopup();

        // Information
        DrawInformation();

        // Toggle shortcut
        bool shortcutEnabled = config.ShortcutEnabled;
        DrawToggleShortcut(shortcutEnabled);

        if (shortcutEnabled)
        {
            // Primary key
            DrawPrimaryKey();

            // Modifier keys
            DrawModifierKeys();
        }

        // UI Components
        DrawUIComponents();

        // Castbars
        DrawCastbars();

        // Tooltips
        DrawTooltips();
    }

    // ----------------------------
    // Draw current state
    // ----------------------------
    private void DrawCurrentState()
    {
        // Draw current state
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Current state : ");
        if (plugin.IsSwapEnabled())
        {
            // Green "Enabled"
            ImGui.SameLine(0, 55f);
            ImGui.AlignTextToFramePadding();
            ImGui.PushStyleColor(ImGuiCol.Text, config.DarkGreen);
            ImGui.Text("Enabled");
            ImGui.PopStyleColor();
        }
        else
        {
            // Red "Disabled"
            ImGui.SameLine(0, 55f);
            ImGui.AlignTextToFramePadding();
            ImGui.PushStyleColor(ImGuiCol.Text, config.LightRed);
            ImGui.Text("Disabled");
            ImGui.PopStyleColor();
        }
    }

    // ----------------------------
    // Draw toggle button
    // ----------------------------
    private void DrawToggleButton(float buttonX, float buttonWidth)
    {
        // Draw toggle button
        ImGui.SameLine();
        ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), buttonX));
        ImGui.PushStyleColor(ImGuiCol.Button, config.RedDalamud);
        if (ImGui.Button(plugin.IsSwapEnabled() ? "Disable" : "Enable", new Vector2(buttonWidth, 0f)))
        {
            // Toggle translation
            plugin.ToggleTranslation();
        }
        ImGui.PopStyleColor(1);
    }

    // ----------------------------
    // Draw target language
    // ----------------------------
    private void DrawTargetLanguage()
    {
        // Get language names and current language
        string[] languages = Enum.GetNames<Language>();
        int currentLang = (int)config.TargetLanguage;

        // Draw target language
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Target Language :");
        ImGui.SameLine(0, 15f);
        ImGui.SetNextItemWidth(120f);
        if (ImGui.Combo("##Language", ref currentLang, languages, languages.Length))
        {
            // Change target language
            TargetLanguageChange((Language)currentLang, () => config.TargetLanguage = (Language)currentLang);
        }
    }

    // ----------------------------
    // Draw customize button
    // ----------------------------
    private void DrawCustomizeButton(float buttonX, float buttonWidth)
    {
        // Draw customize button
        ImGui.SameLine();
        ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), buttonX));
        if (ImGui.Button("Customize", new Vector2(buttonWidth, 0f)))
        {
            // Open customize window
            plugin.ToggleCustomizeUI();
        }
    }

    // ----------------------------
    // Draw auto startup
    // ----------------------------
    private void DrawAutoStartup()
    {
        // Draw auto startup
        bool autoStartup = config.AutoStartup;
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Automatically swap at startup##AutoStartup", ref autoStartup))
        {
            // Change auto startup
            CommonSettingChange("AutoStartup", autoStartup, () => config.AutoStartup = autoStartup);
        }
    }

    // ----------------------------
    // Draw clear cache button
    // ----------------------------
    private static void DrawClearCacheButton(float buttonX, float buttonWidth)
    {
        // Draw clear cache button
        ImGui.SameLine(0);
        ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), buttonX));
        if (ImGui.Button("Clear cache", new Vector2(buttonWidth, 0)))
        {
            // Open popup
            ImGui.OpenPopup("Confirm clear");
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();
    }

    // ----------------------------
    // Draw clear cache popup
    // ----------------------------
    private void DrawClearCachePopup()
    {
        // Draw confirmation popup
        PopupBuilder.DrawConfirmationPopup("Confirm clear", "This will clear all translations cache.    Are you sure ?", "Yes, clear all", "Cancel", new Vector2(400f, 0f), () =>
        {
            // Clear translation cache
            translationCache.Clear();
            log.Information($"{Class} - All translation cache cleared");
        });
    }

    // ----------------------------
    // Draw information
    // ----------------------------
    private void DrawInformation()
    {
        // Draw information
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);

        // Draw debug button
        DrawDebugButton("Press the keyboard shortcut to toogle language swap", "Press the keyboard sh");

        // Draw information text
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGui.TextUnformatted("Press again to restore original language");
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
    }

    // ----------------------------
    // Draw debug button (invisible button over 'o' in "shortcut")
    // ----------------------------
    private void DrawDebugButton(string lineText, string prefixText)
    {
        // Get position and sizes
        Vector2 linePos = ImGui.GetCursorScreenPos();
        Vector2 prefixSize = ImGui.CalcTextSize(prefixText);
        Vector2 letterSize = ImGui.CalcTextSize("o");

        // Draw text
        ImGui.TextUnformatted(lineText);

        // Draw invisible button
        ImGui.SetCursorScreenPos(new Vector2(linePos.X + prefixSize.X, linePos.Y));
        if (ImGui.InvisibleButton("##DebugLetter", letterSize))
        {
            // Open debug window
            plugin.ToggleDebugUI();
        }
    }

    // ----------------------------
    // Draw toggle shortcut
    // ----------------------------
    private void DrawToggleShortcut(bool shortcutEnabled)
    {
        // Draw toggle shortcut
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Toggle shortcut##ShortcutEnabled", ref shortcutEnabled))
        {
            // Change toggle shortcut
            CommonSettingChange("ShortcutEnabled", shortcutEnabled, () => config.ShortcutEnabled = shortcutEnabled);
        }
    }

    // ----------------------------
    // Draw primary key
    // ----------------------------
    private void DrawPrimaryKey()
    {
        // Get selected index
        int selIndex = keys.FindIndex(key => key.Value == config.PrimaryKey);
        if (selIndex < 0) selIndex = 0;

        // Draw primary key
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 25f);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Key :");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(65f);

        // Get key names
        string[] keyNames = [.. keys.ConvertAll(k => k.Key)];
        if (ImGui.Combo("##PrimaryKey", ref selIndex, keyNames, keyNames.Length))
        {
            // Change primary key
            PrimaryKeyChange(keys[selIndex].Value, () => config.PrimaryKey = keys[selIndex].Value);
        }
    }

    // ----------------------------
    // Draw modifier keys
    // ----------------------------
    private void DrawModifierKeys()
    {
        // Draw ctrl key
        bool ctrl = config.Ctrl;
        ImGui.SameLine(0, 24f);
        if (ImGui.Checkbox(" Ctrl", ref ctrl))
        {
            CommonSettingChange("Ctrl", ctrl, () => config.Ctrl = ctrl);
        }

        // Draw alt key
        bool alt = config.Alt;
        ImGui.SameLine(0, 13f);
        if (ImGui.Checkbox(" Alt", ref alt))
        {
            CommonSettingChange("Alt", alt, () => config.Alt = alt);
        }

        // Draw shift key
        bool shift = config.Shift;
        ImGui.SameLine(0, 13f);
        if (ImGui.Checkbox(" Shift", ref shift))
        {
            CommonSettingChange("Shift", shift, () => config.Shift = shift);
        }
    }

    // ----------------------------
    // Draw UI components
    // ----------------------------
    private static void DrawUIComponents()
    {
        // Draw UI components
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGui.Text("Select UI components to translate");
        ImGui.Spacing();
        ImGui.Spacing();
    }

    // ----------------------------
    // Draw castbars
    // ----------------------------
    private void DrawCastbars()
    {
        // Draw castbars
        ImGui.SameLine(0, 15f);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Castbars :");
        ImGui.Spacing();
        ImGui.Spacing();

        // Ally target
        bool alliesCastBarsTarget = config.AlliesCastBarsTarget;
        ImGui.SameLine(0, 25f);
        if (ImGui.Checkbox(" Ally target", ref alliesCastBarsTarget))
        {
            // Change ally target castbar
            UIComponentChange("Allies Target", alliesCastBarsTarget, () => config.AlliesCastBarsTarget = alliesCastBarsTarget);
        }

        // Ally focus
        bool alliesCastBarsFocus = config.AlliesCastBarsFocus;
        ImGui.SameLine(0, 34f);
        if (ImGui.Checkbox(" Ally focus", ref alliesCastBarsFocus))
        {
            // Change ally focus castbar
            UIComponentChange("Allies Focus", alliesCastBarsFocus, () => config.AlliesCastBarsFocus = alliesCastBarsFocus);
        }

        // Party list
        bool alliesCastBarsPartyList = config.AlliesCastBarsPartyList;
        ImGui.SameLine(0, 34f);
        if (ImGui.Checkbox(" Party list", ref alliesCastBarsPartyList))
        {
            // Change party list castbars
            UIComponentChange("Allies Party List", alliesCastBarsPartyList, () => config.AlliesCastBarsPartyList = alliesCastBarsPartyList);
        }
        ImGui.Spacing();
        ImGui.Spacing();

        // Enemy target
        bool enemiesCastBarsTarget = config.EnemiesCastBarsTarget;
        ImGui.SameLine(0, 25f);
        if (ImGui.Checkbox(" Enemy target", ref enemiesCastBarsTarget))
        {
            // Change enemy target castbar
            UIComponentChange("Enemies Target", enemiesCastBarsTarget, () => config.EnemiesCastBarsTarget = enemiesCastBarsTarget);
        }

        // Enemy focus
        bool enemiesCastBarsFocus = config.EnemiesCastBarsFocus;
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Enemy focus", ref enemiesCastBarsFocus))
        {
            // Change enemy focus castbar
            UIComponentChange("Enemies Focus", enemiesCastBarsFocus, () => config.EnemiesCastBarsFocus = enemiesCastBarsFocus);
        }

        // Enmity list
        bool enemiesCastBarsEnmityList = config.EnemiesCastBarsEnmityList;
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Enmity list", ref enemiesCastBarsEnmityList))
        {
            // Change enmity list castbars
            UIComponentChange("Enemies Enmity List", enemiesCastBarsEnmityList, () => config.EnemiesCastBarsEnmityList = enemiesCastBarsEnmityList);
        }
        ImGui.Spacing();
        ImGui.Spacing();
    }

    // ----------------------------
    // Draw tooltips
    // ----------------------------
    private void DrawTooltips()
    {
        // Draw tooltips
        ImGui.SameLine(0, 15f);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Tooltips :");
        ImGui.Spacing();
        ImGui.Spacing();

        // Action tooltip
        bool actionTooltip = config.ActionTooltip;
        ImGui.SameLine(0, 25f);
        if (ImGui.Checkbox(" Action", ref actionTooltip))
        {
            // Change action tooltip
            UIComponentChange("Action tooltip", actionTooltip, () => config.ActionTooltip = actionTooltip);
        }

        // Item tooltip
        bool itemTooltip = config.ItemTooltip;
        ImGui.SameLine(0, 58f);
        if (ImGui.Checkbox(" Item", ref itemTooltip))
        {
            // Change item tooltip
            UIComponentChange("Item tooltip", itemTooltip, () => config.ItemTooltip = itemTooltip);
        }
        ImGui.Spacing();
    }

    // ----------------------------
    // Common setting change
    // ----------------------------
    private void CommonSettingChange(string settingName, bool value, Action applyChange)
    {
        // Log
        log.Information($"{Class} - Setting {settingName} to {value}");

        // Apply change
        applyChange();

        // Save config
        config.Save();
    }

    // ----------------------------
    // Target language change
    // ----------------------------
    private void TargetLanguageChange(Language targetLanguage, Action applyChange)
    {
        // Log
        log.Information($"{Class} - Setting target language to {Enum.GetName(targetLanguage)} ({targetLanguage})");

        // Apply change
        applyChange();

        // Save config
        config.Save();

        // Apply new target language immediately
        plugin.ApplyNewTargetLanguage();
    }

    // ----------------------------
    // Primary key change
    // ----------------------------
    private void PrimaryKeyChange(int value, Action applyChange)
    {
        // Log
        log.Information($"{Class} - Setting PrimaryKey to {value}");

        // Apply change
        applyChange();

        // Save config
        config.Save();
    }

    // ----------------------------
    // UI component change
    // ----------------------------
    private void UIComponentChange(string settingName, bool value, Action applyChange)
    {
        // Log
        log.Information($"{Class} - Setting {settingName} to {value}");

        // Apply change
        applyChange();

        // Save config
        config.Save();

        // Apply new UI components immediately
        plugin.ApplyNewUIComponents();
    }

    // ----------------------------
    // Dispose
    // ----------------------------
    public void Dispose()
    {
        // Finalize
        GC.SuppressFinalize(this);
    }

}