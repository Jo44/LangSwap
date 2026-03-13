using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

namespace LangSwap.tool;

// ----------------------------
// Utilities
// ----------------------------
public unsafe class Utilities(
    Configuration config,
    IPluginLog log)
{
    // Log
    private const string Class = "[Utilities.cs]";

    // Symbols
    private readonly char GlamouredSymbol = config.GlamouredSymbol;
    private readonly char HighQualitySymbol = config.HighQualitySymbol;

    // ----------------------------
    // Read string from StringArrayData at specified index
    // ----------------------------
    public string ReadStringFromArrayData(StringArrayData* stringArrayData, int index)
    {
        try
        {
            // Check for null pointer and valid index
            if (stringArrayData == null || index >= stringArrayData -> AtkArrayData.Size) return string.Empty;

            // Get memory address of the string
            nint address = new(stringArrayData -> StringArray[index]);
            if (address == IntPtr.Zero) return string.Empty;

            // Read SeString from memory
            SeString seString = MemoryHelper.ReadSeStringNullTerminated(address);

            // Convert to string
            return seString?.ToString().Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to read string at index {index}");
            return string.Empty;
        }
    }

    // ----------------------------
    // Write string to StringArrayData at specified index
    // ----------------------------
    public void WriteStringToArrayData(StringArrayData* stringArrayData, int index, string newValue)
    {
        // TODO
    }

    // ----------------------------
    // Determine high quality state
    // ----------------------------
    public bool IsHighQuality(string text)
    {
        bool isHighQuality = false;
        if (!string.IsNullOrEmpty(text)) isHighQuality = text.Contains(HighQualitySymbol);
        return isHighQuality;
    }

    // ----------------------------
    // Add high quality symbol
    // ----------------------------
    public string SetHighQuality(string text)
    {
        if (!string.IsNullOrEmpty(text)) text = text + " " + HighQualitySymbol;
        return text;
    }

    // ----------------------------
    // Remove high quality symbol
    // ----------------------------
    public string UnsetHighQuality(string text)
    {
        if (!string.IsNullOrEmpty(text)) text = text.Replace(HighQualitySymbol.ToString(), "").Trim();
        return text;
    }

    // ----------------------------
    // Add glamour symbol
    // ----------------------------
    public string SetGlamour(string text)
    {
        if (!string.IsNullOrEmpty(text)) text = GlamouredSymbol + " " + text;
        return text;
    }

    // ----------------------------
    // Remove glamour symbol
    // ----------------------------
    public string UnsetGlamour(string text)
    {
        if (!string.IsNullOrEmpty(text)) text = text.Replace(GlamouredSymbol.ToString(), "").Trim();
        return text;
    }

    // ----------------------------
    // Log the structure of StringArrayData for debugging
    // ----------------------------
    public void LogSADStructure(StringArrayData* stringArrayData)
    {
        if (stringArrayData != null)
        {
            log.Debug($"{Class} === StringArrayData Structure ===");
            log.Debug($"{Class} - Total Size: {stringArrayData -> AtkArrayData.Size}");

            // Log each field with its content
            for (int i = 0; i < stringArrayData -> AtkArrayData.Size; i++)
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
                    log.Debug($"{Class} - [{i,2}] {displayText}");
                }
            }

            log.Debug($"{Class} === End of StringArrayData ===");
        }
    }

}