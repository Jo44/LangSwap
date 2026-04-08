using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using LangSwap.hook.@base;
using LangSwap.hook.template;
using LangSwap.translation;
using System;

namespace LangSwap.hook;

// ----------------------------
// Allies CastBars Hook
// ----------------------------
public unsafe class AlliesCastBarsHook(Configuration config, TranslationCache translationCache) : CastBarsHook(config, translationCache)
{
    // Log
    private const string Class = "[AlliesCastBarsHook.cs]";

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
            AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.CastBarAddon, OnCastBarUpdate);
            AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.PartyListAddon, OnPartyListUpdate);

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
        RefreshAddon(GetAddon(config.CastBarAddon), config.CastBarName);
        RefreshAddon(GetAddon(config.TargetInfoAddon), config.TargetInfoName);
        RefreshAddon(GetAddon(config.TargetCastBarAddon), config.TargetCastBarName);
        RefreshAddon(GetAddon(config.FocusCastBarAddon), config.FocusCastBarName);
        RefreshAddon(GetAddon(config.PartyListAddon), config.PartyListName);
    }

    // ----------------------------
    // On cast bar update
    // ----------------------------
    private void OnCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (config.AlliesCastBarsTarget || config.AlliesCastBarsFocus || config.AlliesCastBarsPartyList)
        {
            // Get the action ID
            uint actionID = ObjectTable.LocalPlayer is IBattleChara player && player.IsCasting ? (uint)player.CastActionId : 0;

            // Update the cast bar
            UpdateCastBar(GetAddon(config.CastBarAddon), AddonType.CastBar, actionID);
        }
    }

    // ----------------------------
    // On target info update
    // ----------------------------
    private void OnTargetInfoUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (config.AlliesCastBarsTarget)
        {
            // Get the action ID
            uint actionID = TargetManager.Target is IBattleChara target && target.ObjectKind == ObjectKind.Player && target.IsCasting ? (uint)target.CastActionId : 0;

            // Update the cast bar
            UpdateCastBar(GetAddon(config.TargetInfoAddon), AddonType.TargetInfo, actionID);
        }
    }

    // ----------------------------
    // On target cast bar update
    // ----------------------------
    private void OnTargetCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (config.AlliesCastBarsTarget)
        {
            // Get the action ID
            uint actionID = TargetManager.Target is IBattleChara target && target.ObjectKind == ObjectKind.Player && target.IsCasting ? (uint)target.CastActionId : 0;

            // Update the cast bar
            UpdateCastBar(GetAddon(config.TargetCastBarAddon), AddonType.TargetCastBar, actionID);
        }
    }

    // ----------------------------
    // On focus cast bar update
    // ----------------------------
    private void OnFocusCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (config.AlliesCastBarsFocus)
        {
            // Get the action ID
            uint actionID = TargetManager.FocusTarget is IBattleChara focus && focus.ObjectKind == ObjectKind.Player && focus.IsCasting ? (uint)focus.CastActionId : 0;

            // Update the cast bar
            UpdateCastBar(GetAddon(config.FocusCastBarAddon), AddonType.FocusCastBar, actionID);
        }
    }

    // ----------------------------
    // On party list update
    // ----------------------------
    private void OnPartyListUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (config.AlliesCastBarsPartyList)
        {
            // Get the group manager
            GroupManager* groupManager = GroupManager.Instance();
            if (groupManager == null) return;


            // Get the entity IDs from the party list
            uint[] entityIDs = new uint[8];
            for (int i = 0; i < 8 && i < groupManager -> MainGroup.MemberCount; i++)
            {
                entityIDs[i] = groupManager -> MainGroup.PartyMembers[i].EntityId;
            }

            // Update the party list
            UpdateList(GetAddon(config.PartyListAddon), AddonType.PartyList, CastBarsType.Allies, entityIDs);
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
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.CastBarAddon, OnCastBarUpdate);
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.PartyListAddon, OnPartyListUpdate);

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
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.CastBarAddon, OnCastBarUpdate);
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.PartyListAddon, OnPartyListUpdate);

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