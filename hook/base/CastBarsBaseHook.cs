using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.tool;
using LangSwap.translation;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    // Log
    private const string Class = "[CastBarsBaseHook.cs]";

    // Core components
    protected IAddonLifecycle addonLifecycle = addonLifecycle;
    protected IFramework framework = framework;
    protected IObjectTable objectTable = objectTable;
    protected ITargetManager targetManager = targetManager;

    // Tracking variables
    protected readonly Dictionary<ulong, uint> alliesListCasts = [];
    protected readonly Dictionary<ulong, long> alliesListCastsExpiry = [];
    protected readonly Dictionary<ulong, uint> enemiesListCasts = [];
    protected readonly Dictionary<ulong, long> enemiesListCastsExpiry = [];
    // TODO : 30 sec de rétention des noms de sorts dans les listes
    protected const long ListCastExpiryTicks = 30L * 10_000_000L;

    // ----------------------------
    // Check if member is in list
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
            if (!isLanguageSwapped || (alliesListCasts.Count < 1 && enemiesListCasts.Count < 1) || addon == null || !addon -> IsVisible) return;

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

    // ----------------------------
    // Clean expired allies list casts
    // ----------------------------
    protected void CleanExpiredAlliesListCasts()
    {
        long now = Stopwatch.GetTimestamp() * 10_000_000L / Stopwatch.Frequency;
        List<ulong> toRemove = [];
        foreach (ulong id in alliesListCastsExpiry.Keys)
        {
            if (now - alliesListCastsExpiry[id] > ListCastExpiryTicks) toRemove.Add(id);
        }
        foreach (ulong id in toRemove)
        {
            alliesListCasts.Remove(id);
            alliesListCastsExpiry.Remove(id);
        }
    }

    // ----------------------------
    // Clean expired enemies list casts
    // ----------------------------
    protected void CleanExpiredEnemiesListCasts()
    {
        long now = Stopwatch.GetTimestamp() * 10_000_000L / Stopwatch.Frequency;
        List<ulong> toRemove = [];
        foreach (ulong id in enemiesListCastsExpiry.Keys)
        {
            if (now - enemiesListCastsExpiry[id] > ListCastExpiryTicks) toRemove.Add(id);
        }
        foreach (ulong id in toRemove)
        {
            enemiesListCasts.Remove(id);
            enemiesListCastsExpiry.Remove(id);
        }
    }

}