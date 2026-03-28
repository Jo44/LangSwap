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
    // Log
    private const string Class = "[CastBarsBaseHook.cs]";

    // Core components
    protected IAddonLifecycle addonLifecycle = addonLifecycle;
    protected IFramework framework = framework;
    protected IObjectTable objectTable = objectTable;
    protected ITargetManager targetManager = targetManager;

    // Lists of casts
    protected readonly Dictionary<ulong, uint> listCasts = [];
    protected readonly Dictionary<ulong, long> listCastsExpiry = [];
    protected const long ListCastExpiryTicks = 30L * 10_000_000L; // 30 seconds

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
    // Get display action name
    // ----------------------------
    protected string? GetDisplayActionName(uint actionId)
    {
        try
        {
            // Check for valid action ID
            if (actionId == 0) return null;

            // If target language is same as client language
            if (config.TargetLanguage == config.ClientLanguage)
            {
                // Get the client action name
                string? clientActionName = translationCache.GetActionName(actionId, config.ClientLanguage);
                if (clientActionName.IsNullOrWhitespace()) return null;

                // Check for alternative translation
                string? alternativeName = Utilities.GetAlternativeTranslation(clientActionName, config.AlternativeTranslations);
                if (!alternativeName.IsNullOrWhitespace() && !string.Equals(alternativeName, clientActionName, StringComparison.Ordinal))
                {
                    return alternativeName;
                }
                return null;
            }

            // Get the translated action name
            string? translatedName = translationCache.GetActionName(actionId, config.TargetLanguage);
            if (translatedName.IsNullOrWhitespace()) return null;

            // Check for alternative translation
            return Utilities.GetAlternativeTranslation(translatedName, config.AlternativeTranslations);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error resolving cast display name");
            return null;
        }
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

            // Get display action name
            string? displayName = GetDisplayActionName(actionId);
            if (displayName.IsNullOrWhitespace()) return;

            // Get the text node
            AtkResNode* fieldNode = addon -> UldManager.NodeList[fieldIndex];
            if (fieldNode == null || fieldNode -> Type != NodeType.Text) return;

            // Update text
            AtkTextNode* textNode = (AtkTextNode*)fieldNode;
            if (textNode != null && textNode -> NodeText.Length > 0) textNode -> SetText(displayName);
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
            if (!isLanguageSwapped || (listCasts.Count < 1 && listCasts.Count < 1) || addon == null || !addon -> IsVisible) return;

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