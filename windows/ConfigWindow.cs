using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.translation.@base;
using LangSwap.windows.@base;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace LangSwap.windows;

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
    private readonly List<string> keyNames = ["None"];
    private readonly List<int> keyValues = [-1];

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
        Utilities.InitKeys(keyNames, keyValues);
    }

    // ----------------------------
    // Draw
    // ----------------------------
    public override void Draw()
    {
        // Button size
        const float buttonWidth = 110f;
        const float buttonRightPadding = 15f;
        float buttonX = ImGui.GetWindowContentRegionMax().X - buttonWidth - buttonRightPadding;

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
        string[] languages = Enum.GetNames<LanguageEnum>();
        int currentLang = (int)config.TargetLanguage;
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
        bool autoStartup = config.AutoStartup;
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
        bool shortcutEnabled = config.ShortcutEnabled;
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Toggle shortcut##ShortcutEnabled", ref shortcutEnabled))
        {
            CommonSettingChange("ShortcutEnabled", shortcutEnabled, () => config.ShortcutEnabled = shortcutEnabled);
        }
        if (shortcutEnabled)
        {
            // Primary key
            int selIndex = keyValues.IndexOf(config.PrimaryKey);
            if (selIndex < 0) selIndex = 0;
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
            bool ctrl = config.Ctrl;
            ImGui.SameLine(0, 24f);
            if (ImGui.Checkbox(" Ctrl", ref ctrl))
            {
                CommonSettingChange("Ctrl", ctrl, () => config.Ctrl = ctrl);
            }
            bool alt = config.Alt;
            ImGui.SameLine(0, 13f);
            if (ImGui.Checkbox(" Alt", ref alt))
            {
                CommonSettingChange("Alt", alt, () => config.Alt = alt);
            }
            bool shift = config.Shift;
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
        bool alliesCastBarsTarget = config.AlliesCastBarsTarget;
        ImGui.SameLine(0, 25f);
        if (ImGui.Checkbox(" Ally target", ref alliesCastBarsTarget))
        {
            UIComponentChange("Allies Target", alliesCastBarsTarget, () => config.AlliesCastBarsTarget = alliesCastBarsTarget);
        }

        // Ally focus
        bool alliesCastBarsFocus = config.AlliesCastBarsFocus;
        ImGui.SameLine(0, 34f);
        if (ImGui.Checkbox(" Ally focus", ref alliesCastBarsFocus))
        {
            UIComponentChange("Allies Focus", alliesCastBarsFocus, () => config.AlliesCastBarsFocus = alliesCastBarsFocus);
        }

        // Party list
        bool alliesCastBarsPartyList = config.AlliesCastBarsPartyList;
        ImGui.SameLine(0, 34f);
        if (ImGui.Checkbox(" Party list", ref alliesCastBarsPartyList))
        {
            UIComponentChange("Allies Party List", alliesCastBarsPartyList, () => config.AlliesCastBarsPartyList = alliesCastBarsPartyList);
        }
        ImGui.Spacing();
        ImGui.Spacing();

        // Enemy target
        bool enemiesCastBarsTarget = config.EnemiesCastBarsTarget;
        ImGui.SameLine(0, 25f);
        if (ImGui.Checkbox(" Enemy target", ref enemiesCastBarsTarget))
        {
            UIComponentChange("Enemies Target", enemiesCastBarsTarget, () => config.EnemiesCastBarsTarget = enemiesCastBarsTarget);
        }

        // Enemy focus
        bool enemiesCastBarsFocus = config.EnemiesCastBarsFocus;
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Enemy focus", ref enemiesCastBarsFocus))
        {
            UIComponentChange("Enemies Focus", enemiesCastBarsFocus, () => config.EnemiesCastBarsFocus = enemiesCastBarsFocus);
        }

        // Enmity list
        bool enemiesCastBarsEnmityList = config.EnemiesCastBarsEnmityList;
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
        bool actionTooltip = config.ActionTooltip;
        ImGui.SameLine(0, 25f);
        if (ImGui.Checkbox(" Action", ref actionTooltip))
        {
            UIComponentChange("Action tooltip", actionTooltip, () => config.ActionTooltip = actionTooltip);
        }

        // Item tooltip
        bool itemTooltip = config.ItemTooltip;
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