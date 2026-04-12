using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.hook.template;
using LangSwap.translation;
using LangSwap.translation.@base;
using System;

namespace LangSwap.hook;

// ----------------------------
// Action Tooltip Hook
// ----------------------------
public unsafe class ActionTooltipHook(Configuration config, TranslationCache translationCache) : TooltipHook(config, translationCache)
{
    // Log
    private readonly string Class = $"[{nameof(ActionTooltipHook)}]";

    // Hook name
    public override string Name => "Action Tooltip";

    // Memory signature
    protected override string MemorySignature => config.ActionTooltipSignature;

    // ----------------------------
    // On language swap
    // ----------------------------
    protected override void OnLanguageSwap()
    {
        // Refresh action detail addon
        RefreshAddon(GetAddon(config.ActionDetailAddon), config.ActionDetailName);
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
                    string actionName = ReadStringFromArrayData(stringArrayData, config.ActionNameField);
                    if (!string.IsNullOrWhiteSpace(actionName))
                    {
                        // Get action ID
                        uint actionID = translationCache.GetActionIDByName(actionName, clientLang) ?? 0;
                        if (actionID > 0 && actionID <= config.MaxValidActionID)
                        {
                            /* Action name */

                            // Translate action name
                            string translatedActionName = TranslateActionName(actionID, targetLang);

                            // Apply translated action name
                            if (!string.IsNullOrWhiteSpace(translatedActionName))
                            {
                                if (!WriteStringToArrayData(stringArrayData, config.ActionNameField, translatedActionName))
                                {
                                    Log.Error($"{Class} - Failed to write translated action name ({translatedActionName}) to field {config.ActionNameField}");
                                }
                            }

                            /* Description */

                            // Translate description
                            string translatedDescription = TranslateDescription(actionID, targetLang);

                            // Apply translated description
                            if (!string.IsNullOrWhiteSpace(translatedDescription))
                            {
                                if (!WriteStringToArrayData(stringArrayData, config.ActionDescriptionField, translatedDescription))
                                {
                                    Log.Error($"{Class} - Failed to write translated description ({translatedDescription}) to field {config.ActionDescriptionField}");
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Exception in OnTooltipUpdate");
        }

        // Call original function with modified data
        return tooltipHook!.Original(actionDetailAddon, numberArrayData, stringArrayData);
    }

    // ----------------------------
    // Translate action name
    // ----------------------------
    private string TranslateActionName(uint actionID, Language targetLang)
    {
        // Get translated action name from action ID
        return translationCache.GetActionName(actionID, targetLang) ?? string.Empty;
    }

    // ----------------------------
    // Translate description
    // ----------------------------
    private string TranslateDescription(uint actionID, Language targetLang)
    {
        // Get translated description from action ID
        return translationCache.GetActionDescription(actionID, targetLang) ?? string.Empty;
    }

}