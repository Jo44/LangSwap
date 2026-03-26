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
            MinimumSize = new Vector2(420, 400),
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

        // Target language
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGui.Text("Target Language :");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        if (ImGui.Combo("##Language", ref currentLang, languages, languages.Length))
        {
            log.Information($"{Class} - Setting target language to {languages[currentLang]} ({currentLang})");
            config.TargetLanguage = (LanguageEnum)currentLang;
            config.Save();
        }

        // Startup behavior
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Automatically swap at startup", ref autoStartup))
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

        // Toggle shortcut
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox("Toggle shortcut##ShortcutEnabled", ref shortcutEnabled))
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
            ImGui.Text("Key :");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(55f);
            if (ImGui.Combo("##PrimaryKey", ref selIndex, keyNames.ToArray(), keyNames.Count))
            {
                log.Information($"{Class} - Setting primary key to {keyNames[selIndex]} ({keyValues[selIndex]})");
                config.PrimaryKey = keyValues[selIndex];
                config.Save();
            }

            // Modifier keys
            ImGui.SameLine(0, 34f);
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
        // Warning if translation is active
        if (plugin.IsSwapEnabled())
        {
            ImGui.SameLine(0, 15f);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.5f, 0.0f, 1.0f));
            ImGui.TextWrapped("WARNING : Disable translation before changing settings !");
            ImGui.PopStyleColor();
            ImGui.Spacing();
        }
        ImGui.SameLine(0, 15f);
        ImGui.Text("Select the UI components to translate");
        ImGui.Spacing();
        ImGui.Spacing();

        // Casts
        ImGui.SameLine(0, 15f);
        ImGui.Text("Casts :");
        ImGui.Spacing();
        ImGui.Spacing();
        
        // Ally target
        ImGui.SameLine(0, 25f);
        if (ImGui.Checkbox(" Ally target", ref alliesCastBarsTarget))
        {
            log.Information($"{Class} - Setting Allies Target to {alliesCastBarsTarget}");
            config.AlliesCastBarsTarget = alliesCastBarsTarget;
            config.Save();
        }

        // Ally focus
        ImGui.SameLine(0, 34f);
        if (ImGui.Checkbox(" Ally focus", ref alliesCastBarsFocus))
        {
            log.Information($"{Class} - Setting Allies Focus to {alliesCastBarsFocus}");
            config.AlliesCastBarsFocus = alliesCastBarsFocus;
            config.Save();
        }

        // Party list
        ImGui.SameLine(0, 34f);
        if (ImGui.Checkbox(" Party list", ref alliesCastBarsPartyList))
        {
            log.Information($"{Class} - Setting Allies Party List to {alliesCastBarsPartyList}");
            config.AlliesCastBarsPartyList = alliesCastBarsPartyList;
            config.Save();
        }
        ImGui.Spacing();
        ImGui.Spacing();

        // Enemy target
        ImGui.SameLine(0, 25f);
        if (ImGui.Checkbox(" Enemy target", ref enemiesCastBarsTarget))
        {
            log.Information($"{Class} - Setting Enemies Target to {enemiesCastBarsTarget}");
            config.EnemiesCastBarsTarget = enemiesCastBarsTarget;
            config.Save();
        }

        // Enemy focus
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Enemy focus", ref enemiesCastBarsFocus))
        {
            log.Information($"{Class} - Setting Enemies Focus to {enemiesCastBarsFocus}");
            config.EnemiesCastBarsFocus = enemiesCastBarsFocus;
            config.Save();
        }

        // Enmity list
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Enmity list", ref enemiesCastBarsEnmityList))
        {
            log.Information($"{Class} - Setting Enemies Enmity List to {enemiesCastBarsEnmityList}");
            config.EnemiesCastBarsEnmityList = enemiesCastBarsEnmityList;
            config.Save();
        }
        ImGui.Spacing();
        ImGui.Spacing();

        // Tooltips
        ImGui.SameLine(0, 15f);
        ImGui.Text("Tooltips :");
        ImGui.Spacing();
        ImGui.Spacing();

        // Action tooltip
        ImGui.SameLine(0, 25f);
        if (ImGui.Checkbox(" Action", ref actionTooltip))
        {
            log.Information($"{Class} - Setting Action tooltip to {actionTooltip}");
            config.ActionTooltip = actionTooltip;
            config.Save();
        }

        // Item tooltip
        ImGui.SameLine(0, 58f);
        if (ImGui.Checkbox(" Item", ref itemTooltip))
        {
            log.Information($"{Class} - Setting Item tooltip to {itemTooltip}");
            config.ItemTooltip = itemTooltip;
            config.Save();
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();

        // Clear caches button
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        if (ImGui.Button("Clear translation caches", new Vector2(220f, 0)))
        {
            translationCache.Clear();
            log.Information($"{Class} - All translation caches cleared");
        }
        ImGui.Spacing();
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