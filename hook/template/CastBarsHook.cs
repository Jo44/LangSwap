using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.hook.@base;
using LangSwap.translation;
using LangSwap.translation.@base;
using LangSwap.translation.model;
using System;
using System.Collections.Generic;

namespace LangSwap.hook.template;

// ----------------------------
// Base class for all castbars hooks
// ----------------------------
public unsafe abstract class CastBarsHook(Configuration config, TranslationCache translationCache) : BaseHook(config, translationCache)
{
    // Log
    private const string Class = "[CastBarsHook.cs]";

    // Services
    protected static IAddonLifecycle AddonLifecycle => Plugin.AddonLifecycle;
    protected static IObjectTable ObjectTable => Plugin.ObjectTable;
    protected static ITargetManager TargetManager => Plugin.TargetManager;

    // Cache actions (fallback)
    private readonly Dictionary<string, uint> cacheActions = [];

    // ----------------------------
    // Update cast bar
    // ----------------------------
    protected virtual void UpdateCastBar(AtkUnitBase* addon, AddonType addonType, uint actionId)
    {
        try
        {
            // Check if addon is visible
            if (addon == null || !addon -> IsVisible) return;

            // Check if we have a valid action ID
            if (!IsValidActionID(actionId)) return;

            // Get the action
            string? clientName = translationCache.GetActionName(actionId, config.ClientLanguage);

            // Cache the action
            if (!string.IsNullOrWhiteSpace(clientName)) cacheActions[clientName] = actionId;


            // Cleanup cache actions
            if (cacheActions.Count > 100) cacheActions.Clear();

            // Get cast field
            int castField = GetCastField(addonType);

            // Check node list
            if (addon -> UldManager.NodeList == null || addon -> UldManager.NodeListCount <= castField) return;

            // Get field node
            AtkResNode* fieldNode = addon -> UldManager.NodeList[castField];
            if (fieldNode == null || fieldNode -> Type != NodeType.Text) return;

            // Get text node
            AtkTextNode* textNode = (AtkTextNode*)fieldNode;
            if (textNode == null || textNode -> NodeText.Length == 0) return;

            // Get the current display name
            string currentDisplayName = textNode -> NodeText.ToString();
            if (string.IsNullOrWhiteSpace(currentDisplayName)) return;

            // Remove any target indicator and store it
            string[] displayParts = RemoveTargetIndicator(currentDisplayName);
            string targetIndicator = displayParts[1];

            // Remove any ellipsis
            string cleanDisplayName = RemoveEllipsis(displayParts[0]);

            // Try exact match via action ID
            uint resolveActionID = 0;
            if (IsValidActionID(actionId))
            {
                // Resolve via action ID
                resolveActionID = actionId;
            }
            else if (cacheActions.TryGetValue(cleanDisplayName, out uint cachedActionID))
            {
                // Try fallback match via cached actions
                resolveActionID = cachedActionID;
            }

            // If action ID is resolved
            if (resolveActionID > 0)
            {
                // Resolve action name
                string? actionName = ResolveActionName(resolveActionID, cleanDisplayName);
                if (string.IsNullOrWhiteSpace(actionName)) return;

                // Only update if language is swapped
                if (!isLanguageSwapped) return;

                // Put back target indicator if it was present
                string displayText = string.IsNullOrWhiteSpace(targetIndicator) ? actionName : actionName + " " + targetIndicator;

                // Update the text node
                textNode -> SetText(displayText);
            }
        }
        catch (Exception ex)
        {
            // Get the addon name
            string addonName = GetAddonName(addonType);

            // Log
            Log.Error(ex, $"{Class} - Error updating {addonName} addon");
        }
    }

