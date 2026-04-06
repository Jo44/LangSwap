using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.tool;
using LangSwap.translation;
using System.Collections.Generic;
using System.Diagnostics;

namespace LangSwap.hook.@base;

// ----------------------------
// Base class for all castbars hooks
// ----------------------------
public unsafe abstract class CastBarsBaseHook(
    IAddonLifecycle addonLifecycle,
    Configuration config,
    IFramework framework,
    IObjectTable objectTable,
    ITargetManager targetManager,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : BaseHook(config, translationCache, utilities, log)
{
    // Core components
    protected IAddonLifecycle addonLifecycle = addonLifecycle;
    protected IFramework framework = framework;
    protected IObjectTable objectTable = objectTable;
    protected ITargetManager targetManager = targetManager;

    // Lists of casts
    protected readonly Dictionary<ulong, uint> listCasts = [];
    protected readonly Dictionary<ulong, long> listCastsExpiry = [];
    protected const long ListCastExpiryTicks = 30L * 10_000_000L; // 30 seconds

    // ----------------------------
    // On framework update
    // ----------------------------
    protected abstract void OnFrameworkUpdate(IFramework framework);

    // ----------------------------
    // Update cast bar
    // ----------------------------
    protected abstract void UpdateCastBar(AtkUnitBase* addon, uint actionId, int fieldIndex, string addonName);

    // ----------------------------
    // Update list
    // ----------------------------
    protected abstract void UpdateList(AtkUnitBase* addon, int fieldIndex);

    // ----------------------------
    // Translate slot
    // ----------------------------
    protected abstract void TranslateSlot(AtkTextNode* textNode);

    // ----------------------------
    // Check if member is in list
    // ----------------------------
    protected static bool IsInList(IBattleChara character, StatusFlags flag)
    {
        // Check if character has the specified status flag
        if (character != null && character.StatusFlags.HasFlag(flag)) return true;
        else return false;
    }

    // ----------------------------
    // Clean expired list casts
    // ----------------------------
    protected void CleanExpiredListCasts()
    {
        // Get current time in ticks
        long now = Stopwatch.GetTimestamp() * 10_000_000L / Stopwatch.Frequency;

        // Loop through list casts expiry
        List<ulong> toRemove = [];
        foreach (ulong id in listCastsExpiry.Keys)
        {
            // If expired, add to remove list
            if (now - listCastsExpiry[id] > ListCastExpiryTicks) toRemove.Add(id);
        }
        // Remove expired casts from both lists
        foreach (ulong id in toRemove)
        {
            listCasts.Remove(id);
            listCastsExpiry.Remove(id);
        }
    }

}