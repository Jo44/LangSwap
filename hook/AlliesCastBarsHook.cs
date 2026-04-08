using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using LangSwap.hook.@base;
using LangSwap.hook.ttt;
using LangSwap.tool;
using LangSwap.translation;
using System;

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
    IPluginLog log) : CastBarsHook(addonLifecycle, config, framework, objectTable, targetManager, translationCache, utilities, log)
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
        // Refresh addons
        utilities.RefreshAddon(utilities.GetAddon(config.CastBarAddon), "castbar");
        utilities.RefreshAddon(utilities.GetAddon(config.TargetInfoAddon), "target info");
        utilities.RefreshAddon(utilities.GetAddon(config.TargetCastBarAddon), "target castbar");
        utilities.RefreshAddon(utilities.GetAddon(config.FocusCastBarAddon), "focus castbar");
        utilities.RefreshAddon(utilities.GetAddon(config.PartyListAddon), "party list");
    }

    // ----------------------------
    // On cast bar update
    // ----------------------------
    private void OnCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (config.AlliesCastBarsTarget || config.AlliesCastBarsFocus || config.AlliesCastBarsPartyList)
        {
            uint actionId = objectTable.LocalPlayer is IBattleChara lp && lp.IsCasting ? (uint)lp.CastActionId : 0;
            UpdateCastBar(utilities.GetAddon(config.CastBarAddon), actionId, Addon.Allies, "castbar");
        }
    }

    // ----------------------------
    // On target info update
    // ----------------------------
    private void OnTargetInfoUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (config.AlliesCastBarsTarget)
        {
            uint actionId = targetManager.Target is IBattleChara t && t.ObjectKind == ObjectKind.Player && t.IsCasting ? (uint)t.CastActionId : 0;
            UpdateCastBar(utilities.GetAddon(config.TargetInfoAddon), actionId, Addon.Allies, "target info");
        }
    }

    // ----------------------------
    // On target cast bar update
    // ----------------------------
    private void OnTargetCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (config.AlliesCastBarsTarget)
        {
            uint actionId = targetManager.Target is IBattleChara t && t.ObjectKind == ObjectKind.Player && t.IsCasting ? (uint)t.CastActionId : 0;
            UpdateCastBar(utilities.GetAddon(config.TargetCastBarAddon), actionId, Addon.Allies, "target castbar");
        }
    }

    // ----------------------------
    // On focus cast bar update
    // ----------------------------
    private void OnFocusCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (config.AlliesCastBarsFocus)
        {
            uint actionId = targetManager.FocusTarget is IBattleChara f && f.ObjectKind == ObjectKind.Player && f.IsCasting ? (uint)f.CastActionId : 0;
            UpdateCastBar(utilities.GetAddon(config.FocusCastBarAddon), actionId, config.FocusCastBarField, Addon.Allies);
        }
    }

    // ----------------------------
    // On party list update
    // ----------------------------
    private void OnPartyListUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (config.AlliesCastBarsPartyList)
        {
            GroupManager* groupManager = GroupManager.Instance();
            if (groupManager == null) return;
            int count = config.PartyListEndField - config.PartyListStartField + 1;
            uint[] slotEntityIds = new uint[count];
            for (int i = 0; i < count && i < groupManager->MainGroup.MemberCount; i++)
                slotEntityIds[i] = groupManager->MainGroup.PartyMembers[i].EntityId;
            UpdateList(utilities.GetAddon(config.PartyListAddon), config.PartyListCastField, config.PartyListStartField, config.PartyListEndField, true, Addon.Allies, slotEntityIds);
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