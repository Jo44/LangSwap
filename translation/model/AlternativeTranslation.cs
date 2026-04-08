using System;

namespace LangSwap.translation.model;

// ----------------------------
// Alternative Translation
// ----------------------------
[Serializable]
public class AlternativeTranslation
{
    public string SpellName { get; set; } = string.Empty;
    public string AlternativeName { get; set; } = string.Empty;
}