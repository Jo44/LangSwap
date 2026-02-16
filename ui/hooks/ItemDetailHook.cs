using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.translation;
using System;

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
    private const int ItemEffectsField = 9;
    private const int ItemDescriptionField = 13;
    private const string ItemDetailAddonName = "ItemDetail";

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

                // Translate item effects
                var translatedEffects = translationCache.GetItemEffects(currentItemId, targetLang);
                if (!string.IsNullOrWhiteSpace(translatedEffects))
                {
                    SetTooltipString(stringArrayData, ItemEffectsField, translatedEffects);
                    log.Information($"Translated item {currentItemId} effects to {targetLang}");
                    return generateItemTooltipHook!.Original(addonItemDetail, numberArrayData, stringArrayData);
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