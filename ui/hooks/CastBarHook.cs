using Dalamud.Game.NativeWrapper;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.ui.hooks.@base;
using System;

namespace LangSwap.ui.hooks;

// ----------------------------
// Castbar Hook
// ----------------------------
public unsafe class CastBarHook(
    Configuration config,
    IGameGui gameGui,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : BaseHook(config, gameGui, gameInterop, sigScanner, translationCache, utilities, log)
{
    // Log
    private const string Class = "[CastBarHook.cs]";

    // CastBar Addon
    private readonly string CastBarAddon = config.CastBarAddon;

    // Delegate function
    private delegate void CastBarDelegate(IntPtr castBarPtr, uint actionId, IntPtr actionNamePtr);

    // Hook
    private Hook<CastBarDelegate>? _castBarHook;

    // ----------------------------
    // Enable the hook
    // ----------------------------
    public override void Enable()
    {
        // Prevent multiple enables
        if (isEnabled) return;

        try
        {
            // Get address from signature
            nint castBarAddr = sigScanner.ScanText(config.CastBarSig);
            if (castBarAddr != IntPtr.Zero)
            {
                // Get hook from address
                _castBarHook = gameInterop.HookFromAddress<CastBarDelegate>(castBarAddr, CastBarDetour);

                // Enable hook
                _castBarHook.Enable();

                // Set enabled flag
                isEnabled = true;

                // Log
                log.Debug($"{Class} - Cast bar hook enabled at 0x{castBarAddr:X}");
            }
            else
            {
                log.Warning($"{Class} - Cast bar signature not found");
            }

            isEnabled = true;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to enable cast bar hook");
        }
    }

    // ----------------------------
    // On language swap
    // ----------------------------
    protected override void OnLanguageSwap()
    {
        // Refresh cast bar addon
        try
        {
            // Get pointer to cast bar addon
            AtkUnitBasePtr castBarPtr = gameGui.GetAddonByName(CastBarAddon);
            if (!castBarPtr.IsNull)
            {
                // Get AtkUnitBase from pointer
                AtkUnitBase* castBar = (AtkUnitBase*)castBarPtr.Address;

                // Only refresh if the addon is currently visible
                if (castBar != null && castBar -> IsVisible)
                {
                    castBar -> Hide(true, false, 0);
                    castBar -> Show(true, 0);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to refresh cast bar addon");
        }
    }

    // ----------------------------
    // Cast bar detour
    // ----------------------------
    private void CastBarDetour(IntPtr castBarPtr, uint actionId, IntPtr actionNamePtr)
    {
        // TODO
        try
        {
            uint currentCastActionId = actionId;
            log.Debug($"{Class} - UpdateCastBar called: actionId={actionId}");

            if (isLanguageSwapped && actionId > 0 && actionId < config.MaxValidActionId)
            {
                LanguageEnum targetLang = (LanguageEnum)config.TargetLanguage;
                string translatedName = translationCache.GetActionName(actionId, targetLang) ?? string.Empty;
                
                if (!string.IsNullOrWhiteSpace(translatedName))
                {
                    log.Information($"{Class} - Translating cast bar action {actionId} to: {translatedName}");
                    
                    byte[] translatedBytes = System.Text.Encoding.UTF8.GetBytes(translatedName + "\0");
                    unsafe
                    {
                        fixed (byte* ptr = translatedBytes)
                        {
                            actionNamePtr = new IntPtr(ptr);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Exception in UpdateCastBar for action {actionId}");
        }

        _castBarHook!.Original(castBarPtr, actionId, actionNamePtr);
    }

    // ----------------------------
    // Disable the hook
    // ----------------------------
    public override void Disable()
    {
        // Prevent multiple disables
        if (!isEnabled) return;

        try
        {
            // Disable cast bar hook
            _castBarHook?.Disable();

            // Set disabled flag
            isEnabled = false;
            log.Debug($"{Class} - Cast bar hook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to disable cast bar hook");
        }
    }

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    public override void Dispose()
    {
        try
        {
            // Dispose cast bar hook
            _castBarHook?.Disable();
            _castBarHook?.Dispose();
            _castBarHook = null;

            // Set disabled flag
            isEnabled = false;
            log.Debug($"{Class} - Cast bar hook disposed");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to dispose cast bar hook");
        }

        // Finalize
        GC.SuppressFinalize(this);
    }

}