using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.ui.hooks.@base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LangSwap.ui.hooks;

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

    // Castbars addons
    private readonly AtkUnitBase* castBar = utilities.GetAddon(config.CastBarAddon, "castbar");
    private readonly AtkUnitBase* targetInfo = utilities.GetAddon(config.TargetInfoAddon, "target info");
    private readonly AtkUnitBase* targetCastBar = utilities.GetAddon(config.TargetCastBarAddon, "target castbar");
    private readonly AtkUnitBase* focusCastBar = utilities.GetAddon(config.FocusCastBarAddon, "focus castbar");
    private readonly AtkUnitBase* partyList = utilities.GetAddon(config.PartyListAddon, "party list");

    // Castbars fields
    private readonly int castBarField = config.CastBarField;
    private readonly int targetInfoField = config.TargetInfoField;
    private readonly int targetCastBarField = config.TargetCastBarField;
    private readonly int focusCastBarField = config.FocusCastBarField;
    private readonly int partyListStartField = config.PartyListStartField;
    private readonly int partyListEndField = config.PartyListEndField;
    private readonly int partyListCastField = config.PartyListCastField;

    // Tracking variables
    private uint currentActionId = 0;
    private uint currentTargetActionId = 0;
    private uint currentFocusActionId = 0;

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
            log.Debug($"{Class} - {hookName} hook enabled");
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
        utilities.RefreshAddon(castBar, "castbar");
        utilities.RefreshAddon(targetInfo, "target info");
        utilities.RefreshAddon(targetCastBar, "target castbar");
        utilities.RefreshAddon(focusCastBar, "focus castbar");
        utilities.RefreshAddon(partyList, "party list");
    }

    // ----------------------------
    // On framework update
    // ----------------------------
    private void OnFrameworkUpdate(IFramework framework)
    {
        try
        {
            // Check if language is swapped
            if (!isLanguageSwapped)
            {
                currentActionId = 0;
                currentTargetActionId = 0;
                currentFocusActionId = 0;
                listCasts.Clear();
                return;
            }

            // Get local player
            IPlayerCharacter? player = objectTable.LocalPlayer;
            if (player == null)
            {
                currentActionId = 0;
                currentTargetActionId = 0;
                currentFocusActionId = 0;
                listCasts.Clear();
                return;
            }

            // Get player's ID
            ulong playerId = player.GameObjectId;

            // Get player's target ID
            ulong targetId = player.TargetObjectId;
            
            // Get player's focus ID
            ulong focusId = targetManager.FocusTarget?.GameObjectId ?? 0;

            // Initialize tracking variables
            bool foundPlayer = false;
            bool foundTarget = false;
            bool foundFocus = false;
            HashSet<ulong> currentCasting = [];

            // Iterate through all battle NPCs
            foreach (IGameObject obj in objectTable)
            {
                // Filter for players
                if (obj == null || obj.ObjectKind != ObjectKind.Player) continue;

                // Filter for battle characters
                if (obj is not IBattleChara battleChara) continue;

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
                        // Add to current casting set
                        currentCasting.Add(battleChara.GameObjectId);

                        // Update player
                        if (isCharacter)
                        {
                            currentActionId = actionId;
                            foundPlayer = true;
                        }

                        // Update target
                        if (isTarget)
                        {
                            currentTargetActionId = actionId;
                            foundTarget = true;
                        }

                        // Update focus
                        if (isFocus)
                        {
                            currentFocusActionId = actionId;
                            foundFocus = true;
                        }

                        // Update party list
                        if (isCharacter || inPartyList)
                        {
                            listCasts[battleChara.GameObjectId] = actionId;
                        }
                    }
                }
            }

            // Reset if player not found
            if (!foundPlayer) currentActionId = 0;

            // Reset if target not found
            if (!foundTarget) currentTargetActionId = 0;

            // Reset if focus not found
            if (!foundFocus) currentFocusActionId = 0;

            // Clean up party list of non-casting members
            List<ulong> toRemove = [.. listCasts.Keys.Where(id => !currentCasting.Contains(id))];
            foreach (ulong id in toRemove)
            {
                listCasts.Remove(id);
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
            UpdateCastBar(castBar, currentActionId, castBarField, "castbar");
        }
    }

    // ----------------------------
    // On target info update
    // ----------------------------
    private void OnTargetInfoUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsTarget)
        {
            UpdateCastBar(targetInfo, currentTargetActionId, targetInfoField, "target info");
        }
    }

    // ----------------------------
    // On target cast bar update
    // ----------------------------
    private void OnTargetCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsTarget)
        {
            UpdateCastBar(targetCastBar, currentTargetActionId, targetCastBarField, "target castbar");
        }
    }

    // ----------------------------
    // On focus cast bar update
    // ----------------------------
    private void OnFocusCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsFocus)
        {
            UpdateCastBar(focusCastBar, currentFocusActionId, focusCastBarField, "focus castbar");
        }
    }

    // ----------------------------
    // On party list update
    // ----------------------------
    private void OnPartyListUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsPartyList)
        {
            UpdateList(partyList, partyListStartField, partyListEndField, partyListCastField);
        }
    }

    // ----------------------------
    // Translate the cast text in the party list slot
    // ----------------------------
    protected override void TranslateCastText(AtkTextNode* textNode)
    {
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
            string? clientActionName = translationCache.GetActionName(actionId, (LanguageEnum)config.ClientLanguage);
            if (clientActionName == null) continue;

            // If the client language action name contains the current text, translate it
            if (clientActionName.StartsWith(textWithoutIndicator))
            {
                // Get the translated action name
                string? translatedName = translationCache.GetActionName(actionId, (LanguageEnum)config.TargetLanguage);
                if (!translatedName.IsNullOrWhitespace())
                {
                    if (!targetIndicator.IsNullOrWhitespace())
                    {
                        // Update the text node with the translated name and target indicator
                        textNode -> SetText(translatedName + " " + targetIndicator);
                        break;
                    }
                    else
                    {
                        // Update the text node with the translated name
                        textNode -> SetText(translatedName);
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
            log.Debug($"{Class} - {hookName} hook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to disable {hookName} hook");
        }
    }

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    public override void Dispose(string hookName)
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
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to dispose {hookName} hook");
        }
    }

}