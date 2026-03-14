using Dalamud.Game.NativeWrapper;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.ui.hooks.@base;
using System;

namespace LangSwap.ui.hooks;

// ----------------------------
// Action Tooltip Hook
// ----------------------------
public unsafe class ActionTooltipHook(
    Configuration config,
    IGameGui gameGui,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : BaseHook(config, gameGui, gameInterop, sigScanner, translationCache, utilities, log)
{
    // Log
    private const string Class = "[ActionTooltipHook.cs]";

    // Action Detail Addon
    private readonly string ActionDetailAddon = config.ActionDetailAddon;
    private readonly int ActionNameField = config.ActionNameField;
    private readonly int ActionDescriptionField = config.ActionDescriptionField;

    // Delegate function
    private delegate void* ActionTooltipDelegate(AtkUnitBase* actionDetailAddon, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);

    // Hook
    private Hook<ActionTooltipDelegate>? _actionTooltipHook;

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
            nint actionTooltipAddr = sigScanner.ScanText(config.ActionTooltipSig);
            if (actionTooltipAddr != IntPtr.Zero)
            {
                // Get hook from address
                _actionTooltipHook = gameInterop.HookFromAddress<ActionTooltipDelegate>(actionTooltipAddr, ActionTooltipDetour);

                // Enable hook
                _actionTooltipHook.Enable();

                // Set enabled flag
                isEnabled = true;

                // Log
                log.Debug($"{Class} - Action tooltip hook enabled at 0x{actionTooltipAddr:X}");
            }
            else
            {
                log.Warning($"{Class} - Action tooltip signature not found");
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to enable action tooltip hook");
        }
    }

    // ----------------------------
    // On language swap
    // ----------------------------
    protected override void OnLanguageSwap()
    {
        // Refresh action detail addon
        try
        {
            // Get pointer to ActionDetail addon
            AtkUnitBasePtr actionDetailPtr = gameGui.GetAddonByName(ActionDetailAddon);
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
            log.Error(ex, $"{Class} - Failed to refresh action detail addon");
        }
    }

    // ----------------------------
    // Action tooltip detour
    // ----------------------------
    private void* ActionTooltipDetour(AtkUnitBase* actionDetailAddon, NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
    {
        try
        {
            // Log the structure of StringArrayData for debugging
            // utilities.LogSADStructure(stringArrayData);

            // Check if language is swapped
            if (isLanguageSwapped && stringArrayData != null)
            {
                // Get client language
                LanguageEnum clientLang = (LanguageEnum)config.ClientLanguage;

                // Get target language
                LanguageEnum targetLang = (LanguageEnum)config.TargetLanguage;

                // Get action name
                string actionName = utilities.ReadStringFromArrayData(stringArrayData, ActionNameField);
                if (!string.IsNullOrWhiteSpace(actionName))
                {
                    // Get action ID 
                    uint actionId = translationCache.GetActionIdByName(actionName, clientLang) ?? 0;
                    if (actionId > 0 && actionId <= config.MaxValidActionId)
                    {
                        /* Action name */

                        // Translate action name
                        string translatedActionName = TranslateActionName(actionId, targetLang);

                        // Apply translated action name
                        if (!string.IsNullOrWhiteSpace(translatedActionName))
                        {
                            if (!utilities.WriteStringToArrayData(stringArrayData, ActionNameField, translatedActionName))
                            {
                                log.Error($"{Class} - Failed to write translated action name ({translatedActionName}) to field {ActionNameField}");
                            }
                        }

                        /* Description */

                        // Translate description
                        string translatedDescription = TranslateDescription(actionId, targetLang);
                        
                        // Apply translated description
                        if (!string.IsNullOrWhiteSpace(translatedDescription))
                        {
                            if (!utilities.WriteStringToArrayData(stringArrayData, ActionDescriptionField, translatedDescription))
                            {
                                log.Error($"{Class} - Failed to write translated description ({translatedDescription}) to field {ActionDescriptionField}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Exception in ActionTooltipDetour");
        }

        return _actionTooltipHook!.Original(actionDetailAddon, numberArrayData, stringArrayData);
    }

    // ----------------------------
    // Translate action name
    // ----------------------------
    private string TranslateActionName(uint actionId, LanguageEnum targetLang)
    {
        // Get translated action name from action ID
        string translatedActionName = translationCache.GetActionName(actionId, targetLang) ?? string.Empty;

        // Return translated action name
        return translatedActionName;
    }

    // ----------------------------
    // Translate description
    // ----------------------------
    private string TranslateDescription(uint actionId, LanguageEnum targetLang)
    {
        // Get translated description from action ID
        string translatedDescription = translationCache.GetActionDescription(actionId, targetLang) ?? string.Empty;

        // Return translated description
        return translatedDescription;
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
            // Disable action tooltip hook
            _actionTooltipHook?.Disable();

            // Set disabled flag
            isEnabled = false;
            log.Debug($"{Class} - Action tooltip hook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to disable action tooltip hook");
        }
    }

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    public override void Dispose()
    {
        try
        {
            // Dispose action tooltip hook
            _actionTooltipHook?.Disable();
            _actionTooltipHook?.Dispose();
            _actionTooltipHook = null;

            // Set disabled flag
            isEnabled = false;
            log.Debug($"{Class} - Action tooltip hook disposed");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to dispose action tooltip hook");
        }

        // Finalize
        GC.SuppressFinalize(this);
    }

}