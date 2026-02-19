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
    IPluginLog log) : BaseHook(configuration, gameInterop, sigScanner, translationCache, log)
{
    // TODO : on tri à partir d'ici :)
    private delegate byte ItemHoveredDelegate(IntPtr a1, IntPtr* a2, int* containerId, ushort* slotId, IntPtr a5, uint slotIdInt, IntPtr a7);
    private delegate void* GenerateItemTooltipDelegate(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);

    private Hook<GenerateItemTooltipDelegate>? generateItemTooltipHook;
    private Hook<ItemHoveredDelegate>? itemHoveredHook;

    private uint currentItemId = 0;
    private uint currentGlamourId = 0;

    private readonly Dictionary<string, byte[]> _translatedBytesCache = [];
    private const int MaxCacheSize = 500;

    private readonly int ItemNameField = configuration.ItemNameField;
    private readonly int GlamourNameField = configuration.GlamourNameField;
    private readonly int ItemDescriptionField = configuration.ItemDescriptionField;
    
    private readonly string ItemDetailAddonName = configuration.ItemDetailAddonName;

    private readonly char GlamouredSymbol = configuration.GlamouredSymbol;
    private readonly char HighQualitySymbol = configuration.HighQualitySymbol;

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
            var itemHoveredAddr = sigScanner.ScanText(configuration.ItemHoveredSig);
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
            var generateItemTooltipAddr = sigScanner.ScanText(configuration.GenerateItemTooltipSig);
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
            log.Error(ex, "Failed to enable ItemDetail Hook");
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

        // Refresh item detail to apply translations
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
            var itemDetailPtr = gameGui.GetAddonByName(ItemDetailAddonName);
            if (!itemDetailPtr.IsNull)
            {
                // Get AtkUnitBase from pointer
                AtkUnitBase* itemDetail = (AtkUnitBase*)itemDetailPtr.Address;
                
                // Only refresh if the addon is currently visible
                if (itemDetail != null && itemDetail->IsVisible)
                {
                    itemDetail->Hide(true, false, 0);
                    itemDetail->Show(true, 0);
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
        var returnValue = itemHoveredHook!.Original(a1, a2, containerId, slotId, a5, slotIdInt, a7);
        
        try
        {
            // Read InventoryItem from a7 to get current ID
            var inventoryItem = *(InventoryItem*)a7;
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
            // Get item ID if not already set
            if (currentItemId == 0 || currentItemId > configuration.MaxValidItemId)
            {
                if (numberArrayData != null && numberArrayData->AtkArrayData.Size > 0)
                {
                    var potentialItemId = (uint)numberArrayData->IntArray[0];
                    if (potentialItemId > 0 && potentialItemId < configuration.MaxValidItemId)
                    {
                        currentItemId = potentialItemId;
                    }
                }
            }

            // Modify StringArrayData BEFORE calling original
            if (isLanguageSwapped && currentItemId > 0 && currentItemId < configuration.MaxValidItemId && stringArrayData != null)
            {
                var targetLang = (LanguageEnum)configuration.TargetLanguage;
                
                // Translate item name (preserving formatting and symbols)
                var translatedName = translationCache.GetItemName(currentItemId, targetLang);
                if (!string.IsNullOrWhiteSpace(translatedName))
                {
                    ReplaceTextPreserveFormatting(stringArrayData, ItemNameField, translatedName);
                }

                // Translate glamour name (preserving formatting and symbols)
                if (currentGlamourId > 0 && currentGlamourId < configuration.MaxValidItemId)
                {
                    var translatedGlamourName = translationCache.GetItemName(currentGlamourId, targetLang);
                    if (!string.IsNullOrWhiteSpace(translatedGlamourName))
                    {
                        ReplaceTextPreserveFormatting(stringArrayData, GlamourNameField, translatedGlamourName);
                    }
                }

                // Translate description (simple text)
                var translatedDescription = translationCache.GetItemDescription(currentItemId, targetLang);
                if (!string.IsNullOrWhiteSpace(translatedDescription))
                {
                    SafeSetString(stringArrayData, ItemDescriptionField, translatedDescription);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception in GenerateItemTooltip BEFORE original for item {currentItemId}");
        }

        // Call original to generate the tooltip
        return generateItemTooltipHook!.Original(addonItemDetail, numberArrayData, stringArrayData);
    }

    /// <summary>
    /// Replace text in SeString while preserving formatting and special symbols
    /// </summary>
    private void ReplaceTextPreserveFormatting(StringArrayData* stringArrayData, int field, string newText)
    {
        try
        {
            if (stringArrayData == null || field >= stringArrayData->AtkArrayData.Size)
                return;

            // Read existing SeString
            var address = new IntPtr(stringArrayData->StringArray[field]);
            if (address == IntPtr.Zero)
            {
                SafeSetString(stringArrayData, field, newText);
                return;
            }

            var existingSeString = MemoryHelper.ReadSeStringNullTerminated(address);
            if (existingSeString == null)
            {
                SafeSetString(stringArrayData, field, newText);
                return;
            }

            // If there's existing formatting, preserve it
            if (existingSeString.Payloads.Count > 1)
            {
                var builder = new SeStringBuilder();
                bool textReplaced = false;
                
                // Keep EXACT order, just replace the FIRST TextPayload
                foreach (var payload in existingSeString.Payloads)
                {
                    if (!textReplaced && payload is TextPayload textPayload)
                    {
                        // Extract special symbols from original text
                        var originalText = textPayload.Text ?? "";
                        var translatedTextWithSymbols = PreserveSpecialSymbols(originalText, newText);
                        
                        // Replace first TextPayload with translated text + symbols
                        builder.AddText(translatedTextWithSymbols);
                        textReplaced = true;
                    }
                    else
                    {
                        // Keep ALL other payloads in EXACT order
                        builder.Add(payload);
                    }
                }
                
                var encoded = builder.Build().Encode();
                stringArrayData->SetValue(field, encoded, false, true, false);
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

    /// <summary>
    /// Preserve special symbols (Glamoured, HQ) from original text and add them to translated text
    /// </summary>
    private string PreserveSpecialSymbols(string originalText, string translatedText)
    {
        var result = new StringBuilder(translatedText.Trim());
        
        // Check for Glamoured symbol (usually at the beginning)
        if (originalText.Contains(GlamouredSymbol))
        {
            result.Insert(0, $"{GlamouredSymbol} ");
        }
        
        // Check for HQ symbol (usually at the end)
        if (originalText.Contains(HighQualitySymbol))
        {
            result.Append($" {HighQualitySymbol}");
        }
        
        return result.ToString();
    }

    /// <summary>
    /// Safely set a string in StringArrayData with bounds checking and caching
    /// </summary>
    private void SafeSetString(StringArrayData* stringArrayData, int field, string text)
    {
        try
        {
            if (stringArrayData == null)
                return;

            if (field >= stringArrayData->AtkArrayData.Size)
                return;

            if (string.IsNullOrEmpty(text))
                return;

            // Vérifier le cache
            if (!_translatedBytesCache.TryGetValue(text, out var bytes))
            {
                // Pas dans le cache, créer les bytes
                bytes = System.Text.Encoding.UTF8.GetBytes(text + "\0");
                
                if (bytes.Length > 1024 * 10) // 10KB limit
                    return;

                // Ajouter au cache si la taille n'est pas dépassée
                if (_translatedBytesCache.Count < MaxCacheSize)
                {
                    _translatedBytesCache[text] = bytes;
                }
                else
                {
                    // Cache plein, le vider et recommencer
                    _translatedBytesCache.Clear();
                    _translatedBytesCache[text] = bytes;
                    log.Debug("Translation bytes cache cleared due to size limit");
                }
            }

            stringArrayData->SetValue(field, bytes, false, true, false);
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