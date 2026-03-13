using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.ui.hooks.@base;
using System;

namespace LangSwap.ui.hooks;

/// <summary>
/// Hook for translating cast bars (player, enemy, party)
/// </summary>
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

    private delegate void UpdateCastBarDelegate(IntPtr castBarPtr, uint actionId, IntPtr actionNamePtr);

    private Hook<UpdateCastBarDelegate>? updateCastBarHook;
    private uint currentCastActionId = 0;

    private const string CastBarAddonName = "_CastBar";

    public override void Enable()
    {
        if (isEnabled) return;

        try
        {
            var updateCastBarAddr = sigScanner.ScanText(config.CastBarSig);
            if (updateCastBarAddr != IntPtr.Zero)
            {
                updateCastBarHook = gameInterop.HookFromAddress<UpdateCastBarDelegate>(updateCastBarAddr, UpdateCastBarDetour);
                updateCastBarHook.Enable();
                log.Information($"{Class} - UpdateCastBar hook enabled at 0x{updateCastBarAddr:X}");
            }
            else
            {
                log.Warning($"{Class} - UpdateCastBar signature not found");
            }

            isEnabled = true;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to enable CastBarHook");
        }
    }

    protected override void OnLanguageSwap()
    {
        RefreshCastBar();
    }

    private void RefreshCastBar()
    {
        try
        {
            var castBarPtr = gameGui.GetAddonByName(CastBarAddonName);
            if (!castBarPtr.IsNull)
            {
                var castBar = (AtkUnitBase*)castBarPtr.Address;
                if (castBar != null && castBar -> IsVisible && currentCastActionId > 0)
                {
                    log.Debug($"{Class} - Refreshing CastBar for action {currentCastActionId}");
                    TranslateCastBarText(castBar);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to refresh cast bar");
        }
    }

    private void UpdateCastBarDetour(IntPtr castBarPtr, uint actionId, IntPtr actionNamePtr)
    {
        try
        {
            currentCastActionId = actionId;
            log.Debug($"{Class} - UpdateCastBar called: actionId={actionId}");

            if (isLanguageSwapped && actionId > 0 && actionId < config.MaxValidActionId)
            {
                var targetLang = (LanguageEnum)config.TargetLanguage;
                var translatedName = translationCache.GetActionName(actionId, targetLang);
                
                if (!string.IsNullOrWhiteSpace(translatedName))
                {
                    log.Information($"{Class} - Translating cast bar action {actionId} to: {translatedName}");
                    
                    var translatedBytes = System.Text.Encoding.UTF8.GetBytes(translatedName + "\0");
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

        updateCastBarHook!.Original(castBarPtr, actionId, actionNamePtr);
    }

    private void TranslateCastBarText(AtkUnitBase* castBar)
    {
        try
        {
            if (currentCastActionId == 0 || currentCastActionId > config.MaxValidActionId)
                return;

            for (var i = 0; i < castBar -> UldManager.NodeListCount; i++)
            {
                var node = castBar -> UldManager.NodeList[i];
                if (node == null || (int)node -> Type != 3)
                    continue;

                var textNode = (AtkTextNode*)node;
                if (textNode -> NodeText.ToString().Length == 0)
                    continue;

                var targetLang = (LanguageEnum)config.TargetLanguage;
                var translatedName = translationCache.GetActionName(currentCastActionId, targetLang);
                
                if (!string.IsNullOrWhiteSpace(translatedName))
                {
                    textNode -> SetText(translatedName);
                    log.Information($"{Class} - Updated cast bar text to: {translatedName}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to translate cast bar text");
        }
    }

    public override void Disable()
    {
        if (!isEnabled) return;

        try
        {
            updateCastBarHook?.Disable();
            isEnabled = false;
            log.Information($"{Class} - CastBarHook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to disable CastBarHook");
        }
    }

    public override void Dispose()
    {
        Disable();
        updateCastBarHook?.Dispose();
        GC.SuppressFinalize(this);
    }

}