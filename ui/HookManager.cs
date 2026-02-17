using Dalamud.Plugin.Services;
using LangSwap.translation;
using LangSwap.ui.hooks;
using System;
using System.Collections.Generic;

namespace LangSwap.ui;

/// <summary>
/// Hook manager
/// </summary>
public class HookManager(
    Configuration configuration,
    IGameGui gameGui,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    IPluginLog log) : IDisposable
{
    private readonly List<BaseHook> hooks = [];
    private readonly IPluginLog log = log;

    private readonly ItemDetailHook itemDetailHook = new(configuration, gameInterop, sigScanner, translationCache, log, gameGui);
    private readonly ActionDetailHook actionDetailHook = new(configuration, gameInterop, sigScanner, translationCache, log, gameGui);
    private readonly CastBarHook castBarHook = new(configuration, gameInterop, sigScanner, translationCache, log, gameGui);

    /// <summary>
    /// Enable all translation hooks
    /// </summary>
    public void EnableAll()
    {
        log.Information("Enabling all translation hooks...");
        // Add hooks if component is enabled
        if (configuration.Castbars)
            // TODO : cassé !!! hooks.Add(castBarHook);
        if (configuration.ActionDetails)
            // TODO : cassé aussi !! hooks.Add(actionDetailHook);
        if (configuration.ItemDetails)
            hooks.Add(itemDetailHook);
        // Enable all hooks
        foreach (var hook in hooks)
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
        log.Information("All translation hooks enabled");
    }

    /// <summary>
    /// Swap all hooks to target language
    /// </summary>
    public void SwapLanguage()
    {
        log.Information("Swapping language for all hooks...");
        foreach (var hook in hooks)
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

    /// <summary>
    /// Restore all hooks to original language
    /// </summary>
    public void RestoreLanguage()
    {
        log.Information("Restoring language for all hooks...");
        foreach (var hook in hooks)
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

    /// <summary>
    /// Disable all translation hooks
    /// </summary>
    public void DisableAll()
    {
        log.Information("Disabling all translation hooks...");
        foreach (var hook in hooks)
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
        log.Information("All translation hooks disabled");
    }

    public void Dispose()
    {
        DisableAll();
        foreach (var hook in hooks)
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
        hooks.Clear();
    }
}