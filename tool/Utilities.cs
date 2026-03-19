using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.NativeWrapper;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.translation;
using System;
using System.Collections.Generic;

namespace LangSwap.tool;

// ----------------------------
// Utilities
// ----------------------------
public unsafe class Utilities(
    Configuration config,
    IGameGui gameGui,
    IPluginLog log)
{
    // Log
    private const string Class = "[Utilities.cs]";

    // Symbols
    private readonly char GlamouredSymbol = config.GlamouredSymbol;
    private readonly char HighQualitySymbol = config.HighQualitySymbol;
    private readonly char[] TargetIndicatorSymbols = config.TargetIndicatorSymbols;

    // ----------------------------
    // Get addon
    // ----------------------------
    public AtkUnitBase* GetAddon(string addonName, string errorContext)
    {
        // Initialize
        AtkUnitBase* addon = null;
        try
        {
            // Get pointer from name
            AtkUnitBasePtr addonPtr = gameGui.GetAddonByName(addonName);

            // Get addon from pointer
            if (!addonPtr.IsNull) addon = (AtkUnitBase*)addonPtr.Address;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to get {errorContext} addon");
        }
        return addon;
    }
    // ----------------------------
    // Refresh addon
    // ----------------------------
    public void RefreshAddon(AtkUnitBase* addon, string errorContext)
    {
        try
        {
            // Only refresh if the addon is currently visible
            if (addon != null && addon -> IsVisible)
            {
                addon -> Hide(true, false, 0);
                addon -> Show(true, 0);
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to refresh {errorContext} addon");
        }
    }

    // ----------------------------
    // Read string from StringArrayData at specified index
    // ----------------------------
    public string ReadStringFromArrayData(StringArrayData* stringArrayData, int index)
    {
        // Itinialize result
        string result = string.Empty;

        try
        {
            // Check for null pointer and valid index
            if (stringArrayData != null && index < stringArrayData -> AtkArrayData.Size)
            {
                // Get memory address of the string
                nint address = new(stringArrayData -> StringArray[index]);
                if (address != IntPtr.Zero)
                {
                    // Get SeString from memory address
                    SeString seString = MemoryHelper.ReadSeStringNullTerminated(address);
                    if (seString != null)
                    {
                        // Get string value from SeString
                        result = seString.ToString().Trim();
                    }
                    else
                    {
                        // SeString is null
                        throw new Exception("Invalid SeString");
                    }
                }
            }
            else
            {
                // Null pointer or invalid index
                throw new Exception("Null pointer or invalid index");
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to read string at index {index}");
        }

        // Return result
        return result;
    }

    // ----------------------------
    // Write string to StringArrayData at specified index
    // ----------------------------
    public bool WriteStringToArrayData(StringArrayData* stringArrayData, int index, string translatedText)
    {
        // Itinialize result
        bool result = false;

        try
        {
            // Check if translated text is not null or empty
            if (!string.IsNullOrWhiteSpace(translatedText))
            {
                // Check for null pointer and valid index
                if (stringArrayData != null && index < stringArrayData -> AtkArrayData.Size)
                {
                    // Get memory address of the string
                    nint address = new(stringArrayData -> StringArray[index]);
                    if (address != IntPtr.Zero)
                    {
                        // Get SeString from memory address
                        SeString seString = MemoryHelper.ReadSeStringNullTerminated(address);
                        if (seString != null)
                        {
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
                            stringArrayData -> SetValue(index, bytes, false, true, false);

                            // Set result to true
                            result = true;
                        }
                        else
                        {
                            // SeString is null
                            throw new Exception("Invalid SeString");
                        }
                    }
                    else
                    {
                        // Memory address is null
                        throw new Exception("Invalid memory address");
                    }
                }
                else
                {
                    // Null pointer or invalid index
                    throw new Exception("Null pointer or invalid index");
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to write string at index {index}");
        }

        // Return result
        return result;
    }

    // ----------------------------
    // Remove ellipsis from text
    // ----------------------------
    public static string RemoveEllipsis(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) text = text.Replace("...", "").Trim();
        return text;
    }

    // ----------------------------
    // Remove target indicator from text
    // ----------------------------
    public string[] RemoveTargetIndicator(string text)
    {
        string[] result = [string.Empty, string.Empty];
        if (!string.IsNullOrWhiteSpace(text))
        {
            // Initialize result with original text
            result[0] = text;

            // Check if text contains target indicator
            for (int i = 0; i < TargetIndicatorSymbols.Length; i++)
            {
                if (text.Contains(TargetIndicatorSymbols[i]))
                {
                    // Remove target indicator symbol from text and store it in result
                    result[0] = text.Replace(TargetIndicatorSymbols[i].ToString(), "").Trim();
                    result[1] = TargetIndicatorSymbols[i].ToString();
                    break;
                }
            }
        }
        return result;
    }

    // ----------------------------
    // Determine high quality state
    // ----------------------------
    public bool IsHighQuality(string text)
    {
        bool isHighQuality = false;
        if (!string.IsNullOrWhiteSpace(text)) isHighQuality = text.Contains(HighQualitySymbol);
        return isHighQuality;
    }

    // ----------------------------
    // Add high quality symbol
    // ----------------------------
    public string SetHighQuality(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) text = text + " " + HighQualitySymbol;
        return text;
    }

    // ----------------------------
    // Remove high quality symbol
    // ----------------------------
    public string UnsetHighQuality(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) text = text.Replace(HighQualitySymbol.ToString(), "").Trim();
        return text;
    }

    // ----------------------------
    // Add glamour symbol
    // ----------------------------
    public string SetGlamour(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) text = GlamouredSymbol + " " + text;
        return text;
    }

    // ----------------------------
    // Remove glamour symbol
    // ----------------------------
    public string UnsetGlamour(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) text = text.Replace(GlamouredSymbol.ToString(), "").Trim();
        return text;
    }

    // ----------------------------
    // Convert LanguageEnum to ClientLanguage
    // ----------------------------
    public static ClientLanguage EnumToClientLang(LanguageEnum lang)
    {
        // Map LanguageEnum to ClientLanguage
        return lang switch
        {
            LanguageEnum.Japanese => ClientLanguage.Japanese,
            LanguageEnum.English => ClientLanguage.English,
            LanguageEnum.German => ClientLanguage.German,
            LanguageEnum.French => ClientLanguage.French,
            _ => ClientLanguage.English
        };
    }

    // ----------------------------
    // Initialize primary key names and values
    // ----------------------------
    public static void InitKeys(List<String> keyNames, List<int> keyValues)
    {
        // Letters A-Z
        int startA = (int)VirtualKey.A;
        int endZ = (int)VirtualKey.Z;
        for (int v = startA; v <= endZ; v++)
        {
            keyNames.Add(((VirtualKey)v).ToString());
            keyValues.Add(v);
        }

        // Function keys F1-F12
        if (Enum.TryParse<VirtualKey>("F1", out _))
        {
            int startF1 = (int)VirtualKey.F1;
            int endF12 = (int)VirtualKey.F12;
            for (int v = startF1; v <= endF12; v++)
            {
                keyNames.Add(((VirtualKey)v).ToString());
                keyValues.Add(v);
            }
        }
    }

    // ----------------------------
    // Log the structure of StringArrayData (debugging)
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