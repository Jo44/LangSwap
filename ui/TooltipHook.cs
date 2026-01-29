using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.translation;
using System;

namespace LangSwap.ui;

/// <summary>
/// Hook for intercepting and modifying item and action tooltips
/// </summary>
public unsafe class TooltipHook(
    Configuration configuration,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    IPluginLog log,
    IGameGui gameGui) : IDisposable
{
    // References
    private readonly Configuration configuration = configuration;
    private readonly IGameInteropProvider gameInterop = gameInterop;
    private readonly ISigScanner sigScanner = sigScanner;
    private readonly TranslationCache translationCache = translationCache;
    private readonly IPluginLog log = log;
    private readonly IGameGui gameGui = gameGui;

    // Delegates
    private delegate void* GenerateItemTooltipDelegate(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);
    private delegate void* GenerateActionTooltipDelegate(AtkUnitBase* addonActionDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);
    private delegate byte ItemHoveredDelegate(IntPtr a1, IntPtr* a2, int* containerId, ushort* slotId, IntPtr a5, uint slotIdInt, IntPtr a7);

    // Hooks
    private Hook<GenerateItemTooltipDelegate>? generateItemTooltipHook;
    private Hook<GenerateActionTooltipDelegate>? generateActionTooltipHook;
    private Hook<ItemHoveredDelegate>? itemHoveredHook;

    // State
    private bool isEnabled = false;
    private bool isLanguageSwapped = false;
    private uint currentItemId = 0;
    private uint currentGlamourId = 0;
    private uint currentActionId = 0;

    // Tooltip field indices
    private const int ItemNameField = 0;
    private const int GlamourNameField = 1;
    private const int ItemDescriptionField = 13;
    
    private const int ActionNameField = 0;
    private const int ActionDescriptionField = 13;

    // Addon names for tooltips
    private const string ItemDetailAddonName = "ItemDetail";
    private const string ActionDetailAddonName = "ActionDetail";

    public void Enable()
    {
        if (isEnabled) return;

        try
        {
            // Hook 1: ItemHovered
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

            // Hook 2: GenerateItemTooltip
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

            // Hook 3: GenerateActionTooltip
            var generateActionTooltipAddr = sigScanner.ScanText(configuration.GenerateActionTooltipSig);
            if (generateActionTooltipAddr != IntPtr.Zero)
            {
                generateActionTooltipHook = gameInterop.HookFromAddress<GenerateActionTooltipDelegate>(generateActionTooltipAddr, GenerateActionTooltipDetour);
                generateActionTooltipHook.Enable();
                log.Information($"GenerateActionTooltip hook enabled at 0x{generateActionTooltipAddr:X}");
            }
            else
            {
                log.Warning("GenerateActionTooltip signature not found");
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

    public void SwapLanguage()
    {
        if (isLanguageSwapped)
        {
            log.Debug("Language already swapped, ignoring");
            return;
        }
        
        isLanguageSwapped = true;
        log.Information("Tooltip language swap enabled");
        
        // Force refresh of currently visible tooltips
        RefreshVisibleTooltips();
    }

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
        currentActionId = 0;
        log.Information("Tooltip language swap disabled");
        
        // Force refresh of currently visible tooltips
        RefreshVisibleTooltips();
    }

    /// <summary>
    /// Force refresh of visible tooltips by hiding and reshowing them
    /// </summary>
    private void RefreshVisibleTooltips()
    {
        try
        {
            // Try to refresh ItemDetail addon
            var itemDetailPtr = gameGui.GetAddonByName(ItemDetailAddonName);
            if (itemDetailPtr != IntPtr.Zero)
            {
                var itemDetail = (AtkUnitBase*)itemDetailPtr.Address;
                if (itemDetail != null && itemDetail->IsVisible)
                {
                    log.Debug("Refreshing ItemDetail tooltip");
                    // Hide and show to trigger regeneration
                    itemDetail->Hide(true, false, 0);
                    itemDetail->Show(true, 0);
                }
            }

            // Try to refresh ActionDetail addon
            var actionDetailPtr = gameGui.GetAddonByName(ActionDetailAddonName);
            if (actionDetailPtr != IntPtr.Zero)
            {
                var actionDetail = (AtkUnitBase*)actionDetailPtr.Address;
                if (actionDetail != null && actionDetail->IsVisible)
                {
                    log.Debug("Refreshing ActionDetail tooltip");
                    // Hide and show to trigger regeneration
                    actionDetail->Hide(true, false, 0);
                    actionDetail->Show(true, 0);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to refresh visible tooltips");
        }
    }

    // ========== ITEM HOOKS ==========

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

            if (isLanguageSwapped && currentItemId > 0 && currentItemId < configuration.MaxValidItemId)
            {
                var targetLang = (LanguageEnum)configuration.TargetLanguage;
                
                // Translate item name
                var translatedName = translationCache.GetItemName(currentItemId, targetLang);
                if (!string.IsNullOrWhiteSpace(translatedName))
                {
                    SetTooltipString(stringArrayData, ItemNameField, translatedName);
                    log.Information($"Translated item {currentItemId} name to {targetLang}");
                }

                // Translate glamour name
                if (currentGlamourId > 0 && currentGlamourId < configuration.MaxValidItemId)
                {
                    var translatedGlamourName = translationCache.GetItemName(currentGlamourId, targetLang);
                    if (!string.IsNullOrWhiteSpace(translatedGlamourName))
                    {
                        SetTooltipString(stringArrayData, GlamourNameField, translatedGlamourName);
                        log.Information($"Translated glamour {currentGlamourId} name to {targetLang}");
                    }
                }

                // Translate item description
                var translatedDescription = translationCache.GetItemDescription(currentItemId, targetLang);
                if (!string.IsNullOrWhiteSpace(translatedDescription))
                {
                    SetTooltipString(stringArrayData, ItemDescriptionField, translatedDescription);
                    log.Information($"Translated item {currentItemId} description to {targetLang}");
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception in GenerateItemTooltip for item {currentItemId}");
        }

        return generateItemTooltipHook!.Original(addonItemDetail, numberArrayData, stringArrayData);
    }

    // ========== ACTION HOOKS ==========

    private void* GenerateActionTooltipDetour(AtkUnitBase* addonActionDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
    {
        try
        {
            // Extract action ID from NumberArrayData
            if (numberArrayData != null && numberArrayData->AtkArrayData.Size > 0)
            {
                var potentialActionId = (uint)numberArrayData->IntArray[0];
                if (potentialActionId > 0 && potentialActionId < configuration.MaxValidActionId)
                {
                    currentActionId = potentialActionId;
                }
            }

            log.Verbose($"GenerateActionTooltip - isSwapped={isLanguageSwapped}, actionId={currentActionId}");

            if (isLanguageSwapped && currentActionId > 0 && currentActionId < configuration.MaxValidActionId)
            {
                var targetLang = (LanguageEnum)configuration.TargetLanguage;
                
                var currentName = GetTooltipString(stringArrayData, ActionNameField);
                log.Debug($"Current action name: {currentName}");

                // Translate action name
                var translatedName = translationCache.GetActionName(currentActionId, targetLang);
                if (!string.IsNullOrWhiteSpace(translatedName))
                {
                    SetTooltipString(stringArrayData, ActionNameField, translatedName);
                    log.Information($"Translated action {currentActionId} name: '{currentName}' -> '{translatedName}'");
                }

                // Note: Action descriptions are not currently supported
                // They are constructed dynamically from multiple fields
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception in GenerateActionTooltip for action {currentActionId}");
        }

        return generateActionTooltipHook!.Original(addonActionDetail, numberArrayData, stringArrayData);
    }

    // ========== HELPERS ==========

    private void SetTooltipString(StringArrayData* stringArrayData, int field, string text)
    {
        try
        {
            if (stringArrayData == null || stringArrayData->AtkArrayData.Size <= field)
                return;

            var bytes = System.Text.Encoding.UTF8.GetBytes(text + "\0");
            stringArrayData->SetValue(field, bytes, false, true, false);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to set tooltip string at field {field}");
        }
    }

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

    public void Disable()
    {
        if (!isEnabled) return;

        try
        {
            itemHoveredHook?.Disable();
            generateItemTooltipHook?.Disable();
            generateActionTooltipHook?.Disable();
            isEnabled = false;
            log.Information("Tooltip hooks disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to disable tooltip hooks");
        }
    }

    public void Dispose()
    {
        Disable();
        itemHoveredHook?.Dispose();
        generateItemTooltipHook?.Dispose();
        generateActionTooltipHook?.Dispose();
    }
}
