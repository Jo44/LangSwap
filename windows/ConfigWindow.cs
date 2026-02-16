using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
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
    // Core components
    private readonly Configuration _config;
    private readonly TranslationCache _translationCache;
    private readonly IPluginLog _log;
    private readonly List<string> _keyNames = ["None"];
    private readonly List<int> _keyValues = [-1];

    // ----------------------------
    // Initialization
    // ----------------------------
    public ConfigWindow(Configuration config, TranslationCache translationCache, IPluginLog log) : base("LangSwap Configuration###LangSwapConfig")
    {
        // Window settings
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        // Initialize window size
        Size = new Vector2(420, 273);
        SizeCondition = ImGuiCond.Always;

        // Initialize key names and values
        InitKeys(_keyNames, _keyValues);

        // Store references
        this._config = config;
        this._translationCache = translationCache;
        this._log = log;
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

        // Components
        bool castbars = _config.Castbars;
        bool actionDetails = _config.ActionDetails;
        bool itemDetails = _config.ItemDetails;

        /// Draw UI

        // Instructions
        ImGui.TextWrapped("Press the keyboard shortcut to toogle language swap\nPress again to restore original language");
        ImGui.Spacing();
        ImGui.Separator();

        // Language selection
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Text("Target Language :");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        if (ImGui.Combo("##Language", ref currentLang, languages, languages.Length))
        {
            _log.Information($"Setting target language to {languages[currentLang]} ({currentLang})");
            _config.TargetLanguage = (byte)currentLang;
            _config.Save();
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();

        // Keyboard shortcut
        ImGui.Spacing();
        ImGui.Text("Keyboard Shortcut :");

        // Primary key
        ImGui.Spacing();
        ImGui.Text("Key :");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        if (ImGui.Combo("##PrimaryKey", ref selIndex, _keyNames.ToArray(), _keyNames.Count))
        {
            _log.Information($"Setting primary key to {_keyNames[selIndex]} ({_keyValues[selIndex]})");
            _config.PrimaryKey = _keyValues[selIndex];
            _config.Save();
        }

        // Modifier keys
        ImGui.SameLine(0, 20f);
        if (ImGui.Checkbox("Ctrl", ref ctrl))
        {
            _log.Information($"Setting Ctrl to {ctrl}");
            _config.Ctrl = ctrl;
            _config.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Alt", ref alt))
        {
            _log.Information($"Setting Alt to {alt}");
            _config.Alt = alt;
            _config.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Shift", ref shift))
        {
            _log.Information($"Setting Shift to {shift}");
            _config.Shift = shift;
            _config.Save();
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();

        // Components
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Text("Components :");
        ImGui.SameLine(0, 10f);
        if (ImGui.Checkbox("Castbars", ref castbars))
        {
            _log.Information($"Setting Castbars to {castbars}");
            _config.Castbars = castbars;
            _config.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Action details", ref actionDetails))
        {
            _log.Information($"Setting ActionDetails to {actionDetails}");
            _config.ActionDetails = actionDetails;
            _config.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Item details", ref itemDetails))
        {
            _log.Information($"Setting ItemDetails to {itemDetails}");
            _config.ItemDetails = itemDetails;
            _config.Save();
        }
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();

        // Clear caches button
        ImGui.Spacing();
        ImGui.Spacing();
        if (ImGui.Button("Clear all translation caches"))
        {
            _translationCache.Clear();
            _log.Information("All translation caches cleared");
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
    public void Dispose() { }

}
