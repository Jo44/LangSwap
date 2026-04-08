using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
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
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;

    // Symbols
    private readonly char[] targetIndicatorSymbols = config.TargetIndicatorSymbols;

    // ----------------------------
    // Update cast bar
    // ----------------------------
    protected virtual void UpdateCastBar(AtkUnitBase* addon, AddonType addonType, uint actionId)
    {
        try
        {
            // Check if we have a valid action ID and the addon is visible
            if (actionId == 0 || actionId > config.MaxValidActionId || addon == null || !addon -> IsVisible) return;

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
            // Check if the addon is visible and we have at least one valid entity ID
            if (!isLanguageSwapped || addon == null || !addon -> IsVisible) return;

            ObjectKind objectKind = castBarsType == CastBarsType.Allies ? ObjectKind.Player : ObjectKind.BattleNpc;

            // Build current-tick EntityId -> CastActionId snapshot
            Dictionary<uint, uint> currentCasts = [];
            foreach (IGameObject obj in ObjectTable)
            {
                if (obj is not IBattleChara bc || !bc.IsCasting || bc.ObjectKind != objectKind) continue;
                uint castActionId = (uint)bc.CastActionId;
                if (castActionId > 0) currentCasts[bc.EntityId] = castActionId;
            }
            if (currentCasts.Count < 1) return;

            // TODO
            bool ascending = castBarsType == CastBarsType.Allies;
            int step = ascending ? 1 : -1;
            int startField = castBarsType == CastBarsType.Allies ? config.PartyListStartField : config.EnmityListStartField;
            int endField = castBarsType == CastBarsType.Allies ? config.PartyListEndField : config.EnmityListEndField;
            int fieldIndex = castBarsType == CastBarsType.Allies ? config.PartyListCastField : config.EnmityListCastField;

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
                uint entityId = slotRelativeIndex < entityIDs.Length ? entityIDs[slotRelativeIndex] : 0;

                if (entityId > 0 && currentCasts.TryGetValue(entityId, out uint directActionId))
                {
                    // Get the current display name
                    string currentDisplayName = textNode -> NodeText.ToString();

                    // Remove any target indicator and store it
                    string[] textParts = RemoveTargetIndicator(currentDisplayName);
                    string targetIndicator = textParts[1];

                    // Remove any ellipsis
                    textParts[0] = RemoveEllipsis(textParts[0]);

                    // Resolve action name
                    string? actionName = ResolveActionName(directActionId, textParts[0]);
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
    // Resolve an action name for display, handling obfuscation and alternatives
    // Returns null if the name should not be displayed (unknown obfuscated or empty)
    // ----------------------------
    private string? ResolveActionName(uint actionId, string currentDisplayName)
    {
        // TODO : clean up
        string? actionName = translationCache.GetActionName(actionId, config.TargetLanguage);
        if (actionName.IsNullOrWhitespace()) return null;

        if (actionName.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal))
        {
            string? resolvedName = GetObfuscatedTranslation(actionId, config.TargetLanguage);
            if (!resolvedName.IsNullOrWhitespace())
            {
                actionName = resolvedName;
            }
            else
            {
                ScanObfuscatedTranslation(actionId, actionName, currentDisplayName, config.ClientLanguage);
                return null;
            }
        }

        string? alternativeName = GetAlternativeTranslation(actionName, config.AlternativeTranslations);
        if (!alternativeName.IsNullOrWhitespace()) actionName = alternativeName;

        return actionName;
    }

    // ----------------------------
    // Get obfuscated translation
    // ----------------------------
    private string? GetObfuscatedTranslation(uint actionId, Language targetLanguage)
    {
        // Check for valid action ID
        if (actionId <= 0) return null;

        // Priority order : remote -> scanned -> local
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
            ObfuscatedTranslation? obfuscatedTranslation = obfuscatedTranslations.FindLast(translation => translation.Id == actionId);
            if (obfuscatedTranslation != null)
            {
                return targetLanguage switch
                {
                    Language.Japanese => obfuscatedTranslation.JapaneseName,
                    Language.English => obfuscatedTranslation.EnglishName,
                    Language.German => obfuscatedTranslation.GermanName,
                    Language.French => obfuscatedTranslation.FrenchName,
                    _ => obfuscatedTranslation.EnglishName,
                };
            }
        }
        return null;
    }

    // ----------------------------
    // Scan an obfuscated translation discovered
    // ----------------------------
    private void ScanObfuscatedTranslation(uint actionId, string obfuscatedName, string displayName, Language clientLanguage)
    {
        // Skip if action ID, obfuscated name or display name are invalid
        if (actionId < 0 || actionId > config.MaxValidActionId || obfuscatedName.IsNullOrWhitespace() || displayName.IsNullOrWhitespace()) return;

        // Skip if already scanned
        if (config.ScannedObfuscatedTranslations.FindIndex(translation => translation.Id == (int)actionId) >= 0) return;

        // TODO : can already be scanned but missing some language display name, in that case we should update the existing entry instead of creating a new one

        // Create new scanned entry with the client language display name
        ObfuscatedTranslation scanned = new()
        {
            Id = (int)actionId,
            ObfuscatedName = obfuscatedName
        };

        switch (clientLanguage)
        {
            case Language.Japanese: scanned.JapaneseName = displayName; break;
            case Language.English: scanned.EnglishName = displayName; break;
            case Language.German: scanned.GermanName = displayName; break;
            case Language.French: scanned.FrenchName = displayName; break;
        }

        config.ScannedObfuscatedTranslations.Add(scanned);
        config.Save();

        Log.Information($"{Class} - Scanned obfuscated translation: ID={actionId}, Obfuscated={obfuscatedName}, {clientLanguage}={displayName}");
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

    // ----------------------------
    // Remove target indicator from text
    // ----------------------------
    private string[] RemoveTargetIndicator(string text)
    {
        // Check for null or empty text
        if (string.IsNullOrWhiteSpace(text)) return [string.Empty, string.Empty];

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