using Dalamud.Game.NativeWrapper;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.ui.hooks.@base;
using System;
using System.Collections.Generic;
using System.Text;

namespace LangSwap.ui.hooks;

// ----------------------------
// Action Tooltip Hook
// ----------------------------
public unsafe class ActionTooltipHook(
    Configuration config,
    IGameGui gameGui,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : BaseHook(config, gameGui, gameInterop, sigScanner, translationCache, utilities, log)
{
    // Log
    private const string Class = "[ActionTooltipHook.cs]";

    // Action Detail Addon
    private readonly string ActionDetailAddon = config.ActionDetailAddon;
    private readonly int ActionNameField = config.ActionNameField;
    private readonly int ActionDescriptionField = config.ActionDescriptionField;

    // Delegate function
    private delegate void* ActionTooltipDelegate(AtkUnitBase* actionDetailAddon, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);

    // Hook
    private Hook<ActionTooltipDelegate>? _actionTooltipHook;

    // TODO : supprimer ça (=> déplacer dans Utilities à terme)

    // ID
    private uint currentActionId = 0;

    // Cache for translated bytes
    private readonly Dictionary<string, byte[]> _bytesCache = [];
    private const int MaxCacheSize = 500;



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
            nint actionTooltipAddr = sigScanner.ScanText(config.ActionTooltipSig);
            if (actionTooltipAddr != IntPtr.Zero)
            {
                // Get hook from address
                _actionTooltipHook = gameInterop.HookFromAddress<ActionTooltipDelegate>(actionTooltipAddr, ActionTooltipDetour);

                // Enable hook
                _actionTooltipHook.Enable();

                // Set enabled flag
                isEnabled = true;

                // Log
                log.Debug($"{Class} - Action tooltip hook enabled at 0x{actionTooltipAddr:X}");
            }
            else
            {
                log.Warning($"{Class} - Action tooltip signature not found");
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to enable action tooltip hook");
        }
    }

    // ----------------------------
    // On language swap
    // ----------------------------
    protected override void OnLanguageSwap()
    {
        // Clear bytes cache
        // TODO
        // _bytesCache.Clear();

        // Refresh action detail addon
        try
        {
            // Get pointer to ActionDetail addon
            AtkUnitBasePtr actionDetailPtr = gameGui.GetAddonByName(ActionDetailAddon);
            if (!actionDetailPtr.IsNull)
            {
                // Get AtkUnitBase from pointer
                AtkUnitBase* actionDetail = (AtkUnitBase*)actionDetailPtr.Address;

                // Only refresh if the addon is currently visible
                if (actionDetail != null && actionDetail -> IsVisible)
                {
                    actionDetail -> Hide(true, false, 0);
                    actionDetail -> Show(true, 0);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to refresh action detail addon");
        }
    }

    // ----------------------------
    // On action tooltip generation
    // ----------------------------
    private void* ActionTooltipDetour(AtkUnitBase* actionDetailAddon, NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
    {
        try
        {
            // Log the structure of StringArrayData for debugging
            // utilities.LogSADStructure(stringArrayData);

            // Get client language
            LanguageEnum clientLang = (LanguageEnum)config.ClientLanguage;

            // Get action name from StringArrayData
            string? actionName = utilities.ReadStringFromArrayData(stringArrayData, ActionNameField);

            // Get action ID 
            currentActionId = translationCache.GetActionIdByName(actionName, clientLang) ?? 0;

            // Modify texts in StringArrayData
            if (isLanguageSwapped && currentActionId > 0 && currentActionId < config.MaxValidActionId)
            {
                // Get target language
                LanguageEnum targetLang = (LanguageEnum)config.TargetLanguage;

                // Translate action name
                string? translatedName = translationCache.GetActionName(currentActionId, targetLang);
                if (!string.IsNullOrWhiteSpace(translatedName))
                {
                    ReplaceText(stringArrayData, ActionNameField, translatedName);
                }

                // Translate action description
                string? translatedDescription = translationCache.GetActionDescription(currentActionId, targetLang);
                if (!string.IsNullOrWhiteSpace(translatedDescription))
                {
                    ReplaceText(stringArrayData, ActionDescriptionField, translatedDescription);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Exception in ActionTooltipDetour");
        }

        return _actionTooltipHook!.Original(actionDetailAddon, numberArrayData, stringArrayData);
    }

    // ----------------------------
    // Replace text in SeString
    // ----------------------------
    private void ReplaceText(StringArrayData* stringArrayData, int field, string newText)
    {
        try
        {
            // Check for null pointer and valid field index
            if (stringArrayData == null || field >= stringArrayData->AtkArrayData.Size)
                return;

            // Get memory address of existing string
            nint address = new(stringArrayData->StringArray[field]);
            if (address == IntPtr.Zero)
            {
                // Set plain text if no existing string
                SafeSetString(stringArrayData, field, newText);
                return;
            }

            // Get existing SeString from memory address
            SeString existingSeString = MemoryHelper.ReadSeStringNullTerminated(address);
            if (existingSeString == null)
            {
                // Set plain text if failed to read existing string
                SafeSetString(stringArrayData, field, newText);
                return;
            }

            // If there's existing formatting, preserve it
            if (existingSeString.Payloads.Count > 1)
            {
                // Build new SeString with translated text while preserving formatting
                // TODO : test - byte[] encoded = BuildSeStringWithTranslation(existingSeString, newText);
                byte[] encoded = [];

                // Set the new encoded SeString value in StringArrayData
                stringArrayData -> SetValue(field, encoded, false, true, false);
            }
            else
            {
                // No complex formatting, set plain text
                SafeSetString(stringArrayData, field, newText);
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to replace text with formatting at field {field}");
            SafeSetString(stringArrayData, field, newText);
        }
    }

    // ----------------------------
    // Safely set a string in StringArrayData
    // ----------------------------
    private void SafeSetString(StringArrayData* stringArrayData, int field, string text)
    {
        try
        {
            // Check for null pointer
            if (stringArrayData == null)
                return;

            // Check field index is within bounds
            if (field >= stringArrayData -> AtkArrayData.Size)
                return;

            // Check for empty or null text
            if (string.IsNullOrWhiteSpace(text))
                return;

            // Check cache first to avoid unnecessary encoding
            if (!_bytesCache.TryGetValue(text, out byte[]? bytes))
            {
                // Get bytes of text
                bytes = Encoding.UTF8.GetBytes(text + "\0");

                // Check for excessively long text (10KB limit)
                if (bytes.Length > 1024 * 10)
                    return;

                // Add to cache if size limit not exceeded
                if (_bytesCache.Count < MaxCacheSize)
                {
                    _bytesCache[text] = bytes;
                }
                else
                {
                    // Clear cache before if limit exceeded
                    _bytesCache.Clear();
                    _bytesCache[text] = bytes;
                }
            }

            // Set the string value in StringArrayData
            stringArrayData->SetValue(field, bytes, false, true, false);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to safely set string at field {field}");
        }
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
            // Disable action tooltip hook
            _actionTooltipHook?.Disable();

            // Set disabled flag
            isEnabled = false;
            log.Debug($"{Class} - Action tooltip hook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to disable action tooltip hook");
        }
    }

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    public override void Dispose()
    {
        try
        {
            // Clear cache
            // TODO
            // _bytesCache.Clear();

            // Dispose action tooltip hook
            _actionTooltipHook?.Disable();
            _actionTooltipHook?.Dispose();
            _actionTooltipHook = null;

            // Set disabled flag
            isEnabled = false;
            log.Debug($"{Class} - Action tooltip hook disposed");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to dispose action tooltip hook");
        }

        // Finalize
        GC.SuppressFinalize(this);
    }

}