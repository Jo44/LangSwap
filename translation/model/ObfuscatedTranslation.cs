using System;

namespace LangSwap.translation.model;

// ----------------------------
// Obfuscated Translation
// ----------------------------
[Serializable]
public class ObfuscatedTranslation
{
    public int ID { get; set; } = 0;
    public string ObfuscatedName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public string FrenchName { get; set; } = string.Empty;
    public string GermanName { get; set; } = string.Empty;
    public string JapaneseName { get; set; } = string.Empty;
}