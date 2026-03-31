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
// Enemies CastBars Hook
// ----------------------------
public unsafe class EnemiesCastBarsHook(
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
    private const string Class = "[EnemiesCastBarsHook.cs]";

    // UI components
    private bool castBarsTarget = false;
    private bool castBarsFocus = false;
    private bool castBarsEnmityList = false;

    // Castbars fields
    private readonly int targetInfoField = config.TargetInfoField;
    private readonly int targetCastBarField = config.TargetCastBarField;
    private readonly int focusCastBarField = config.FocusCastBarField;
    private readonly int enemyListStartField = config.EnemyListStartField;
    private readonly int enemyListEndField = config.EnemyListEndField;
    private readonly int enemyListCastField = config.EnemyListCastField;

    // Action IDs
    private uint currentEnemyTargetActionId = 0;
    private uint currentEnemyFocusActionId = 0;

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
            addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.EnemyListAddon, OnEnemyListUpdate);

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
        castBarsTarget = config.EnemiesCastBarsTarget;
        castBarsFocus = config.EnemiesCastBarsFocus;
        castBarsEnmityList = config.EnemiesCastBarsEnmityList;

        // Refresh addons
        utilities.RefreshAddon(utilities.GetAddon(config.TargetInfoAddon), "target info");
        utilities.RefreshAddon(utilities.GetAddon(config.TargetCastBarAddon), "target castbar");
        utilities.RefreshAddon(utilities.GetAddon(config.FocusCastBarAddon), "focus castbar");
        utilities.RefreshAddon(utilities.GetAddon(config.EnemyListAddon), "enemy list");
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
                currentEnemyTargetActionId = 0;
                currentEnemyFocusActionId = 0;
                listCasts.Clear();
                listCastsExpiry.Clear();
                return;
            }

            // Get local player
            IPlayerCharacter? player = objectTable.LocalPlayer;
            if (player == null)
            {
                currentEnemyTargetActionId = 0;
                currentEnemyFocusActionId = 0;
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

            // Clean expired enemies list casts
            CleanExpiredEnemiesListCasts();

            // Iterate through all battle NPCs
            foreach (IGameObject obj in objectTable)
            {
                // Filter for battle NPCs
                if (obj == null || obj.ObjectKind != ObjectKind.BattleNpc || obj is not IBattleChara battleChara || obj is IPlayerCharacter) continue;

                // Check if this character is the current player's target
                bool isTarget = battleChara.GameObjectId == targetId;

                // Check if this character is the current player's focus
                bool isFocus = battleChara.GameObjectId == focusId;

                // Check if this character is in the current player's enmity list
                bool inEnmityList = IsInList(battleChara, StatusFlags.InCombat);

                // Skip if not relevant
                if (!isTarget && !isFocus && !inEnmityList) continue;

                // Check if casting
                if (battleChara.IsCasting)
                {
                    // Get action ID
                    uint actionId = (uint)battleChara.CastActionId;
                    if (actionId > 0)
                    {
                        // Update target
                        if (isTarget) currentEnemyTargetActionId = actionId;

                        // Update focus
                        if (isFocus) currentEnemyFocusActionId = actionId;

                        // Update enemy list
                        if (inEnmityList)
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
    // On target info update
    // ----------------------------
    private void OnTargetInfoUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsTarget)
        {
            UpdateCastBar(utilities.GetAddon(config.TargetInfoAddon), currentEnemyTargetActionId, targetInfoField, "target info");
        }
    }

    // ----------------------------
    // On target cast bar update
    // ----------------------------
    private void OnTargetCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsTarget)
        {
            UpdateCastBar(utilities.GetAddon(config.TargetCastBarAddon), currentEnemyTargetActionId, targetCastBarField, "target castbar");
        }
    }

    // ----------------------------
    // On focus cast bar update
    // ----------------------------
    private void OnFocusCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsFocus)
        {
            UpdateCastBar(utilities.GetAddon(config.FocusCastBarAddon), currentEnemyFocusActionId, focusCastBarField, "focus castbar");
        }
    }

    // ----------------------------
    // On enemy list update
    // ----------------------------
    private void OnEnemyListUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsEnmityList)
        {
            UpdateList(utilities.GetAddon(config.EnemyListAddon), enemyListStartField, enemyListEndField, enemyListCastField);
        }
    }

    // ----------------------------
    // Translate the cast text in the enmity list slot
    // ----------------------------
    protected override void TranslateCastText(AtkTextNode* textNode)
    {
        // Get the current text
        string currentText = textNode -> NodeText.ToString();
        if (string.IsNullOrWhiteSpace(currentText)) return;

        // Remove ellipsis for comparison
        currentText = Utilities.RemoveEllipsis(currentText);

        // Track a single unresolved obfuscated action for UI-based detection
        uint unresolvedObfuscatedActionId = 0;
        bool hasMultipleUnresolvedObfuscatedActions = false;

        // Check if the current text contains any of the casts in the enmity list and translate it
        foreach (KeyValuePair<ulong, uint> cast in listCasts)
        {
            // Get the action ID
            uint actionId = cast.Value;

            // Get the client language action name
            string? clientActionName = translationCache.GetActionName(actionId, config.ClientLanguage);
            if (clientActionName == null) continue;

            // Resolve obfuscated client action name from known mappings
            bool isObfuscatedClientActionName = clientActionName.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal);
            if (isObfuscatedClientActionName)
            {
                string? knownClientActionName = GetObfuscatedTranslationName(clientActionName, config.ClientLanguage);
                if (!knownClientActionName.IsNullOrWhitespace()) clientActionName = knownClientActionName;
            }

            // If the client language action name contains the current text, translate it
            if (clientActionName.StartsWith(currentText))
            {
                // Get the display action name
                string? displayName = GetDisplayActionName(actionId, currentText);
                if (!displayName.IsNullOrWhitespace())
                {
                    // Update the text node with the display name
                    textNode -> SetText(displayName);
                    break;
                }
            }

            // If the action is still unresolved and the UI shows a visible name, keep a single safe fallback candidate
            if (isObfuscatedClientActionName &&
                clientActionName.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal) &&
                !currentText.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal))
            {
                if (unresolvedObfuscatedActionId == 0 || unresolvedObfuscatedActionId == actionId)
                {
                    unresolvedObfuscatedActionId = actionId;
                }
                else
                {
                    hasMultipleUnresolvedObfuscatedActions = true;
                }
            }
        }

        // If exactly one unresolved obfuscated action remains, use the visible UI text to trigger detection and optional translation
        if (unresolvedObfuscatedActionId > 0 && !hasMultipleUnresolvedObfuscatedActions)
        {
            string? displayName = GetDisplayActionName(unresolvedObfuscatedActionId, currentText);
            if (!displayName.IsNullOrWhitespace())
            {
                // Update the text node with the display name
                textNode -> SetText(displayName);
            }
        }
    }

    // ----------------------------
    // Clean expired enemies list casts
    // ----------------------------
    private void CleanExpiredEnemiesListCasts()
    {
        long now = Stopwatch.GetTimestamp() * 10_000_000L / Stopwatch.Frequency;
        List<ulong> toRemove = [];
        foreach (ulong id in listCastsExpiry.Keys)
        {
            if (now - listCastsExpiry[id] > ListCastExpiryTicks) toRemove.Add(id);
        }
        foreach (ulong id in toRemove)
        {
            listCasts.Remove(id);
            listCastsExpiry.Remove(id);
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
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.EnemyListAddon, OnEnemyListUpdate);

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
    public override void Dispose(string hookName)
    {
        try
        {
            // Unsubscribe from framework update
            framework.Update -= OnFrameworkUpdate;

            // Unregister addon lifecycle listeners
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.EnemyListAddon, OnEnemyListUpdate);

            // Set disabled flag
            isEnabled = false;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to dispose {hookName} hook");
        }
    }

}