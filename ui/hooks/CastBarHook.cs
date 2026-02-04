using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.translation;
using System;

namespace LangSwap.ui.hooks;

/// <summary>
/// Hook for translating cast bars (player, enemy, party)
/// </summary>
public unsafe class CastBarHook : BaseHook
{
    private delegate void UpdateCastBarDelegate(IntPtr castBarPtr, uint actionId, IntPtr actionNamePtr);

    private Hook<UpdateCastBarDelegate>? updateCastBarHook;
    private uint currentCastActionId = 0;

    private const string CastBarAddonName = "_CastBar";

    private readonly IGameGui gameGui;

    public CastBarHook(
        Configuration configuration,
        IGameInteropProvider gameInterop,
        ISigScanner sigScanner,
        TranslationCache translationCache,
        IPluginLog log,
        IGameGui gameGui)
        : base(configuration, gameInterop, sigScanner, translationCache, log)
    {
        this.gameGui = gameGui;
    }

    public override void Enable()
    {
        if (isEnabled) return;

        try
        {
            var updateCastBarAddr = sigScanner.ScanText(configuration.CastBarSig);
            if (updateCastBarAddr != IntPtr.Zero)
            {
                updateCastBarHook = gameInterop.HookFromAddress<UpdateCastBarDelegate>(updateCastBarAddr, UpdateCastBarDetour);
                updateCastBarHook.Enable();
                log.Information($"UpdateCastBar hook enabled at 0x{updateCastBarAddr:X}");
            }
            else
            {
                log.Warning("UpdateCastBar signature not found");
            }

            isEnabled = true;
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to enable CastBarHook");
        }
    }

    protected override void OnLanguageSwapped()
    {
        RefreshCastBar();
    }

    protected override void OnLanguageRestored()
    {
        currentCastActionId = 0;
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
                if (castBar != null && castBar->IsVisible && currentCastActionId > 0)
                {
                    log.Debug($"Refreshing CastBar for action {currentCastActionId}");
                    TranslateCastBarText(castBar);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to refresh cast bar");
        }
    }

    private void UpdateCastBarDetour(IntPtr castBarPtr, uint actionId, IntPtr actionNamePtr)
    {
        try
        {
            currentCastActionId = actionId;
            log.Debug($"UpdateCastBar called: actionId={actionId}");

            if (isLanguageSwapped && actionId > 0 && actionId < configuration.MaxValidActionId)
            {
                var targetLang = (LanguageEnum)configuration.TargetLanguage;
                var translatedName = translationCache.GetActionName(actionId, targetLang);
                
                if (!string.IsNullOrWhiteSpace(translatedName))
                {
                    log.Information($"Translating cast bar action {actionId} to: {translatedName}");
                    
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
            log.Error(ex, $"Exception in UpdateCastBar for action {actionId}");
        }

        updateCastBarHook!.Original(castBarPtr, actionId, actionNamePtr);
    }

    private void TranslateCastBarText(AtkUnitBase* castBar)
    {
        try
        {
            if (currentCastActionId == 0 || currentCastActionId > configuration.MaxValidActionId)
                return;

            for (var i = 0; i < castBar->UldManager.NodeListCount; i++)
            {
                var node = castBar->UldManager.NodeList[i];
                if (node == null || (int)node->Type != 3)
                    continue;

                var textNode = (AtkTextNode*)node;
                if (textNode->NodeText.ToString().Length == 0)
                    continue;

                var targetLang = (LanguageEnum)configuration.TargetLanguage;
                var translatedName = translationCache.GetActionName(currentCastActionId, targetLang);
                
                if (!string.IsNullOrWhiteSpace(translatedName))
                {
                    textNode->SetText(translatedName);
                    log.Information($"Updated cast bar text to: {translatedName}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to translate cast bar text");
        }
    }

    public override void Disable()
    {
        if (!isEnabled) return;

        try
        {
            updateCastBarHook?.Disable();
            isEnabled = false;
            log.Information("CastBarHook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to disable CastBarHook");
        }
    }

    public override void Dispose()
    {
        Disable();
        updateCastBarHook?.Dispose();
    }
}