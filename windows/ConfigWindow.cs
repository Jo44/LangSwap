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
            MinimumSize = new Vector2(420, 420),
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
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.00f, 0.80f, 0.13f, 1.00f));
            ImGui.Text("Enabled");
            ImGui.PopStyleColor();
        }
        else
        {
            ImGui.SameLine(0, 55f);
            ImGui.AlignTextToFramePadding();
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.57f, 0.13f, 0.13f, 1.00f));
            ImGui.Text("Disabled");
            ImGui.PopStyleColor();
        }

        // Toggle button
        ImGui.SameLine();
        ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), buttonX));
        if (ImGui.Button(plugin.IsSwapEnabled() ? "Disable" : "Enable", new Vector2(buttonWidth, 0f)))
        {
            plugin.ToggleTranslation();
        }

        // Target language
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Target Language :");
        ImGui.SameLine(0, 10f);
        ImGui.SetNextItemWidth(120f);
        if (ImGui.Combo("##Language", ref currentLang, languages, languages.Length))
        {
            log.Information($"{Class} - Setting target language to {Enum.GetName(typeof(LanguageEnum), currentLang)} ({currentLang})");
            config.TargetLanguage = (LanguageEnum)currentLang;
            config.Save();
            plugin.ApplyNewTargetLanguage();
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
            log.Information($"{Class} - Setting AutoStartup to {autoStartup}");
            config.AutoStartup = autoStartup;
            config.Save();
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();

        // Instructions
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGui.TextWrapped("Press the keyboard shortcut to toogle language swap\nPress again to restore original language");
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        // Toggle shortcut
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Toggle shortcut##ShortcutEnabled", ref shortcutEnabled))
        {
            log.Information($"{Class} - Setting ShortcutEnabled to {shortcutEnabled}");
            config.ShortcutEnabled = shortcutEnabled;
            config.Save();
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
                log.Information($"{Class} - Setting primary key to {keyNames[selIndex]} ({keyValues[selIndex]})");
                config.PrimaryKey = keyValues[selIndex];
                config.Save();
            }

            // Modifier keys
            ImGui.SameLine(0, 24f);
            if (ImGui.Checkbox(" Ctrl", ref ctrl))
            {
                log.Information($"{Class} - Setting Ctrl to {ctrl}");
                config.Ctrl = ctrl;
                config.Save();
            }
            ImGui.SameLine(0, 13f);
            if (ImGui.Checkbox(" Alt", ref alt))
            {
                log.Information($"{Class} - Setting Alt to {alt}");
                config.Alt = alt;
                config.Save();
            }
            ImGui.SameLine(0, 13f);
            if (ImGui.Checkbox(" Shift", ref shift))
            {
                log.Information($"{Class} - Setting Shift to {shift}");
                config.Shift = shift;
                config.Save();
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
        ImGui.Spacing();
        ImGui.Separator();

        // Clear translation caches
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        if (ImGui.Button("Clear translation caches", new Vector2(200f, 0)))
        {
            ImGui.OpenPopup("Confirm clear");
        }
        ImGui.Spacing();

        // Draw clear translation caches popup
        DrawClearTranslationCachesPopup();
    }

    // ----------------------------
    // UI component change
    // ----------------------------
    private void UIComponentChange(string settingName, bool value, Action applyChange)
    {
        log.Information($"{Class} - Setting {settingName} to {value}");
        applyChange();
        config.Save();
        plugin.ApplyNewUIComponents();
    }

    // ----------------------------
    // Draw clear translation caches popup
    // ----------------------------
    private void DrawClearTranslationCachesPopup()
    {
        // Popup for clear confirmation
        if (!ImGui.BeginPopupModal("Confirm clear", ImGuiWindowFlags.AlwaysAutoResize)) return;

        // Message
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 30f);
        ImGui.TextWrapped("This will clear all translation caches.   Are you sure ?");
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        // Clear button
        if (ImGui.Button("Yes, clear all", new Vector2(200f, 0f)))
        {
            // Clear translation cache
            translationCache.Clear();
            log.Information($"{Class} - All translation caches cleared");

            // Close popup
            ImGui.CloseCurrentPopup();
        }

        // Cancel button
        ImGui.SameLine(0, 10f);
        if (ImGui.Button("Cancel", new Vector2(200f, 0f)))
        {
            // Close popup
            ImGui.CloseCurrentPopup();
        }

        // End popup
        ImGui.EndPopup();
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