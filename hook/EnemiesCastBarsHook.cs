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

    // Tracking variables
    private uint currentTargetActionId = 0;
    private uint currentFocusActionId = 0;

    // Linger counts
    private int targetLingerCount = 0;
    private int focusLingerCount = 0;

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
        long startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            // Check if language is swapped
            if (!isLanguageSwapped)
            {
                currentTargetActionId = 0;
                targetLingerCount = 0;
                currentFocusActionId = 0;
                focusLingerCount = 0;
                listCasts.Clear();
                lingeringCasts.Clear();
                return;
            }

            // Get local player
            IPlayerCharacter? player = objectTable.LocalPlayer;
            if (player == null)
            {
                currentTargetActionId = 0;
                targetLingerCount = 0;
                currentFocusActionId = 0;
                focusLingerCount = 0;
                listCasts.Clear();
                lingeringCasts.Clear();
                return;
            }

            // Get player's ID
            ulong playerId = player.GameObjectId;

            // Get player's target ID
            ulong targetId = player.TargetObjectId;

            // Get player's focus ID
            ulong focusId = targetManager.FocusTarget?.GameObjectId ?? 0;

            // Initialize tracking variables
            bool foundTarget = false;
            bool foundFocus = false;
            HashSet<ulong> currentCasting = [];

            // Iterate through all battle NPCs
            foreach (IGameObject obj in objectTable)
            {
                // Filter for battle NPCs
                if (obj == null || obj.ObjectKind != ObjectKind.BattleNpc) continue;

                // Filter for battle characters
                if (obj is not IBattleChara battleChara) continue;

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
                        // Add to current casting set
                        currentCasting.Add(battleChara.GameObjectId);

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

                        // Update enemy list
                        if (inEnmityList)
                        {
                            listCasts[battleChara.GameObjectId] = actionId;
                        }
                    }
                }
            }

            // Reset if target not found
            if (foundTarget) targetLingerCount = CastLingerFrames;
            else if (targetLingerCount > 0) targetLingerCount--;
            else currentTargetActionId = 0;

            // Reset if focus not found
            if (foundFocus) focusLingerCount = CastLingerFrames;
            else if (focusLingerCount > 0) focusLingerCount--;
            else currentFocusActionId = 0;

            // Clean up enemy list of non-casting enemies
            List<ulong> toRemove = [];
            foreach (ulong id in listCasts.Keys)
            {
                // Lingering casts
                if (currentCasting.Contains(id)) lingeringCasts.Remove(id);
                else if (!lingeringCasts.TryGetValue(id, out int frames)) lingeringCasts[id] = CastLingerFrames;
                else if (frames > 0) lingeringCasts[id] = frames - 1;
                else
                {
                    toRemove.Add(id);
                    lingeringCasts.Remove(id);
                }
            }
            foreach (ulong id in toRemove)
            {
                listCasts.Remove(id);
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error in OnFrameworkUpdate");
        }
        finally
        {
            PerformanceMonitor.Record(StatEnum.Framework, startTimestamp);
        }
    }

    // ----------------------------
    // On target info update
    // ----------------------------
    private void OnTargetInfoUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsTarget)
        {
            long startTimestamp = Stopwatch.GetTimestamp();
            UpdateCastBar(utilities.GetAddon(config.TargetInfoAddon), currentTargetActionId, targetInfoField, "target info");
            PerformanceMonitor.Record(StatEnum.Enemy_Cast, startTimestamp);
        }
    }

    // ----------------------------
    // On target cast bar update
    // ----------------------------
    private void OnTargetCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsTarget)
        {
            long startTimestamp = Stopwatch.GetTimestamp();
            UpdateCastBar(utilities.GetAddon(config.TargetCastBarAddon), currentTargetActionId, targetCastBarField, "target castbar");
            PerformanceMonitor.Record(StatEnum.Enemy_Cast, startTimestamp);
        }
    }

    // ----------------------------
    // On focus cast bar update
    // ----------------------------
    private void OnFocusCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsFocus)
        {
            long startTimestamp = Stopwatch.GetTimestamp();
            UpdateCastBar(utilities.GetAddon(config.FocusCastBarAddon), currentFocusActionId, focusCastBarField, "focus castbar");
            PerformanceMonitor.Record(StatEnum.Enemy_Cast, startTimestamp);
        }
    }

    // ----------------------------
    // On enemy list update
    // ----------------------------
    private void OnEnemyListUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsEnmityList)
        {
            long startTimestamp = Stopwatch.GetTimestamp();
            UpdateList(utilities.GetAddon(config.EnemyListAddon), enemyListStartField, enemyListEndField, enemyListCastField);
            PerformanceMonitor.Record(StatEnum.Enmity_List, startTimestamp);
        }
    }

    // ----------------------------
    // Translate the cast text in the enemy list slot
    // ----------------------------
    protected override void TranslateCastText(AtkTextNode* textNode)
    {
        // Get the current text
        string currentText = textNode -> NodeText.ToString();
        if (string.IsNullOrWhiteSpace(currentText)) return;

        // Remove ellipsis for comparison
        currentText = Utilities.RemoveEllipsis(currentText);

        // Check if the current text contains any of the casts in the enemy list and translate it
        foreach (KeyValuePair<ulong, uint> cast in listCasts)
        {
            // Get the action ID
            uint actionId = cast.Value;

            // Get the client language action name
            string? clientActionName = translationCache.GetActionName(actionId, config.ClientLanguage);
            if (clientActionName == null) continue;

            // If the client language action name contains the current text, translate it
            if (clientActionName.StartsWith(currentText))
            {
                // Get the translated action name
                string? translatedName = translationCache.GetActionName(actionId, config.TargetLanguage);
                if (!translatedName.IsNullOrWhitespace())
                {
                    // Update the text node with the translated name
                    textNode -> SetText(translatedName);
                    break;
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
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.EnemyListAddon, OnEnemyListUpdate);

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