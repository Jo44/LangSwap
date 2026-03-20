using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.tool;
using LangSwap.translation;
using System;

namespace LangSwap.ui.hooks.@base;

// ----------------------------
// Base class for all tooltips hooks
// ----------------------------
public unsafe abstract class TooltipBaseHook(
    Configuration config,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : BaseHook(config, translationCache, utilities, log)
{
    // Log
    private const string Class = "[TooltipBaseHook.cs]";

    // Core component
    private readonly IGameInteropProvider gameInterop = gameInterop;
    private readonly ISigScanner sigScanner = sigScanner;

    // Delegate function
    protected delegate void* TooltipDelegate(AtkUnitBase* actionDetailAddon, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);

    // Hook
    protected Hook<TooltipDelegate>? _tooltipHook;

    // Memory signature
    protected abstract string MemorySignature { get; }

    // Addon
    protected abstract AtkUnitBase* Addon { get; }

    // ----------------------------
    // Enable the hook
    // ----------------------------
    public override void Enable(string hookName)
    {
        // Prevent multiple enables
        if (isEnabled) return;

        try
        {
            // Get address from signature
            nint tooltipAddr = sigScanner.ScanText(MemorySignature);
            if (tooltipAddr != IntPtr.Zero)
            {
                // Get hook from address
                _tooltipHook = gameInterop.HookFromAddress<TooltipDelegate>(tooltipAddr, OnTooltipUpdate);

                // Enable hook
                _tooltipHook.Enable();

                // Set enabled flag
                isEnabled = true;

                // Log
                log.Debug($"{Class} - {hookName} hook enabled at 0x{tooltipAddr:X}");
            }
            else
            {
                log.Error($"{Class} - {hookName} signature not found");
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to enable {hookName} hook");
        }
    }

    // ----------------------------
    // On tooltip update
    // ----------------------------
    protected abstract void* OnTooltipUpdate(AtkUnitBase* tooltipAddon, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);

    // ----------------------------
    // Disable the hook
    // ----------------------------
    public override void Disable(string hookName)
    {
        // Prevent multiple disables
        if (!isEnabled) return;

        try
        {
            // Disable tooltip hook
            _tooltipHook?.Disable();

            // Set disabled flag
            isEnabled = false;
            log.Debug($"{Class} - {hookName} hook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to disable {hookName} hook");
        }
    }

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    public override void Dispose(string hookName)
    {
        try
        {
            // Dispose action tooltip hook
            _tooltipHook?.Disable();
            _tooltipHook?.Dispose();
            _tooltipHook = null;

            // Set disabled flag
            isEnabled = false;

            // Dispose base resources
            Dispose();
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to dispose {hookName} hook");
        }
    }

}