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
    IPluginLog log)
{
    // Constant
    private const string Class = "[Utilities.cs]";

    // ----------------------------
    // Read string from StringArrayData at specified index
    // ----------------------------
    public string ReadStringFromArray(StringArrayData* stringArrayData, int index)
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
            return seString?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to read string at index {index}");
            return string.Empty;
        }
    }

}