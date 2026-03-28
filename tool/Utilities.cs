using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.NativeWrapper;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
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
    IKeyState keyState,
    IPluginLog log)
{
    // Log
    private const string Class = "[Utilities.cs]";

    // Symbols
    private readonly char glamouredSymbol = config.GlamouredSymbol;
    private readonly char highQualitySymbol = config.HighQualitySymbol;
    private readonly char[] targetIndicatorSymbols = config.TargetIndicatorSymbols;

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
    // Check if the configured shortcut is currently pressed
    // ----------------------------
    public bool IsShortcutPressed()
    {
        // Validate key state
        if (keyState is null) return false;

        // Shortcut disabled
        if (!config.ShortcutEnabled) return false;

        // Primary key
        bool primary = config.PrimaryKey < 0 || IsKeyDown(config.PrimaryKey);

        // Modifier keys
        bool ctrl = !config.Ctrl || IsKeyDown((int)VirtualKey.LCONTROL) || IsKeyDown((int)VirtualKey.RCONTROL) || IsKeyDown((int)VirtualKey.CONTROL);
        bool alt = !config.Alt || IsKeyDown((int)VirtualKey.LMENU) || IsKeyDown((int)VirtualKey.RMENU) || IsKeyDown((int)VirtualKey.MENU);
        bool shift = !config.Shift || IsKeyDown((int)VirtualKey.LSHIFT) || IsKeyDown((int)VirtualKey.RSHIFT) || IsKeyDown((int)VirtualKey.SHIFT);

        // Always return false if no keys are configured
        if (config.PrimaryKey == 0 && !config.Ctrl && !config.Alt && !config.Shift) return false;

        // Final evaluation
        return primary && ctrl && alt && shift;
    }

    // ----------------------------
    // Check if a specific virtual key is currently down
    // ----------------------------
    private bool IsKeyDown(int vkCode)
    {
        try
        {
            // Attempt to convert vkCode to VirtualKey enum
            Type underlying = Enum.GetUnderlyingType(typeof(VirtualKey));
            VirtualKey converted;
            try
            {
                converted = (VirtualKey)Convert.ChangeType(vkCode, underlying);
            }
            catch
            {
                int rawFallback = keyState.GetRawValue(vkCode);
                return rawFallback != 0;
            }

            // Check if the converted value is a defined VirtualKey
            if (Enum.IsDefined(converted))
            {
                VirtualKey vk = (VirtualKey)Enum.ToObject(typeof(VirtualKey), converted);
                if (keyState.IsVirtualKeyValid(vk))
                {
                    int raw = keyState.GetRawValue(vk);
                    return raw != 0;
                }
            }

            // Fallback to raw value check
            int rawInt = keyState.GetRawValue(vkCode);
            return rawInt != 0;
        }
        catch (Exception ex)
        {
            // Log exception and return false
            log.Warning($"{Class} - ShortcutDetector.IsKeyDown exception for vk = {vkCode} : {ex.Message}");
            return false;
        }
    }

    // ----------------------------
    // Get alternative translation
    // ----------------------------
    public static string? GetAlternativeTranslation(string spellName, List<AlternativeTranslation> alternativeTranslations)
    {
        // Check for null or empty spell name or translations list
        if (spellName.IsNullOrWhitespace() || alternativeTranslations == null || alternativeTranslations.Count == 0) 
            return null;

        // Find the last alternative translation that matches the spell name
        return alternativeTranslations.FindLast(alternative => alternative.SpellName == spellName)?.AlternativeName;
    }

    // ----------------------------
    // Export CSV
    // ----------------------------
    public static string ExportCSV(List<AlternativeTranslation> translations)
    {
        // Check for null or empty list
        if (translations == null || translations.Count == 0) return string.Empty;

        // Build CSV lines
        List<string> lines = [];
        foreach (AlternativeTranslation translation in translations)
        {
            // Sanitize fields
            string spell = SanitizeCSVField(translation.SpellName);
            string replacement = SanitizeCSVField(translation.AlternativeName);

            // Add line to CSV
            lines.Add($"{spell};{replacement}");
        }

        // Join lines with newline character
        return string.Join(Environment.NewLine, lines);
    }

    // ----------------------------
    // Sanitize CSV field
    // ----------------------------
    private static string SanitizeCSVField(string field)
    {
        // Remove line breaks and trim whitespace to prevent CSV formatting issues
        return (field ?? string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
    }

    // ----------------------------
    // Import CSV
    // ----------------------------
    public static bool ImportCSV(string csv, List<AlternativeTranslation> translations, out string status)
    {
        // Initialize
        status = string.Empty;
        List<AlternativeTranslation> imported = [];

        // Check for null target list
        if (translations == null)
        {
            status = "Target list is null";
            return false;
        }

        // Clear list if CSV is empty
        if (string.IsNullOrWhiteSpace(csv))
        {
            translations.Clear();
            return true;
        }

        // Split CSV into lines and process each line
        string[] lines = csv.Replace("\r", string.Empty).Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            // Trim line and skip if empty
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Validate line format (must contain exactly one ';' separator)
            int separatorIndex = line.IndexOf(';');
            if (separatorIndex <= 0 || separatorIndex >= line.Length - 1)
            {
                // Invalid format
                status = $"Invalid CSV at line {i + 1}";
                return false;
            }

            // Extract spell name and alternative name
            string spellName = line[..separatorIndex].Trim();
            string alternativeName = line[(separatorIndex + 1)..].Trim();

            // Validate extracted values (non-empty and no semicolons)
            if (string.IsNullOrWhiteSpace(spellName) || spellName.Contains(';') || string.IsNullOrWhiteSpace(alternativeName) || alternativeName.Contains(';'))
            {
                status = $"Invalid values at line {i + 1}";
                return false;
            }

            // Add to imported list
            imported.Add(new AlternativeTranslation
            {
                SpellName = spellName,
                AlternativeName = alternativeName
            });
        }

        // Replace original list with imported translations
        translations.Clear();
        translations.AddRange(imported);
        return true;
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
    // Get addon
    // ----------------------------
    public AtkUnitBase* GetAddon(string addonName)
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
            log.Error(ex, $"{Class} - Failed to get {addonName} addon");
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
    // Log the structure of StringArrayData (for debugging)
    // ----------------------------
    public void LogSADStructure(StringArrayData* stringArrayData)
    {
        if (stringArrayData != null)
        {
            log.Debug($"{Class} === StringArrayData Structure ===");
            log.Debug($"{Class} - Total Size: {stringArrayData->AtkArrayData.Size}");

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
                    log.Debug($"{Class} - [{i,2}] {displayText}");
                }
            }

            log.Debug($"{Class} === End of StringArrayData ===");
        }
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
            for (int i = 0; i < targetIndicatorSymbols.Length; i++)
            {
                if (text.Contains(targetIndicatorSymbols[i]))
                {
                    // Remove target indicator symbol from text and store it in result
                    result[0] = text.Replace(targetIndicatorSymbols[i].ToString(), "").Trim();
                    result[1] = targetIndicatorSymbols[i].ToString();
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
        if (!string.IsNullOrWhiteSpace(text)) isHighQuality = text.Contains(highQualitySymbol);
        return isHighQuality;
    }

    // ----------------------------
    // Add high quality symbol
    // ----------------------------
    public string SetHighQuality(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) text = text + " " + highQualitySymbol;
        return text;
    }

    // ----------------------------
    // Remove high quality symbol
    // ----------------------------
    public string UnsetHighQuality(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) text = text.Replace(highQualitySymbol.ToString(), "").Trim();
        return text;
    }

    // ----------------------------
    // Add glamour symbol
    // ----------------------------
    public string SetGlamour(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) text = glamouredSymbol + " " + text;
        return text;
    }

    // ----------------------------
    // Remove glamour symbol
    // ----------------------------
    public string UnsetGlamour(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) text = text.Replace(glamouredSymbol.ToString(), "").Trim();
        return text;
    }

}