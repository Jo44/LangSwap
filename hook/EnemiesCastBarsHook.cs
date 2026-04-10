using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using LangSwap.hook.@base;
using LangSwap.hook.template;
using LangSwap.translation;
using System;

namespace LangSwap.hook;

// ----------------------------
// Enemies CastBars Hook
// ----------------------------
public unsafe class EnemiesCastBarsHook(Configuration config, TranslationCache translationCache) : CastBarsHook(config, translationCache)
{
    // Log
    private const string Class = "[EnemiesCastBarsHook.cs]";

    // ----------------------------
    // Enable the hook
    // ----------------------------
    public override void Enable(string hookName)
    {
        // Prevent multiple enables
        if (isEnabled) return;

        try
        {
            // Register addon lifecycle listeners
            AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.EnmityListAddon, OnEnmityListUpdate);

            // Set enabled flag
            isEnabled = true;

            // Log
            Log.Information($"{Class} - {hookName} hook enabled");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Failed to enable {hookName} hook");
        }
    }

    // ----------------------------
    // On language swap
    // ----------------------------
    protected override void OnLanguageSwap()
    {
        // Refresh addons
        RefreshAddon(GetAddon(config.TargetInfoAddon), config.TargetInfoName);
        RefreshAddon(GetAddon(config.TargetCastBarAddon), config.TargetCastBarName);
        RefreshAddon(GetAddon(config.FocusCastBarAddon), config.FocusCastBarName);
        RefreshAddon(GetAddon(config.EnmityListAddon), config.EnmityListName);
    }

    // ----------------------------
    // On target info update
    // ----------------------------
    private void OnTargetInfoUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (config.EnemiesCastBarsTarget)
        {
            // Get the action ID
            uint actionID = TargetManager.Target is IBattleChara target && target.ObjectKind == ObjectKind.BattleNpc && target.IsCasting ? (uint)target.CastActionId : 0;

            // Update the cast bar
            UpdateCastBar(GetAddon(config.TargetInfoAddon), AddonType.TargetInfo, actionID);
        }
    }

    // ----------------------------
    // On target cast bar update
    // ----------------------------
    private void OnTargetCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (config.EnemiesCastBarsTarget)
        {
            // Get the action ID
            uint actionID = TargetManager.Target is IBattleChara target && target.ObjectKind == ObjectKind.BattleNpc && target.IsCasting ? (uint)target.CastActionId : 0;

            // Update the cast bar
            UpdateCastBar(GetAddon(config.TargetCastBarAddon), AddonType.TargetCastBar, actionID);
        }
    }

    // ----------------------------
    // On focus cast bar update
    // ----------------------------
    private void OnFocusCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (config.EnemiesCastBarsFocus)
        {
            // Get the action ID
            uint actionID = TargetManager.FocusTarget is IBattleChara focus && focus.ObjectKind == ObjectKind.BattleNpc && focus.IsCasting ? (uint)focus.CastActionId : 0;

            // Update the cast bar
            UpdateCastBar(GetAddon(config.FocusCastBarAddon), AddonType.FocusCastBar, actionID);
        }
    }

    // ----------------------------
    // On enmity list update
    // ----------------------------
    private void OnEnmityListUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (config.EnemiesCastBarsEnmityList)
        {
            // Get the hater
            UIState* uiState = UIState.Instance();
            Hater* hater = uiState != null ? &uiState -> Hater : null;
            if (hater == null) return;

            // Get the entity IDs from the enmity list
            uint[] entityIDs = new uint[8];
            for (int i = 0; i < 8 && i < hater -> HaterCount; i++)
            {
                // Store the entity ID
                entityIDs[i] = ((HaterInfo*)hater)[i].EntityId;
            }

            // Update the enmity list
            UpdateList(GetAddon(config.EnmityListAddon), AddonType.EnmityList, CastBarsType.Ennemies, entityIDs);
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
            // Unregister addon lifecycle listeners
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.EnmityListAddon, OnEnmityListUpdate);

            // Set disabled flag
            isEnabled = false;
            Log.Information($"{Class} - {hookName} hook disabled");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Failed to disable {hookName} hook");
        }
    }

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    protected override void Dispose(string hookName)
    {
        try
        {
            // Unregister addon lifecycle listeners
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.EnmityListAddon, OnEnmityListUpdate);

            // Set disabled flag
            isEnabled = false;

            // Dispose base resources
            Dispose();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Failed to dispose {hookName} hook");
        }
    }

}