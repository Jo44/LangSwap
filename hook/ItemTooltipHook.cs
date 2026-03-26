using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.hook.@base;
using LangSwap.tool;
using LangSwap.translation;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace LangSwap.hook;

// ----------------------------
// Item Tooltip Hook
// ----------------------------
public unsafe partial class ItemTooltipHook(
    Configuration config,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : TooltipBaseHook(config, gameInterop, sigScanner, translationCache, utilities, log)
{
    // Log
    private const string Class = "[ItemTooltipHook.cs]";

    // Memory signature
    protected override string MemorySignature => config.ItemTooltipSignature;

    // Item detail fields
    private readonly int itemNameField = config.ItemNameField;
    private readonly int glamourNameField = config.GlamourNameField;
    private readonly int itemDescriptionField = config.ItemDescriptionField;
    private readonly int itemEffectsField = config.ItemEffectsField;
    private readonly int itemBonusesStartField = config.ItemBonusesStartField;
    private readonly int itemBonusesEndField = config.ItemBonusesEndField;
    private readonly int itemMateriaNameStartField = config.ItemMateriaNameStartField;
    private readonly int itemMateriaNameEndField = config.ItemMateriaNameEndField;
    private readonly int itemMateriaStatStartField = config.ItemMateriaStatStartField;
    private readonly int itemMateriaStatEndField = config.ItemMateriaStatEndField;

    // Generated Regex
    [GeneratedRegex(@"\+\d+")]
    private static partial Regex StatLineRegex();

    // ----------------------------
    // On language swap
    // ----------------------------
    protected override void OnLanguageSwap()
    {
        // Refresh item detail addon
        utilities.RefreshAddon(utilities.GetAddon(config.ActionDetailAddon), "item detail");
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
                LanguageEnum clientLang = config.ClientLanguage;

                // Get target language
                LanguageEnum targetLang = config.TargetLanguage;

                // Get item name
                string itemName = utilities.ReadStringFromArrayData(stringArrayData, itemNameField);
                if (!string.IsNullOrWhiteSpace(itemName))
                {
                    // Check for high quality symbol in item name
                    bool isHighQualityItem = utilities.IsHighQuality(itemName);

                    // Remove it temporarily for ID lookup
                    if (isHighQualityItem) itemName = utilities.UnsetHighQuality(itemName);

                    // Get item ID
                    uint itemId = translationCache.GetItemIdByName(itemName, clientLang) ?? 0;
                    if (itemId > 0 && itemId <= config.MaxValidItemId)
                    {
                        /* Item name */

                        // Translate item name
                        string translatedItemName = TranslateItemName(itemId, isHighQualityItem, targetLang);

                        // Apply translated item name
                        if (!string.IsNullOrWhiteSpace(translatedItemName))
                        {
                            if (!utilities.WriteStringToArrayData(stringArrayData, itemNameField, translatedItemName))
                            {
                                log.Error($"{Class} - Failed to write translated item name ({translatedItemName}) to field {itemNameField}");
                            }
                        }

                        /* Glamour name */

                        // Get glamour name
                        string glamourName = utilities.ReadStringFromArrayData(stringArrayData, glamourNameField);
                        if (!string.IsNullOrWhiteSpace(glamourName))
                        {
                            // Check for high quality symbol in glamour name
                            bool isHighQualityGlamour = utilities.IsHighQuality(glamourName);

                            // Remove symbols temporarily for ID lookup
                            glamourName = utilities.UnsetGlamour(glamourName);
                            if (isHighQualityGlamour) glamourName = utilities.UnsetHighQuality(glamourName);

                            // Get glamour ID
                            uint glamourId = translationCache.GetItemIdByName(glamourName, clientLang) ?? 0;
                            if (glamourId > 0 && glamourId <= config.MaxValidItemId)
                            {
                                // Translate glamour name
                                string translatedGlamourName = TranslateGlamourName(glamourId, isHighQualityGlamour, targetLang);

                                // Apply translated glamour name
                                if (!string.IsNullOrWhiteSpace(translatedGlamourName))
                                {
                                    if (!utilities.WriteStringToArrayData(stringArrayData, glamourNameField, translatedGlamourName))
                                    {
                                        log.Error($"{Class} - Failed to write translated glamour name ({translatedGlamourName}) to field {glamourNameField}");
                                    }
                                }
                            }
                        }

                        /* Description */

                        // Translate description
                        string translatedDescription = TranslateDescription(itemId, targetLang);

                        // Apply translated description
                        if (!string.IsNullOrWhiteSpace(translatedDescription))
                        {
                            if (!utilities.WriteStringToArrayData(stringArrayData, itemDescriptionField, translatedDescription))
                            {
                                log.Error($"{Class} - Failed to write translated description ({translatedDescription}) to field {itemDescriptionField}");
                            }
                        }

                        /* Effects */

                        // Get effects
                        string effects = utilities.ReadStringFromArrayData(stringArrayData, itemEffectsField);

                        // Translate effects
                        string translatedEffects = TranslateEffects(effects, clientLang, targetLang);

                        // Apply translated effects
                        if (!string.IsNullOrWhiteSpace(translatedEffects))
                        {
                            if (!utilities.WriteStringToArrayData(stringArrayData, itemEffectsField, translatedEffects))
                            {
                                log.Error($"{Class} - Failed to write translated effects ({translatedEffects}) to field {itemEffectsField}");
                            }
                        }
                        
                        /* Bonuses */

                        // Translate bonuses
                        for (int i = itemBonusesStartField; i <= itemBonusesEndField; i++)
                        {
                            // Get bonus
                            string bonus = utilities.ReadStringFromArrayData(stringArrayData, i);

                            // Translate bonus
                            string translatedBonus = TranslateBonus(bonus, clientLang, targetLang);

                            // Apply translated bonus
                            if (!string.IsNullOrWhiteSpace(translatedBonus))
                            {
                                if (!utilities.WriteStringToArrayData(stringArrayData, i, translatedBonus))
                                {
                                    log.Error($"{Class} - Failed to write translated bonus ({translatedBonus}) to field {i}");
                                }
                            }
                        }

                        /* Materia names */

                        // Translate materia names
                        for (int j = itemMateriaNameStartField; j <= itemMateriaNameEndField; j++)
                        {
                            // Get materia name
                            string materiaName = utilities.ReadStringFromArrayData(stringArrayData, j);

                            // Get materia ID
                            uint materiaId = translationCache.GetItemIdByName(materiaName, clientLang) ?? 0;
                            if (materiaId > 0 && materiaId <= config.MaxValidItemId)
                            {
                                // Translate materia name
                                string translatedMateriaName = TranslateMateriaName(materiaId, targetLang);

                                // Apply translated materia name
                                if (!string.IsNullOrWhiteSpace(translatedMateriaName))
                                {
                                    if (!utilities.WriteStringToArrayData(stringArrayData, j, translatedMateriaName))
                                    {
                                        log.Error($"{Class} - Failed to write translated materia name ({translatedMateriaName}) to field {j}");
                                    }
                                }
                            }
                        }

                        /* Materia stats */

                        // Translate materia stats
                        for (int k = itemMateriaStatStartField; k <= itemMateriaStatEndField; k++)
                        {
                            // Get materia stat
                            string materiaStat = utilities.ReadStringFromArrayData(stringArrayData, k);

                            // Translate materia stat
                            string translatedMateriaStat = TranslateMateriaStat(materiaStat, clientLang, targetLang);

                            // Apply translated materia stat
                            if (!string.IsNullOrWhiteSpace(translatedMateriaStat))
                            {
                                if (!utilities.WriteStringToArrayData(stringArrayData, k, translatedMateriaStat))
                                {
                                    log.Error($"{Class} - Failed to write translated materia stat ({translatedMateriaStat}) to field {k}");
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Exception in OnItemTooltipUpdate");
        }

        // Call original function with modified data
        return tooltipHook!.Original(itemDetailAddon, numberArrayData, stringArrayData);
    }

    // ----------------------------
    // Translate item name
    // ----------------------------
    private string TranslateItemName(uint itemId, bool isHighQuality, LanguageEnum targetLang)
    {
        // Get translated item name from item ID
        string translatedItemName = translationCache.GetItemName(itemId, targetLang) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(translatedItemName))
        {
            // Reapply high quality symbol if it was present
            if (isHighQuality) translatedItemName = utilities.SetHighQuality(translatedItemName);
        }
        // Return translated item name
        return translatedItemName;
    }

    // ----------------------------
    // Translate glamour name
    // ----------------------------
    private string TranslateGlamourName(uint glamourId, bool isHighQualityGlamour, LanguageEnum targetLang)
    {
        // Get translated glamour name from glamour ID
        string translatedGlamourName = translationCache.GetItemName(glamourId, targetLang) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(translatedGlamourName))
        {
            // Reapply glamour symbol
            translatedGlamourName = utilities.SetGlamour(translatedGlamourName);

            // Reapply high quality symbol if it was present
            if (isHighQualityGlamour) translatedGlamourName = utilities.SetHighQuality(translatedGlamourName);
        }
        // Return translated glamour name
        return translatedGlamourName;
    }

    // ----------------------------
    // Translate description
    // ----------------------------
    private string TranslateDescription(uint itemId, LanguageEnum targetLang)
    {
        // Get translated description from item ID
        string translatedDescription = translationCache.GetItemDescription(itemId, targetLang) ?? string.Empty;

        // Return translated description
        return translatedDescription;
    }

    // ----------------------------
    // Translate effects
    // ----------------------------
    private string TranslateEffects(string effects, LanguageEnum clientLang, LanguageEnum targetLang)
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
    private string TranslateBonus(string bonus, LanguageEnum clientLang, LanguageEnum targetLang)
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
    private string TranslateMateriaName(uint materiaId, LanguageEnum targetLang)
    {
        // Get translated materia name from materia ID
        string translatedMateriaName = translationCache.GetItemName(materiaId, targetLang) ?? string.Empty;

        // Return translated materia name
        return translatedMateriaName;
    }

    // ----------------------------
    // Translate materia stat
    // ----------------------------
    private string TranslateMateriaStat(string materiaStat, LanguageEnum clientLang, LanguageEnum targetLang)
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
    private string TranslateStat(string stat, LanguageEnum clientLang, LanguageEnum targetLang)
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

}