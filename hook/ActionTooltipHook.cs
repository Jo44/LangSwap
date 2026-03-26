using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.hook.@base;
using LangSwap.tool;
using LangSwap.translation;
using System;

namespace LangSwap.hook;

// ----------------------------
// Action Tooltip Hook
// ----------------------------
public unsafe class ActionTooltipHook(
    Configuration config,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : TooltipBaseHook(config, gameInterop, sigScanner, translationCache, utilities, log)
{
    // Log
    private const string Class = "[ActionTooltipHook.cs]";

    // Memory signature
    protected override string MemorySignature => config.ActionTooltipSignature;

    // Action detail fields
    private readonly int actionNameField = config.ActionNameField;
    private readonly int actionDescriptionField = config.ActionDescriptionField;

    // ----------------------------
    // On language swap
    // ----------------------------
    protected override void OnLanguageSwap()
    {
        // Refresh action detail addon
        utilities.RefreshAddon(utilities.GetAddon(config.ActionDetailAddon), "action detail");
    }

    // ----------------------------
    // On tooltip update
    // ----------------------------
    protected override void* OnTooltipUpdate(AtkUnitBase* actionDetailAddon, NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
    {
        try
        {
            // Log the structure of StringArrayData for debugging
            // utilities.LogSADStructure(stringArrayData);

            // Check if language is swapped
            if (isLanguageSwapped && stringArrayData != null)
            {
                // Get client language
                LanguageEnum clientLang = config.ClientLanguage;

                // Get target language
                LanguageEnum targetLang = config.TargetLanguage;

                // Get action name
                string actionName = utilities.ReadStringFromArrayData(stringArrayData, actionNameField);
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
                            if (!utilities.WriteStringToArrayData(stringArrayData, actionNameField, translatedActionName))
                            {
                                log.Error($"{Class} - Failed to write translated action name ({translatedActionName}) to field {actionNameField}");
                            }
                        }

                        /* Description */

                        // Translate description
                        string translatedDescription = TranslateDescription(actionId, targetLang);
                        
                        // Apply translated description
                        if (!string.IsNullOrWhiteSpace(translatedDescription))
                        {
                            if (!utilities.WriteStringToArrayData(stringArrayData, actionDescriptionField, translatedDescription))
                            {
                                log.Error($"{Class} - Failed to write translated description ({translatedDescription}) to field {actionDescriptionField}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Exception in OnActionTooltipUpdate");
        }

        // Call original function with modified data
        return tooltipHook!.Original(actionDetailAddon, numberArrayData, stringArrayData);
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

}