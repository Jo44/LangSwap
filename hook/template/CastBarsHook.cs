using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.hook.@base;
using LangSwap.tool;
using LangSwap.translation;
using System;
using System.Collections.Generic;

namespace LangSwap.hook.template;

// ----------------------------
// Base class for all castbars hooks
// ----------------------------
public unsafe abstract class CastBarsHook(
    IAddonLifecycle addonLifecycle,
    Configuration config,
    IFramework framework,
    IObjectTable objectTable,
    ITargetManager targetManager,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : BaseHook(config, translationCache, utilities, log)
{
    // Log
    private const string Class = "[CastBarsHook.cs]";

    // Core components
    protected IAddonLifecycle addonLifecycle = addonLifecycle;
    protected IFramework framework = framework;
    protected IObjectTable objectTable = objectTable;
    protected ITargetManager targetManager = targetManager;

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
            log.Error(ex, $"{Class} - Error updating {addonName} addon");
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
            foreach (IGameObject obj in objectTable)
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
                    // Get the current display name and target indicator (if any)
                    string[] textParts = utilities.RemoveTargetIndicator(textNode -> NodeText.ToString());
                    string targetIndicator = textParts[1];

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
            log.Error(ex, $"{Class} - Error updating {addonName} addon");
        }
    }

    // ----------------------------
    // Check if character is in list
    // ----------------------------
    protected static bool IsInList(IBattleChara character, StatusFlags flag)
    {
        // Check if character has the specified status flag
        if (character != null && character.StatusFlags.HasFlag(flag)) return true;
        else return false;
    }

    // ----------------------------
    // Resolve an action name for display, handling obfuscation and alternatives
    // Returns null if the name should not be displayed (unknown obfuscated or empty)
    // ----------------------------
    protected string? ResolveActionName(uint actionId, string currentDisplayName)
    {
        // TODO : clean up
        string? actionName = translationCache.GetActionName(actionId, config.TargetLanguage);
        if (actionName.IsNullOrWhitespace()) return null;

        if (actionName.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal))
        {
            string? resolvedName = utilities.GetObfuscatedTranslation(actionId, config.TargetLanguage);
            if (!resolvedName.IsNullOrWhitespace())
            {
                actionName = resolvedName;
            }
            else
            {
                utilities.ScanObfuscatedTranslation(actionId, actionName, currentDisplayName, config.ClientLanguage);
                return null;
            }
        }

        string? alternativeName = Utilities.GetAlternativeTranslation(actionName, config.AlternativeTranslations);
        if (!alternativeName.IsNullOrWhitespace()) actionName = alternativeName;

        return actionName;
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