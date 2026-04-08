using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
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

            // Get the cast field
            int castField = GetCastField(addonType);

            // Get the text node
            AtkResNode * fieldNode = addon -> UldManager.NodeList[castField];
            if (fieldNode == null || fieldNode -> Type != NodeType.Text) return;
            AtkTextNode* textNode = (AtkTextNode*)fieldNode;
            if (textNode == null || textNode -> NodeText.Length == 0) return;

            // Resolve action name
            string? actionName = ResolveActionName(actionId, textNode -> NodeText.ToString());
            if (actionName.IsNullOrWhitespace()) return;

            // Only update if language is swapped
            if (!isLanguageSwapped) return;

            // Update the text node
            textNode -> SetText(actionName);
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

            // Get current casts for objects in the list
            Dictionary<uint, uint> currentCasts = [];
            foreach (IGameObject obj in ObjectTable)
            {
                // Filter by object kind and casting status
                if (obj is not IBattleChara battleCharacter || battleCharacter.ObjectKind != objectKind || !battleCharacter.IsCasting) continue;

                // Add to current casts if we have a valid action ID
                uint actionID = (uint)battleCharacter.CastActionId;
                if (IsValidActionID(actionID)) currentCasts[battleCharacter.EntityId] = actionID;
            }

            // Skip if no current casts
            if (currentCasts.Count < 1) return;

            // Initialize loop parameters based on cast bars type
            bool ascending = castBarsType == CastBarsType.Allies;
            int step = ascending ? 1 : -1;
            int startField = castBarsType == CastBarsType.Allies ? config.PartyListStartField : config.EnmityListStartField;
            int endField = castBarsType   == CastBarsType.Allies ? config.PartyListEndField   : config.EnmityListEndField;
            int fieldIndex = castBarsType == CastBarsType.Allies ? config.PartyListCastField  : config.EnmityListCastField;

            // Loop through the list slots
            for (int slotIndex = ascending ? startField : endField; ascending ? slotIndex <= endField : slotIndex >= startField; slotIndex += step)
            {
                // Get the slot node
                AtkResNode* slotNode = addon -> UldManager.NodeList[slotIndex];
                if (slotNode == null || !slotNode -> IsVisible() || (ushort)slotNode -> Type < 1000) break;

                // Get the component node
                AtkComponentNode* componentNode = (AtkComponentNode*)slotNode;
                if (componentNode -> Component == null) continue;

                // Get the uld manager
                AtkUldManager* uldManager = &componentNode -> Component -> UldManager;
                if (uldManager == null || uldManager -> NodeListCount == 0) continue;

                // Get the field node
                AtkResNode* fieldNode = uldManager -> NodeList[fieldIndex];
                if (fieldNode == null || fieldNode -> Type != NodeType.Text) continue;

                // Get the text node
                AtkTextNode* textNode = (AtkTextNode*)fieldNode;
                if (textNode == null || textNode -> NodeText.Length == 0) continue;

                // Calculate the relative index of the slot within the list
                int slotRelativeIndex = ascending ? slotIndex - startField : endField - slotIndex;

                // Get the entity ID for this slot
                uint entityID = slotRelativeIndex < entityIDs.Length ? entityIDs[slotRelativeIndex] : 0;

                // Check if we have a current cast for this entity ID
                if (entityID > 0 && currentCasts.TryGetValue(entityID, out uint directActionID))
                {
                    // Get the current display name
                    string currentDisplayName = textNode -> NodeText.ToString();

                    // Remove any target indicator and store it
                    string[] textParts = RemoveTargetIndicator(currentDisplayName);
                    string targetIndicator = textParts[1];

                    // Remove any ellipsis
                    textParts[0] = RemoveEllipsis(textParts[0]);

                    // Resolve action name
                    string? actionName = ResolveActionName(directActionID, textParts[0]);
                    if (actionName.IsNullOrWhitespace()) continue;
                    
                    // Only update if language is swapped
                    if (!isLanguageSwapped) return;

                    // Update the text node
                    textNode -> SetText(targetIndicator.IsNullOrWhitespace() ? actionName : actionName + " " + targetIndicator);
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
    // Resolve an action name to display
    // ----------------------------
    private string? ResolveActionName(uint actionID, string currentDisplayName)
    {
        // Get the action name
        string? actionName = translationCache.GetActionName(actionID, config.TargetLanguage);
        if (actionName.IsNullOrWhitespace()) return null;

        // Check if the action name is obfuscated
        if (actionName.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal))
        {
            // Try to get a resolved name for this obfuscated action ID
            string? resolvedName = GetObfuscatedTranslation(actionID, config.TargetLanguage);
            if (!resolvedName.IsNullOrWhitespace())
            {
                // If we have a resolved name, use it instead of the obfuscated one
                actionName = resolvedName;
            }
            else
            {
                // If we don't have a resolved name, scan it for future reference
                ScanObfuscatedTranslation(actionID, actionName, currentDisplayName, config.ClientLanguage);
                return null;
            }
        }

        // Check for alternative translation for this action name
        string? alternativeName = GetAlternativeTranslation(actionName, config.AlternativeTranslations);
        if (!alternativeName.IsNullOrWhitespace()) actionName = alternativeName;

        // Return the final action name to display
        return actionName;
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
                return targetLanguage switch
                {
                    Language.Japanese => obfuscatedTranslation.JapaneseName,
                    Language.English  => obfuscatedTranslation.EnglishName,
                    Language.German   => obfuscatedTranslation.GermanName,
                    Language.French   => obfuscatedTranslation.FrenchName,
                    _                 => obfuscatedTranslation.EnglishName,
                };
            }
        }
        return null;
    }

    // ----------------------------
    // Scan an obfuscated translation discovered
    // ----------------------------
    private void ScanObfuscatedTranslation(uint actionID, string obfuscatedName, string displayName, Language clientLanguage)
    {
        // Check for valid action ID
        if (!IsValidActionID(actionID)) return;

        // Check if obfuscated name or display name are invalid
        if (obfuscatedName.IsNullOrWhitespace() || displayName.IsNullOrWhitespace()) return;

        // Check if already scanned
        int existingIndex = config.ScannedObfuscatedTranslations.FindIndex(translation => translation.ID == (int)actionID);
        if (existingIndex >= 0)
        {
            // Already scanned - check if the client language name is missing
            ObfuscatedTranslation existing = config.ScannedObfuscatedTranslations[existingIndex];

            string? currentLanguageName = clientLanguage switch
            {
                Language.Japanese => existing.JapaneseName,
                Language.English  => existing.EnglishName,
                Language.German   => existing.GermanName,
                Language.French   => existing.FrenchName,
                _                 => existing.EnglishName,
            };

            // Skip if the language name is already set
            if (!currentLanguageName.IsNullOrWhitespace()) return;

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
            Log.Information($"{Class} - Updated scanned obfuscated translation: ID = {actionID}, {clientLanguage} = {displayName}");
            return;
        }

        // Create new scanned entry with the client language display name
        ObfuscatedTranslation scanned = new()
        {
            ID = (int)actionID,
            ObfuscatedName = obfuscatedName
        };

        // Set the display name for the client language
        switch (clientLanguage)
        {
            case Language.Japanese: scanned.JapaneseName = displayName; break;
            case Language.English:  scanned.EnglishName  = displayName; break;
            case Language.German:   scanned.GermanName   = displayName; break;
            case Language.French:   scanned.FrenchName   = displayName; break;
        }

        // Add to scanned translations and save config
        config.ScannedObfuscatedTranslations.Add(scanned);
        config.Save();

        // Log
        Log.Information($"{Class} - Scanned : ID = {actionID}, Obfuscated = {obfuscatedName}, {clientLanguage} = {displayName}");
    }


    // ----------------------------
    // Get alternative translation
    // ----------------------------
    private static string? GetAlternativeTranslation(string spellName, List<AlternativeTranslation> alternativeTranslations)
    {
        // Check for null, empty spell name or empty translations list
        if (!spellName.IsNullOrWhitespace() && alternativeTranslations != null && alternativeTranslations.Count > 0)
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
        return actionID >= 0 && actionID <= config.MaxValidActionID;
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
            AddonType.EnmityList    => config.EnmityListCastField,
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
            AddonType.EnmityList    => config.EnmityListName,
            _                       => throw new ArgumentOutOfRangeException(nameof(addonType))
        };
    }

}