using Dalamud.Game.NativeWrapper;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.translation;
using LangSwap.ui.hooks.@base;
using System;
using System.Collections.Generic;
using System.Text;

namespace LangSwap.ui.hooks;

// ----------------------------
// Action Detail Hook
// ----------------------------
public unsafe class ActionDetailHook(
    Configuration configuration,
    IGameGui gameGui,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    IPluginLog log) : BaseHook(configuration, gameGui, gameInterop, sigScanner, translationCache, log)
{
    // Delegates functions
    private delegate byte ActionHoveredDelegate(IntPtr a1, IntPtr* a2, int* containerId, ushort* slotId, IntPtr a5, uint slotIdInt, IntPtr a7);
    private delegate void* GenerateActionTooltipDelegate(AtkUnitBase* addonActionDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);

    // Hooks
    private Hook<ActionHoveredDelegate>? actionHoveredHook;
    private Hook<GenerateActionTooltipDelegate>? generateActionTooltipHook;

    // ID
    private uint currentActionId = 0;

    // Cache for translated bytes
    private readonly Dictionary<string, byte[]> _translatedBytesCache = [];
    private const int MaxCacheSize = 500;

    // Action Detail Addon
    private readonly string ActionDetailAddonName = configuration.ActionDetailAddonName;
    private readonly int ActionNameField = configuration.ActionNameField;
    private readonly int ActionDescriptionField = configuration.ActionDescriptionField;

    // ----------------------------
    // Enable the hook
    // ----------------------------
    public override void Enable()
    {
        // Prevent multiple enables
        if (isEnabled) return;

        try
        {
            // Hook ActionHovered (-> get current action ID when hovering an action)
            nint actionHoveredAddr = sigScanner.ScanText(configuration.ActionHoveredSig);
            if (actionHoveredAddr != IntPtr.Zero)
            {
                actionHoveredHook = gameInterop.HookFromAddress<ActionHoveredDelegate>(actionHoveredAddr, ActionHoveredDetour);
                actionHoveredHook.Enable();
                log.Debug($"ActionHovered hook enabled at 0x{actionHoveredAddr:X}");
            }
            else
            {
                log.Warning("ActionHovered signature not found");
            }

            // Hook GenerateActionTooltip (-> modify action tooltip datas when generating it)
            nint generateActionTooltipAddr = sigScanner.ScanText(configuration.GenerateActionTooltipSig);
            if (generateActionTooltipAddr != IntPtr.Zero)
            {
                generateActionTooltipHook = gameInterop.HookFromAddress<GenerateActionTooltipDelegate>(generateActionTooltipAddr, GenerateActionTooltipDetour);
                generateActionTooltipHook.Enable();
                log.Debug($"GenerateActionTooltip hook enabled at 0x{generateActionTooltipAddr:X}");
            }
            else
            {
                log.Warning("GenerateActionTooltip signature not found");
            }

            // Set enabled flag
            isEnabled = true;
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to enable ActionDetail hooks");
        }
    }

    // ----------------------------
    // Swap to target language
    // ----------------------------
    protected override void OnLanguageSwapped()
    {
        // Refresh action detail to apply translations
        _translatedBytesCache.Clear();
        RefreshActionDetail();
    }

    // ----------------------------
    // Restore to original language
    // ----------------------------
    protected override void OnLanguageRestored()
    {
        // Clear current action ID
        currentActionId = 0;

        // Refresh action detail to apply translations
        _translatedBytesCache.Clear();
        RefreshActionDetail();
    }

    // ----------------------------
    // Refresh the ActionDetail addon
    // ----------------------------
    private void RefreshActionDetail()
    {
        try
        {
            // Get pointer to ActionDetail addon
            AtkUnitBasePtr actionDetailPtr = gameGui.GetAddonByName(ActionDetailAddonName);
            if (!actionDetailPtr.IsNull)
            {
                // Get AtkUnitBase from pointer
                AtkUnitBase* actionDetail = (AtkUnitBase*)actionDetailPtr.Address;

                // Only refresh if the addon is currently visible
                if (actionDetail != null && actionDetail -> IsVisible)
                {
                    actionDetail -> Hide(true, false, 0);
                    actionDetail -> Show(true, 0);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to refresh ActionDetail");
        }
    }

    // ----------------------------
    // On Action Hovered
    // -> Get current action ID when hovering an action
    // ----------------------------
    private byte ActionHoveredDetour(IntPtr a1, IntPtr* a2, int* containerId, ushort* slotId, IntPtr a5, uint slotIdInt, IntPtr a7)
    {
        // Call original first to ensure action ID is set correctly
        byte returnValue = actionHoveredHook!.Original(a1, a2, containerId, slotId, a5, slotIdInt, a7);

        try
        {
            // TODO
            ActionManager actionManager = *(ActionManager*)a7;
            currentActionId = 0;
        }
        catch (Exception ex)
        {
            log.Error(ex, "Exception in ActionHovered detour");
        }

        // Return original value
        return returnValue;
    }

    // ----------------------------
    // On Generate Action Tooltip
    // -> Modify action tooltip datas when generating it
    // ----------------------------
    private void* GenerateActionTooltipDetour(AtkUnitBase* addonActionDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
    {
        try
        {
            // Modify texts in StringArrayData
            if (numberArrayData != null && numberArrayData -> AtkArrayData.Size > 0)
            {
                uint potentialActionId = (uint)numberArrayData -> IntArray[0];
                if (potentialActionId > 0 && potentialActionId < configuration.MaxValidActionId)
                {
                    currentActionId = potentialActionId;
                }
            }

            log.Verbose($"GenerateActionTooltip - isSwapped={isLanguageSwapped}, actionId={currentActionId}");

            if (isLanguageSwapped && currentActionId > 0 && currentActionId < configuration.MaxValidActionId)
            {
                LanguageEnum lang = (LanguageEnum)configuration.TargetLanguage;
                string? translatedName = translationCache.GetActionName(currentActionId, lang);
                
                if (!string.IsNullOrWhiteSpace(translatedName))
                {
                    SetTooltipString(stringArrayData, ActionNameField, translatedName);
                    log.Information($"Translated action {currentActionId} name to {lang}");
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
            if (stringArrayData == null || stringArrayData -> AtkArrayData.Size <= field)
                return;

            byte[]? bytes = Encoding.UTF8.GetBytes(text + "\0");
            stringArrayData -> SetValue(field, bytes, false, true, false);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to set tooltip string at field {field}");
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
            // Disable ActionHovered hook
            actionHoveredHook?.Disable();

            // Disable GenerateActionTooltip hook
            generateActionTooltipHook?.Disable();

            // Set disabled flag
            isEnabled = false;
            log.Debug("ActionDetail hooks disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to disable ActionDetail hooks");
        }
    }

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    public override void Dispose()
    {
        try
        {
            // Dispose ActionHovered hook
            actionHoveredHook?.Disable();
            actionHoveredHook?.Dispose();
            actionHoveredHook = null;

            // Dispose GenerateActionTooltip hook
            generateActionTooltipHook?.Disable();
            generateActionTooltipHook?.Dispose();
            generateActionTooltipHook = null;

            // Clear cache
            _translatedBytesCache.Clear();

            // Set disabled flag
            isEnabled = false;
            log.Debug("ActionDetail hooks disposed");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to dispose ActionDetail hooks");
        }

        // Finalize
        GC.SuppressFinalize(this);
    }
}