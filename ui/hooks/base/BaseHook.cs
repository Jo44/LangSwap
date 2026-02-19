using Dalamud.Plugin.Services;
using LangSwap.translation;
using System;

namespace LangSwap.ui.hooks.@base;

// ----------------------------
// Base class for all hooks
// ----------------------------
public abstract class BaseHook(
    Configuration configuration,
    IGameGui gameGui,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    IPluginLog log) : IDisposable
{
    // Core components
    protected readonly Configuration configuration = configuration;
    protected readonly IGameGui gameGui = gameGui;
    protected readonly IGameInteropProvider gameInterop = gameInterop;
    protected readonly ISigScanner sigScanner = sigScanner;
    protected readonly TranslationCache translationCache = translationCache;
    protected readonly IPluginLog log = log;

    // Toggle state
    protected bool isEnabled = false;
    protected bool isLanguageSwapped = false;

    // ----------------------------
    // Enable the hook
    // ----------------------------
    public abstract void Enable();

    // ----------------------------
    // Swap to target language
    // ----------------------------
    public virtual void SwapLanguage()
    {
        // Prevent redundant swaps
        if (isLanguageSwapped) return;

        // Set flag and log
        isLanguageSwapped = true;
        log.Debug($"{GetType().Name}: Language swap enabled");

        // Call hook-specific behavior
        OnLanguageSwapped();
    }

    // ----------------------------
    // Restore to original language
    // ----------------------------
    public virtual void RestoreLanguage()
    {
        // Prevent redundant restores
        if (!isLanguageSwapped) return;

        // Clear flag and log
        isLanguageSwapped = false;
        log.Debug($"{GetType().Name}: Language swap disabled");

        // Call hook-specific behavior
        OnLanguageRestored();
    }

    // ----------------------------
    // Called when language is swapped
    // ----------------------------
    protected virtual void OnLanguageSwapped() { }

    // ----------------------------
    // Called when language is restored
    // ----------------------------
    protected virtual void OnLanguageRestored() { }

    // ----------------------------
    // Disable the hook
    // ----------------------------
    public abstract void Disable();

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    public abstract void Dispose();
}