    // ----------------------------
    // Update list
    // ----------------------------
    protected void UpdateList(AtkUnitBase* addon, AddonType addonType, CastBarsType castBarsType, uint[] entityIDs)
    {
        try
        {
            // Check if addon is visible
            if (addon == null || !addon -> IsVisible) return;

            // Get object kind
            ObjectKind objectKind = castBarsType == CastBarsType.Allies ? ObjectKind.Player : ObjectKind.BattleNpc;

            // Get current casts for objects
            Dictionary<uint, uint> currentCasts = [];
            foreach (IGameObject obj in ObjectTable)
            {
                // Filter by object kind and casting status
                if (obj is not IBattleChara battleCharacter || battleCharacter.ObjectKind != objectKind || !battleCharacter.IsCasting) continue;

                // Get the action ID
                uint actionID = (uint)battleCharacter.CastActionId;
                if (IsValidActionID(actionID))
                {
                    // Add to current casts
                    currentCasts[battleCharacter.EntityId] = actionID;

                    // Cache the action
                    string? clientName = translationCache.GetActionName(actionID, config.ClientLanguage);
                    if (!string.IsNullOrWhiteSpace(clientName)) cacheActions[clientName] = actionID;
                }
            }

            // Cleanup cache actions
            if (cacheActions.Count > 100) cacheActions.Clear();

            // Initialize fields based on castbars type
            int startField = castBarsType == CastBarsType.Allies ? config.PartyListStartField : config.HateListStartField;
            int endField = castBarsType   == CastBarsType.Allies ? config.PartyListEndField   : config.HateListEndField;
            int fieldIndex = castBarsType == CastBarsType.Allies ? config.PartyListCastField  : config.HateListCastField;

            // Check node list
            if (addon -> UldManager.NodeList == null || addon -> UldManager.NodeListCount <= endField) return;

            // Loop through the list slots
            for (int slotIndex = endField; slotIndex >= startField; slotIndex--)
            {
                // Get the slot node
                AtkResNode* slotNode = addon -> UldManager.NodeList[slotIndex];
                if (slotNode == null || !slotNode -> IsVisible() || (ushort)slotNode -> Type < 1000) continue;

                // Get the component node
                AtkComponentNode* componentNode = (AtkComponentNode*)slotNode;
                if (componentNode -> Component == null) continue;

                // Get the uld manager
                AtkUldManager* uldManager = &componentNode -> Component -> UldManager;
                if (uldManager == null || uldManager -> NodeList == null || uldManager -> NodeListCount <= fieldIndex) continue;

                // Get the field node
                AtkResNode* fieldNode = uldManager -> NodeList[fieldIndex];
                if (fieldNode == null || fieldNode -> Type != NodeType.Text) continue;

                // Get the text node
                AtkTextNode* textNode = (AtkTextNode*)fieldNode;
                if (textNode == null || textNode -> NodeText.Length == 0) continue;

                // Calculate the relative index of the slot within the list
                int slotRelativeIndex = endField - slotIndex;

                // Get the entity ID for this slot
                uint entityID = slotRelativeIndex < entityIDs.Length ? entityIDs[slotRelativeIndex] : 0;

                // Get the current display name
                string currentDisplayName = textNode -> NodeText.ToString();
                if (string.IsNullOrWhiteSpace(currentDisplayName)) continue;

                // Remove any target indicator and store it
                string[] displayParts = RemoveTargetIndicator(currentDisplayName);
                string targetIndicator = displayParts[1];

                // Remove any ellipsis
                string cleanDisplayName = RemoveEllipsis(displayParts[0]);

                // Try exact match via entity ID
                uint resolveActionID = 0;
                if (entityID > 0 && currentCasts.TryGetValue(entityID, out uint matchedActionID))
                {
                    // Resolve match via entity ID
                    resolveActionID = matchedActionID;
                }
                else
                {
                    // Try fallback match via cached actions
                    if (cacheActions.TryGetValue(cleanDisplayName, out uint cachedActionID))
                    {
                        // Resolve match via cached actions
                        resolveActionID = cachedActionID;
                    }
                    else
                    {
                        // Try fallback match via translation cache
                        foreach (uint actionID in currentCasts.Values)
                        {
                            // Get the client action name for this action ID
                            string? clientName = translationCache.GetActionName(actionID, config.ClientLanguage);

                            // Check for match with the clean display name
                            if (!string.IsNullOrWhiteSpace(clientName) && clientName.Equals(cleanDisplayName, StringComparison.OrdinalIgnoreCase))
                            {
                                // Resolve match via translation cache and update fallback cache
                                resolveActionID = actionID;
                                cacheActions[clientName] = actionID;
                                break;
                            }
                        }
                    }
                }

                // If action ID is resolved
                if (resolveActionID > 0)
                {
                    // Resolve action name
                    string? actionName = ResolveActionName(resolveActionID, cleanDisplayName);
                    if (string.IsNullOrWhiteSpace(actionName)) return;

                    // Only update if language is swapped
                    if (!isLanguageSwapped) return;

                    // Put back target indicator if it was present
                    string displayText = string.IsNullOrWhiteSpace(targetIndicator) ? actionName : actionName + " " + targetIndicator;

                    // Update the text node
                    textNode -> SetText(displayText);
                }
            }
        }
        catch (Exception ex)
        {
            // Get the addon name
            string addonName = GetAddonName(addonType);

            // Log
            Log.Error(ex, $"{Class} - Error updating {addonName} addon");
        }
    }

