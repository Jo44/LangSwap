using System;
using System.Text.Json.Serialization;

namespace LangSwap.translation.model;

// ----------------------------
// Upload Translation
//
// @author Jo44
// @version 1.7 (23/04/2026)
// @since 01/01/2026
// ----------------------------
[Serializable]
public class UploadTranslation
{
    [JsonPropertyName("actionId")]
    public int ActionId { get; set; } = 0;

    [JsonPropertyName("obfuscatedName")]
    public string ObfuscatedName { get; set; } = string.Empty;

    [JsonPropertyName("deobfuscatedName")]
    public string DeobfuscatedName { get; set; } = string.Empty;

    [JsonPropertyName("languageId")]
    public int LanguageId { get; set; } = 0;

    [JsonPropertyName("characterName")]
    public string CharacterName { get; set; } = string.Empty;
}