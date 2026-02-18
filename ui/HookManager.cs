using Dalamud.Plugin.Services;
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
    Configuration configuration,
    IGameGui gameGui,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    IPluginLog log) : IDisposable
{
    // Individual hooks
    private readonly CastBarHook castBarHook = new(configuration, gameGui, gameInterop, sigScanner, translationCache, log);
    private readonly ActionDetailHook actionDetailHook = new(configuration, gameGui, gameInterop, sigScanner, translationCache, log);
    private readonly ItemDetailHook itemDetailHook = new(configuration, gameGui, gameInterop, sigScanner, translationCache, log);

    // Active hooks
    private readonly HashSet<BaseHook> hooks = [];

    // ----------------------------
    // Enable all translation hooks
    // ----------------------------
    public void EnableAll()
    {
        // Add hooks if component is enabled
        if (configuration.Castbars)
            // TODO : hooks.Add(castBarHook);
        if (configuration.ActionDetails)
            // TODO : hooks.Add(actionDetailHook);
        if (configuration.ItemDetails)
            hooks.Add(itemDetailHook);

        // Enable all hooks
        foreach (BaseHook hook in hooks)
        {
            try
            {
                hook.Enable();
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Failed to enable {hook.GetType().Name}");
            }
        }
    }

    // ----------------------------
    // Enable translation hook
    // ----------------------------
    public void EnableHook(HookEnum hook)
    {
        try
        {
            switch (hook)
            {
                // Add cast bar hook
                case HookEnum.CastBar:
                    if (hooks.Add(castBarHook))
                        castBarHook.Enable();
                    break;
                // Add action detail hook
                case HookEnum.ActionDetail:
                    if (hooks.Add(actionDetailHook))
                        actionDetailHook.Enable();
                    break;
                // Add item detail hook
                case HookEnum.ItemDetail:
                    if (hooks.Add(itemDetailHook))
                        itemDetailHook.Enable();
                    break;
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to enable {hook}");
        }
    }

    // ----------------------------
    // Swap all hooks to target language
    // ----------------------------
    public void SwapLanguage()
    {
        // Swap language for all hooks
        foreach (BaseHook hook in hooks)
        {
            try
            {
                hook.SwapLanguage();
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Failed to swap language for {hook.GetType().Name}");
            }
        }
    }

    // ----------------------------
    // Restore all hooks to original language
    // ----------------------------
    public void RestoreLanguage()
    {
        // Restore language for all hooks
        foreach (BaseHook hook in hooks)
        {
            try
            {
                hook.RestoreLanguage();
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Failed to restore language for {hook.GetType().Name}");
            }
        }
    }

    // ----------------------------
    // Disable translation hook
    // ----------------------------
    public void DisableHook(HookEnum hook)
    {
        try
        {
            switch (hook)
            {
                // Remove cast bar hook
                case HookEnum.CastBar:
                    if (hooks.Remove(castBarHook))
                        castBarHook.Disable();
                    break;
                // Remove action detail hook
                case HookEnum.ActionDetail:
                    if (hooks.Remove(actionDetailHook))
                        actionDetailHook.Disable();
                    break;
                // Remove item detail hook
                case HookEnum.ItemDetail:
                    if (hooks.Remove(itemDetailHook))
                        itemDetailHook.Disable();
                    break;
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to enable {hook}");
        }
    }

    // ----------------------------
    // Disable all translation hooks
    // ----------------------------
    public void DisableAll()
    {
        // Disable all hooks
        foreach (BaseHook hook in hooks)
        {
            try
            {
                hook.Disable();
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Failed to disable {hook.GetType().Name}");
            }
        }
    }

    // ----------------------------
    // Dispose all hooks
    // ----------------------------
    public void Dispose()
    {
        // Disable all hooks
        DisableAll();

        // Dispose all hooks
        foreach (BaseHook hook in hooks)
        {
            try
            {
                hook.Dispose();
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Failed to dispose {hook.GetType().Name}");
            }
        }

        // Clear hook list
        hooks.Clear();

        // Finalize
        GC.SuppressFinalize(this);
    }
}