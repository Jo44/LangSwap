using Dalamud.Plugin.Services;
using LangSwap.translation;
using System;

namespace LangSwap.ui.hooks;

/// <summary>
/// Base class for all hooks
/// </summary>
public abstract class BaseHook(
    Configuration configuration,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    IPluginLog log) : IDisposable
{
    // TODO : vraiment utile ça ?
    protected readonly Configuration configuration = configuration;
    protected readonly IGameInteropProvider gameInterop = gameInterop;
    protected readonly ISigScanner sigScanner = sigScanner;
    protected readonly TranslationCache translationCache = translationCache;
    protected readonly IPluginLog log = log;

    protected bool isEnabled = false;
    protected bool isLanguageSwapped = false;

    /// <summary>
    /// Enable the hook
    /// </summary>
    public abstract void Enable();

    /// <summary>
    /// Swap to target language
    /// </summary>
    public virtual void SwapLanguage()
    {
        if (isLanguageSwapped)
        {
            log.Debug($"{GetType().Name}: Language already swapped, ignoring");
            return;
        }
        
        isLanguageSwapped = true;
        log.Information($"{GetType().Name}: Language swap enabled");
        OnLanguageSwapped();
    }

    /// <summary>
    /// Restore to original language
    /// </summary>
    public virtual void RestoreLanguage()
    {
        if (!isLanguageSwapped)
        {
            log.Debug($"{GetType().Name}: Language not swapped, ignoring restore");
            return;
        }
        
        isLanguageSwapped = false;
        log.Information($"{GetType().Name}: Language swap disabled");
        OnLanguageRestored();
    }

    /// <summary>
    /// Called when language is swapped - override to add custom behavior
    /// </summary>
    protected virtual void OnLanguageSwapped() { }

    /// <summary>
    /// Called when language is restored - override to add custom behavior
    /// </summary>
    protected virtual void OnLanguageRestored() { }

    /// <summary>
    /// Disable the hook
    /// </summary>
    public abstract void Disable();

    /// <summary>
    /// Dispose the hook
    /// </summary>
    public abstract void Dispose();
}