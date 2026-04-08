using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using LangSwap.hook.@base;
using LangSwap.hook.template;
using LangSwap.tool;
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
            // Get action ID
            uint actionId = TargetManager.Target is IBattleChara t && t.ObjectKind == ObjectKind.BattleNpc && t.IsCasting ? (uint)t.CastActionId : 0;

            // Update cast bar
            UpdateCastBar(GetAddon(config.TargetInfoAddon), AddonType.TargetInfo, actionId);
        }
    }

    // ----------------------------
    // On target cast bar update
    // ----------------------------
    private void OnTargetCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (config.EnemiesCastBarsTarget)
        {
            // Get action ID
            uint actionId = TargetManager.Target is IBattleChara t && t.ObjectKind == ObjectKind.BattleNpc && t.IsCasting ? (uint)t.CastActionId : 0;

            // Update cast bar
            UpdateCastBar(GetAddon(config.TargetCastBarAddon), AddonType.TargetCastBar, actionId);
        }
    }

    // ----------------------------
    // On focus cast bar update
    // ----------------------------
    private void OnFocusCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (config.EnemiesCastBarsFocus)
        {
            // Get action ID
            uint actionId = TargetManager.FocusTarget is IBattleChara f && f.ObjectKind == ObjectKind.BattleNpc && f.IsCasting ? (uint)f.CastActionId : 0;

            // Update cast bar
            UpdateCastBar(GetAddon(config.FocusCastBarAddon), AddonType.FocusCastBar, actionId);
        }
    }

    // ----------------------------
    // On enmity list update
    // ----------------------------
    private void OnEnmityListUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (config.EnemiesCastBarsEnmityList)
        {
            // TODO
            UIState* uiState = UIState.Instance();
            Hater* hater = uiState != null ? &uiState->Hater : null;
            int count = config.EnmityListEndField - config.EnmityListStartField + 1;
            uint[] entityIDs = new uint[count];
            if (hater != null)
                for (int i = 0; i < count && i < hater->HaterCount; i++)
                    entityIDs[i] = ((HaterInfo*)hater)[i].EntityId;

            // Update enmity list
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