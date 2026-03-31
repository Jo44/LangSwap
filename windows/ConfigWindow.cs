using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LangSwap.tool;
using LangSwap.translation;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace LangSwap.Windows;

// ----------------------------
// Configuration Window
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
    private readonly List<string> keyNames;
    private readonly List<int> keyValues;

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

        // Auto-adjust size
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 410),
            MaximumSize = new Vector2(420, float.MaxValue)
        };

        // Initialize key names and values
        keyNames = ["None"];
        keyValues = [-1];
        Utilities.InitKeys(keyNames, keyValues);
    }

    // ----------------------------
    // Config Window Draw
    // ----------------------------
    public override void Draw()
    {
        /// Settings UI

        // Custom Button
        const float buttonWidth = 110f;
        const float buttonRightPadding = 15f;
        float buttonX = ImGui.GetWindowContentRegionMax().X - buttonWidth - buttonRightPadding;

        // Language
        string[] languages = Enum.GetNames<LanguageEnum>();
        int currentLang = (int)config.TargetLanguage;

        // Primary Key
        int selIndex = keyValues.IndexOf(config.PrimaryKey);
        if (selIndex < 0) selIndex = 0;

        // Startup behavior
        bool autoStartup = config.AutoStartup;

        // Shortcut state
        bool shortcutEnabled = config.ShortcutEnabled;

        // Modifiers
        bool ctrl = config.Ctrl;
        bool alt = config.Alt;
        bool shift = config.Shift;

        // UI Components
        bool alliesCastBarsTarget = config.AlliesCastBarsTarget;
        bool alliesCastBarsFocus = config.AlliesCastBarsFocus;
        bool alliesCastBarsPartyList = config.AlliesCastBarsPartyList;
        bool enemiesCastBarsTarget = config.EnemiesCastBarsTarget;
        bool enemiesCastBarsFocus = config.EnemiesCastBarsFocus;
        bool enemiesCastBarsEnmityList = config.EnemiesCastBarsEnmityList;
        bool actionTooltip = config.ActionTooltip;
        bool itemTooltip = config.ItemTooltip;

        /// Draw UI

        // Current state
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Current state : ");
        if (plugin.IsSwapEnabled())
        {
            ImGui.SameLine(0, 55f);
            ImGui.AlignTextToFramePadding();
            ImGui.PushStyleColor(ImGuiCol.Text, config.DarkGreen);
            ImGui.Text("Enabled");
            ImGui.PopStyleColor();
        }
        else
        {
            ImGui.SameLine(0, 55f);
            ImGui.AlignTextToFramePadding();
            ImGui.PushStyleColor(ImGuiCol.Text, config.LightRed);
            ImGui.Text("Disabled");
            ImGui.PopStyleColor();
        }

        // Toggle button
        ImGui.SameLine();
        ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), buttonX));
        ImGui.PushStyleColor(ImGuiCol.Button, config.RedDalamud);
        if (ImGui.Button(plugin.IsSwapEnabled() ? "Disable" : "Enable", new Vector2(buttonWidth, 0f)))
        {
            plugin.ToggleTranslation();
        }
        ImGui.PopStyleColor(1);

        // Target language
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Target Language :");
        ImGui.SameLine(0, 15f);
        ImGui.SetNextItemWidth(120f);
        if (ImGui.Combo("##Language", ref currentLang, languages, languages.Length))
        {
            TargetLanguageChange((LanguageEnum)currentLang, () => config.TargetLanguage = (LanguageEnum)currentLang); 
        }

        // Customize button
        ImGui.SameLine();
        ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), buttonX));
        if (ImGui.Button("Customize", new Vector2(buttonWidth, 0f)))
        {
            plugin.ToggleCustomizeUI();
        }

        // Startup behavior
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Automatically swap at startup##AutoStartup", ref autoStartup))
        {
            CommonSettingChange("AutoStartup", autoStartup, () => config.AutoStartup = autoStartup);
        }

        // Clear translation caches
        ImGui.SameLine(0);
        ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), buttonX));
        if (ImGui.Button("Clear cache", new Vector2(buttonWidth, 0)))
        {
            ImGui.OpenPopup("Confirm clear");
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();

        // Instructions
        DrawInstructions();

        // Toggle shortcut
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Toggle shortcut##ShortcutEnabled", ref shortcutEnabled))
        {
            CommonSettingChange("ShortcutEnabled", shortcutEnabled, () => config.ShortcutEnabled = shortcutEnabled);
        }
        if (shortcutEnabled)
        {
            // Primary key
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.SameLine(0, 25f);
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Key :");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(65f);
            if (ImGui.Combo("##PrimaryKey", ref selIndex, keyNames.ToArray(), keyNames.Count))
            {
                PrimaryKeyChange(keyValues[selIndex], () => config.PrimaryKey = keyValues[selIndex]);
            }

            // Modifier keys
            ImGui.SameLine(0, 24f);
            if (ImGui.Checkbox(" Ctrl", ref ctrl))
            {
                CommonSettingChange("Ctrl", ctrl, () => config.Ctrl = ctrl);
            }
            ImGui.SameLine(0, 13f);
            if (ImGui.Checkbox(" Alt", ref alt))
            {
                CommonSettingChange("Alt", alt, () => config.Alt = alt);
            }
            ImGui.SameLine(0, 13f);
            if (ImGui.Checkbox(" Shift", ref shift))
            {
                CommonSettingChange("Shift", shift, () => config.Shift = shift);
            }
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();

        // UI Components
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGui.Text("Select UI components to translate");
        ImGui.Spacing();
        ImGui.Spacing();

        // Castbars
        ImGui.SameLine(0, 15f);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Castbars :");
        ImGui.Spacing();
        ImGui.Spacing();
        
        // Ally target
        ImGui.SameLine(0, 25f);
        if (ImGui.Checkbox(" Ally target", ref alliesCastBarsTarget))
        {
            UIComponentChange("Allies Target", alliesCastBarsTarget, () => config.AlliesCastBarsTarget = alliesCastBarsTarget);
        }

        // Ally focus
        ImGui.SameLine(0, 34f);
        if (ImGui.Checkbox(" Ally focus", ref alliesCastBarsFocus))
        {
            UIComponentChange("Allies Focus", alliesCastBarsFocus, () => config.AlliesCastBarsFocus = alliesCastBarsFocus);
        }

        // Party list
        ImGui.SameLine(0, 34f);
        if (ImGui.Checkbox(" Party list", ref alliesCastBarsPartyList))
        {
            UIComponentChange("Allies Party List", alliesCastBarsPartyList, () => config.AlliesCastBarsPartyList = alliesCastBarsPartyList);
        }
        ImGui.Spacing();
        ImGui.Spacing();

        // Enemy target
        ImGui.SameLine(0, 25f);
        if (ImGui.Checkbox(" Enemy target", ref enemiesCastBarsTarget))
        {
            UIComponentChange("Enemies Target", enemiesCastBarsTarget, () => config.EnemiesCastBarsTarget = enemiesCastBarsTarget);
        }

        // Enemy focus
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Enemy focus", ref enemiesCastBarsFocus))
        {
            UIComponentChange("Enemies Focus", enemiesCastBarsFocus, () => config.EnemiesCastBarsFocus = enemiesCastBarsFocus);
        }

        // Enmity list
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Enmity list", ref enemiesCastBarsEnmityList))
        {
            UIComponentChange("Enemies Enmity List", enemiesCastBarsEnmityList, () => config.EnemiesCastBarsEnmityList = enemiesCastBarsEnmityList);
        }
        ImGui.Spacing();
        ImGui.Spacing();

        // Tooltips
        ImGui.SameLine(0, 15f);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Tooltips :");
        ImGui.Spacing();
        ImGui.Spacing();

        // Action tooltip
        ImGui.SameLine(0, 25f);
        if (ImGui.Checkbox(" Action", ref actionTooltip))
        {
            UIComponentChange("Action tooltip", actionTooltip, () => config.ActionTooltip = actionTooltip);
        }

        // Item tooltip
        ImGui.SameLine(0, 58f);
        if (ImGui.Checkbox(" Item", ref itemTooltip))
        {
            UIComponentChange("Item tooltip", itemTooltip, () => config.ItemTooltip = itemTooltip);
        }
        ImGui.Spacing();

        // Draw clear translation caches popup
        DrawClearPopup();
    }

    // ----------------------------
    // Draw instructions
    // ----------------------------
    private void DrawInstructions()
    {
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        DrawDebugButton("Press the keyboard shortcut to toogle language swap", "Press the keyboard sh");
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
        // Get positions and sizes
        Vector2 linePos = ImGui.GetCursorScreenPos();
        Vector2 prefixSize = ImGui.CalcTextSize(prefixText);
        Vector2 letterSize = ImGui.CalcTextSize("o");

        // Draw text
        ImGui.TextUnformatted(lineText);

        // Draw invisible button
        ImGui.SetCursorScreenPos(new Vector2(linePos.X + prefixSize.X, linePos.Y));
        if (ImGui.InvisibleButton("##DebugLetter", letterSize))
        {
            plugin.ToggleDebugUI();
        }
    }

    // ----------------------------
    // Draw clear popup
    // ----------------------------
    private void DrawClearPopup()
    {
        // Draw confirmation popup
        PopupHelper.DrawConfirmationPopup("Confirm clear", "This will clear all translations cache.    Are you sure ?", "Yes, clear all", "Cancel", new Vector2(400f, 0f), () =>
        {
            // Clear translation cache
            translationCache.Clear();
            log.Information($"{Class} - All translation cache cleared");
        });
    }

    // ----------------------------
    // Target language change
    // ----------------------------
    private void TargetLanguageChange(LanguageEnum targetLanguage, Action applyChange)
    {
        log.Information($"{Class} - Setting target language to {Enum.GetName(targetLanguage)} ({targetLanguage})");
        // Apply change
        applyChange();
        // Save config
        config.Save();
        // Apply new target language immediately
        plugin.ApplyNewTargetLanguage();
    }

    // ----------------------------
    // Common setting change
    // ----------------------------
    private void CommonSettingChange(string settingName, bool value, Action applyChange)
    {
        log.Information($"{Class} - Setting {settingName} to {value}");
        // Apply change
        applyChange();
        // Save config
        config.Save();
    }
    // ----------------------------
    // Primary key change
    // ----------------------------
    private void PrimaryKeyChange(int value, Action applyChange)
    {
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