using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.translation;
using LangSwap.ui.hooks.@base;
using System;

namespace LangSwap.ui.hooks;

/// <summary>
/// Hook for translating ActionDetail component (action tooltips)
/// </summary>
public unsafe class ActionDetailHook(
    Configuration configuration,
    IGameGui gameGui,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    IPluginLog log) : BaseHook(configuration, gameGui, gameInterop, sigScanner, translationCache, log)
{
    private delegate void* GenerateActionTooltipDelegate(AtkUnitBase* addonActionDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);

    private Hook<GenerateActionTooltipDelegate>? generateActionTooltipHook;
    private uint currentActionId = 0;

    private const int ActionNameField = 0;
    private const int ActionDescriptionField = 13;
    private const string ActionDetailAddonName = "ActionDetail";

    public override void Enable()
    {
        if (isEnabled) return;

        try
        {
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
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to enable ActionDetailHook");
        }
    }

    protected override void OnLanguageSwapped()
    {
        RefreshActionDetail();
    }

    protected override void OnLanguageRestored()
    {
        currentActionId = 0;
        RefreshActionDetail();
    }

    private void RefreshActionDetail()
    {
        try
        {
            var actionDetailPtr = gameGui.GetAddonByName(ActionDetailAddonName);
            if (!actionDetailPtr.IsNull)
            {
                var actionDetail = (AtkUnitBase*)actionDetailPtr.Address;
                if (actionDetail != null && actionDetail->IsVisible)
                {
                    log.Debug("Refreshing ActionDetail");
                    actionDetail->Hide(true, false, 0);
                    actionDetail->Show(true, 0);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to refresh ActionDetail");
        }
    }

    private void* GenerateActionTooltipDetour(AtkUnitBase* addonActionDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
    {
        try
        {
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
                var translatedName = translationCache.GetActionName(currentActionId, targetLang);
                
                if (!string.IsNullOrWhiteSpace(translatedName))
                {
                    SetTooltipString(stringArrayData, ActionNameField, translatedName);
                    log.Information($"Translated action {currentActionId} name to {targetLang}");
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception in GenerateActionTooltip for action {currentActionId}");
        }

        return generateActionTooltipHook!.Original(addonActionDetail, numberArrayData, stringArrayData);
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
            generateActionTooltipHook?.Disable();
            isEnabled = false;
            log.Information("ActionDetailHook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to disable ActionDetailHook");
        }
    }

    public override void Dispose()
    {
        try
        {
            generateActionTooltipHook?.Disable();
            generateActionTooltipHook?.Dispose();
            generateActionTooltipHook = null;
            
            isEnabled = false;
            log.Information("ActionDetailHook disposed");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to dispose ActionDetailHook");
        }
        GC.SuppressFinalize(this);
    }
}