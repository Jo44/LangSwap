using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using LangSwap.translation;
using System;

namespace LangSwap.ui;

// Hook for intercepting and modifying item tooltips
public class TooltipHook(
    Configuration configuration,
    IGameGui gameGui,
    IGameInteropProvider gameInterop,
    TranslationCache translationCache,
    IPluginLog log
    ) : IDisposable
{
    // References
    private readonly Configuration configuration = configuration;
    private readonly IGameGui gameGui = gameGui;
    private readonly IGameInteropProvider gameInterop = gameInterop;
    private readonly TranslationCache translationCache = translationCache;
    private readonly IPluginLog log = log;
    private delegate void UpdateItemTooltipDelegate(IntPtr tooltip, uint itemId, bool isHq, IntPtr data);
    private Hook<UpdateItemTooltipDelegate>? tooltipHook;
    private LanguageEnum targetLanguage = (LanguageEnum)configuration.TargetLanguage;
    private bool isEnabled = false;
    private bool isLanguageSwapped = false;

    // Enable the hook
    public void Enable()
    {
        if (isEnabled) return;

        try
        {
            // Note: The actual function address needs to be found via reverse engineering
            // For now, we'll set up the structure but the hook address needs to be determined
            // Common addresses for tooltip functions in FFXIV (these may need adjustment):
            // - Item tooltip update: typically around 0x140xxxxxx range
            // 
            // For Dalamud plugins, you typically need to:
            // 1. Find the function signature via reverse engineering
            // 2. Use SigScanner to find the function address
            // 3. Create the hook with the correct signature
            
            // Placeholder: This will need the actual function address
            // var address = gameGui.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B F8 48 85 C0 74 ?? 48 8B 4F ??");
            // tooltipHook = gameInterop.HookFromAddress<UpdateItemTooltipDelegate>(address, UpdateItemTooltipDetour);
            // tooltipHook?.Enable();
            
            log.Debug("Tooltip hook structure initialized (address needs to be configured)");
            isEnabled = true;
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to enable tooltip hook");
            isEnabled = false;
        }
    }

    // Disable the hook
    public void Disable()
    {
        if (!isEnabled) return;

        try
        {
            tooltipHook?.Disable();
            isEnabled = false;
            log.Debug("Tooltip hook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to disable tooltip hook");
        }
    }

    // Set language swap state
    public void SetLanguageSwapped(bool swapped)
    {
        isLanguageSwapped = swapped;
        // TODO : on est sur de vouloir faire ca ?
        // if (swapped)
            // targetLanguage = (LanguageEnum)configuration.TargetLanguage;
        // else
            // targetLanguage = (LanguageEnum)configuration.ClientLanguage;
    }

    // Hooked function that intercepts tooltip updates
    private void UpdateItemTooltipDetour(IntPtr tooltip, uint itemId, bool isHq, IntPtr data)
    {
        try
        {
            // Call original function first to populate the tooltip
            tooltipHook?.Original(tooltip, itemId, isHq, data);

            // If language is swapped, modify the tooltip text
            if (isLanguageSwapped && itemId > 0)
            {
                // Get translated item name
                var translatedName = translationCache.GetItemName(itemId, targetLanguage);
                
                if (!string.IsNullOrEmpty(translatedName))
                {
                    // Modify the tooltip text
                    // This requires unsafe code to modify the tooltip structure
                    // The actual implementation depends on the tooltip structure
                    ModifyTooltipTextUnsafe(tooltip, translatedName);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception in tooltip hook for item {itemId}");
        }
    }

    // Modify the tooltip text (unsafe implementation)
    private unsafe void ModifyTooltipTextUnsafe(IntPtr tooltipPtr, string newText)
    {
        // This requires FFXIVClientStructs or similar to access the tooltip structure
        // For now, this is a placeholder that shows the structure needed
        // 
        // Typical tooltip structure in FFXIV:
        // - AtkUnitBase* tooltip
        // - Text nodes containing item name (usually node ID 2 or 3)
        // - Need to find the correct text node and replace its content
        
        try
        {
            // Placeholder: Actual implementation requires:
            // 1. Cast IntPtr to AtkUnitBase*
            // 2. Find the text node containing item name
            // 3. Replace the text content
            // 4. Update the node
            
            log.Debug($"Would modify tooltip text to: {newText}");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Exception while modifying tooltip text");
        }
    }

    // Get item ID from tooltip (helper method)
    public uint? GetItemIdFromTooltip(IntPtr tooltipPtr)
    {
        // This would require parsing the tooltip structure
        // For now, the item ID is passed directly to the hook function
        return null;
    }

    public void Dispose()
    {
        Disable();
        tooltipHook?.Dispose();
    }
}
