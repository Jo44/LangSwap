using Dalamud.Plugin.Services;
using LangSwap.tool;
using LangSwap.translation;
using System;

namespace LangSwap.ui.hooks.@base;

// ----------------------------
// Base class for all hooks
// ----------------------------
public abstract class BaseHook(
    IClientState clientState,
    Configuration config,
    IFramework framework,
    IGameGui gameGui,
    IGameInteropProvider gameInterop,
    IObjectTable objectTable,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : IDisposable
{
    // Log
    private const string Class = "[BaseHook.cs]";

    // Core components
    protected readonly IClientState clientState = clientState;
    protected readonly Configuration config = config;
    protected readonly IFramework framework = framework;
    protected readonly IGameGui gameGui = gameGui;
    protected readonly IGameInteropProvider gameInterop = gameInterop;
    protected readonly IObjectTable objectTable = objectTable;
    protected readonly ISigScanner sigScanner = sigScanner;
    protected readonly TranslationCache translationCache = translationCache;
    protected readonly Utilities utilities = utilities;
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
    public abstract void Disable();

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    public abstract void Dispose();

}