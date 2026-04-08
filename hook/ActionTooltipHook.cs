using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.hook.ttt;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.translation.@base;
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
    IPluginLog log) : TooltipHook(config, gameInterop, sigScanner, translationCache, utilities, log)
{
    // Log
    private const string Class = "[ActionTooltipHook.cs]";

    // Memory signature
    protected override string MemorySignature => config.ActionTooltipSignature;

    // ----------------------------
    // On language swap
    // ----------------------------
    protected override void OnLanguageSwap()
    {
        // Refresh action detail addon
        utilities.RefreshAddon(utilities.GetAddon(config.ActionDetailAddon), config.ActionDetailName);
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
                Language clientLang = config.ClientLanguage;

                // Get target language
                Language targetLang = config.TargetLanguage;

                // Only proceed if client language and target language are different
                if (clientLang != targetLang)
                {
                    // Get action name
                    string actionName = utilities.ReadStringFromArrayData(stringArrayData, config.ActionNameField);
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
                                if (!utilities.WriteStringToArrayData(stringArrayData, config.ActionNameField, translatedActionName))
                                {
                                    log.Error($"{Class} - Failed to write translated action name ({translatedActionName}) to field {config.ActionNameField}");
                                }
                            }

                            /* Description */

                            // Translate description
                            string translatedDescription = TranslateDescription(actionId, targetLang);

                            // Apply translated description
                            if (!string.IsNullOrWhiteSpace(translatedDescription))
                            {
                                if (!utilities.WriteStringToArrayData(stringArrayData, config.ActionDescriptionField, translatedDescription))
                                {
                                    log.Error($"{Class} - Failed to write translated description ({translatedDescription}) to field {config.ActionDescriptionField}");
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Exception in OnTooltipUpdate");
        }

        // Call original function with modified data
        return tooltipHook!.Original(actionDetailAddon, numberArrayData, stringArrayData);
    }

    // ----------------------------
    // Translate action name
    // ----------------------------
    private string TranslateActionName(uint actionId, Language targetLang)
    {
        // Get translated action name from action ID
        return translationCache.GetActionName(actionId, targetLang) ?? string.Empty;
    }

    // ----------------------------
    // Translate description
    // ----------------------------
    private string TranslateDescription(uint actionId, Language targetLang)
    {
        // Get translated description from action ID
        return translationCache.GetActionDescription(actionId, targetLang) ?? string.Empty;
    }

}