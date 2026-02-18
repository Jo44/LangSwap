using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using System;

namespace LangSwap;

// ----------------------------
// Plugin Configuration
// ----------------------------
[Serializable]
public class Configuration : IPluginConfiguration
{
    // Version
    public int Version { get; set; } = 1;

    // Language : 0 = Japanese, 1 = English, 2 = German, 3 = French
    public byte ClientLanguage { get; set; } = 1;
    public byte TargetLanguage { get; set; } = 1;

    // Primary key
    public int PrimaryKey { get; set; } = (int)VirtualKey.Y;

    // Modifier keys
    public bool Ctrl { get; set; } = false;
    public bool Alt { get; set; } = false;
    public bool Shift { get; set; } = true;

    // Components
    public bool Castbars { get; set; } = true;
    public bool ActionDetails { get; set; } = true;
    public bool ItemDetails { get; set; } = true;

    // Valid ID ranges
    public int MaxValidItemId { get; } = 100000;
    public int MaxValidActionId { get; } = 100000;

    // Signatures
    public string CastBarSig { get; } = "48 83 EC 38 48 8B 92";
    public string ItemHoveredSig { get; } = "E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? 48 89 9C 24 ?? ?? ?? ?? 4C 89 A4 24";
    public string GenerateActionTooltipSig { get; } = "E8 ?? ?? ?? ?? 48 8B 43 28 48 8B AF";
    public string GenerateItemTooltipSig { get; } = "48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 83 EC ?? 48 8B 42 ?? 4C 8B EA";

    // Save configuration
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }

}
