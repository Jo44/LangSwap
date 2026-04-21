using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.translation;
using System;

namespace LangSwap.hook.template;

// ----------------------------
// Base class for all tooltips hooks
//
// @author Jo44
// @version 1.7 (21/04/2026)
// @since 01/01/2026
// ----------------------------
public unsafe abstract class TooltipHook(Configuration config, TranslationCache translationCache) : BaseHook(config, translationCache)
{
    // Log
    private readonly string Class = $"[{nameof(TooltipHook)}]";

    // Services
    private static IGameInteropProvider GameInterop => Plugin.GameInterop;
    private static ISigScanner SigScanner => Plugin.SigScanner;

    // Delegate signature for tooltip function
    protected delegate void* TooltipDelegate(AtkUnitBase* actionDetailAddon, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);

    // Hook
    protected Hook<TooltipDelegate>? tooltipHook;

    // Memory signature
    protected abstract string MemorySignature { get; }

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
            nint tooltipAddr = SigScanner.ScanText(MemorySignature);
            if (tooltipAddr != IntPtr.Zero)
            {
                // Get hook from address
                tooltipHook = GameInterop.HookFromAddress<TooltipDelegate>(tooltipAddr, OnTooltipUpdate);

                // Enable hook
                tooltipHook.Enable();

                // Set enabled flag
                isEnabled = true;

                // Log
                Log.Information($"{Class} - {Name} hook enabled at 0x{tooltipAddr:X}");
            }
            else
            {
                Log.Error($"{Class} - {Name} signature not found");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Failed to enable {Name} hook");
        }
    }

    // ----------------------------
    // On tooltip update
    // ----------------------------
    protected abstract void* OnTooltipUpdate(AtkUnitBase* tooltipAddon, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);

    // ----------------------------
    // Read string from StringArrayData at specified index
    // ----------------------------
    protected string ReadStringFromArrayData(StringArrayData* stringArrayData, int index)
    {
        try
        {
            // Check for null pointer or invalid index
            if (stringArrayData == null || index >= stringArrayData->AtkArrayData.Size) return string.Empty;

            // Get memory address of the string
            nint address = new(stringArrayData->StringArray[index]);
            if (address == IntPtr.Zero) return string.Empty;

            // Get SeString from memory address
            SeString seString = MemoryHelper.ReadSeStringNullTerminated(address);

            // Return string value from SeString
            if (seString != null) return seString.ToString().Trim();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Failed to read string at index {index}");
        }
        return string.Empty;
    }

    // ----------------------------
    // Write string to StringArrayData at specified index
    // ----------------------------
    protected bool WriteStringToArrayData(StringArrayData* stringArrayData, int index, string translatedText)
    {
        try
        {
            // Check if translated text is null or empty
            if (string.IsNullOrWhiteSpace(translatedText)) return false;

            // Check for null pointer or invalid index
            if (stringArrayData == null || index >= stringArrayData->AtkArrayData.Size) return false;

            // Get memory address of the string
            nint address = new(stringArrayData->StringArray[index]);
            if (address == IntPtr.Zero) return false;

            // Get SeString from memory address
            SeString seString = MemoryHelper.ReadSeStringNullTerminated(address);
            if (seString == null) return false;

            // Prepare a new SeString builder
            SeStringBuilder builder = new();
            bool textReplaced = false;

            // Iterate through the payloads of the original SeString
            foreach (Payload payload in seString.Payloads)
            {
                if (!textReplaced && payload is TextPayload textPayload)
                {
                    // Replace first TextPayload with translated text
                    builder.AddText(translatedText);

                    // Flag to indicate text has been replaced
                    textReplaced = true;
                }
                else
                {
                    // Clean other payloads of any text
                    if (payload is TextPayload otherTextPayload)
                    {
                        otherTextPayload.Text = "";
                    }
                    // Keep them in same order
                    builder.Add(payload);
                }
            }

            // Encode the modified SeString
            byte[] bytes = builder.Build().Encode();

            // Write the new bytes into StringArrayData at the specified index
            stringArrayData->SetValue(index, bytes, false, true, false);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Failed to write string at index {index}");
        }
        return false;
    }

    // ----------------------------
    // Log the structure of StringArrayData (debugging)
    // ----------------------------
    protected void LogSADStructure(StringArrayData* stringArrayData)
    {
        if (stringArrayData != null)
        {
            Log.Debug($"{Class} === StringArrayData Structure ===");
            Log.Debug($"{Class} - Total Size: {stringArrayData->AtkArrayData.Size}");

            // Log each field with its content
            for (int i = 0; i < stringArrayData->AtkArrayData.Size; i++)
            {
                // Read the string at this index
                string text = ReadStringFromArrayData(stringArrayData, i);

                // Log all non-empty fields
                if (!string.IsNullOrWhiteSpace(text))
                {
                    // Truncate long text for readability
                    string displayText = text.Length > 100 ? text[..100] + "..." : text;

                    // Replace line breaks for compact display
                    displayText = displayText.Replace("\n", " | ");

                    // Log
                    Log.Debug($"{Class} - [{i,2}] {displayText}");
                }
            }

            Log.Debug($"{Class} === End of StringArrayData ===");
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
            // Disable tooltip hook
            tooltipHook?.Disable();

            // Set disabled flag
            isEnabled = false;
            Log.Information($"{Class} - {Name} hook disabled");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Failed to disable {Name} hook");
        }
    }

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    public override void Dispose()
    {
        try
        {
            // Dispose tooltip hook
            tooltipHook?.Disable();
            tooltipHook?.Dispose();
            tooltipHook = null;

            // Set disabled flag
            isEnabled = false;

            // Finalize
            GC.SuppressFinalize(this);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Failed to dispose {Name} hook");
        }
    }

}