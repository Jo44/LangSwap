using Dalamud.Game.NativeWrapper;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.translation;
using LangSwap.ui.hooks.@base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LangSwap.ui.hooks;

// ----------------------------
// Item Detail Hook
// ----------------------------
public unsafe partial class ItemDetailHook(
    Configuration configuration,
    IGameGui gameGui,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    IPluginLog log) : BaseHook(configuration, gameGui, gameInterop, sigScanner, translationCache, log)
{
    // Generated Regex
    [GeneratedRegex(@"\+\d+")]
    private static partial Regex StatLineRegex();

    // Delegates functions
    private delegate byte ItemHoveredDelegate(IntPtr a1, IntPtr* a2, int* containerId, ushort* slotId, IntPtr a5, uint slotIdInt, IntPtr a7);
    private delegate void* GenerateItemTooltipDelegate(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);

    // Hooks
    private Hook<ItemHoveredDelegate>? itemHoveredHook;
    private Hook<GenerateItemTooltipDelegate>? generateItemTooltipHook;

    // IDs
    private uint currentItemId = 0;
    private uint currentGlamourId = 0;

    // Cache for translated bytes
    private readonly Dictionary<string, byte[]> _translatedBytesCache = [];
    private const int MaxCacheSize = 500;

    // Miscellaneous
    private readonly char GlamouredSymbol = configuration.GlamouredSymbol;
    private readonly char HighQualitySymbol = configuration.HighQualitySymbol;

    // Item Detail Addon
    private readonly string ItemDetailAddonName = configuration.ItemDetailAddonName;
    private readonly int ItemNameField = configuration.ItemNameField;
    private readonly int GlamourNameField = configuration.GlamourNameField;
    private readonly int ItemDescriptionField = configuration.ItemDescriptionField;
    private readonly int ItemEffectsField = configuration.ItemEffectsField;
    private readonly int ItemBonusesStartField = configuration.ItemBonusesStartField;
    private readonly int ItemBonusesEndField = configuration.ItemBonusesEndField;
    private readonly int ItemMateriaNameStartField = configuration.ItemMateriaNameStartField;
    private readonly int ItemMateriaNameEndField = configuration.ItemMateriaNameEndField;
    private readonly int ItemMateriaStatStartField = configuration.ItemMateriaStatStartField;
    private readonly int ItemMateriaStatEndField = configuration.ItemMateriaStatEndField;

    // ----------------------------
    // Enable the hook
    // ----------------------------
    public override void Enable()
    {
        // Prevent multiple enables
        if (isEnabled) return;

        try
        {
            // Hook ItemHovered (-> get current item ID when hovering an item)
            nint itemHoveredAddr = sigScanner.ScanText(configuration.ItemHoveredSig);
            if (itemHoveredAddr != IntPtr.Zero)
            {
                itemHoveredHook = gameInterop.HookFromAddress<ItemHoveredDelegate>(itemHoveredAddr, ItemHoveredDetour);
                itemHoveredHook.Enable();
                log.Debug($"ItemHovered hook enabled at 0x{itemHoveredAddr:X}");
            }
            else
            {
                log.Warning("ItemHovered signature not found");
            }

            // Hook GenerateItemTooltip (-> modify item tooltip datas when generating it)
            nint generateItemTooltipAddr = sigScanner.ScanText(configuration.GenerateItemTooltipSig);
            if (generateItemTooltipAddr != IntPtr.Zero)
            {
                generateItemTooltipHook = gameInterop.HookFromAddress<GenerateItemTooltipDelegate>(generateItemTooltipAddr, GenerateItemTooltipDetour);
                generateItemTooltipHook.Enable();
                log.Debug($"GenerateItemTooltip hook enabled at 0x{generateItemTooltipAddr:X}");
            }
            else
            {
                log.Warning("GenerateItemTooltip signature not found");
            }

            // Set enabled flag
            isEnabled = true;
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to enable ItemDetail hooks");
        }
    }

    // ----------------------------
    // Swap to target language
    // ----------------------------
    protected override void OnLanguageSwapped()
    {
        // Refresh item detail to apply translations
        _translatedBytesCache.Clear();
        RefreshItemDetail();
    }

    // ----------------------------
    // Restore to original language
    // ----------------------------
    protected override void OnLanguageRestored()
    {
        // Clear current item and glamour IDs
        currentItemId = 0;
        currentGlamourId = 0;

        // Refresh item detail to restore original texts
        _translatedBytesCache.Clear();
        RefreshItemDetail();
    }

    // ----------------------------
    // Refresh the ItemDetail addon
    // ----------------------------
    private void RefreshItemDetail()
    {
        try
        {
            // Get pointer to ItemDetail addon
            AtkUnitBasePtr itemDetailPtr = gameGui.GetAddonByName(ItemDetailAddonName);
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
            log.Error(ex, "Failed to refresh ItemDetail");
        }
    }

    // ----------------------------
    // On Item Hovered
    // -> Get current item ID when hovering an item
    // ----------------------------
    private byte ItemHoveredDetour(IntPtr a1, IntPtr* a2, int* containerId, ushort* slotId, IntPtr a5, uint slotIdInt, IntPtr a7)
    {
        // Call original first to ensure item ID is set correctly
        byte returnValue = itemHoveredHook!.Original(a1, a2, containerId, slotId, a5, slotIdInt, a7);
        
        try
        {
            // Read InventoryItem from a7 to get current ID
            InventoryItem inventoryItem = *(InventoryItem*)a7;

            // Get current item ID from InventoryItem struct
            currentItemId = inventoryItem.ItemId;

            try
            {
                // Get current glamour ID from InventoryItem struct
                currentGlamourId = inventoryItem.GlamourId;
            }
            catch
            {
                currentGlamourId = 0;
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, "Exception in ItemHovered detour");
        }

        // Return original value
        return returnValue;
    }

    // ----------------------------
    // On Generate Item Tooltip
    // -> Modify item tooltip datas when generating it
    // ----------------------------
    private void* GenerateItemTooltipDetour(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
    {
        try
        {
            // Log the structure of StringArrayData for debugging
            // LogSADStructure(stringArrayData);

            // Modify texts in StringArrayData
            if (isLanguageSwapped && currentItemId > 0 && currentItemId < configuration.MaxValidItemId && stringArrayData != null)
            {
                // Get target language
                LanguageEnum lang = (LanguageEnum)configuration.TargetLanguage;

                // Translate item name
                string? translatedName = translationCache.GetItemName(currentItemId, lang);
                if (!string.IsNullOrWhiteSpace(translatedName))
                {
                    ReplaceText(stringArrayData, ItemNameField, translatedName);
                }

                // Translate glamour name
                if (currentGlamourId > 0 && currentGlamourId < configuration.MaxValidItemId)
                {
                    string? translatedGlamour = translationCache.GetItemName(currentGlamourId, lang);
                    if (!string.IsNullOrWhiteSpace(translatedGlamour))
                    {
                        ReplaceText(stringArrayData, GlamourNameField, translatedGlamour);
                    }
                }

                // Translate description
                string? translatedDescription = translationCache.GetItemDescription(currentItemId, lang);
                if (!string.IsNullOrWhiteSpace(translatedDescription))
                {
                    ReplaceText(stringArrayData, ItemDescriptionField, translatedDescription);
                }

                // Translate effects
                string? effects = ReadStringFromArray(stringArrayData, ItemEffectsField);
                if (!string.IsNullOrWhiteSpace(effects))
                {
                    string translatedEffects = TranslateEffects(effects, lang);
                    ReplaceText(stringArrayData, ItemEffectsField, translatedEffects);
                }

                // Translate bonuses
                for (int i = ItemBonusesStartField; i <= ItemBonusesEndField; i++)
                {
                    string bonus = ReadStringFromArray(stringArrayData, i);

                    // -> Only translate stat line
                    if (!string.IsNullOrWhiteSpace(bonus) && StatLineRegex().IsMatch(bonus))
                    {
                        string translatedBonus = TranslateStat(bonus, lang);
                        ReplaceText(stringArrayData, i, translatedBonus);
                    }
                }

                // Translate materia names
                for (int j = ItemMateriaNameStartField; j <= ItemMateriaNameEndField; j++)
                {
                    string materia = ReadStringFromArray(stringArrayData, j);
                    if (!string.IsNullOrWhiteSpace(materia))
                    {
                        string translatedMateria = TranslateMateria(materia, lang);
                        ReplaceText(stringArrayData, j, translatedMateria);
                    }
                }

                // Translate materia stats
                for (int k = ItemMateriaStatStartField; k <= ItemMateriaStatEndField; k++)
                {
                    string materia = ReadStringFromArray(stringArrayData, k);
                    
                    // -> Only translate stat line
                    if (!string.IsNullOrWhiteSpace(materia) && StatLineRegex().IsMatch(materia))
                    {
                        string translatedMateria = TranslateStat(materia, lang);
                        ReplaceText(stringArrayData, k, translatedMateria);

                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception in GenerateItemTooltip before original for item {currentItemId}");
        }

        // Call original to generate the tooltip
        return generateItemTooltipHook!.Original(addonItemDetail, numberArrayData, stringArrayData);
    }

    // ----------------------------
    // Read string from StringArrayData at specified index
    // ----------------------------
    private string ReadStringFromArray(StringArrayData* stringArrayData, int index)
    {
        try
        {
            // Check for null pointer and valid index
            if (stringArrayData == null || index >= stringArrayData -> AtkArrayData.Size)
                return string.Empty;

            // Get memory address of the string
            nint address = new(stringArrayData -> StringArray[index]);
            if (address == IntPtr.Zero)
                return string.Empty;

            // Read SeString from memory and convert to string
            SeString seString = MemoryHelper.ReadSeStringNullTerminated(address);
            return seString?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to read string at index {index}");
            return string.Empty;
        }
    }

    // ----------------------------
    // Replace text in SeString
    // ----------------------------
    private void ReplaceText(StringArrayData* stringArrayData, int field, string newText)
    {
        try
        {
            // Check for null pointer and valid field index
            if (stringArrayData == null || field >= stringArrayData -> AtkArrayData.Size)
                return;

            // Get memory address of existing string
            nint address = new(stringArrayData -> StringArray[field]);
            if (address == IntPtr.Zero)
            {
                // Set plain text if no existing string
                SafeSetString(stringArrayData, field, newText);
                return;
            }

            // Get existing SeString from memory address
            SeString existingSeString = MemoryHelper.ReadSeStringNullTerminated(address);
            if (existingSeString == null)
            {
                // Set plain text if failed to read existing string
                SafeSetString(stringArrayData, field, newText);
                return;
            }

            // If there's existing formatting, preserve it
            if (existingSeString.Payloads.Count > 1)
            {
                // Build new SeString with translated text while preserving formatting
                byte[] encoded = BuildSeStringWithTranslation(existingSeString, newText);
                
                // Set the new encoded SeString value in StringArrayData
                stringArrayData -> SetValue(field, encoded, false, true, false);
            }
            else
            {
                // No complex formatting, set plain text
                SafeSetString(stringArrayData, field, newText);
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to replace text with formatting at field {field}");
            SafeSetString(stringArrayData, field, newText);
        }
    }

    // ----------------------------
    // Safely set a string in StringArrayData
    // ----------------------------
    private void SafeSetString(StringArrayData* stringArrayData, int field, string text)
    {
        try
        {
            // Check for null pointer
            if (stringArrayData == null)
                return;

            // Check field index is within bounds
            if (field >= stringArrayData -> AtkArrayData.Size)
                return;

            // Check for empty or null text
            if (string.IsNullOrEmpty(text))
                return;

            // Check cache first to avoid unnecessary encoding
            if (!_translatedBytesCache.TryGetValue(text, out byte[]? bytes))
            {
                // Get bytes of text
                bytes = Encoding.UTF8.GetBytes(text + "\0");

                // Check for excessively long text (10KB limit)
                if (bytes.Length > 1024 * 10)
                    return;

                // Add to cache if size limit not exceeded
                if (_translatedBytesCache.Count < MaxCacheSize)
                {
                    _translatedBytesCache[text] = bytes;
                }
                else
                {
                    // Clear cache before if limit exceeded
                    _translatedBytesCache.Clear();
                    _translatedBytesCache[text] = bytes;
                }
            }

            // Set the string value in StringArrayData
            stringArrayData -> SetValue(field, bytes, false, true, false);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to safely set string at field {field}");
        }
    }

    // ----------------------------
    // Build SeString with translated text preserving formatting
    // ----------------------------
    private byte[] BuildSeStringWithTranslation(SeString existingSeString, string newText)
    {
        // Prepare a new builder for the SeString
        SeStringBuilder builder = new();
        bool textReplaced = false;

        // Replace the first TextPayload found
        foreach (Payload payload in existingSeString.Payloads)
        {
            if (!textReplaced && payload is TextPayload textPayload)
            {
                // Extract special symbols from original text
                string originalText = textPayload.Text ?? "";
                string translatedTextWithSymbols = PreserveSpecialSymbols(originalText, newText);
                        
                // Replace first TextPayload with translated text + symbols
                builder.AddText(translatedTextWithSymbols);

                // Flag to indicate text has been replaced
                textReplaced = true;
            }
            else
            {
                // Clean other payloads of any text
                if (payload is TextPayload otherTextPayload)
                {
                    otherTextPayload.Text = "";
                }
                // Keep them in exact order
                builder.Add(payload);
            }
        }

        // Return the built SeString
        return builder.Build().Encode();
    }

    // ----------------------------
    // Preserve special symbols (Glamoured, HQ)
    // ----------------------------
    private string PreserveSpecialSymbols(string originalText, string translatedText)
    {
        // Prepare result
        StringBuilder result = new(translatedText.Trim());

        // Check for Glamoured symbol in original text
        if (originalText.Contains(GlamouredSymbol))
        {
            // Insert Glamoured symbol in translated text
            result.Insert(0, $"{GlamouredSymbol} ");
        }

        // Check for HQ symbol in original text
        if (originalText.Contains(HighQualitySymbol))
        {
            // Insert HQ symbol in translated text
            result.Append($" {HighQualitySymbol}");
        }

        // Return final text with preserved symbols
        return result.ToString();
    }

    // ----------------------------
    // Remove special symbols (Glamoured, HQ)
    // ----------------------------
    private string RemoveSpecialSymbols(string text)
    {
        // Remove Glamoured and HQ symbols from text
        return text.Replace(GlamouredSymbol.ToString(), "").Replace(HighQualitySymbol.ToString(), "").Trim();
    }

    // ----------------------------
    // Translate effects
    // ----------------------------
    private string TranslateEffects(string effects, LanguageEnum targetLang)
    {
        // No effects
        if (string.IsNullOrWhiteSpace(effects))
            return effects;

        try
        {
            // Split effects by line breaks
            string[] lines = effects.Split('\n');

            // Translate each line individually
            string[] translatedLines = [.. lines.Select(line => 
                string.IsNullOrWhiteSpace(line) ? line : TranslateStat(line, targetLang)
            )];

            // Join translated lines back together
            return string.Join("\n", translatedLines);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to translate effects");
            return effects;
        }
    }

    // ----------------------------
    // Translate stat name
    // ----------------------------
    private string TranslateStat(string stat, LanguageEnum targetLang)
    {
        try
        {
            // Extract stat name from line
            int plusIndex = stat.IndexOf('+');

            // No stat format found
            if (plusIndex <= 0)
                return stat;

            // Get stat name and the rest of the line
            string statName = stat[..plusIndex].Trim();
            string valuePart = stat[plusIndex..];

            // Get client language
            LanguageEnum clientLang = (LanguageEnum)configuration.ClientLanguage;

            // Translate stat name from client language to target language
            string? translatedStat = translationCache.GetBaseParamName(statName, clientLang, targetLang);

            // Return translated stat name with value
            return !string.IsNullOrWhiteSpace(translatedStat) 
                ? $"{translatedStat} {valuePart}" 
                : stat;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to translate stat name: {stat}");
            return stat;
        }
    }

    // ----------------------------
    // Translate materia name
    // ----------------------------
    private string TranslateMateria(string materiaName, LanguageEnum targetLang)
    {
        try
        {
            // Get client language
            LanguageEnum clientLang = (LanguageEnum)configuration.ClientLanguage;

            // Try to get materia ID from name
            uint? materiaId = translationCache.GetItemIdByName(materiaName, clientLang);

            if (materiaId.HasValue && materiaId.Value > 0 && materiaId.Value < configuration.MaxValidItemId)
            {
                // Translate using item name translation
                string? translatedMateria = translationCache.GetItemName(materiaId.Value, targetLang);
                if (!string.IsNullOrWhiteSpace(translatedMateria))
                    return translatedMateria;
            }

            // If no translation found, return original name
            return materiaName;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to translate materia name: {materiaName}");
            return materiaName;
        }
    }

    // ----------------------------
    // Log the structure of StringArrayData for debugging
    // ----------------------------
    private void LogSADStructure(StringArrayData* stringArrayData)
    {
        if (stringArrayData != null)
        {
            log.Debug("=== StringArrayData Structure ===");
            log.Debug($"Total Size: {stringArrayData -> AtkArrayData.Size}");

            // Log each field with its content
            for (int i = 0; i < stringArrayData -> AtkArrayData.Size; i++)
            {
                // Read the string at this index
                string text = ReadStringFromArray(stringArrayData, i);

                // Log all non-empty fields
                if (!string.IsNullOrWhiteSpace(text))
                {
                    // Truncate long text for readability
                    string displayText = text.Length > 100 ? text[..100] + "..." : text;

                    // Replace line breaks for compact display
                    displayText = displayText.Replace("\n", " | ");

                    log.Debug($"[{i,2}] {displayText}");
                }
            }

            log.Debug("=== End of StringArrayData ===");
        }
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
            // Disable ItemHovered hook
            itemHoveredHook?.Disable();

            // Disable GenerateItemTooltip hook
            generateItemTooltipHook?.Disable();

            // Set disabled flag
            isEnabled = false;
            log.Debug("ItemDetail hooks disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to disable ItemDetail hooks");
        }
    }

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    public override void Dispose()
    {
        try
        {
            // Dispose ItemHovered hook
            itemHoveredHook?.Disable();
            itemHoveredHook?.Dispose();
            itemHoveredHook = null;

            // Dispose GenerateItemTooltip hook
            generateItemTooltipHook?.Disable();
            generateItemTooltipHook?.Dispose();
            generateItemTooltipHook = null;

            // Clear cache
            _translatedBytesCache.Clear();

            // Set disabled flag
            isEnabled = false;
            log.Debug("ItemDetail hooks disposed");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to dispose ItemDetail hooks");
        }

        // Finalize
        GC.SuppressFinalize(this);
    }
}