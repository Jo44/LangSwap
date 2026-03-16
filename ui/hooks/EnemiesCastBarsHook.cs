using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.NativeWrapper;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.ui.hooks.@base;
using System;
using System.Collections.Generic;

namespace LangSwap.ui.hooks;

// ----------------------------
// Enemies CastBars Hook
// ----------------------------
public unsafe class EnemiesCastBarsHook(
    IAddonLifecycle addonLifecycle,
    Configuration config,
    IFramework framework,
    IGameGui gameGui,
    IGameInteropProvider gameInterop,
    IObjectTable objectTable,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : BaseHook(config, gameGui, gameInterop, translationCache, utilities, log)
{
    // Log
    private const string Class = "[EnemiesCastBarsHook.cs]";

    // Core components
    private readonly IAddonLifecycle _addonLifecycle = addonLifecycle;
    protected readonly IFramework framework = framework;
    protected readonly IObjectTable objectTable = objectTable;

    // Last casts
    private readonly Dictionary<ulong, uint> _lastCasts = [];
    private uint _currentTargetActionId = 0;

    // ----------------------------
    // Enable the hook
    // ----------------------------
    public override void Enable()
    {
        // Prevent multiple enables
        if (isEnabled) return;

        try
        {
            // Subscribe to framework update for tracking casts
            framework.Update += OnFrameworkUpdate;

            // Subscribe to addon lifecycle for updating text
            _addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);

            // Set enabled flag
            isEnabled = true;

            // Log
            log.Debug($"{Class} - Enemies castbars hook enabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to enable enemies castbars hook");
        }
    }

    // ----------------------------
    // On language swap
    // ----------------------------
    protected override void OnLanguageSwap()
    {
        // Refresh target, focus & enemy list addons
        try
        {
            // Get pointer to target castbar addon
            AtkUnitBasePtr targetCastBarPtr = gameGui.GetAddonByName(config.TargetCastBarAddon);
            if (!targetCastBarPtr.IsNull)
            {
                // Get AtkUnitBase from pointer
                AtkUnitBase* targetCastBar = (AtkUnitBase*)targetCastBarPtr.Address;

                // Only refresh if the addon is currently visible
                if (targetCastBar != null && targetCastBar -> IsVisible)
                {
                    targetCastBar -> Hide(true, false, 0);
                    targetCastBar -> Show(true, 0);
                }
            }

            // Get pointer to focus castbar addon
            AtkUnitBasePtr focusCastBarPtr = gameGui.GetAddonByName(config.FocusCastBarAddon);
            if (!focusCastBarPtr.IsNull)
            {
                // Get AtkUnitBase from pointer
                AtkUnitBase* focusCastBar = (AtkUnitBase*)focusCastBarPtr.Address;

                // Only refresh if the addon is currently visible
                if (focusCastBar != null && focusCastBar -> IsVisible)
                {
                    focusCastBar -> Hide(true, false, 0);
                    focusCastBar -> Show(true, 0);
                }
            }

            // Get pointer to enemy list addon
            AtkUnitBasePtr enemyListPtr = gameGui.GetAddonByName(config.EnemyListAddon);
            if (!enemyListPtr.IsNull)
            {
                // Get AtkUnitBase from pointer
                AtkUnitBase* enemyList = (AtkUnitBase*)enemyListPtr.Address;

                // Only refresh if the addon is currently visible
                if (enemyList != null && enemyList -> IsVisible)
                {
                    enemyList -> Hide(true, false, 0);
                    enemyList -> Show(true, 0);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to refresh enemies castbars addons");
        }
    }

    // ----------------------------
    // On framework update
    // ----------------------------
    private void OnFrameworkUpdate(IFramework framework)
    {
        try
        {
            // Only if language is swapped
            if (isLanguageSwapped)
            {
                // Get local player
                var player = objectTable.LocalPlayer;
                if (player == null)
                {
                    _currentTargetActionId = 0;
                    return;
                }

                // Check if player has a target that is casting
                bool foundTargetCasting = false;

                // Iterate through all objects in object table
                foreach (IGameObject obj in objectTable)
                {
                    // Skip if object is null
                    if (obj == null) continue;

                    // Skip if object is not a battle NPC
                    if (obj.ObjectKind != ObjectKind.BattleNpc) continue;

                    // Skip if object is not a battle character
                    if (obj is not IBattleChara battleChara) continue;

                    // Check if this is the player's target
                    bool isPlayerTarget = player.TargetObjectId == battleChara.GameObjectId;

                    // Skip if player is not targeting the battle character and is not in enmity list
                    if (!isPlayerTarget && !IsInEnmityList(battleChara)) continue;

                    // Check if the battle character is casting
                    if (battleChara.IsCasting)
                    {
                        // Get the current action ID being cast
                        uint actionId = (uint)battleChara.CastActionId;
                        if (actionId > 0)
                        {
                            // Check if this is a new cast
                            if (!_lastCasts.TryGetValue(battleChara.GameObjectId, out uint lastActionId) || lastActionId != actionId)
                            {
                                _lastCasts[battleChara.GameObjectId] = actionId;
                                log.Debug($"{Class} - {battleChara.Name} ({battleChara.EntityId}) casting ActionId={actionId}");
                            }

                            // Store current target action ID
                            if (isPlayerTarget)
                            {
                                _currentTargetActionId = actionId;
                                foundTargetCasting = true;
                            }
                        }
                    }
                    else
                    {
                        _lastCasts.Remove(battleChara.GameObjectId);
                    }
                }

                // Reset if target is not casting
                if (!foundTargetCasting)
                {
                    _currentTargetActionId = 0;
                }
            }
            else
            {
                _currentTargetActionId = 0;
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error in OnFrameworkUpdate");
        }
    }

    // ----------------------------
    // On target cast bar update (addon lifecycle)
    // ----------------------------
    private void OnTargetCastBarUpdate(AddonEvent type, AddonArgs args)
    {
        try
        {
            // Only if language is swapped and we have an action ID to translate
            if (!isLanguageSwapped || _currentTargetActionId == 0) return;

            // Get translated action name
            string? translatedName = translationCache.GetActionName(_currentTargetActionId, (LanguageEnum)config.TargetLanguage);
            if (translatedName == null) return;

            // Get cast bar addon
            var castBarPtr = (AtkUnitBasePtr)args.Addon;
            if (castBarPtr.IsNull) return;

            AtkUnitBase* castBar = (AtkUnitBase*)castBarPtr.Address;
            if (castBar == null || !castBar->IsVisible) return;

            // Find and update the text node
            for (int i = 0; i < castBar->UldManager.NodeListCount; i++)
            {
                var node = castBar->UldManager.NodeList[i];
                if (node == null || node->Type != NodeType.Text) continue;

                var textNode = (AtkTextNode*)node;
                if (textNode != null && textNode->NodeText.Length > 0)
                {
                    textNode->SetText(translatedName);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error in OnTargetCastBarUpdate");
        }
    }

    // ----------------------------
    // Check if enemy is in enmity list
    // ----------------------------
    private static bool IsInEnmityList(IBattleChara enemy)
    {
        bool inEnmityList = false;
        try
        {
            if (enemy.StatusFlags.HasFlag(StatusFlags.InCombat)) inEnmityList = true;
        }
        catch
        {
            inEnmityList = false;
        }
        return inEnmityList;
    }

    // ----------------------------
    // Disable the hook
    // ----------------------------
    public override void Disable()
    {
        // Prevent multiple disables
        if (!isEnabled) return;

        try
        {
            // Unsubscribe from framework update
            framework.Update -= OnFrameworkUpdate;

            // Unsubscribe from addon lifecycle
            _addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);

            // Set disabled flag
            isEnabled = false;
            log.Debug($"{Class} - Enemies castbars hook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to disable enemies castbars hook");
        }
    }

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    public override void Dispose()
    {
        try
        {
            // Unsubscribe from framework update
            framework.Update -= OnFrameworkUpdate;

            // Unsubscribe from addon lifecycle
            _addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);

            // Set disabled flag
            isEnabled = false;
            log.Debug($"{Class} - Enemies castbars hook disposed");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to dispose enemies castbars hook");
        }

        // Finalize
        GC.SuppressFinalize(this);
    }

}