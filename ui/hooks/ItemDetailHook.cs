using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.translation;
using LangSwap.ui.hooks.@base;
using System;
using System.Collections.Generic;
using System.Text;
using Dalamud.Game.NativeWrapper;

namespace LangSwap.ui.hooks;

// ----------------------------
// Item Detail Hook
// ----------------------------
public unsafe class ItemDetailHook(
    Configuration configuration,
    IGameGui gameGui,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    IPluginLog log) : BaseHook(configuration, gameGui, gameInterop, sigScanner, translationCache, log)
{
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
            currentItemId = inventoryItem.ItemId;

            // Try to get GlamourId
            try
            {
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
                    string? translatedGlamourName = translationCache.GetItemName(currentGlamourId, lang);
                    if (!string.IsNullOrWhiteSpace(translatedGlamourName))
                    {
                        ReplaceText(stringArrayData, GlamourNameField, translatedGlamourName);
                    }
                }

                // Translate description
                string? translatedDescription = translationCache.GetItemDescription(currentItemId, lang);
                if (!string.IsNullOrWhiteSpace(translatedDescription))
                {
                    ReplaceText(stringArrayData, ItemDescriptionField, translatedDescription);
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
                // Prepare a new SeStringBuilder
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
                        textReplaced = true;
                    }
                    else
                    {
                        // Keep all other payloads in exact order
                        builder.Add(payload);
                    }
                }

                // Encode the modified SeString and set it back to StringArrayData
                byte[] encoded = builder.Build().Encode();
                stringArrayData -> SetValue(field, encoded, false, true, false);
                return;
            }
            
            // No existing formatting, set plain text
            SafeSetString(stringArrayData, field, newText);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to replace text with formatting at field {field}");
            SafeSetString(stringArrayData, field, newText);
        }
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