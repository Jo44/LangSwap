using Dalamud.Game.NativeWrapper;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.ui.hooks.@base;
using Lumina.Data.Parsing;
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

    // Delegate function
    private delegate void* CastBarDelegate(void* agentHud, void* numberArray, void* stringArray, StatusManager* statusManager, GameObject* target, void* isLocalPlayerAndRollPlaying);

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
            // TODO : externaliser la signature
            nint castBarAddr = sigScanner.ScanText("4C 8B DC 53 48 81 EC 30 03 00 00");
            if (castBarAddr != IntPtr.Zero)
            {
                // Get hook from address
                _castBarHook = gameInterop.HookFromAddress<CastBarDelegate>(castBarAddr, OnCastBarUpdate);

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
        // TODO : 2 addon à modifier => CastBar et TargetCastBar

        // Refresh cast bar addon
        try
        {
            // Get pointer to cast bar addon
            AtkUnitBasePtr castBarPtr = gameGui.GetAddonByName(config.CastBarAddon);
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
    // On cast bar update
    // ----------------------------
    private void* OnCastBarUpdate(void* agentHud, void* numberArray, void* stringArray, StatusManager* statusManager, GameObject* target, void* isLocalPlayerAndRolePlaying)
    {
        // TODO
        log.Debug($"CAST BAR DETOUR");

        // Call original function
        return _castBarHook!.Original(agentHud, numberArray, stringArray, statusManager, target, isLocalPlayerAndRolePlaying);
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