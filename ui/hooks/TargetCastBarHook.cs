using Dalamud.Game.NativeWrapper;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.ui.hooks.@base;
using System;

namespace LangSwap.ui.hooks;

// ----------------------------
// Target CastBar Hook
// ----------------------------
public unsafe class TargetCastBarHook(
    Configuration config,
    IGameGui gameGui,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : BaseHook(config, gameGui, gameInterop, sigScanner, translationCache, utilities, log)
{
    // Log
    private const string Class = "[TargetCastBarHook.cs]";

    // Delegate function
    private delegate void* TargetCastBarDelegate(void* agentHud, void* numberArray, void* stringArray, StatusManager* statusManager, GameObject* target, void* isLocalPlayerAndRollPlaying);

    // Hook
    private Hook<TargetCastBarDelegate>? _targetCastBarHook;

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
            nint castBarAddr = sigScanner.ScanText(config.TargetCastBarSig);
            if (castBarAddr != IntPtr.Zero)
            {
                // Get hook from address
                _targetCastBarHook = gameInterop.HookFromAddress<TargetCastBarDelegate>(castBarAddr, OnTargetCastBarUpdate);

                // Enable hook
                _targetCastBarHook.Enable();

                // Set enabled flag
                isEnabled = true;

                // Log
                log.Debug($"{Class} - Target castbar hook enabled at 0x{castBarAddr:X}");
            }
            else
            {
                log.Warning($"{Class} - Target castbar signature not found");
            }

            isEnabled = true;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to enable target castbar hook");
        }
    }

    // ----------------------------
    // On language swap
    // ----------------------------
    protected override void OnLanguageSwap()
    {
        // Refresh target & focus castbar addons
        try
        {
            // Get pointer to target castbar addon
            AtkUnitBasePtr targetCastBarPtr = gameGui.GetAddonByName(config.TargetCastBarAddon);
            if (!targetCastBarPtr.IsNull)
            {
                // Get AtkUnitBase from pointer
                AtkUnitBase* targetCastBar = (AtkUnitBase*)targetCastBarPtr.Address;

                // Only refresh if the addon is currently visible
                if (targetCastBar != null && targetCastBar -> IsVisible)
                {
                    targetCastBar -> Hide(true, false, 0);
                    targetCastBar -> Show(true, 0);
                }
            }

            // Get pointer to focus castbar addon
            AtkUnitBasePtr focusCastBarPtr = gameGui.GetAddonByName(config.FocusCastBarAddon);
            if (!focusCastBarPtr.IsNull)
            {
                // Get AtkUnitBase from pointer
                AtkUnitBase* focusCastBar = (AtkUnitBase*)focusCastBarPtr.Address;

                // Only refresh if the addon is currently visible
                if (focusCastBar != null && focusCastBar -> IsVisible)
                {
                    focusCastBar -> Hide(true, false, 0);
                    focusCastBar -> Show(true, 0);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to refresh castbar addons");
        }
    }

    // ----------------------------
    // On target castbar update
    // ----------------------------
    private void* OnTargetCastBarUpdate(void* agentHud, void* numberArray, void* stringArray, StatusManager* statusManager, GameObject* target, void* isLocalPlayerAndRolePlaying)
    {
        log.Debug($"ON TARGET CASTBAR UPDATE");
        // TODO : récupérer l'action ID (ou nom pour reverse lookup)

        // Call original function
        return _targetCastBarHook!.Original(agentHud, numberArray, stringArray, statusManager, target, isLocalPlayerAndRolePlaying);
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
            // Disable target castbar hook
            _targetCastBarHook?.Disable();

            // Set disabled flag
            isEnabled = false;
            log.Debug($"{Class} - Target castbar hook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to disable target castbar hook");
        }
    }

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    public override void Dispose()
    {
        try
        {
            // Dispose target castbar hook
            _targetCastBarHook?.Disable();
            _targetCastBarHook?.Dispose();
            _targetCastBarHook = null;

            // Set disabled flag
            isEnabled = false;
            log.Debug($"{Class} - Target castbar hook disposed");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to dispose target castbar hook");
        }

        // Finalize
        GC.SuppressFinalize(this);
    }

}