using Dalamud.Game.NativeWrapper;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.translation;
using System;

namespace LangSwap.hook.template;

// ----------------------------
// Base class for all hooks
//
// @author Jo44
// @version 1.7 (23/04/2026)
// @since 01/01/2026
// ----------------------------
public unsafe abstract class BaseHook(Configuration config, TranslationCache translationCache) : IDisposable
{
    // Log
    private readonly string Class = $"[{nameof(BaseHook)}]";

    // Hook name
    public abstract string Name { get; }

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
    protected AtkUnitBase* GetAddon(string addonName)
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
    public abstract void Disable();

    // ----------------------------
    // Dispose
    // ----------------------------
    public abstract void Dispose();

}