using Dalamud.Configuration;
using System;

namespace LangSwap;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    /// <summary>
    /// The target language to switch to when key combination is held.
    /// Language codes: 0=Japanese, 1=English, 2=German, 3=French
    /// </summary>
    public byte TargetLanguage { get; set; } = 1; // Default to English

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
