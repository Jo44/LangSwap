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
// Base class for all cast bars hooks
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
    // Log
    private const string Class = "[CastBarsBaseHook.cs]";

    // Core components
    protected IAddonLifecycle addonLifecycle = addonLifecycle;
    protected IFramework framework = framework;
    protected IObjectTable objectTable = objectTable;
    protected ITargetManager targetManager = targetManager;

    // Tracking variables
    protected readonly Dictionary<ulong, uint> listCasts = [];

    // Linger counts
    protected readonly Dictionary<ulong, int> lingeringCasts = [];
    protected const int CastLingerFrames = 5;

    // ----------------------------
    // Check if member is in list (party/enmity)
    // ----------------------------
    protected static bool IsInList(IBattleChara character, StatusFlags flag)
    {
        bool inList = false;
        try
        {
            if (character != null && character.StatusFlags.HasFlag(flag)) inList = true;
        }
        catch
        {
            inList = false;
        }
        return inList;
    }


    // ----------------------------
    // Update cast bar
    // ----------------------------
    protected void UpdateCastBar(AtkUnitBase* addon, uint actionId, int fieldIndex, string addonName)
    {
        try
        {
            // Only update if language is swapped, we have a valid action ID and the addon is visible
            if (!isLanguageSwapped || actionId == 0 || addon == null || !addon -> IsVisible) return;

            // Get translated action name
            string? translatedName = translationCache.GetActionName(actionId, config.TargetLanguage);
            if (translatedName.IsNullOrWhitespace()) return;

            // Get the text node
            AtkResNode* fieldNode = addon -> UldManager.NodeList[fieldIndex];
            if (fieldNode == null || fieldNode -> Type != NodeType.Text) return;

            // Update text
            AtkTextNode* textNode = (AtkTextNode*)fieldNode;
            if (textNode != null && textNode -> NodeText.Length > 0) textNode -> SetText(translatedName);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error updating {addonName} addon");
        }
    }

    // ----------------------------
    // Update list
    // ----------------------------
    protected void UpdateList(AtkUnitBase* addon, int listStartField, int listEndField, int castField)
    {
        try
        {
            // Only update if language is swapped, we have casts to translate and the addon is visible
            if (!isLanguageSwapped || listCasts.Count < 1 || addon == null || !addon -> IsVisible) return;

            // Process each slot in the list
            for (int slotIndex = listStartField; slotIndex <= listEndField; slotIndex++)
            {
                ProcessSlot(addon, slotIndex, castField);
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error updating list addon");
        }
    }

    // ----------------------------
    // Process slot
    // ----------------------------
    protected void ProcessSlot(AtkUnitBase* addon, int slotIndex, int castField)
    {
        // Get the slot node
        AtkResNode* slotNode = addon -> UldManager.NodeList[slotIndex];
        if (slotNode == null || !slotNode -> IsVisible() || (ushort)slotNode -> Type < 1000) return;

        // Get the component node
        AtkComponentNode* componentNode = (AtkComponentNode*)slotNode;
        if (componentNode -> Component == null) return;

        // Get the uld manager
        AtkUldManager* uldManager = &componentNode -> Component -> UldManager;
        if (uldManager == null || uldManager -> NodeListCount == 0) return;

        // Get the field node
        AtkResNode* fieldNode = uldManager -> NodeList[castField];
        if (fieldNode == null || fieldNode -> Type != NodeType.Text) return;

        // Cast to text node
        AtkTextNode* textNode = (AtkTextNode*)fieldNode;
        if (textNode == null || textNode -> NodeText.Length == 0) return;

        // Translate the cast text
        TranslateCastText(textNode);
    }

    // ----------------------------
    // Translate cast text in the list slot
    // ----------------------------
    protected abstract void TranslateCastText(AtkTextNode* textNode);

}