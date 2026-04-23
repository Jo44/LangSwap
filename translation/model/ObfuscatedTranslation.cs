using System;

namespace LangSwap.translation.model;

// ----------------------------
// Obfuscated Translation
//
// @author Jo44
// @version 1.7 (23/04/2026)
// @since 01/01/2026
// ----------------------------
[Serializable]
public class ObfuscatedTranslation
{
    public int ActionID { get; set; } = 0;
    public string ObfuscatedName { get; set; } = string.Empty;
    public int LanguageID { get; set; } = 0;
    public string DeobfuscatedName { get; set; } = string.Empty;
}