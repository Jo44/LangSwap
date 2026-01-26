using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Windowing;
using LangSwap.translation;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace LangSwap.Windows;

// Window configuration
public class ConfigWindow : Window, IDisposable
{
    // References
    private readonly Plugin plugin;
    private readonly Configuration configuration;
    private readonly string[] languages = Enum.GetNames(typeof(LanguageEnum));
    private readonly List<string> keyNames = new List<string> { "None" };
    private readonly List<int> keyValues = new List<int> { -1 };

    // Constructor
    public ConfigWindow(Plugin plugin) : base("LangSwap Configuration###LangSwapConfig")
    {
        // Window settings
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        // Initialize window size
        Size = new Vector2(370, 190);
        SizeCondition = ImGuiCond.Always;

        // Initialize key names and values
        InitKeys(keyNames, keyValues);

        // Store references
        this.plugin = plugin;
        this.configuration = plugin.Configuration;
    }

    // Draw
    public override void Draw()
    {
        /// Settings UI

        // Language
        var currentLang = (int)configuration.TargetLanguage;

        // Primary Key
        int selIndex = keyValues.IndexOf(configuration.PrimaryKey);
        if (selIndex < 0) selIndex = 0;

        // Modifiers
        bool useCtrl = configuration.UseCtrl;
        bool useAlt = configuration.UseAlt;
        bool useShift = configuration.UseShift;

        /// Draw UI

        // Instructions
        ImGui.TextWrapped("Hold keyboard shortcut to temporarily switch language.\nRelease to restore original language.");
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
            Plugin.Log.Information($"Setting target language to {languages[currentLang]} ({currentLang})");
            configuration.TargetLanguage = (byte)currentLang;
            configuration.Save();
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
        if (ImGui.Combo("##PrimaryKey", ref selIndex, keyNames.ToArray(), keyNames.Count))
        {
            Plugin.Log.Information($"Setting primary key to {keyNames[selIndex]} ({keyValues[selIndex]})");
            configuration.PrimaryKey = keyValues[selIndex];
            configuration.Save();
        }

        // Modifier keys
        ImGui.SameLine(0, 20f);
        if (ImGui.Checkbox("Ctrl", ref useCtrl))
        {
            Plugin.Log.Information($"Setting UseCtrl to {useCtrl}");
            configuration.UseCtrl = useCtrl;
            configuration.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Alt", ref useAlt))
        {
            Plugin.Log.Information($"Setting UseAlt to {useAlt}");
            configuration.UseAlt = useAlt;
            configuration.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Shift", ref useShift))
        {
            Plugin.Log.Information($"Setting UseShift to {useShift}");
            configuration.UseShift = useShift;
            configuration.Save();
        }
        ImGui.Spacing();
        
    }

    // Initialize primary key names and values
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

    // Dispose
    public void Dispose() { }

}
