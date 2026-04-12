using Dalamud.Plugin.Services;
using LangSwap.hook.template;
using LangSwap.translation;
using System;
using System.Collections.Generic;

namespace LangSwap.hook.manager;

// ----------------------------
// Hook Manager
// ----------------------------
public class HookManager(Configuration config, TranslationCache translationCache) : IDisposable
{
    // Log
    private const string Class = "[HookManager.cs]";

    // Service
    private static IPluginLog Log => Plugin.Log;

    // Individual hooks
    private readonly AlliesCastBarsHook alliesCastBarsHook = new(config, translationCache);
    private readonly EnemiesCastBarsHook enemiesCastBarsHook = new(config, translationCache);
    private readonly ActionTooltipHook actionTooltipHook = new(config, translationCache);
    private readonly ItemTooltipHook itemTooltipHook = new(config, translationCache);

    // Active hooks
    private readonly HashSet<BaseHook> hooks = [];

    // ----------------------------
    // Enable all translation hooks
    // ----------------------------
    public void EnableAll()
    {
        // Add hooks based on configuration
        if (config.AlliesCastBarsTarget || config.AlliesCastBarsFocus || config.AlliesCastBarsPartyList) hooks.Add(alliesCastBarsHook);
        if (config.EnemiesCastBarsTarget || config.EnemiesCastBarsFocus || config.EnemiesCastBarsHateList) hooks.Add(enemiesCastBarsHook);
        if (config.ActionTooltip) hooks.Add(actionTooltipHook);
        if (config.ItemTooltip) hooks.Add(itemTooltipHook);

        // Enable all active hooks
        foreach (BaseHook hook in hooks)
        {
            try
            {
                // For each hook type
                switch (hook)
                {
                    case AlliesCastBarsHook:
                        hook.Enable("Allies CastBars");
                        break;
                    case EnemiesCastBarsHook:
                        hook.Enable("Enemies CastBars");
                        break;
                    case ActionTooltipHook:
                        hook.Enable("Action Tooltip");
                        break;
                    case ItemTooltipHook:
                        hook.Enable("Item Tooltip");
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{Class} - Failed to enable {hook.GetType().Name}");
            }
        }
    }

    // ----------------------------
    // Update hooks based on configuration
    // ----------------------------
    public void UpdateHooks()
    {
        // Allies CastBars
        bool alliesEnabled = config.AlliesCastBarsTarget || config.AlliesCastBarsFocus || config.AlliesCastBarsPartyList;
        if (alliesEnabled && !hooks.Contains(alliesCastBarsHook))
        {
            try { alliesCastBarsHook.Enable("Allies CastBars"); hooks.Add(alliesCastBarsHook); }
            catch (Exception ex) { Log.Error(ex, $"{Class} - Failed to enable Allies CastBars"); }
        }
        else if (!alliesEnabled && hooks.Contains(alliesCastBarsHook))
        {
            try { alliesCastBarsHook.Disable("Allies CastBars"); hooks.Remove(alliesCastBarsHook); }
            catch (Exception ex) { Log.Error(ex, $"{Class} - Failed to disable Allies CastBars"); }
        }

        // Enemies CastBars
        bool enemiesEnabled = config.EnemiesCastBarsTarget || config.EnemiesCastBarsFocus || config.EnemiesCastBarsHateList;
        if (enemiesEnabled && !hooks.Contains(enemiesCastBarsHook))
        {
            try { enemiesCastBarsHook.Enable("Enemies CastBars"); hooks.Add(enemiesCastBarsHook); }
            catch (Exception ex) { Log.Error(ex, $"{Class} - Failed to enable Enemies CastBars"); }
        }
        else if (!enemiesEnabled && hooks.Contains(enemiesCastBarsHook))
        {
            try { enemiesCastBarsHook.Disable("Enemies CastBars"); hooks.Remove(enemiesCastBarsHook); }
            catch (Exception ex) { Log.Error(ex, $"{Class} - Failed to disable Enemies CastBars"); }
        }

        // Action Tooltip
        if (config.ActionTooltip && !hooks.Contains(actionTooltipHook))
        {
            try { actionTooltipHook.Enable("Action Tooltip"); hooks.Add(actionTooltipHook); }
            catch (Exception ex) { Log.Error(ex, $"{Class} - Failed to enable Action Tooltip"); }
        }
        else if (!config.ActionTooltip && hooks.Contains(actionTooltipHook))
        {
            try { actionTooltipHook.Disable("Action Tooltip"); hooks.Remove(actionTooltipHook); }
            catch (Exception ex) { Log.Error(ex, $"{Class} - Failed to disable Action Tooltip"); }
        }

        // Item Tooltip
        if (config.ItemTooltip && !hooks.Contains(itemTooltipHook))
        {
            try { itemTooltipHook.Enable("Item Tooltip"); hooks.Add(itemTooltipHook); }
            catch (Exception ex) { Log.Error(ex, $"{Class} - Failed to enable Item Tooltip"); }
        }
        else if (!config.ItemTooltip && hooks.Contains(itemTooltipHook))
        {
            try { itemTooltipHook.Disable("Item Tooltip"); hooks.Remove(itemTooltipHook); }
            catch (Exception ex) { Log.Error(ex, $"{Class} - Failed to disable Item Tooltip"); }
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
                Log.Error(ex, $"{Class} - Failed to swap language for {hook.GetType().Name}");
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
                Log.Error(ex, $"{Class} - Failed to restore language for {hook.GetType().Name}");
            }
        }
    }

    // ----------------------------
    // Disable all translation hooks
    // ----------------------------
    private void DisableAll()
    {
        // Disable all active hooks
        foreach (BaseHook hook in hooks)
        {
            try
            {
                // For each hook type
                switch (hook)
                {
                    case AlliesCastBarsHook:
                        hook.Disable("Allies CastBars");
                        break;
                    case EnemiesCastBarsHook:
                        hook.Disable("Enemies CastBars");
                        break;
                    case ActionTooltipHook:
                        hook.Disable("Action Tooltip");
                        break;
                    case ItemTooltipHook:
                        hook.Disable("Item Tooltip");
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{Class} - Failed to disable {hook.GetType().Name}");
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
                Log.Error(ex, $"{Class} - Failed to dispose {hook.GetType().Name}");
            }
        }

        // Clear hook list
        hooks.Clear();

        // Finalize
        GC.SuppressFinalize(this);
    }

}