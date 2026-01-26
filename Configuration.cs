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

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
