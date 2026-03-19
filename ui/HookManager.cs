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
                hook.Enable();
            }
            catch (Exception ex)
            {
                log.Error(ex, $"{Class} - Failed to enable {hook.GetType().Name}");
            }
        }
    }

    // ----------------------------
    // Enable translation hook
    // ----------------------------
    public void Enable(HookEnum hookEnum)
    {
        try
        {
            switch (hookEnum)
            {
                // Action Tooltip
                case HookEnum.ActionTooltip:
                    // Enable hook
                    if (hooks.Add(actionTooltipHook)) actionTooltipHook.Enable();
                    break;
                // Item Tooltip
                case HookEnum.ItemTooltip:
                    // Enable hook
                    if (hooks.Add(itemTooltipHook)) itemTooltipHook.Enable();
                    break;
                // Allies Cast Bars
                case HookEnum.AlliesCastBars:
                    // Enable hook
                    if (hooks.Add(alliesCastBarsHook)) alliesCastBarsHook.Enable();
                    break;
                // Enemies Cast Bars
                case HookEnum.EnemiesCastBars:
                    // Enable hook
                    if (hooks.Add(enemiesCastBarsHook)) enemiesCastBarsHook.Enable();
                    break;
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to enable {hookEnum}");
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
    // Disable translation hook
    // ----------------------------
    public void Disable(HookEnum hookEnum)
    {
        try
        {
            switch (hookEnum)
            {
                // Action Tooltip
                case HookEnum.ActionTooltip:
                    // Disable hook
                    if (hooks.Remove(actionTooltipHook)) actionTooltipHook.Disable();
                    break;
                // Item Tooltip
                case HookEnum.ItemTooltip:
                    // Disable hook
                    if (hooks.Remove(itemTooltipHook)) itemTooltipHook.Disable();
                    break;
                // Allies Cast Bars
                case HookEnum.AlliesCastBars:
                    // Disable hook
                    if (hooks.Remove(alliesCastBarsHook)) alliesCastBarsHook.Disable();
                    break;
                // Enemies Cast Bars
                case HookEnum.EnemiesCastBars:
                    // Disable hook
                    if (hooks.Remove(enemiesCastBarsHook)) enemiesCastBarsHook.Disable();
                    break;
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to enable {hookEnum}");
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
                hook.Disable();
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