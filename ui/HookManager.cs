using Dalamud.Plugin.Services;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.ui.hooks;
using LangSwap.ui.hooks.@base;
using System;
using System.Collections.Generic;

namespace LangSwap.ui;

// ----------------------------
// Hook Manager
// ----------------------------
public class HookManager(
    IAddonLifecycle addonLifecycle,
    Configuration config,
    IFramework framework,
    IGameInteropProvider gameInterop,
    IObjectTable objectTable,
    ISigScanner sigScanner,
    ITargetManager targetManager,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : IDisposable
{
    // Log
    private const string Class = "[HookManager.cs]";

    // Individual hooks
    private readonly ActionTooltipHook actionTooltipHook = new(config, gameInterop, sigScanner, translationCache, utilities, log);
    private readonly ItemTooltipHook itemTooltipHook = new(config, gameInterop, sigScanner, translationCache, utilities, log);
    private readonly AlliesCastBarsHook alliesCastBarsHook = new(addonLifecycle, config, framework, objectTable, targetManager, translationCache, utilities, log);
    private readonly EnemiesCastBarsHook enemiesCastBarsHook = new(addonLifecycle, config, framework, objectTable, targetManager, translationCache, utilities, log);

    // Active hooks
    private readonly HashSet<BaseHook> hooks = [];

    // ----------------------------
    // Enable all translation hooks
    // ----------------------------
    public void EnableAll()
    {
        // Add hook if component is enabled
        if (config.ActionTooltip) hooks.Add(actionTooltipHook);
        if (config.ItemTooltip) hooks.Add(itemTooltipHook);
        if (config.AlliesCastBarsTarget || config.AlliesCastBarsFocus || config.AlliesCastBarsPartyList) hooks.Add(alliesCastBarsHook);
        if (config.EnemiesCastBarsTarget || config.EnemiesCastBarsFocus || config.EnemiesCastBarsEnmityList) hooks.Add(enemiesCastBarsHook);

        // Enable all active hooks
        foreach (BaseHook hook in hooks)
        {
            try
            {
                if (hook is ActionTooltipHook) hook.Enable("Action Tooltip");
                else if (hook is ItemTooltipHook) hook.Enable("Item Tooltip");
                else if (hook is AlliesCastBarsHook) hook.Enable("Allies CastBars");
                else if (hook is EnemiesCastBarsHook) hook.Enable("Enemies CastBars");
            }
            catch (Exception ex)
            {
                log.Error(ex, $"{Class} - Failed to enable {hook.GetType().Name}");
            }
        }
    }

    // ----------------------------
    // Update hooks according to current configuration
    // ----------------------------
    public void UpdateHooks()
    {
        // Action Tooltip
        if (config.ActionTooltip && !hooks.Contains(actionTooltipHook))
        {
            try { actionTooltipHook.Enable("Action Tooltip"); hooks.Add(actionTooltipHook); }
            catch (Exception ex) { log.Error(ex, $"{Class} - Failed to enable Action Tooltip"); }
        }
        else if (!config.ActionTooltip && hooks.Contains(actionTooltipHook))
        {
            try { actionTooltipHook.Disable("Action Tooltip"); hooks.Remove(actionTooltipHook); }
            catch (Exception ex) { log.Error(ex, $"{Class} - Failed to disable Action Tooltip"); }
        }

        // Item Tooltip
        if (config.ItemTooltip && !hooks.Contains(itemTooltipHook))
        {
            try { itemTooltipHook.Enable("Item Tooltip"); hooks.Add(itemTooltipHook); }
            catch (Exception ex) { log.Error(ex, $"{Class} - Failed to enable Item Tooltip"); }
        }
        else if (!config.ItemTooltip && hooks.Contains(itemTooltipHook))
        {
            try { itemTooltipHook.Disable("Item Tooltip"); hooks.Remove(itemTooltipHook); }
            catch (Exception ex) { log.Error(ex, $"{Class} - Failed to disable Item Tooltip"); }
        }

        // Allies CastBars
        bool alliesEnabled = config.AlliesCastBarsTarget || config.AlliesCastBarsFocus || config.AlliesCastBarsPartyList;
        if (alliesEnabled && !hooks.Contains(alliesCastBarsHook))
        {
            try { alliesCastBarsHook.Enable("Allies CastBars"); hooks.Add(alliesCastBarsHook); }
            catch (Exception ex) { log.Error(ex, $"{Class} - Failed to enable Allies CastBars"); }
        }
        else if (!alliesEnabled && hooks.Contains(alliesCastBarsHook))
        {
            try { alliesCastBarsHook.Disable("Allies CastBars"); hooks.Remove(alliesCastBarsHook); }
            catch (Exception ex) { log.Error(ex, $"{Class} - Failed to disable Allies CastBars"); }
        }

        // Enemies CastBars
        bool enemiesEnabled = config.EnemiesCastBarsTarget || config.EnemiesCastBarsFocus || config.EnemiesCastBarsEnmityList;
        if (enemiesEnabled && !hooks.Contains(enemiesCastBarsHook))
        {
            try { enemiesCastBarsHook.Enable("Enemies CastBars"); hooks.Add(enemiesCastBarsHook); }
            catch (Exception ex) { log.Error(ex, $"{Class} - Failed to enable Enemies CastBars"); }
        }
        else if (!enemiesEnabled && hooks.Contains(enemiesCastBarsHook))
        {
            try { enemiesCastBarsHook.Disable("Enemies CastBars"); hooks.Remove(enemiesCastBarsHook); }
            catch (Exception ex) { log.Error(ex, $"{Class} - Failed to disable Enemies CastBars"); }
        }
    }

    // ----------------------------
    // Swap all hooks to target language
    // ----------------------------
    public void SwapLanguage()
    {
        // Swap language for all active hooks
        foreach (BaseHook hook in hooks)
        {
            try
            {
                hook.SwapLanguage();
            }
            catch (Exception ex)
            {
                log.Error(ex, $"{Class} - Failed to swap language for {hook.GetType().Name}");
            }
        }
    }

    // ----------------------------
    // Restore all hooks to original language
    // ----------------------------
    public void RestoreLanguage()
    {
        // Restore language for all active hooks
        foreach (BaseHook hook in hooks)
        {
            try
            {
                hook.RestoreLanguage();
            }
            catch (Exception ex)
            {
                log.Error(ex, $"{Class} - Failed to restore language for {hook.GetType().Name}");
            }
        }
    }

    // ----------------------------
    // Disable all translation hooks
    // ----------------------------
    public void DisableAll()
    {
        // Disable all active hooks
        foreach (BaseHook hook in hooks)
        {
            try
            {
                if (hook is ActionTooltipHook) hook.Disable("Action Tooltip");
                else if (hook is ItemTooltipHook) hook.Disable("Item Tooltip");
                else if (hook is AlliesCastBarsHook) hook.Disable("Allies CastBars");
                else if (hook is EnemiesCastBarsHook) hook.Disable("Enemies CastBars");
            }
            catch (Exception ex)
            {
                log.Error(ex, $"{Class} - Failed to disable {hook.GetType().Name}");
            }
        }
    }

    // ----------------------------
    // Dispose all hooks
    // ----------------------------
    public void Dispose()
    {
        // Disable all active hooks
        DisableAll();

        // Dispose all active hooks
        foreach (BaseHook hook in hooks)
        {
            try
            {
                hook.Dispose();
            }
            catch (Exception ex)
            {
                log.Error(ex, $"{Class} - Failed to dispose {hook.GetType().Name}");
            }
        }

        // Clear hook list
        hooks.Clear();

        // Finalize
        GC.SuppressFinalize(this);
    }

}