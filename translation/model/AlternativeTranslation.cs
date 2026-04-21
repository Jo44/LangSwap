using System;

namespace LangSwap.translation.model;

// ----------------------------
// Alternative Translation
//
// @author Jo44
// @version 1.7 (21/04/2026)
// @since 01/01/2026
// ----------------------------
[Serializable]
public class AlternativeTranslation
{
    public string SpellName { get; set; } = string.Empty;
    public string AlternativeName { get; set; } = string.Empty;
}