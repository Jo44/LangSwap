using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.translation;
using System;
using System.Text;

namespace LangSwap.ui.hooks;

/// <summary>
/// Hook for translating ItemDetail component (item tooltips)
/// </summary>
public unsafe class ItemDetailHook : BaseHook
{
    private delegate void* GenerateItemTooltipDelegate(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);
    private delegate byte ItemHoveredDelegate(IntPtr a1, IntPtr* a2, int* containerId, ushort* slotId, IntPtr a5, uint slotIdInt, IntPtr a7);

    private Hook<GenerateItemTooltipDelegate>? generateItemTooltipHook;
    private Hook<ItemHoveredDelegate>? itemHoveredHook;

    private uint currentItemId = 0;
    private uint currentGlamourId = 0;

    private const int ItemNameField = 0;
    private const int GlamourNameField = 1;
    private const int ItemDescriptionField = 13;
    
    private const string ItemDetailAddonName = "ItemDetail";

    // Special Unicode characters for item symbols
    private const char GlamouredSymbol = '\uE03B'; // Mirage symbol
    private const char HighQualitySymbol = '\uE03C'; // HQ symbol

    private readonly IGameGui gameGui;

    public ItemDetailHook(
        Configuration configuration,
        IGameInteropProvider gameInterop,
        ISigScanner sigScanner,
        TranslationCache translationCache,
        IPluginLog log,
        IGameGui gameGui)
        : base(configuration, gameInterop, sigScanner, translationCache, log)
    {
        this.gameGui = gameGui;
    }

    public override void Enable()
    {
        if (isEnabled) return;

        try
        {
            // Hook ItemHovered
            var itemHoveredAddr = sigScanner.ScanText(configuration.ItemHoveredSig);
            if (itemHoveredAddr != IntPtr.Zero)
            {
                itemHoveredHook = gameInterop.HookFromAddress<ItemHoveredDelegate>(itemHoveredAddr, ItemHoveredDetour);
                itemHoveredHook.Enable();
                log.Information($"ItemHovered hook enabled at 0x{itemHoveredAddr:X}");
            }
            else
            {
                log.Warning("ItemHovered signature not found");
            }

            // Hook GenerateItemTooltip
            var generateItemTooltipAddr = sigScanner.ScanText(configuration.GenerateItemTooltipSig);
            if (generateItemTooltipAddr != IntPtr.Zero)
            {
                generateItemTooltipHook = gameInterop.HookFromAddress<GenerateItemTooltipDelegate>(generateItemTooltipAddr, GenerateItemTooltipDetour);
                generateItemTooltipHook.Enable();
                log.Information($"GenerateItemTooltip hook enabled at 0x{generateItemTooltipAddr:X}");
            }
            else
            {
                log.Warning("GenerateItemTooltip signature not found");
            }

            isEnabled = true;
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to enable ItemDetailHook");
        }
    }

    protected override void OnLanguageSwapped()
    {
        RefreshItemDetail();
    }

    protected override void OnLanguageRestored()
    {
        currentItemId = 0;
        currentGlamourId = 0;
        RefreshItemDetail();
    }

    private void RefreshItemDetail()
    {
        try
        {
            var itemDetailPtr = gameGui.GetAddonByName(ItemDetailAddonName);
            if (!itemDetailPtr.IsNull)
            {
                var itemDetail = (AtkUnitBase*)itemDetailPtr.Address;
                if (itemDetail != null && itemDetail->IsVisible)
                {
                    log.Debug("Refreshing ItemDetail");
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

    private byte ItemHoveredDetour(IntPtr a1, IntPtr* a2, int* containerId, ushort* slotId, IntPtr a5, uint slotIdInt, IntPtr a7)
    {
        var returnValue = itemHoveredHook!.Original(a1, a2, containerId, slotId, a5, slotIdInt, a7);
        
        try
        {
            var inventoryItem = *(InventoryItem*)a7;
            currentItemId = inventoryItem.ItemId;
            
            try
            {
                currentGlamourId = inventoryItem.GlamourId;
            }
            catch
            {
                currentGlamourId = 0;
            }
            
            log.Debug($"ItemHovered: ItemId={currentItemId}, GlamourId={currentGlamourId}");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Exception in ItemHovered detour");
        }
        
        return returnValue;
    }

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
    /// Safely set a string in StringArrayData with bounds checking
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

            var bytes = System.Text.Encoding.UTF8.GetBytes(text + "\0");
            
            if (bytes.Length > 1024 * 10) // 10KB limit
                return;

            stringArrayData->SetValue(field, bytes, false, true, false);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to safely set string at field {field}");
        }
    }

    public override void Disable()
    {
        if (!isEnabled) return;

        try
        {
            itemHoveredHook?.Disable();
            generateItemTooltipHook?.Disable();
            isEnabled = false;
            log.Information("ItemDetailHook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to disable ItemDetailHook");
        }
    }

    public override void Dispose()
    {
        Disable();
        itemHoveredHook?.Dispose();
        generateItemTooltipHook?.Dispose();
    }
}