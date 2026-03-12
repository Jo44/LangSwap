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
// Action Detail Hook
// ----------------------------
public unsafe class ActionDetailHook(
    Configuration configuration,
    IGameGui gameGui,
    IGameInteropProvider gameInterop,
    ISigScanner sigScanner,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : BaseHook(configuration, gameGui, gameInterop, sigScanner, translationCache, utilities, log)
{
    // Constant
    private const string Class = "[ActionDetailHook.cs]";

    // Delegate function
    private delegate void* GenerateActionTooltipDelegate(AtkUnitBase* addonActionDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);

    // Hook
    private Hook<GenerateActionTooltipDelegate>? generateActionTooltipHook;

    // ID
    private uint currentActionId = 0;

    // Cache for translated bytes
    private readonly Dictionary<string, byte[]> _translatedBytesCache = [];
    private const int MaxCacheSize = 500;

    // Action Detail Addon
    private readonly string ActionDetailAddonName = configuration.ActionDetailAddonName;
    private readonly int ActionNameField = configuration.ActionNameField;
    private readonly int ActionDescriptionField = configuration.ActionDescriptionField;

    // ----------------------------
    // Enable the hook
    // ----------------------------
    public override void Enable()
    {
        // Prevent multiple enables
        if (isEnabled) return;

        try
        {
            // Hook GenerateActionTooltip (-> modify action tooltip datas when generating it)
            nint generateActionTooltipAddr = sigScanner.ScanText(configuration.GenerateActionTooltipSig);
            if (generateActionTooltipAddr != IntPtr.Zero)
            {
                generateActionTooltipHook = gameInterop.HookFromAddress<GenerateActionTooltipDelegate>(generateActionTooltipAddr, GenerateActionTooltipDetour);
                generateActionTooltipHook.Enable();
                log.Debug($"{Class} - GenerateActionTooltip hook enabled at 0x{generateActionTooltipAddr:X}");
            }
            else
            {
                log.Warning($"{Class} - GenerateActionTooltip signature not found");
            }

            // Set enabled flag
            isEnabled = true;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to enable ActionDetail hook");
        }
    }

    // ----------------------------
    // On language swap
    // ----------------------------
    protected override void OnLanguageSwap()
    {
        // Refresh action detail to apply translations
        _translatedBytesCache.Clear();
        RefreshActionDetail();
    }

    // ----------------------------
    // Refresh the ActionDetail addon
    // ----------------------------
    private void RefreshActionDetail()
    {
        try
        {
            // Get pointer to ActionDetail addon
            AtkUnitBasePtr actionDetailPtr = gameGui.GetAddonByName(ActionDetailAddonName);
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
            log.Error(ex, $"{Class} - Failed to refresh ActionDetail");
        }
    }

    // ----------------------------
    // On Generate Action Tooltip
    // -> Modify action tooltip datas when generating it
    // ----------------------------
    private void* GenerateActionTooltipDetour(AtkUnitBase* addonActionDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
    {
        try
        {
            // TODO : TEST - Log the structure of StringArrayData for debugging
            LogSADStructure(stringArrayData);

            // Get client language
            LanguageEnum clientLang = (LanguageEnum)configuration.ClientLanguage;

            // Get action name from StringArrayData
            string? actionName = utilities.ReadStringFromArray(stringArrayData, ActionNameField);

            // Get action ID 
            currentActionId = translationCache.GetActionIdByName(actionName, clientLang) ?? 0;

            // Modify texts in StringArrayData
            if (isLanguageSwapped && currentActionId > 0 && currentActionId < configuration.MaxValidActionId)
            {
                // Get target language
                LanguageEnum targetLang = (LanguageEnum)configuration.TargetLanguage;

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
            log.Error(ex, $"{Class} - Exception in GenerateActionTooltip for action {currentActionId}");
        }

        return generateActionTooltipHook!.Original(addonActionDetail, numberArrayData, stringArrayData);
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
            if (string.IsNullOrEmpty(text))
                return;

            // Check cache first to avoid unnecessary encoding
            if (!_translatedBytesCache.TryGetValue(text, out byte[]? bytes))
            {
                // Get bytes of text
                bytes = Encoding.UTF8.GetBytes(text + "\0");

                // Check for excessively long text (10KB limit)
                if (bytes.Length > 1024 * 10)
                    return;

                // Add to cache if size limit not exceeded
                if (_translatedBytesCache.Count < MaxCacheSize)
                {
                    _translatedBytesCache[text] = bytes;
                }
                else
                {
                    // Clear cache before if limit exceeded
                    _translatedBytesCache.Clear();
                    _translatedBytesCache[text] = bytes;
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
    // Log the structure of StringArrayData for debugging
    // ----------------------------
    private void LogSADStructure(StringArrayData* stringArrayData)
    {
        if (stringArrayData != null)
        {
            log.Debug("{Class} === StringArrayData Structure ===");
            log.Debug($"{Class} - Total Size: {stringArrayData->AtkArrayData.Size}");

            // Log each field with its content
            for (int i = 0; i < stringArrayData->AtkArrayData.Size; i++)
            {
                // Read the string at this index
                string text = utilities.ReadStringFromArray(stringArrayData, i);

                // Log all non-empty fields
                if (!string.IsNullOrWhiteSpace(text))
                {
                    // Truncate long text for readability
                    string displayText = text.Length > 100 ? text[..100] + "..." : text;

                    // Replace line breaks for compact display
                    displayText = displayText.Replace("\n", " | ");

                    log.Debug($"{Class} - [{i,2}] {displayText}");
                }
            }

            log.Debug("{Class} === End of StringArrayData ===");
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
            // Disable GenerateActionTooltip hook
            generateActionTooltipHook?.Disable();

            // Set disabled flag
            isEnabled = false;
            log.Debug($"{Class} - ActionDetail hook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to disable ActionDetail hook");
        }
    }

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    public override void Dispose()
    {
        try
        {
            // Dispose GenerateActionTooltip hook
            generateActionTooltipHook?.Disable();
            generateActionTooltipHook?.Dispose();
            generateActionTooltipHook = null;

            // Clear cache
            _translatedBytesCache.Clear();

            // Set disabled flag
            isEnabled = false;
            log.Debug($"{Class} - ActionDetail hook disposed");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to dispose ActionDetail hook");
        }

        // Finalize
        GC.SuppressFinalize(this);
    }

}