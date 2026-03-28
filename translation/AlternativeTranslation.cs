using System;

namespace LangSwap.translation;

// ----------------------------
// Alternative translation
// ----------------------------
[Serializable]
public class AlternativeTranslation
{
    public string SpellName { get; set; } = string.Empty;
    public string AlternativeName { get; set; } = string.Empty;
}