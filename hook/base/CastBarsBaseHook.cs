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
    // Log
    private const string Class = "[CastBarsBaseHook.cs]";

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
    protected abstract void UpdateList(AtkUnitBase* addon, int listStartField, int listEndField, int castField);

    // ----------------------------
    // Process list
    // ----------------------------
    protected abstract void ProcessList(AtkUnitBase* addon, int slotIndex, int castField);

    // ----------------------------
    // Translate slot
    // ----------------------------
    protected abstract void TranslateSlot(AtkTextNode* textNode);

    // ----------------------------
    // Check if member is in list
    // ----------------------------
    protected static bool IsInList(IBattleChara character, StatusFlags flag)
    {
        bool inList = false;
        try
        {
            if (character != null && character.StatusFlags.HasFlag(flag)) inList = true;
        }
        catch
        {
            inList = false;
        }
        return inList;
    }

    // ----------------------------
    // Clean expired list casts
    // ----------------------------
    protected void CleanExpiredListCasts()
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

}