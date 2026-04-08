using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.hook.template;
using LangSwap.translation;
using LangSwap.translation.@base;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace LangSwap.hook;

// ----------------------------
// Item Tooltip Hook
// ----------------------------
public unsafe partial class ItemTooltipHook(Configuration config, TranslationCache translationCache) : TooltipHook(config, translationCache)
{
    // Log
    private const string Class = "[ItemTooltipHook.cs]";

    // Memory signature
    protected override string MemorySignature => config.ItemTooltipSignature;

    // Generated Regex
    [GeneratedRegex(@"\+\d+")]
    private static partial Regex StatLineRegex();

    // ----------------------------
    // On language swap
    // ----------------------------
    protected override void OnLanguageSwap()
    {
        // Refresh item detail addon
        RefreshAddon(GetAddon(config.ActionDetailAddon), config.ItemDetailName);
    }

    // ----------------------------
    // On tooltip update
    // ----------------------------
    protected override void* OnTooltipUpdate(AtkUnitBase* itemDetailAddon, NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
    {
        try
        {
            // Log the structure of StringArrayData for debugging
            // utilities.LogSADStructure(stringArrayData);

            // Check if language is swapped
            if (isLanguageSwapped && stringArrayData != null)
            {
                // Get client language
                Language clientLang = config.ClientLanguage;

                // Get target language
                Language targetLang = config.TargetLanguage;

                // Only proceed if client language and target language are different
                if (clientLang != targetLang)
                {
                    // Get item name
                    string itemName = ReadStringFromArrayData(stringArrayData, config.ItemNameField);
                    if (!string.IsNullOrWhiteSpace(itemName))
                    {
                        // Check for high quality symbol in item name
                        bool isHighQualityItem = IsHighQuality(itemName);

                        // Remove it temporarily for ID lookup
                        if (isHighQualityItem) itemName = UnsetHighQuality(itemName);

                        // Get item ID
                        uint itemID = translationCache.GetItemIDByName(itemName, clientLang) ?? 0;
                        if (itemID > 0 && itemID <= config.MaxValidItemID)
                        {
                            /* Item name */

                            // Translate item name
                            string translatedItemName = TranslateItemName(itemID, isHighQualityItem, targetLang);

                            // Apply translated item name
                            if (!string.IsNullOrWhiteSpace(translatedItemName))
                            {
                                if (!WriteStringToArrayData(stringArrayData, config.ItemNameField, translatedItemName))
                                {
                                    Log.Error($"{Class} - Failed to write translated item name ({translatedItemName}) to field {config.ItemNameField}");
                                }
                            }

                            /* Glamour name */

                            // Get glamour name
                            string glamourName = ReadStringFromArrayData(stringArrayData, config.GlamourNameField);
                            if (!string.IsNullOrWhiteSpace(glamourName))
                            {
                                // Check for high quality symbol in glamour name
                                bool isHighQualityGlamour = IsHighQuality(glamourName);

                                // Remove symbols temporarily for ID lookup
                                glamourName = UnsetGlamour(glamourName);
                                if (isHighQualityGlamour) glamourName = UnsetHighQuality(glamourName);

                                // Get glamour ID
                                uint glamourID = translationCache.GetItemIDByName(glamourName, clientLang) ?? 0;
                                if (glamourID > 0 && glamourID <= config.MaxValidItemID)
                                {
                                    // Translate glamour name
                                    string translatedGlamourName = TranslateGlamourName(glamourID, isHighQualityGlamour, targetLang);

                                    // Apply translated glamour name
                                    if (!string.IsNullOrWhiteSpace(translatedGlamourName))
                                    {
                                        if (!WriteStringToArrayData(stringArrayData, config.GlamourNameField, translatedGlamourName))
                                        {
                                            Log.Error($"{Class} - Failed to write translated glamour name ({translatedGlamourName}) to field {config.GlamourNameField}");
                                        }
                                    }
                                }
                            }

                            /* Description */

                            // Translate description
                            string translatedDescription = TranslateDescription(itemID, targetLang);

                            // Apply translated description
                            if (!string.IsNullOrWhiteSpace(translatedDescription))
                            {
                                if (!WriteStringToArrayData(stringArrayData, config.ItemDescriptionField, translatedDescription))
                                {
                                    Log.Error($"{Class} - Failed to write translated description ({translatedDescription}) to field {config.ItemDescriptionField}");
                                }
                            }

                            /* Effects */

                            // Get effects
                            string effects = ReadStringFromArrayData(stringArrayData, config.ItemEffectsField);

                            // Translate effects
                            string translatedEffects = TranslateEffects(effects, clientLang, targetLang);

                            // Apply translated effects
                            if (!string.IsNullOrWhiteSpace(translatedEffects))
                            {
                                if (!WriteStringToArrayData(stringArrayData, config.ItemEffectsField, translatedEffects))
                                {
                                    Log.Error($"{Class} - Failed to write translated effects ({translatedEffects}) to field {config.ItemEffectsField}");
                                }
                            }

                            /* Bonuses */

                            // Translate bonuses
                            for (int i = config.ItemBonusesStartField; i <= config.ItemBonusesEndField; i++)
                            {
                                // Get bonus
                                string bonus = ReadStringFromArrayData(stringArrayData, i);

                                // Translate bonus
                                string translatedBonus = TranslateBonus(bonus, clientLang, targetLang);

                                // Apply translated bonus
                                if (!string.IsNullOrWhiteSpace(translatedBonus))
                                {
                                    if (!WriteStringToArrayData(stringArrayData, i, translatedBonus))
                                    {
                                        Log.Error($"{Class} - Failed to write translated bonus ({translatedBonus}) to field {i}");
                                    }
                                }
                            }

                            /* Materia names */

                            // Translate materia names
                            for (int j = config.ItemMateriaNameStartField; j <= config.ItemMateriaNameEndField; j++)
                            {
                                // Get materia name
                                string materiaName = ReadStringFromArrayData(stringArrayData, j);

                                // Get materia ID
                                uint materiaID = translationCache.GetItemIDByName(materiaName, clientLang) ?? 0;
                                if (materiaID > 0 && materiaID <= config.MaxValidItemID)
                                {
                                    // Translate materia name
                                    string translatedMateriaName = TranslateMateriaName(materiaID, targetLang);

                                    // Apply translated materia name
                                    if (!string.IsNullOrWhiteSpace(translatedMateriaName))
                                    {
                                        if (!WriteStringToArrayData(stringArrayData, j, translatedMateriaName))
                                        {
                                            Log.Error($"{Class} - Failed to write translated materia name ({translatedMateriaName}) to field {j}");
                                        }
                                    }
                                }
                            }

                            /* Materia stats */

                            // Translate materia stats
                            for (int k = config.ItemMateriaStatStartField; k <= config.ItemMateriaStatEndField; k++)
                            {
                                // Get materia stat
                                string materiaStat = ReadStringFromArrayData(stringArrayData, k);

                                // Translate materia stat
                                string translatedMateriaStat = TranslateMateriaStat(materiaStat, clientLang, targetLang);

                                // Apply translated materia stat
                                if (!string.IsNullOrWhiteSpace(translatedMateriaStat))
                                {
                                    if (!WriteStringToArrayData(stringArrayData, k, translatedMateriaStat))
                                    {
                                        Log.Error($"{Class} - Failed to write translated materia stat ({translatedMateriaStat}) to field {k}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Exception in OnTooltipUpdate");
        }

        // Call original function with modified data
        return tooltipHook!.Original(itemDetailAddon, numberArrayData, stringArrayData);
    }

    // ----------------------------
    // Translate item name
    // ----------------------------
    private string TranslateItemName(uint itemID, bool isHighQuality, Language targetLang)
    {
        // Get translated item name from item ID
        string translatedItemName = translationCache.GetItemName(itemID, targetLang) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(translatedItemName))
        {
            // Reapply high quality symbol if it was present
            if (isHighQuality) translatedItemName = SetHighQuality(translatedItemName);
        }
        // Return translated item name
        return translatedItemName;
    }

    // ----------------------------
    // Translate glamour name
    // ----------------------------
    private string TranslateGlamourName(uint glamourID, bool isHighQualityGlamour, Language targetLang)
    {
        // Get translated glamour name from glamour ID
        string translatedGlamourName = translationCache.GetItemName(glamourID, targetLang) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(translatedGlamourName))
        {
            // Reapply glamour symbol
            translatedGlamourName = SetGlamour(translatedGlamourName);

            // Reapply high quality symbol if it was present
            if (isHighQualityGlamour) translatedGlamourName = SetHighQuality(translatedGlamourName);
        }
        // Return translated glamour name
        return translatedGlamourName;
    }

    // ----------------------------
    // Translate description
    // ----------------------------
    private string TranslateDescription(uint itemID, Language targetLang)
    {
        // Get translated description from item ID
        return translationCache.GetItemDescription(itemID, targetLang) ?? string.Empty;
    }

    // ----------------------------
    // Translate effects
    // ----------------------------
    private string TranslateEffects(string effects, Language clientLang, Language targetLang)
    {
        // Initialize translated effects
        string translatedEffects = string.Empty;

        // Only translate if effects text is not empty
        if (!string.IsNullOrWhiteSpace(effects))
        {
            // Split effects by line breaks
            string[] lines = effects.Split('\n');

            // Translate each line individually
            string[] translatedLines = [.. lines.Select(line =>
                string.IsNullOrWhiteSpace(line) ? line : TranslateStat(line, clientLang, targetLang)
            )];

            // Reconstruct the translated effects
            for (int i = 0; i < translatedLines.Length; i++)
            {
                // Add line break if needed
                if (i > 0) translatedEffects += "\n";

                // Use translated line if available, otherwise fallback to original line
                if (!string.IsNullOrWhiteSpace(translatedLines[i])) translatedEffects += translatedLines[i];
                else translatedEffects += lines[i];
            }
        }

        // Return translated effects
        return translatedEffects;
    }

    // ----------------------------
    // Translate bonus
    // ----------------------------
    private string TranslateBonus(string bonus, Language clientLang, Language targetLang)
    {
        // Initialize translated bonus
        string translatedBonus = string.Empty;

        // Only translate if line contains a stat format (e.g. "+10 Strength")
        if (!string.IsNullOrWhiteSpace(bonus) && StatLineRegex().IsMatch(bonus))
        {
            translatedBonus = TranslateStat(bonus, clientLang, targetLang);
        }

        // Return translated bonus
        return translatedBonus;
    }

    // ----------------------------
    // Translate materia name
    // ----------------------------
    private string TranslateMateriaName(uint materiaID, Language targetLang)
    {
        // Get translated materia name from materia ID
        return translationCache.GetItemName(materiaID, targetLang) ?? string.Empty;
    }

    // ----------------------------
    // Translate materia stat
    // ----------------------------
    private string TranslateMateriaStat(string materiaStat, Language clientLang, Language targetLang)
    {
        // Initialize translated materia stat
        string translatedMateriaStat = string.Empty;

        // Only translate if line contains a stat format (e.g. "+10 Strength")
        if (!string.IsNullOrWhiteSpace(materiaStat) && StatLineRegex().IsMatch(materiaStat))
        {
            translatedMateriaStat = TranslateStat(materiaStat, clientLang, targetLang);
        }

        // Return translated materia stat
        return translatedMateriaStat;
    }

    // ----------------------------
    // Translate stat name
    // ----------------------------
    private string TranslateStat(string stat, Language clientLang, Language targetLang)
    {
        // Initialize translated stat
        string translatedStat = string.Empty;

        // Extract stat name from line
        int plusIndex = stat.IndexOf('+');
        if (plusIndex > 0)
        {
            // Split stat name and stat value parts
            string statName = stat[..plusIndex].Trim();
            string statValue = stat[plusIndex..];

            // Translate stat name
            string translatedStatName = translationCache.GetBaseParamName(statName, clientLang, targetLang) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(translatedStatName))
            {
                // Reconstruct the translated line
                translatedStat = $"{translatedStatName} {statValue}";
            }
        }

        // Return translated stat
        return translatedStat;
    }

    // ----------------------------
    // Determine high quality state
    // ----------------------------
    private bool IsHighQuality(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) return text.Contains(config.HighQualitySymbol);
        else return false;
    }

    // ----------------------------
    // Add high quality symbol
    // ----------------------------
    private string SetHighQuality(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) return text + " " + config.HighQualitySymbol;
        else return text;
    }

    // ----------------------------
    // Remove high quality symbol
    // ----------------------------
    private string UnsetHighQuality(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) return text.Replace(config.HighQualitySymbol.ToString(), "").Trim();
        else return text;
    }

    // ----------------------------
    // Add glamour symbol
    // ----------------------------
    private string SetGlamour(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) return config.GlamouredSymbol + " " + text;
        else return text;
    }

    // ----------------------------
    // Remove glamour symbol
    // ----------------------------
    private string UnsetGlamour(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) return text.Replace(config.GlamouredSymbol.ToString(), "").Trim();
        else return text;
    }

}