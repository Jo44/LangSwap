using Dalamud.Game.NativeWrapper;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.translation;
using System;

namespace LangSwap.hook.template;

// ----------------------------
// Base class for all hooks
// ----------------------------
public unsafe abstract class BaseHook(Configuration config, TranslationCache translationCache) : IDisposable
{
    // Log
    private const string Class = "[BaseHook.cs]";

    // Services
    private static IGameGui GameGui => Plugin.GameGui;
    protected static IPluginLog Log => Plugin.Log;

    // Core components
    protected readonly Configuration config = config;
    protected readonly TranslationCache translationCache = translationCache;

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
        Log.Debug($"{Class} - Swap enabled");

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
        Log.Debug($"{Class} - Swap disabled");

        // Call hook-specific behavior
        OnLanguageSwap();
    }

    // ----------------------------
    // Called when language is swapped or restored
    // ----------------------------
    protected virtual void OnLanguageSwap() { }

    // ----------------------------
    // Get addon
    // ----------------------------
    protected static AtkUnitBase* GetAddon(string addonName)
    {
        try
        {
            // Get pointer from name
            AtkUnitBasePtr addonPtr = GameGui.GetAddonByName(addonName);

            // Check for null pointer
            if (addonPtr.IsNull) return null;

            // Return addon from pointer
            return (AtkUnitBase*)addonPtr.Address;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Failed to get {addonName} addon");
        }
        return null;
    }

    // ----------------------------
    // Refresh addon
    // ----------------------------
    protected void RefreshAddon(AtkUnitBase* addon, string errorContext)
    {
        try
        {
            // Only refresh if the addon is currently visible
            if (addon != null && addon -> IsVisible)
            {
                addon -> Hide(true, false, 0);
                addon -> Show(true, 0);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Failed to refresh {errorContext} addon");
        }
    }

    // ----------------------------
    // Disable the hook
    // ----------------------------
    public abstract void Disable(string hookName);

    // ----------------------------
    // Dispose
    // ----------------------------
    public abstract void Dispose();

}