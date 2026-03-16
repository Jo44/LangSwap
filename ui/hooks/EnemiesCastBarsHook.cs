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
    IClientState clientState,
    Configuration config,
    IFramework framework,
    IGameGui gameGui,
    IGameInteropProvider gameInterop,
    IObjectTable objectTable,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : BaseHook(clientState, config, framework, gameGui, gameInterop, objectTable, sigScanner, translationCache, utilities, log)
{
    // Log
    private const string Class = "[EnemiesCastBarsHook.cs]";

    // Last casts
    private readonly Dictionary<ulong, uint> _lastCasts = [];

    // ----------------------------
    // Enable the hook
    // ----------------------------
    public override void Enable()
    {
        // Prevent multiple enables
        if (isEnabled) return;

        try
        {
            // Subscribe to framework update
            framework.Update += OnFrameworkUpdate;

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
        // TODO : continue
        try
        {
            // Get local player
            var player = objectTable.LocalPlayer;
            if (player == null) return;

            // Iterate through all objects in object table
            foreach (var obj in objectTable)
            {
                if (obj == null) continue;
             
                if (obj.ObjectKind != ObjectKind.BattleNpc) continue;

                if (obj is not IBattleChara battleChara) continue;

                if (battleChara.TargetObjectId != player.GameObjectId && !IsInEnmityList(battleChara)) continue;

                if (battleChara.IsCasting)
                {
                    uint actionId = (uint)battleChara.CastActionId;

                    if (actionId > 0)
                    {
                        if (!_lastCasts.TryGetValue(battleChara.GameObjectId, out uint lastActionId) || lastActionId != actionId)
                        {
                            _lastCasts[battleChara.GameObjectId] = actionId;
                            log.Debug($"{Class} - {battleChara.Name} ({battleChara.EntityId}) casting ActionId={actionId}");
                        }
                    }
                }
                else
                {
                    _lastCasts.Remove(battleChara.GameObjectId);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error in OnFrameworkUpdate");
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