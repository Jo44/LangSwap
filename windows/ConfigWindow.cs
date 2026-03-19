using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LangSwap.translation;
using LangSwap.ui;
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
    private readonly Configuration _config;
    private readonly HookManager _hookManager;
    private readonly Plugin _plugin;
    private readonly TranslationCache _translationCache;
    private readonly IPluginLog _log;

    // Primary key options
    private readonly List<string> _keyNames = ["None"];
    private readonly List<int> _keyValues = [-1];

    // ----------------------------
    // Initialization
    // ----------------------------
    public ConfigWindow(Configuration config, HookManager hookManager, Plugin plugin, TranslationCache translationCache, IPluginLog log) : base("LangSwap Configuration###LangSwapConfig")
    {
        // Store references
        this._config = config;
        this._hookManager = hookManager;
        this._plugin = plugin;
        this._translationCache = translationCache;
        this._log = log;

        // Window settings
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        // Initialize window size
        Size = new Vector2(410, 455);
        SizeCondition = ImGuiCond.Always;

        // Initialize key names and values
        InitKeys(_keyNames, _keyValues);
    }

    // ----------------------------
    // Config Window Draw
    // ----------------------------
    public override void Draw()
    {
        /// Settings UI

        // Language
        string[] languages = Enum.GetNames<LanguageEnum>();
        int currentLang = (int)_config.TargetLanguage;

        // Primary Key
        int selIndex = _keyValues.IndexOf(_config.PrimaryKey);
        if (selIndex < 0) selIndex = 0;

        // Modifiers
        bool ctrl = _config.Ctrl;
        bool alt = _config.Alt;
        bool shift = _config.Shift;

        // UI Components
        bool actionTooltip = _config.ActionTooltip;
        bool itemTooltip = _config.ItemTooltip;
        bool alliesCastBarsTarget = _config.AlliesCastBarsTarget;
        bool alliesCastBarsFocus = _config.AlliesCastBarsFocus;
        bool alliesCastBarsPartyList = _config.AlliesCastBarsPartyList;
        bool enemiesCastBarsTarget = _config.EnemiesCastBarsTarget;
        bool enemiesCastBarsFocus = _config.EnemiesCastBarsFocus;
        bool enemiesCastBarsEnmityList = _config.EnemiesCastBarsEnmityList;

        // Startup behavior
        bool autoStartup = _config.AutoStartup;

        /// Draw UI

        // Instructions
        ImGui.SameLine(0, 5f);
        ImGui.TextWrapped("Press the keyboard shortcut to toogle language swap\nPress again to restore original language");
        ImGui.Spacing();
        ImGui.Separator();

        // Language selection
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 5f);
        ImGui.Text("Target Language :");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        if (ImGui.Combo("##Language", ref currentLang, languages, languages.Length))
        {
            _log.Information($"{Class} - Setting target language to {languages[currentLang]} ({currentLang})");
            _config.TargetLanguage = (byte)currentLang;
            _config.Save();
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();

        // Keyboard shortcut
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 5f);
        ImGui.Text("Keyboard Shortcut :");

        // Primary key
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGui.Text("Key :");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(55f);
        if (ImGui.Combo("##PrimaryKey", ref selIndex, _keyNames.ToArray(), _keyNames.Count))
        {
            _log.Information($"{Class} - Setting primary key to {_keyNames[selIndex]} ({_keyValues[selIndex]})");
            _config.PrimaryKey = _keyValues[selIndex];
            _config.Save();
        }

        // Modifier keys
        ImGui.SameLine(0, 40f);
        if (ImGui.Checkbox(" Ctrl", ref ctrl))
        {
            _log.Information($"{Class} - Setting Ctrl to {ctrl}");
            _config.Ctrl = ctrl;
            _config.Save();
        }
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Alt", ref alt))
        {
            _log.Information($"{Class} - Setting Alt to {alt}");
            _config.Alt = alt;
            _config.Save();
        }
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Shift", ref shift))
        {
            _log.Information($"{Class} - Setting Shift to {shift}");
            _config.Shift = shift;
            _config.Save();
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();

        // UI Components
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 5f);
        ImGui.Text("Select the UI components to translate");
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGui.Text("Tooltips :");
        ImGui.Spacing();
        ImGui.Spacing();

        // Action tooltip
        ImGui.SameLine(0, 25f);
        if (ImGui.Checkbox(" Action", ref actionTooltip))
        {
            _log.Information($"{Class} - Setting Action tooltip to {actionTooltip}");
            _config.ActionTooltip = actionTooltip;
            _config.Save();
            if (actionTooltip) _hookManager.Enable(HookEnum.ActionTooltip);
            else _hookManager.Disable(HookEnum.ActionTooltip);
        }

        // Item tooltip
        ImGui.SameLine(0, 58f);
        if (ImGui.Checkbox(" Item", ref itemTooltip))
        {
            _log.Information($"{Class} - Setting Item tooltip to {itemTooltip}");
            _config.ItemTooltip = itemTooltip;
            _config.Save();
            if (itemTooltip) _hookManager.Enable(HookEnum.ItemTooltip);
            else _hookManager.Disable(HookEnum.ItemTooltip);
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGui.Text("Casts :");
        ImGui.Spacing();
        ImGui.Spacing();
        
        // Ally target
        ImGui.SameLine(0, 25f);
        if (ImGui.Checkbox(" Ally target", ref alliesCastBarsTarget))
        {
            _log.Information($"{Class} - Setting Allies Target to {alliesCastBarsTarget}");
            _config.AlliesCastBarsTarget = alliesCastBarsTarget;
            _config.Save();
            if (alliesCastBarsTarget) _hookManager.Enable(HookEnum.AlliesCastBars);
            else _hookManager.Disable(HookEnum.AlliesCastBars);
        }

        // Ally focus
        ImGui.SameLine(0, 34f);
        if (ImGui.Checkbox(" Ally focus", ref alliesCastBarsFocus))
        {
            _log.Information($"{Class} - Setting Allies Focus to {alliesCastBarsFocus}");
            _config.AlliesCastBarsFocus = alliesCastBarsFocus;
            _config.Save();
            if (alliesCastBarsFocus) _hookManager.Enable(HookEnum.AlliesCastBars);
            else _hookManager.Disable(HookEnum.AlliesCastBars);
        }

        // Party list
        ImGui.SameLine(0, 34f);
        if (ImGui.Checkbox(" Party List", ref alliesCastBarsPartyList))
        {
            _log.Information($"{Class} - Setting Allies Party List to {alliesCastBarsPartyList}");
            _config.AlliesCastBarsPartyList = alliesCastBarsPartyList;
            _config.Save();
            if (alliesCastBarsPartyList) _hookManager.Enable(HookEnum.AlliesCastBars);
            else _hookManager.Disable(HookEnum.AlliesCastBars);
        }
        ImGui.Spacing();
        ImGui.Spacing();

        // Enemy target
        ImGui.SameLine(0, 25f);
        if (ImGui.Checkbox(" Enemy target", ref enemiesCastBarsTarget))
        {
            _log.Information($"{Class} - Setting Enemies Target to {enemiesCastBarsTarget}");
            _config.EnemiesCastBarsTarget = enemiesCastBarsTarget;
            _config.Save();
            if (enemiesCastBarsTarget) _hookManager.Enable(HookEnum.EnemiesCastBars);
            else _hookManager.Disable(HookEnum.EnemiesCastBars);
        }

        // Enemy focus
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Enemy focus", ref enemiesCastBarsFocus))
        {
            _log.Information($"{Class} - Setting Enemies Focus to {enemiesCastBarsFocus}");
            _config.EnemiesCastBarsFocus = enemiesCastBarsFocus;
            _config.Save();
            if (enemiesCastBarsFocus) _hookManager.Enable(HookEnum.EnemiesCastBars);
            else _hookManager.Disable(HookEnum.EnemiesCastBars);
        }

        // Enmity list
        ImGui.SameLine(0, 15f);
        if (ImGui.Checkbox(" Enmity List", ref enemiesCastBarsEnmityList))
        {
            _log.Information($"{Class} - Setting Enemies Enmity List to {enemiesCastBarsEnmityList}");
            _config.EnemiesCastBarsEnmityList = enemiesCastBarsEnmityList;
            _config.Save();
            if (enemiesCastBarsEnmityList) _hookManager.Enable(HookEnum.EnemiesCastBars);
            else _hookManager.Disable(HookEnum.EnemiesCastBars);
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();

        // Startup behavior
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 5f);
        if (ImGui.Checkbox(" Automatically swap at startup", ref autoStartup))
        {
            _log.Information($"{Class} - Setting AutoStartup to {autoStartup}");
            _config.AutoStartup = autoStartup;
            _config.Save();
        }

        // Clear caches button
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 5f);
        if (ImGui.Button("Clear all translation caches"))
        {
            _translationCache.Clear();
            _log.Information($"{Class} - All translation caches cleared");
        }

        // Warning if translation is active
        if (_plugin.IsSwapEnabled())
        {
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.SameLine(0, 5f);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.5f, 0.0f, 1.0f));
            ImGui.TextWrapped("WARNING : Disable translation before changing settings !");
            ImGui.PopStyleColor();
            ImGui.Spacing();
        }
    }

    // ----------------------------
    // Initialize primary key names and values
    // ----------------------------
    private static void InitKeys(List<String> keyNames, List<int> keyValues)
    {
        // Letters A-Z
        int startA = (int)VirtualKey.A;
        int endZ = (int)VirtualKey.Z;
        for (int v = startA; v <= endZ; v++)
        {
            keyNames.Add(((VirtualKey)v).ToString());
            keyValues.Add(v);
        }

        // Function keys F1-F12
        if (Enum.TryParse<VirtualKey>("F1", out _))
        {
            int startF1 = (int)VirtualKey.F1;
            int endF12 = (int)VirtualKey.F12;
            for (int v = startF1; v <= endF12; v++)
            {
                keyNames.Add(((VirtualKey)v).ToString());
                keyValues.Add(v);
            }
        }
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