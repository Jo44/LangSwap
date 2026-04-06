using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.hook.@base;
using LangSwap.tool;
using LangSwap.translation;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LangSwap.hook;

// ----------------------------
// Allies CastBars Hook
// ----------------------------
public unsafe class AlliesCastBarsHook(
    IAddonLifecycle addonLifecycle,
    Configuration config,
    IFramework framework,
    IObjectTable objectTable,
    ITargetManager targetManager,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : CastBarsBaseHook(addonLifecycle, config, framework, objectTable, targetManager, translationCache, utilities, log)
{
    // Log
    private const string Class = "[AlliesCastBarsHook.cs]";

    // UI components
    private bool castBarsTarget = false;
    private bool castBarsFocus = false;
    private bool castBarsPartyList = false;

    // Castbars fields
    private readonly int castBarField = config.CastBarField;
    private readonly int targetInfoField = config.TargetInfoField;
    private readonly int targetCastBarField = config.TargetCastBarField;
    private readonly int focusCastBarField = config.FocusCastBarField;
    private readonly int partyListStartField = config.PartyListStartField;
    private readonly int partyListEndField = config.PartyListEndField;
    private readonly int partyListCastField = config.PartyListCastField;

    // Action IDs
    private uint currentActionId = 0;
    private uint currentAllyTargetActionId = 0;
    private uint currentAllyFocusActionId = 0;

    // ----------------------------
    // Enable the hook
    // ----------------------------
    public override void Enable(string hookName)
    {
        // Prevent multiple enables
        if (isEnabled) return;

        try
        {
            // Subscribe to framework update
            framework.Update += OnFrameworkUpdate;

            // Register addon lifecycle listeners
            addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.CastBarAddon, OnCastBarUpdate);
            addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.PartyListAddon, OnPartyListUpdate);

            // Set enabled flag
            isEnabled = true;

            // Log
            log.Information($"{Class} - {hookName} hook enabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to enable {hookName} hook");
        }
    }

    // ----------------------------
    // On language swap
    // ----------------------------
    protected override void OnLanguageSwap()
    {
        // Initialize UI components
        castBarsTarget = config.AlliesCastBarsTarget;
        castBarsFocus = config.AlliesCastBarsFocus;
        castBarsPartyList = config.AlliesCastBarsPartyList;

        // Refresh addons
        utilities.RefreshAddon(utilities.GetAddon(config.CastBarAddon), "castbar");
        utilities.RefreshAddon(utilities.GetAddon(config.TargetInfoAddon), "target info");
        utilities.RefreshAddon(utilities.GetAddon(config.TargetCastBarAddon), "target castbar");
        utilities.RefreshAddon(utilities.GetAddon(config.FocusCastBarAddon), "focus castbar");
        utilities.RefreshAddon(utilities.GetAddon(config.PartyListAddon), "party list");
    }

    // ----------------------------
    // On framework update
    // ----------------------------
    protected override void OnFrameworkUpdate(IFramework framework)
    {
        try
        {
            // Check if language is swapped
            if (!isLanguageSwapped)
            {
                currentActionId = 0;
                currentAllyTargetActionId = 0;
                currentAllyFocusActionId = 0;
                listCasts.Clear();
                listCastsExpiry.Clear();
                return;
            }

            // Get local player
            IPlayerCharacter? player = objectTable.LocalPlayer;
            if (player == null)
            {
                currentActionId = 0;
                currentAllyTargetActionId = 0;
                currentAllyFocusActionId = 0;
                listCasts.Clear();
                listCastsExpiry.Clear();
                return;
            }

            // Get player's ID
            ulong playerId = player.GameObjectId;

            // Get player's target ID
            ulong targetId = player.TargetObjectId;
            
            // Get player's focus ID
            ulong focusId = targetManager.FocusTarget?.GameObjectId ?? 0;

            // Clean expired list casts
            CleanExpiredListCasts();

            // Iterate through all players
            foreach (IGameObject obj in objectTable)
            {
                // Filter for players
                if (obj == null || obj.ObjectKind != ObjectKind.Player || obj is not IPlayerCharacter || obj is not IBattleChara battleChara) continue;

                // Check if this character is the current player
                bool isCharacter = battleChara.GameObjectId == playerId;
                
                // Check if this character is the current player's target
                bool isTarget = battleChara.GameObjectId == targetId;

                // Check if this character is the current player's focus
                bool isFocus = battleChara.GameObjectId == focusId;

                // Check if this character is in the current player's party list
                bool inPartyList = IsInList(battleChara, StatusFlags.PartyMember);

                // Skip if not relevant
                if (!isCharacter && !isTarget && !isFocus && !inPartyList) continue;

                // Check if casting
                if (battleChara.IsCasting)
                {
                    // Get action ID
                    uint actionId = (uint)battleChara.CastActionId;
                    if (actionId > 0)
                    {
                        // Update player
                        if (isCharacter) currentActionId = actionId;

                        // Update target
                        if (isTarget) currentAllyTargetActionId = actionId;

                        // Update focus
                        if (isFocus) currentAllyFocusActionId = actionId;

                        // Update party list
                        if (isCharacter || inPartyList)
                        {
                            listCasts[battleChara.GameObjectId] = actionId;
                            listCastsExpiry[battleChara.GameObjectId] = Stopwatch.GetTimestamp() * 10_000_000L / Stopwatch.Frequency;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error in OnFrameworkUpdate");
        }
    }

    // ----------------------------
    // On cast bar update
    // ----------------------------
    private void OnCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsTarget || castBarsFocus || castBarsPartyList)
        {
            UpdateCastBar(utilities.GetAddon(config.CastBarAddon), currentActionId, castBarField, "castbar");
        }
    }

    // ----------------------------
    // On target info update
    // ----------------------------
    private void OnTargetInfoUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsTarget)
        {
            UpdateCastBar(utilities.GetAddon(config.TargetInfoAddon), currentAllyTargetActionId, targetInfoField, "target info");
        }
    }

    // ----------------------------
    // On target cast bar update
    // ----------------------------
    private void OnTargetCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsTarget)
        {
            UpdateCastBar(utilities.GetAddon(config.TargetCastBarAddon), currentAllyTargetActionId, targetCastBarField, "target castbar");
        }
    }

    // ----------------------------
    // On focus cast bar update
    // ----------------------------
    private void OnFocusCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsFocus)
        {
            UpdateCastBar(utilities.GetAddon(config.FocusCastBarAddon), currentAllyFocusActionId, focusCastBarField, "focus castbar");
        }
    }

    // ----------------------------
    // On party list update
    // ----------------------------
    private void OnPartyListUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsPartyList)
        {
            UpdateList(utilities.GetAddon(config.PartyListAddon), partyListCastField);
        }
    }

    // ----------------------------
    // Update cast bar
    // ----------------------------
    protected override void UpdateCastBar(AtkUnitBase* addon, uint actionId, int fieldIndex, string addonName)
    {
        try
        {
            // Only update if language is swapped, we have a valid action ID and the addon is visible
            if (!isLanguageSwapped || actionId == 0 || addon == null || !addon -> IsVisible) return;

            // Get action name
            string? actionName = translationCache.GetActionName(actionId, config.TargetLanguage);
            if (actionName.IsNullOrWhitespace()) return;

            // Check for alternative translation
            string? alternativeName = Utilities.GetAlternativeTranslation(actionName, config.AlternativeTranslations);
            if (!alternativeName.IsNullOrWhitespace()) actionName = alternativeName;

            // Get the text node
            AtkResNode* fieldNode = addon -> UldManager.NodeList[fieldIndex];
            if (fieldNode == null || fieldNode -> Type != NodeType.Text) return;
            AtkTextNode* textNode = (AtkTextNode*)fieldNode;
            if (textNode == null || textNode -> NodeText.Length == 0) return;

            // Update the text node
            textNode -> SetText(actionName);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error updating {addonName} addon");
        }
    }

    // ----------------------------
    // Update list
    // ----------------------------
    protected override void UpdateList(AtkUnitBase* addon, int fieldIndex)
    {
        try
        {
            // Only update if language is swapped, we have casts to translate and the addon is visible
            if (!isLanguageSwapped || (listCasts.Count < 1) || addon == null || !addon -> IsVisible) return;

            // Process each slot in the list
            for (int slotIndex = partyListStartField; slotIndex <= partyListEndField; slotIndex++)
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
                AtkResNode* fieldNode = uldManager -> NodeList[fieldIndex];
                if (fieldNode == null || fieldNode -> Type != NodeType.Text) return;

                // Get the text node
                AtkTextNode* textNode = (AtkTextNode*)fieldNode;
                if (textNode == null || textNode -> NodeText.Length == 0) return;

                // Translate the slot
                TranslateSlot(textNode);
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error updating partylist addon");
        }
    }

    // ----------------------------
    // Translate the slot
    // ----------------------------
    protected override void TranslateSlot(AtkTextNode* textNode)
    {
        // TODO : change name
        // Get the current text
        string currentText = textNode -> NodeText.ToString();
        if (string.IsNullOrWhiteSpace(currentText)) return;

        // Remove target indicator for comparison
        string[] textParts = utilities.RemoveTargetIndicator(currentText);
        string textWithoutIndicator = Utilities.RemoveEllipsis(textParts[0]);
        string targetIndicator = textParts[1];

        // Check if the current text contains any of the casts in the party list and translate it
        foreach (KeyValuePair<ulong, uint> cast in listCasts)
        {
            // Get the action ID
            uint actionId = cast.Value;

            // Get the client language action name
            string? clientActionName = translationCache.GetActionName(actionId, config.ClientLanguage);
            if (clientActionName.IsNullOrWhitespace()) continue;

            // If the client language action name contains the current text, translate it
            if (clientActionName.StartsWith(textWithoutIndicator))
            {
                // Get the target language action name
                string? actionName = translationCache.GetActionName(actionId, config.TargetLanguage);
                if (!actionName.IsNullOrWhitespace())
                {
                    // Check for alternative translation
                    string? alternativeName = Utilities.GetAlternativeTranslation(actionName, config.AlternativeTranslations);
                    if (!alternativeName.IsNullOrWhitespace()) actionName = alternativeName;

                    // If the original text had a target indicator, preserve it in the translation
                    if (!targetIndicator.IsNullOrWhitespace())
                    {
                        // Update the text node with the action name and target indicator
                        textNode -> SetText(actionName + " " + targetIndicator);
                        break;
                    }
                    else
                    {
                        // Update the text node with the action name
                        textNode -> SetText(actionName);
                        break;
                    }
                }
            }
        }
    }

    // ----------------------------
    // Disable the hook
    // ----------------------------
    public override void Disable(string hookName)
    {
        // Prevent multiple disables
        if (!isEnabled) return;

        try
        {
            // Unsubscribe from framework update
            framework.Update -= OnFrameworkUpdate;

            // Unregister addon lifecycle listeners
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.CastBarAddon, OnCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.PartyListAddon, OnPartyListUpdate);

            // Set disabled flag
            isEnabled = false;
            log.Information($"{Class} - {hookName} hook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to disable {hookName} hook");
        }
    }

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    protected override void Dispose(string hookName)
    {
        try
        {
            // Unsubscribe from framework update
            framework.Update -= OnFrameworkUpdate;

            // Unregister addon lifecycle listeners
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.CastBarAddon, OnCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.PartyListAddon, OnPartyListUpdate);

            // Set disabled flag
            isEnabled = false;

            // Dispose base resources
            Dispose();
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to dispose {hookName} hook");
        }
    }

}