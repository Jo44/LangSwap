using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using System;

namespace LangSwap;

// Plugin configuration
[Serializable]
public class Configuration : IPluginConfiguration
{
    // Version
    public int Version { get; set; } = 1;

    // Language code: 0=Japanese, 1=English, 2=German, 3=French
    public byte ClientLanguage { get; set; } = 1; // Default to English
    public byte TargetLanguage { get; set; } = 1; // Default to English

    // Primary key
    public int PrimaryKey { get; set; } = (int)VirtualKey.Y; // Default to Y

    // Modifier keys
    public bool UseCtrl { get; set; } = false;
    public bool UseAlt { get; set; } = false;
    public bool UseShift { get; set; } = true; // Default to Shift

    // Valid item ID range
    public int MaxValidItemId { get; } = 100000;

    // Signature for item hovered function
    public string ItemHoveredSig { get; } = "E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? 48 89 9C 24 ?? ?? ?? ?? 4C 89 A4 24";

    // Signature for tooltip generation function
    public string GenerateTooltipSig { get; } = "48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 83 EC ?? 48 8B 42 ?? 4C 8B EA";

    // Save configuration
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
