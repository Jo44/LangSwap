using System;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;

namespace LangSwap.input;

// Combo detector
public class ComboDetector(IKeyState keyState, Configuration config, IPluginLog log)
{
    // References
    private readonly IKeyState keyState = keyState;
    private readonly Configuration config = config;
    private readonly IPluginLog log = log;

    // Check if the configured combo is currently held down
    public bool IsComboHeld()
    {
        if (keyState is null) return false;

        // Primary key
        bool primaryOk = config.PrimaryKey < 0 || IsKeyDown(config.PrimaryKey);

        // Modifier keys
        bool ctrlOk = !config.UseCtrl || IsKeyDown((int)VirtualKey.LCONTROL) || IsKeyDown((int)VirtualKey.RCONTROL) || IsKeyDown((int)VirtualKey.CONTROL);
        bool altOk = !config.UseAlt || IsKeyDown((int)VirtualKey.LMENU) || IsKeyDown((int)VirtualKey.RMENU) || IsKeyDown((int)VirtualKey.MENU);
        bool shiftOk = !config.UseShift || IsKeyDown((int)VirtualKey.LSHIFT) || IsKeyDown((int)VirtualKey.RSHIFT) || IsKeyDown((int)VirtualKey.SHIFT);

        // If no keys are configured, always return false
        if (config.PrimaryKey == 0 && !config.UseCtrl && !config.UseAlt && !config.UseShift)
            return false;

        return primaryOk && ctrlOk && altOk && shiftOk;
    }

    // Check if a specific virtual key is currently down
    private bool IsKeyDown(int vkCode)
    {
        try
        {
            // Attempt to convert vkCode to VirtualKey enum
            var underlying = Enum.GetUnderlyingType(typeof(VirtualKey));
            object converted;
            try
            {
                converted = Convert.ChangeType(vkCode, underlying);
            }
            catch
            {
                var rawFallback = keyState.GetRawValue(vkCode);
                return rawFallback != 0;
            }

            // Check if the converted value is a defined VirtualKey
            if (Enum.IsDefined(typeof(VirtualKey), converted))
            {
                var vk = (VirtualKey)Enum.ToObject(typeof(VirtualKey), converted);
                if (keyState.IsVirtualKeyValid(vk))
                {
                    var raw = keyState.GetRawValue(vk);
                    return raw != 0;
                }
            }

            // Fallback to raw value check
            var rawInt = keyState.GetRawValue(vkCode);
            return rawInt != 0;
        }
        catch (Exception ex)
        {
            // Log exception and return false
            log?.Warning($"ComboDetector.IsKeyDown exception for vk={vkCode}: {ex.Message}");
            return false;
        }
    }

}