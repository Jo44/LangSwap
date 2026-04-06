using Dalamud.Plugin.Services;
using LangSwap.tool;
using LangSwap.translation;
using System;

namespace LangSwap.hook.@base;

// ----------------------------
// Base class for all hooks
// ----------------------------
public abstract class BaseHook(
    Configuration config,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : IDisposable
{
    // Log
    private const string Class = "[BaseHook.cs]";

    // Core components
    protected readonly Configuration config = config;
    protected readonly TranslationCache translationCache = translationCache;
    protected readonly Utilities utilities = utilities;
    protected readonly IPluginLog log = log;

    // Toggle states
    protected bool isEnabled = false;
    protected bool isLanguageSwapped = false;

    // ----------------------------
    // Enable the hook
    // ----------------------------
    public abstract void Enable(string hookName);

    // ----------------------------
    // Swap to target language
    // ----------------------------
    public virtual void SwapLanguage()
    {
        // Prevent redundant swaps
        if (isLanguageSwapped) return;

        // Set flag
        isLanguageSwapped = true;

        // Log
        log.Debug($"{Class} - {GetType().Name} : Swap enabled");

        // Call hook-specific behavior
        OnLanguageSwap();
    }

    // ----------------------------
    // Restore to original language
    // ----------------------------
    public virtual void RestoreLanguage()
    {
        // Prevent redundant restores
        if (!isLanguageSwapped) return;

        // Clear flag
        isLanguageSwapped = false;

        // Log
        log.Debug($"{Class} - {GetType().Name} : Swap disabled");

        // Call hook-specific behavior
        OnLanguageSwap();
    }

    // ----------------------------
    // Called when language is swapped or restored
    // ----------------------------
    protected virtual void OnLanguageSwap() { }

    // ----------------------------
    // Disable the hook
    // ----------------------------
    public abstract void Disable(string hookName);

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    protected abstract void Dispose(string hookName);

    // ----------------------------
    // Dispose
    // ----------------------------
    public void Dispose()
    {
        // Finalize
        GC.SuppressFinalize(this);
    }

}