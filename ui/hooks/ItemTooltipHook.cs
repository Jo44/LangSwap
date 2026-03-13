using Dalamud.Game.NativeWrapper;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.ui.hooks.@base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LangSwap.ui.hooks;

// ----------------------------
// Item Tooltip Hook
// ----------------------------
public unsafe partial class ItemTooltipHook(
    Configuration config,
    IGameGui gameGui,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : BaseHook(config, gameGui, gameInterop, sigScanner, translationCache, utilities, log)
{
    // Log
    private const string Class = "[ItemTooltipHook.cs]";

    // Item Detail Addon
    private readonly string ItemDetailAddon = config.ItemDetailAddon;
    private readonly int ItemNameField = config.ItemNameField;
    private readonly int GlamourNameField = config.GlamourNameField;
    private readonly int ItemDescriptionField = config.ItemDescriptionField;
    private readonly int ItemEffectsField = config.ItemEffectsField;
    private readonly int ItemBonusesStartField = config.ItemBonusesStartField;
    private readonly int ItemBonusesEndField = config.ItemBonusesEndField;
    private readonly int ItemMateriaNameStartField = config.ItemMateriaNameStartField;
    private readonly int ItemMateriaNameEndField = config.ItemMateriaNameEndField;
    private readonly int ItemMateriaStatStartField = config.ItemMateriaStatStartField;
    private readonly int ItemMateriaStatEndField = config.ItemMateriaStatEndField;

    // Generated Regex
    [GeneratedRegex(@"\+\d+")]
    private static partial Regex StatLineRegex();

    // Delegate function
    private delegate void* ItemTooltipDelegate(AtkUnitBase* itemDetailAddon, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);

    // Hook
    private Hook<ItemTooltipDelegate>? _itemTooltipHook;

    // ----------------------------
    // Enable the hook
    // ----------------------------
    public override void Enable()
    {
        // Prevent multiple enables
        if (isEnabled) return;

        try
        {
            // Get address from signature
            nint itemTooltipAddr = sigScanner.ScanText(config.ItemTooltipSig);
            if (itemTooltipAddr != IntPtr.Zero)
            {
                // Get hook from address
                _itemTooltipHook = gameInterop.HookFromAddress<ItemTooltipDelegate>(itemTooltipAddr, ItemTooltipDetour);

                // Enable hook
                _itemTooltipHook.Enable();

                // Set enabled flag
                isEnabled = true;

                // Log
                log.Debug($"{Class} - Item tooltip hook enabled at 0x{itemTooltipAddr:X}");
            }
            else
            {
                log.Error($"{Class} - Item tooltip signature not found");
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to enable item tooltip hook");
        }
    }

    // ----------------------------
    // On language swap
    // ----------------------------
    protected override void OnLanguageSwap()
    {
        // Clear bytes cache
        // TODO
        // _bytesCache.Clear();

        // Refresh item detail addon
        try
        {
            // Get pointer to ItemDetail addon
            AtkUnitBasePtr itemDetailPtr = gameGui.GetAddonByName(ItemDetailAddon);
            if (!itemDetailPtr.IsNull)
            {
                // Get AtkUnitBase from pointer
                AtkUnitBase* itemDetail = (AtkUnitBase*)itemDetailPtr.Address;

                // Only refresh if the addon is currently visible
                if (itemDetail != null && itemDetail -> IsVisible)
                {
                    itemDetail -> Hide(true, false, 0);
                    itemDetail -> Show(true, 0);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to refresh item detail addon");
        }
    }

    // ----------------------------
    // On item tooltip generation
    // ----------------------------
    private void* ItemTooltipDetour(AtkUnitBase* itemDetailAddon, NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
    {
        try
        {
            // Log the structure of StringArrayData for debugging
            // utilities.LogSADStructure(stringArrayData);

            // IDs
            uint itemId = 0;
            uint glamourId = 0;

            // HQ flags
            bool isHighQuality = false;
            bool isHighQualityGlamour = false;

            // Translated texts
            string translatedItemName = string.Empty;
            string translatedGlamourName = string.Empty;
            string translatedItemDescription = string.Empty;
            string translatedEffects = string.Empty;
            List<string> translatedBonuses = [];
            List<string> translatedMateriaNames = [];
            List<string> translatedMateriaStats = [];

            // Check if language is swapped
            if (isLanguageSwapped && stringArrayData != null)
            {
                // Get client language
                LanguageEnum clientLang = (LanguageEnum)config.ClientLanguage;

                // Get target language
                LanguageEnum targetLang = (LanguageEnum)config.TargetLanguage;

                // Get item name
                string itemName = utilities.ReadStringFromArrayData(stringArrayData, ItemNameField);
                if (!itemName.IsNullOrEmpty())
                {
                    // Check for high quality symbol in item name
                    isHighQuality = utilities.IsHighQuality(itemName);

                    // Remove it temporarily for ID lookup
                    if (isHighQuality) itemName = utilities.UnsetHighQuality(itemName);

                    // Get item ID
                    itemId = translationCache.GetItemIdByName(itemName, clientLang) ?? 0;
                    if (itemId > 0 && itemId <= config.MaxValidItemId)
                    {
                        // Translate item name
                        translatedItemName = TranslateItemName(itemId, isHighQuality, targetLang);

                        // Get glamour name
                        string glamourName = utilities.ReadStringFromArrayData(stringArrayData, GlamourNameField);
                        if (!glamourName.IsNullOrEmpty())
                        {
                            // Check for high quality symbol in glamour name
                            isHighQualityGlamour = utilities.IsHighQuality(glamourName);

                            // Remove symbols temporarily for ID lookup
                            glamourName = utilities.UnsetGlamour(glamourName);
                            if (isHighQualityGlamour) glamourName = utilities.UnsetHighQuality(glamourName);

                            // Get glamour ID
                            glamourId = translationCache.GetItemIdByName(glamourName, clientLang) ?? 0;
                            if (glamourId > 0 && glamourId <= config.MaxValidItemId)
                            {
                                // Translate glamour name
                                translatedGlamourName = TranslateGlamourName(glamourId, isHighQualityGlamour, targetLang);
                            }
                        }

                        // Translate description
                        translatedItemDescription = TranslateDescription(itemId, targetLang);

                        // Get effects
                        string effects = utilities.ReadStringFromArrayData(stringArrayData, ItemEffectsField);

                        // Translate effects
                        translatedEffects = TranslateEffects(effects, clientLang, targetLang);

                        // Translate bonuses
                        for (int i = ItemBonusesStartField; i <= ItemBonusesEndField; i++)
                        {
                            // Get bonus
                            string bonus = utilities.ReadStringFromArrayData(stringArrayData, i);

                            // Translate bonus
                            translatedBonuses.Add(TranslateBonus(bonus, clientLang, targetLang));
                        }

                        // Translate materia names
                        for (int j = ItemMateriaNameStartField; j <= ItemMateriaNameEndField; j++)
                        {
                            // Get materia name
                            string materiaName = utilities.ReadStringFromArrayData(stringArrayData, j);

                            // Get materia ID
                            uint materiaId = translationCache.GetItemIdByName(materiaName, clientLang) ?? 0;
                            if (materiaId > 0 && materiaId <= config.MaxValidItemId)
                            {
                                // Translate materia name
                                string translatedMateriaName = TranslateMateriaName(materiaId, targetLang);

                                // Add to list
                                translatedMateriaNames.Add(translatedMateriaName);
                            }
                            else
                            {
                                // No valid materia ID found
                                translatedMateriaNames.Add(string.Empty);
                            }
                        }

                        // Translate materia stats
                        for (int k = ItemMateriaStatStartField; k <= ItemMateriaStatEndField; k++)
                        {
                            // Get materia stat
                            string materiaStat = utilities.ReadStringFromArrayData(stringArrayData, k);

                            // Translate materia stat
                            string translatedMateriaStat = TranslateMateriaStat(materiaStat, clientLang, targetLang);

                            // Add to list
                            translatedMateriaStats.Add(translatedMateriaStat);
                        }

                        // Modify the StringArrayData with translated texts
                        // TODO
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Exception in ItemTooltipDetour");
        }

        // Call original to generate the tooltip
        return _itemTooltipHook!.Original(itemDetailAddon, numberArrayData, stringArrayData);
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

            // Join translated lines back together
            translatedEffects = string.Join("\n", translatedLines);
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
        // Translate using item name translation
        string translatedMateriaName = translationCache.GetItemName(materiaId, targetLang) ?? string.Empty;

        // Return translated materia name
        return translatedMateriaName;
    }

    // ----------------------------
    // Translate materia stat
    // ----------------------------
    private string TranslateMateriaStat(string materiaStat, LanguageEnum clientLang, LanguageEnum targetLang)
    {
        // Initialize translated bonus
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
            // Get stat name and the rest of the line
            string statName = stat[..plusIndex].Trim();
            string valuePart = stat[plusIndex..];

            // Translate stat name
            string translatedStatName = translationCache.GetBaseParamName(statName, clientLang, targetLang) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(translatedStatName))
            {
                // Reconstruct the translated line
                translatedStat = $"{translatedStatName} {valuePart}";
            }
        }

        // Return translated stat
        return translatedStat;
    }

    // ----------------------------
    // Disable the hook
    // ----------------------------
    public override void Disable()
    {
        // Prevent multiple disables
        if (!isEnabled) return;

        try
        {
            // Disable item tooltip hook
            _itemTooltipHook?.Disable();

            // Set disabled flag
            isEnabled = false;
            log.Debug($"{Class} - Item tooltip hook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to disable item tooltip hook");
        }
    }

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    public override void Dispose()
    {
        try
        {
            // Clear cache
            // TODO
            // _bytesCache.Clear();

            // Dispose item tooltip hook
            _itemTooltipHook?.Disable();
            _itemTooltipHook?.Dispose();
            _itemTooltipHook = null;

            // Set disabled flag
            isEnabled = false;
            log.Debug($"{Class} - Item tooltip hook disposed");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to dispose item tooltip hook");
        }

        // Finalize
        GC.SuppressFinalize(this);
    }

}