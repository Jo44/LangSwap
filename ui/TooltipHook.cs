using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.translation;
using System;

namespace LangSwap.ui;

/// <summary>
/// Hook for intercepting and modifying item tooltips based on SimpleTweaksPlugin approach.
/// </summary>
public unsafe class TooltipHook(
    Configuration configuration,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    IPluginLog log) : IDisposable
{
    // References
    private readonly Configuration configuration = configuration;
    private readonly IGameInteropProvider gameInterop = gameInterop;
    private readonly ISigScanner sigScanner = sigScanner;
    private readonly TranslationCache translationCache = translationCache;
    private readonly IPluginLog log = log;

    // Delegates
    private delegate void* GenerateItemTooltipDelegate(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);
    private delegate byte ItemHoveredDelegate(IntPtr a1, IntPtr* a2, int* containerId, ushort* slotId, IntPtr a5, uint slotIdInt, IntPtr a7);

    // Hooks
    private Hook<GenerateItemTooltipDelegate>? generateItemTooltipHook;
    private Hook<ItemHoveredDelegate>? itemHoveredHook;

    // State
    private bool isEnabled = false;
    private bool isLanguageSwapped = false;
    private uint currentItemId = 0;
    private uint currentGlamourId = 0;

    // Item tooltip field indices (from SimpleTweaksPlugin)
    private const int ItemNameField = 0;
    private const int GlamourNameField = 1;
    private const int ItemDescriptionField = 13;

    /// <summary>
    /// Enable tooltip hooks
    /// </summary>
    public void Enable()
    {
        if (isEnabled) return;

        try
        {
            // Hook 1: ItemHovered - captures the actual item being hovered
            var itemHoveredSig = configuration.ItemHoveredSig;
            var itemHoveredAddr = sigScanner.ScanText(itemHoveredSig);
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

            // Hook 2: GenerateItemTooltip - modifies tooltip content
            var generateTooltipSig = configuration.GenerateTooltipSig;
            var generateTooltipAddr = sigScanner.ScanText(generateTooltipSig);
            if (generateTooltipAddr != IntPtr.Zero)
            {
                generateItemTooltipHook = gameInterop.HookFromAddress<GenerateItemTooltipDelegate>(generateTooltipAddr, GenerateItemTooltipDetour);
                generateItemTooltipHook.Enable();
                log.Information($"GenerateItemTooltip hook enabled at 0x{generateTooltipAddr:X}");
            }
            else
            {
                log.Warning("GenerateItemTooltip signature not found");
            }

            isEnabled = true;
            log.Information("Tooltip hooks setup completed");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to enable tooltip hooks");
            isEnabled = false;
        }
    }

    /// <summary>
    /// Swap to target language
    /// </summary>
    public void SwapLanguage()
    {
        if (isLanguageSwapped)
        {
            log.Debug("Language already swapped, ignoring");
            return;
        }
        
        isLanguageSwapped = true;
        log.Information("Tooltip language swap enabled");
    }

    /// <summary>
    /// Restore to original language
    /// </summary>
    public void RestoreLanguage()
    {
        if (!isLanguageSwapped)
        {
            log.Debug("Language not swapped, ignoring restore");
            return;
        }
        
        isLanguageSwapped = false;
        currentItemId = 0;
        currentGlamourId = 0;
        log.Information("Tooltip language swap disabled");
    }

    /// <summary>
    /// Detour for ItemHovered - captures the item ID and glamour ID from inventory
    /// </summary>
    private byte ItemHoveredDetour(IntPtr a1, IntPtr* a2, int* containerId, ushort* slotId, IntPtr a5, uint slotIdInt, IntPtr a7)
    {
        var returnValue = itemHoveredHook!.Original(a1, a2, containerId, slotId, a5, slotIdInt, a7);
        
        try
        {
            // Extract item from inventory data (a7 contains InventoryItem struct)
            var inventoryItem = *(InventoryItem*)a7;
            currentItemId = inventoryItem.ItemId;
            
            // Extract glamour ID (may be named GlamourId, GlamourItemId, or similar)
            // Try to access the glamour field - adjust property name if needed
            try
            {
                currentGlamourId = inventoryItem.GlamourId;
            }
            catch
            {
                // If GlamourId property doesn't exist, try alternative names or set to 0
                currentGlamourId = 0;
            }
            
            log.Debug($"ItemHovered: ItemId={currentItemId}, GlamourId={currentGlamourId}, Container={*containerId}, Slot={*slotId}");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Exception in ItemHovered detour");
        }
        
        return returnValue;
    }

    /// <summary>
    /// Detour for GenerateItemTooltip - modifies tooltip strings
    /// </summary>
    private void* GenerateItemTooltipDetour(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
    {
        try
        {
            // If we don't have an item ID from ItemHovered, try to extract from NumberArrayData
            if (currentItemId == 0 || currentItemId > configuration.MaxValidItemId)
            {
                // NumberArrayData typically contains item ID at index 0 or 1
                if (numberArrayData != null && numberArrayData->AtkArrayData.Size > 0)
                {
                    var potentialItemId = (uint)numberArrayData->IntArray[0];
                    if (potentialItemId > 0 && potentialItemId < configuration.MaxValidItemId)
                    {
                        currentItemId = potentialItemId;
                        log.Debug($"Extracted ItemId from NumberArrayData: {currentItemId}");
                    }
                }
            }

            log.Verbose($"GenerateItemTooltip - isSwapped={isLanguageSwapped}, itemId={currentItemId}, glamourId={currentGlamourId}");

            // If language is swapped and we have a valid item ID, translate
            if (isLanguageSwapped && currentItemId > 0 && currentItemId < configuration.MaxValidItemId)
            {
                var targetLang = (LanguageEnum)configuration.TargetLanguage;
                
                // Get current fields for comparison
                var currentName = GetTooltipString(stringArrayData, ItemNameField);
                var currentGlamourName = GetTooltipString(stringArrayData, GlamourNameField);
                var currentDescription = GetTooltipString(stringArrayData, ItemDescriptionField);
                
                var descPreview = currentDescription?.Length > 50 
                    ? string.Concat(currentDescription.AsSpan(0, 50), "...")
                    : currentDescription ?? "null";
                log.Debug($"Current tooltip - Name: '{currentName}', GlamourName: '{currentGlamourName}', Desc: '{descPreview}'");

                // Translate item name
                var translatedName = translationCache.GetItemName(currentItemId, targetLang);
                if (!string.IsNullOrWhiteSpace(translatedName))
                {
                    SetTooltipString(stringArrayData, ItemNameField, translatedName);
                    log.Information($"Translated item {currentItemId} name: '{currentName}' -> '{translatedName}'");
                }
                else
                {
                    log.Warning($"No translation found for item {currentItemId} name ('{currentName}') in language {targetLang}");
                }

                // Translate glamour name if a glamour is applied
                if (currentGlamourId > 0 && currentGlamourId < configuration.MaxValidItemId)
                {
                    var translatedGlamourName = translationCache.GetItemName(currentGlamourId, targetLang);
                    if (!string.IsNullOrWhiteSpace(translatedGlamourName))
                    {
                        SetTooltipString(stringArrayData, GlamourNameField, translatedGlamourName);
                        log.Information($"Translated glamour {currentGlamourId} name: '{currentGlamourName}' -> '{translatedGlamourName}'");
                    }
                    else
                    {
                        log.Warning($"No translation found for glamour {currentGlamourId} in language {targetLang}");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(currentGlamourName))
                {
                    log.Debug($"Glamour name present but no glamourId tracked: '{currentGlamourName}'");
                }

                // Translate item description
                var translatedDescription = translationCache.GetItemDescription(currentItemId, targetLang);
                if (!string.IsNullOrWhiteSpace(translatedDescription))
                {
                    SetTooltipString(stringArrayData, ItemDescriptionField, translatedDescription);
                    log.Information($"Translated item {currentItemId} description");
                }
                else
                {
                    log.Debug($"No description translation found for item {currentItemId} in language {targetLang}");
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception in GenerateItemTooltip for item {currentItemId}");
        }

        // Call original to complete tooltip generation
        return generateItemTooltipHook!.Original(addonItemDetail, numberArrayData, stringArrayData);
    }

    /// <summary>
    /// Helper: Set a tooltip string field
    /// </summary>
    private void SetTooltipString(StringArrayData* stringArrayData, int field, string text)
    {
        try
        {
            if (stringArrayData == null)
            {
                log.Warning("StringArrayData is null");
                return;
            }

            if (stringArrayData->AtkArrayData.Size <= field)
            {
                log.Warning($"Field index {field} is out of range (size: {stringArrayData->AtkArrayData.Size})");
                return;
            }

            // Encode string as null-terminated UTF-8 bytes
            var bytes = System.Text.Encoding.UTF8.GetBytes(text + "\0");
            
            // Use FFXIVClientStructs SetValue method
            stringArrayData->SetValue(field, bytes, false, true, false);
            
            log.Verbose($"Set tooltip field {field} to: {text}");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to set tooltip string at field {field}");
        }
    }

    /// <summary>
    /// Helper: Get a tooltip string field
    /// </summary>
    private string? GetTooltipString(StringArrayData* stringArrayData, int field)
    {
        try
        {
            if (stringArrayData == null || stringArrayData->AtkArrayData.Size <= field)
                return null;

            var stringAddress = new IntPtr(stringArrayData->StringArray[field]);
            if (stringAddress == IntPtr.Zero)
                return null;

            return MemoryHelper.ReadStringNullTerminated(stringAddress);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to read tooltip string at field {field}");
            return null;
        }
    }

    /// <summary>
    /// Disable hooks
    /// </summary>
    public void Disable()
    {
        if (!isEnabled) return;

        try
        {
            itemHoveredHook?.Disable();
            generateItemTooltipHook?.Disable();
            isEnabled = false;
            log.Information("Tooltip hooks disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to disable tooltip hooks");
        }
    }

    /// <summary>
    /// Dispose hooks
    /// </summary>
    public void Dispose()
    {
        Disable();
        itemHoveredHook?.Dispose();
        generateItemTooltipHook?.Dispose();
    }
}
