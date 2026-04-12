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
    private readonly string Class = $"[{nameof(HookManager)}]";

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
    // Enable all active hooks
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
                hook.Enable();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{Class} - Failed to enable {hook.Name}");
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
            try { alliesCastBarsHook.Enable(); hooks.Add(alliesCastBarsHook); }
            catch (Exception ex) { Log.Error(ex, $"{Class} - Failed to enable {alliesCastBarsHook.Name}"); }
        }
        else if (!alliesEnabled && hooks.Contains(alliesCastBarsHook))
        {
            try { alliesCastBarsHook.Disable(); hooks.Remove(alliesCastBarsHook); }
            catch (Exception ex) { Log.Error(ex, $"{Class} - Failed to disable {alliesCastBarsHook.Name}"); }
        }

        // Enemies CastBars
        bool enemiesEnabled = config.EnemiesCastBarsTarget || config.EnemiesCastBarsFocus || config.EnemiesCastBarsHateList;
        if (enemiesEnabled && !hooks.Contains(enemiesCastBarsHook))
        {
            try { enemiesCastBarsHook.Enable(); hooks.Add(enemiesCastBarsHook); }
            catch (Exception ex) { Log.Error(ex, $"{Class} - Failed to enable {enemiesCastBarsHook.Name}"); }
        }
        else if (!enemiesEnabled && hooks.Contains(enemiesCastBarsHook))
        {
            try { enemiesCastBarsHook.Disable(); hooks.Remove(enemiesCastBarsHook); }
            catch (Exception ex) { Log.Error(ex, $"{Class} - Failed to disable {enemiesCastBarsHook.Name}"); }
        }

        // Action Tooltip
        if (config.ActionTooltip && !hooks.Contains(actionTooltipHook))
        {
            try { actionTooltipHook.Enable(); hooks.Add(actionTooltipHook); }
            catch (Exception ex) { Log.Error(ex, $"{Class} - Failed to enable {actionTooltipHook.Name}"); }
        }
        else if (!config.ActionTooltip && hooks.Contains(actionTooltipHook))
        {
            try { actionTooltipHook.Disable(); hooks.Remove(actionTooltipHook); }
            catch (Exception ex) { Log.Error(ex, $"{Class} - Failed to disable {actionTooltipHook.Name}"); }
        }

        // Item Tooltip
        if (config.ItemTooltip && !hooks.Contains(itemTooltipHook))
        {
            try { itemTooltipHook.Enable(); hooks.Add(itemTooltipHook); }
            catch (Exception ex) { Log.Error(ex, $"{Class} - Failed to enable {itemTooltipHook.Name}"); }
        }
        else if (!config.ItemTooltip && hooks.Contains(itemTooltipHook))
        {
            try { itemTooltipHook.Disable(); hooks.Remove(itemTooltipHook); }
            catch (Exception ex) { Log.Error(ex, $"{Class} - Failed to disable {itemTooltipHook.Name}"); }
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
                Log.Error(ex, $"{Class} - Failed to swap language for {hook.Name}");
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
                Log.Error(ex, $"{Class} - Failed to restore language for {hook.Name}");
            }
        }
    }

    // ----------------------------
    // Disable all active hooks
    // ----------------------------
    private void DisableAll()
    {
        // Disable all active hooks
        foreach (BaseHook hook in hooks)
        {
            try
            {
                hook.Disable();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{Class} - Failed to disable {hook.Name}");
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
                Log.Error(ex, $"{Class} - Failed to dispose {hook.Name}");
            }
        }

        // Clear hook list
        hooks.Clear();

        // Finalize
        GC.SuppressFinalize(this);
    }

}