    // ----------------------------
    // Get cast field
    // ----------------------------
    private int GetCastField(AddonType addonType)
    {
        return addonType switch
        {
            AddonType.CastBar       => config.CastBarField,
            AddonType.TargetInfo    => config.TargetInfoField,
            AddonType.TargetCastBar => config.TargetCastBarField,
            AddonType.FocusCastBar  => config.FocusCastBarField,
            AddonType.PartyList     => config.PartyListCastField,
            AddonType.HateList      => config.HateListCastField,
            _                       => throw new ArgumentOutOfRangeException(nameof(addonType))
        };
    }

    // ----------------------------
    // Get addon name
    // ----------------------------
    private string GetAddonName(AddonType addonType)
    {
        return addonType switch
        {
            AddonType.CastBar       => config.CastBarName,
            AddonType.TargetInfo    => config.TargetInfoName,
            AddonType.TargetCastBar => config.TargetCastBarName,
            AddonType.FocusCastBar  => config.FocusCastBarName,
            AddonType.PartyList     => config.PartyListName,
            AddonType.HateList      => config.HateListName,
            _                       => throw new ArgumentOutOfRangeException(nameof(addonType))
        };
    }

    // ----------------------------
    // Resolve an action name to display
    // ----------------------------
    private string? ResolveActionName(uint actionID, string displayName)
    {
        // Initialize resolve name
        string resolveName = displayName;

        // Get the action name from a different language
        Language differentLang = config.ClientLanguage == Language.English ? Language.Japanese : Language.English;
        string? checkName = translationCache.GetActionName(actionID, differentLang);

        // Check if name is obfuscated
        if (!string.IsNullOrWhiteSpace(checkName) && checkName.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal))
        {
            // Detect new obfuscated translation
            DetectObfuscatedTranslation(actionID, checkName, displayName);

            // Try to get a resolved name
            string? obfusctedName = GetObfuscatedTranslation(actionID, config.TargetLanguage);

            // Use the obfuscated name if found
            if (!string.IsNullOrWhiteSpace(obfusctedName)) resolveName = obfusctedName;
        }
        else
        {
            // Get action name from target language
            resolveName = translationCache.GetActionName(actionID, config.TargetLanguage) ?? string.Empty;
        }

        // Check for alternative translation
        string? alternativeName = GetAlternativeTranslation(resolveName, config.AlternativeTranslations);
        if (!string.IsNullOrWhiteSpace(alternativeName)) resolveName = alternativeName;

