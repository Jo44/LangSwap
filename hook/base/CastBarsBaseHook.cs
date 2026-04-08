using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.tool;
using LangSwap.translation;
using System;
using System.Collections.Generic;

namespace LangSwap.hook.@base;

// ----------------------------
// Base class for all castbars hooks
// ----------------------------
public unsafe abstract class CastBarsBaseHook(
    IAddonLifecycle addonLifecycle,
    Configuration config,
    IFramework framework,
    IObjectTable objectTable,
    ITargetManager targetManager,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : BaseHook(config, translationCache, utilities, log)
{
    // Core components
    protected IAddonLifecycle addonLifecycle = addonLifecycle;
    protected IFramework framework = framework;
    protected IObjectTable objectTable = objectTable;
    protected ITargetManager targetManager = targetManager;

    // ----------------------------
    // Update cast bar
    // ----------------------------
    protected virtual void UpdateCastBar(AtkUnitBase* addon, uint actionId, int fieldIndex, string addonName)
    {
        try
        {
            // Only update if language is swapped, we have a valid action ID and the addon is visible
            if (!isLanguageSwapped || actionId == 0 || addon == null || !addon -> IsVisible) return;

            // Get the text node
            AtkResNode* fieldNode = addon -> UldManager.NodeList[fieldIndex];
            if (fieldNode == null || fieldNode -> Type != NodeType.Text) return;
            AtkTextNode* textNode = (AtkTextNode*)fieldNode;
            if (textNode == null || textNode -> NodeText.Length == 0) return;

            // Resolve action name (handles obfuscation detection and alternative translations)
            string? actionName = ResolveActionName(actionId, textNode -> NodeText.ToString());
            if (actionName.IsNullOrWhitespace()) return;

            // Update the text node
            textNode -> SetText(actionName);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"[{GetType().Name}] - Error updating {addonName} addon");
        }
    }

    // ----------------------------
    // Update list
    // ----------------------------
    protected void UpdateList(AtkUnitBase* addon, int fieldIndex, int startField, int endField, bool ascending, ObjectKind objectKind, uint[] slotEntityIds)
    {
        // TODO : clean up
        try
        {
            if (!isLanguageSwapped || addon == null || !addon -> IsVisible) return;

            // Build current-tick EntityId -> CastActionId snapshot
            Dictionary<uint, uint> currentCasts = [];
            foreach (IGameObject obj in objectTable)
            {
                if (obj is not IBattleChara bc || !bc.IsCasting || bc.ObjectKind != objectKind) continue;
                uint castActionId = (uint)bc.CastActionId;
                if (castActionId > 0) currentCasts[bc.EntityId] = castActionId;
            }
            if (currentCasts.Count < 1) return;

            int step = ascending ? 1 : -1;
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

                int slotRelativeIndex = ascending ? slotIndex - startField : endField - slotIndex;
                uint entityId = slotRelativeIndex < slotEntityIds.Length ? slotEntityIds[slotRelativeIndex] : 0;

                if (entityId > 0 && currentCasts.TryGetValue(entityId, out uint directActionId))
                {
                    string[] textParts = utilities.RemoveTargetIndicator(textNode -> NodeText.ToString());
                    string targetIndicator = textParts[1];

                    string? actionName = ResolveActionName(directActionId, textParts[0]);
                    if (actionName.IsNullOrWhitespace()) continue;

                    textNode -> SetText(targetIndicator.IsNullOrWhitespace() ? actionName : actionName + " " + targetIndicator);
                }
            }
        }
        catch (Exception ex)
        {
            // TODO : clean up
            log.Error(ex, $"[{GetType().Name}] - Error updating list addon");
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

}