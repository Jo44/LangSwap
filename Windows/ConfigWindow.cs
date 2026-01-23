using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace LangSwap.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;

    public ConfigWindow(Plugin plugin) : base("LangSwap Configuration###LangSwapConfig")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(300, 150);
        SizeCondition = ImGuiCond.Always;

        this.plugin = plugin;
        this.configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Text("LangSwap Configuration");
        ImGui.Separator();
        ImGui.Spacing();

        // Language selection
        ImGui.Text("Target Language (when Ctrl+Alt is held):");
        var languages = new[] { "Japanese", "English", "German", "French" };
        var currentLang = (int)configuration.TargetLanguage;
        
        if (ImGui.Combo("##Language", ref currentLang, languages, languages.Length))
        {
            configuration.TargetLanguage = (byte)currentLang;
            configuration.Save();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextWrapped("Hold key combination to temporarily switch language.\nRelease to restore original language.");
    }
}