        // Return resolve name
        return resolveName;
    }

    // ----------------------------
    // Get obfuscated translation
    // ----------------------------
    private string? GetObfuscatedTranslation(uint actionID, Language targetLanguage)
    {
        // Check for valid action ID
        if (!IsValidActionID(actionID)) return null;

        // Obfuscated translations sources in priority order
        List<ObfuscatedTranslation>[] obfuscatedTranslationsSources =
        [
            config.RemoteObfuscatedTranslations,
            config.ScannedObfuscatedTranslations,
            config.LocalObfuscatedTranslations
        ];

        // Search for translation in each source
        foreach (List<ObfuscatedTranslation> obfuscatedTranslations in obfuscatedTranslationsSources)
        {
            // Find obfuscated translation by action ID
            ObfuscatedTranslation? obfuscatedTranslation = obfuscatedTranslations.FindLast(translation => translation.ID == actionID);
            if (obfuscatedTranslation != null)
            {
                // Get the name for the target language 
                string? result = targetLanguage switch
                {
                    Language.Japanese => obfuscatedTranslation.JapaneseName,
                    Language.English  => obfuscatedTranslation.EnglishName,
                    Language.German   => obfuscatedTranslation.GermanName,
                    Language.French   => obfuscatedTranslation.FrenchName,
                    _                 => obfuscatedTranslation.EnglishName,
                };

                // Return the result if valid
                if (!string.IsNullOrWhiteSpace(result)) return result;
            }
        }
        return null;
    }

    // ----------------------------
    // Detect obfuscated translation
    // ----------------------------
    private void DetectObfuscatedTranslation(uint actionID, string obfuscatedName, string displayName)
    {
        // Check if ID, obfuscated name or display name are invalid
        if (!IsValidActionID(actionID) || string.IsNullOrWhiteSpace(obfuscatedName) || string.IsNullOrWhiteSpace(displayName)) return;

        // Get the client language
        Language clientLanguage = config.ClientLanguage;

        // Check if already scanned
        int existingIndex = config.ScannedObfuscatedTranslations.FindIndex(translation => translation.ID == (int)actionID);
        if (existingIndex >= 0)
        {
            // Already scanned - check if the client language name is missing
            ObfuscatedTranslation existing = config.ScannedObfuscatedTranslations[existingIndex];

            // Get the current name
            string currentName = clientLanguage switch
            {
                Language.Japanese => existing.JapaneseName,
                Language.English  => existing.EnglishName,
                Language.German   => existing.GermanName,
                Language.French   => existing.FrenchName,
                _                 => existing.EnglishName,
            };

            // Skip if the language name is already set
            if (!string.IsNullOrWhiteSpace(currentName)) return;

            // Update the existing entry with the missing language name
            switch (clientLanguage)
            {
                case Language.Japanese: existing.JapaneseName = displayName; break;
                case Language.English:  existing.EnglishName  = displayName; break;
                case Language.German:   existing.GermanName   = displayName; break;
                case Language.French:   existing.FrenchName   = displayName; break;
            }

            // Update the scanned translations and save config
            config.ScannedObfuscatedTranslations[existingIndex] = existing;
            config.Save();

            // Log
            Log.Information($"{Class} - Updated scanned obfuscation : ID = {actionID}, {displayName} ({clientLanguage}), {obfuscatedName}");
        }
        else
        {
            // Create new scanned entry with the client language display name
            ObfuscatedTranslation scanned = new()
            {
                ID = (int)actionID,
                ObfuscatedName = obfuscatedName,
                EnglishName    = clientLanguage == Language.English  ? displayName : string.Empty,
                JapaneseName   = clientLanguage == Language.Japanese ? displayName : string.Empty,
                GermanName     = clientLanguage == Language.German   ? displayName : string.Empty,
                FrenchName     = clientLanguage == Language.French   ? displayName : string.Empty
            };

            // Add to scanned translations and save config
            config.ScannedObfuscatedTranslations.Add(scanned);
            config.Save();

            // Log
            Log.Information($"{Class} - Added scanned obfuscation : ID = {actionID}, {displayName} ({clientLanguage}), {obfuscatedName}");
        }
    }


    // ----------------------------
    // Get alternative translation
    // ----------------------------
    private static string? GetAlternativeTranslation(string spellName, List<AlternativeTranslation> alternativeTranslations)
    {
        // Check for null, empty spell name or empty translations list
        if (!string.IsNullOrWhiteSpace(spellName) && alternativeTranslations != null && alternativeTranslations.Count > 0)
        {
            // Find alternative translation by spell name
            return alternativeTranslations.FindLast(translation => translation.SpellName == spellName)?.AlternativeName ?? string.Empty;
        }
        return null;
    }

    // ----------------------------
    // Check if action ID is valid
    // ----------------------------
    private bool IsValidActionID(uint actionID)
    {
        return actionID > 0 && actionID <= config.MaxValidActionID;
    }

    // ----------------------------
    // Remove target indicator from text
    // ----------------------------
    private string[] RemoveTargetIndicator(string text)
    {
        // Check for null or empty text
        if (string.IsNullOrWhiteSpace(text)) return [string.Empty, string.Empty];

        // Get target indicator symbols from config
        char[] targetIndicatorSymbols = config.TargetIndicatorSymbols;

        // Initialize result with original text
        string[] result = [text, string.Empty];

        // Check if text contains any target indicator
        for (int i = 0; i < targetIndicatorSymbols.Length; i++)
        {
            if (text.Contains(targetIndicatorSymbols[i]))
            {
                // Remove target indicator symbol from text and store it
                result[0] = text.Replace(targetIndicatorSymbols[i].ToString(), "").Trim();
                result[1] = targetIndicatorSymbols[i].ToString();
                break;
            }
        }
        return result;
    }

    // ----------------------------
    // Remove ellipsis from text
    // ----------------------------
    private static string RemoveEllipsis(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) return text.Replace("...", "").Trim();
        else return text;
    }

}