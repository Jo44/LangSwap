using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.NativeWrapper;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.hook.@base;
using LangSwap.translation.@base;
using LangSwap.translation.model;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace LangSwap.tool;

// ----------------------------
// Utilities
// ----------------------------
public unsafe class Utilities(
    IClientState clientState,
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
    // Convert ClientLanguage to Language
    // ----------------------------
    private static Language ClientLangToEnum(ClientLanguage lang)
    {
        // Map ClientLanguage to Language
        return lang switch
        {
            ClientLanguage.Japanese => Language.Japanese,
            ClientLanguage.English  => Language.English,
            ClientLanguage.German   => Language.German,
            ClientLanguage.French   => Language.French,
            _                       => Language.English
        };
    }

    // ----------------------------
    // Convert Language to ClientLanguage
    // ----------------------------
    public static ClientLanguage EnumToClientLang(Language lang)
    {
        // Map Language to ClientLanguage
        return lang switch
        {
            Language.Japanese => ClientLanguage.Japanese,
            Language.English  => ClientLanguage.English,
            Language.German   => ClientLanguage.German,
            Language.French   => ClientLanguage.French,
            _                 => ClientLanguage.English
        };
    }

    // ----------------------------
    // Detect client language
    // ----------------------------
    public void DetectClientLanguage()
    {
        // Get client language
        ClientLanguage lang = clientState.ClientLanguage;

        // Store detected language in configuration
        config.ClientLanguage = ClientLangToEnum(lang);

        // Save configuration
        config.Save();

        // Log unrecognized language
        if ((int)lang < 0 || (int)lang > 3) log.Warning($"{Class} - Unrecognized client language: {lang}, defaulting to English");
    }

    // ----------------------------
    // Initialize primary keys
    // ----------------------------
    public static void InitPrimaryKeys(List<KeyValuePair<string, int>> keys)
    {
        // Letters A-Z
        int startA = (int)VirtualKey.A;
        int endZ = (int)VirtualKey.Z;
        for (int i = startA; i <= endZ; i++)
        {
            // Add key name and value to list
            keys.Add(new KeyValuePair<string, int>(((VirtualKey)i).ToString(), i));
        }

        // Function keys F1-F12
        if (Enum.TryParse<VirtualKey>("F1", out _))
        {
            int startF1 = (int)VirtualKey.F1;
            int endF12 = (int)VirtualKey.F12;
            for (int j = startF1; j <= endF12; j++)
            {
                // Add key name and value to list
                keys.Add(new KeyValuePair<string, int>(((VirtualKey)j).ToString(), j));
            }
        }
    }

    // ----------------------------
    // Check if the shortcut is currently pressed
    // ----------------------------
    public bool IsShortcutPressed()
    {
        // Check for null key state
        if (keyState is null) return false;

        // Check if shortcut is enabled
        if (!config.ShortcutEnabled) return false;

        // Get primary key state
        bool primary = config.PrimaryKey < 0 || IsKeyDown(config.PrimaryKey);

        // Get modifier keys states
        bool ctrl = !config.Ctrl || IsKeyDown((int)VirtualKey.LCONTROL) || IsKeyDown((int)VirtualKey.RCONTROL) || IsKeyDown((int)VirtualKey.CONTROL);
        bool alt = !config.Alt || IsKeyDown((int)VirtualKey.LMENU) || IsKeyDown((int)VirtualKey.RMENU) || IsKeyDown((int)VirtualKey.MENU);
        bool shift = !config.Shift || IsKeyDown((int)VirtualKey.LSHIFT) || IsKeyDown((int)VirtualKey.RSHIFT) || IsKeyDown((int)VirtualKey.SHIFT);

        // Always return false if no keys are configured
        if (config.PrimaryKey == 0 && !config.Ctrl && !config.Alt && !config.Shift) return false;

        // Final evaluation
        return primary && ctrl && alt && shift;
    }

    // ----------------------------
    // Check if a virtual key is currently down
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
            log.Warning($"{Class} - ShortcutDetector.IsKeyDown exception for vk = {vkCode} : {ex.Message}");
            return false;
        }
    }

    // ----------------------------
    // Export obfuscated translations CSV
    // ----------------------------
    public static string ExportObfuscatedTranslationsCSV(List<ObfuscatedTranslation> translations)
    {
        // Check for null or empty list
        if (translations == null || translations.Count == 0) return string.Empty;

        // Build CSV lines
        List<string> lines = [];
        foreach (ObfuscatedTranslation translation in translations)
        {
            // Get fields
            string id = translation.Id.ToString();
            string obfuscatedName = SanitizeCSVField(translation.ObfuscatedName);
            string englishName = SanitizeCSVField(translation.EnglishName);
            string frenchName = SanitizeCSVField(translation.FrenchName);
            string germanName = SanitizeCSVField(translation.GermanName);
            string japaneseName = SanitizeCSVField(translation.JapaneseName);

            // Add line to CSV
            lines.Add($"{id};{obfuscatedName};{englishName};{frenchName};{germanName};{japaneseName}");
        }

        // Join lines
        return string.Join(Environment.NewLine, lines);
    }

    // ----------------------------
    // Import obfuscated translations CSV
    // ----------------------------
    public static bool ImportObfuscatedTranslationsCSV(string csv, List<ObfuscatedTranslation> translations, out string status)
    {
        // Initialize
        status = string.Empty;
        List<ObfuscatedTranslation> imported = [];

        // Check for null target list
        if (translations == null)
        {
            status = "Target list is null";
            return false;
        }

        // Check for empty CSV
        if (string.IsNullOrWhiteSpace(csv))
        {
            status = "CSV is empty - paste CSV data here";
            return false;
        }

        // Split CSV into lines and process each line
        string[] lines = csv.Replace("\r", string.Empty).Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            // Trim line and skip if empty
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Validate line format (must contain exactly 5 ';' separators)
            string[] parts = line.Split(';');
            if (parts.Length != 6)
            {
                status = $"Invalid CSV at line {i + 1}";
                return false;
            }

            // Extract fields
            string idStr = parts[0].Trim();
            string obfuscatedName = parts[1].Trim();
            string englishName = parts[2].Trim();
            string frenchName = parts[3].Trim();
            string germanName = parts[4].Trim();
            string japaneseName = parts[5].Trim();

            // Validate required fields
            if (!int.TryParse(idStr, out int id) || id < 0 || string.IsNullOrWhiteSpace(obfuscatedName))
            {
                status = $"Invalid value at line {i + 1}";
                return false;
            }

            // Add to imported list
            imported.Add(new ObfuscatedTranslation
            {
                Id = id,
                ObfuscatedName = obfuscatedName,
                EnglishName = englishName,
                FrenchName = frenchName,
                GermanName = germanName,
                JapaneseName = japaneseName
            });
        }

        // Replace original list with imported translations
        translations.Clear();
        translations.AddRange(imported);
        return true;
    }

    // ----------------------------
    // Export alternative translations CSV
    // ----------------------------
    public static string ExportAlternativeTranslationsCSV(List<AlternativeTranslation> translations)
    {
        // Check for null or empty list
        if (translations == null || translations.Count == 0) return string.Empty;

        // Build CSV lines
        List<string> lines = [];
        foreach (AlternativeTranslation translation in translations)
        {
            // Get fields
            string spell = SanitizeCSVField(translation.SpellName);
            string replacement = SanitizeCSVField(translation.AlternativeName);

            // Add line to CSV
            lines.Add($"{spell};{replacement}");
        }

        // Join lines
        return string.Join(Environment.NewLine, lines);
    }

    // ----------------------------
    // Import alternative translations CSV
    // ----------------------------
    public static bool ImportAlternativeTranslationsCSV(string csv, List<AlternativeTranslation> translations, out string status)
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

        // Check for empty CSV
        if (string.IsNullOrWhiteSpace(csv))
        {
            status = "CSV is empty - paste CSV data here";
            return false;
        }

        // Split CSV into lines and process each line
        string[] lines = csv.Replace("\r", string.Empty).Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            // Trim line and skip if empty
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Validate line format (must contain exactly one ';' separator)
            string[] parts = line.Split(';');
            if (parts.Length != 2)
            {
                status = $"Invalid CSV at line {i + 1}";
                return false;
            }

            // Extract fields
            string spellName = parts[0].Trim();
            string alternativeName = parts[1].Trim();

            // Validate required fields
            if (string.IsNullOrWhiteSpace(spellName) || string.IsNullOrWhiteSpace(alternativeName))
            {
                status = $"Invalid value at line {i + 1}";
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
    // Sanitize CSV field
    // ----------------------------
    private static string SanitizeCSVField(string field)
    {
        // Remove line breaks and trim whitespace to prevent CSV formatting issues
        return (field ?? string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
    }

    // ----------------------------
    // Get remote obfuscated translations
    // ----------------------------
    public void GetRemoteObfuscatedTranslations()
    {
        try
        {
            // Validate URL
            if (string.IsNullOrWhiteSpace(config.RemoteUrl)) return;

            // Download remote CSV
            using HttpClient httpClient = new();
            string csv = httpClient.GetStringAsync(config.RemoteUrl).GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(csv))
            {
                log.Warning($"{Class} - Remote obfuscated translations CSV is empty");
                return;
            }

            // Import CSV content into a temporary list
            List<ObfuscatedTranslation> importedTranslations = [];
            if (!Utilities.ImportObfuscatedTranslationsCSV(csv, importedTranslations, out string status))
            {
                log.Warning($"{Class} - Failed to import remote obfuscated translations CSV: {status}");
                return;
            }

            // Replace current remote translations and persist
            config.RemoteObfuscatedTranslations.Clear();
            config.RemoteObfuscatedTranslations.AddRange(importedTranslations);
            config.Save();
        }
        catch (Exception ex)
        {
            log.Warning(ex, $"{Class} - Failed to download remote obfuscated translations CSV");
        }
    }

    // ----------------------------
    // Get obfuscated translation
    // ----------------------------
    public string? GetObfuscatedTranslation(uint actionId, Language targetLanguage)
    {
        // Check for valid action ID
        if (actionId <= 0) return null;
        
        // Priority order : remote -> scanned -> local
        List<ObfuscatedTranslation>[] obfuscatedTranslationsSources = 
        [
            config.RemoteObfuscatedTranslations,
            config.ScannedObfuscatedTranslations,
            config.LocalObfuscatedTranslations
        ];

        // Search for translation in each source
        foreach (List<ObfuscatedTranslation> obfuscatedTranslations in obfuscatedTranslationsSources)
        {
            // Find obfuscated translation by action ID
            ObfuscatedTranslation? obfuscatedTranslation = obfuscatedTranslations.FindLast(translation => translation.Id == actionId);
            if (obfuscatedTranslation != null)
            {
                return targetLanguage switch
                {
                    Language.Japanese => obfuscatedTranslation.JapaneseName,
                    Language.English  => obfuscatedTranslation.EnglishName,
                    Language.German   => obfuscatedTranslation.GermanName,
                    Language.French   => obfuscatedTranslation.FrenchName,
                    _                 => obfuscatedTranslation.EnglishName,
                };
            }
        }
        return null;
    }

    // ----------------------------
    // Scan an obfuscated translation discovered
    // ----------------------------
    public void ScanObfuscatedTranslation(uint actionId, string obfuscatedName, string displayName, Language clientLanguage)
    {
        // Skip if action ID, obfuscated name or display name are invalid
        if (actionId < 0 || actionId > config.MaxValidActionId || obfuscatedName.IsNullOrWhitespace() || displayName.IsNullOrWhitespace()) return;

        // Skip if already scanned
        if (config.ScannedObfuscatedTranslations.FindIndex(translation => translation.Id == (int)actionId) >= 0) return;

        // TODO : can already be scanned but missing some language display name, in that case we should update the existing entry instead of creating a new one

        // Create new scanned entry with the client language display name
        ObfuscatedTranslation scanned = new()
        {
            Id = (int)actionId,
            ObfuscatedName = obfuscatedName
        };

        switch (clientLanguage)
        {
            case Language.Japanese: scanned.JapaneseName = displayName; break;
            case Language.English: scanned.EnglishName = displayName; break;
            case Language.German: scanned.GermanName = displayName; break;
            case Language.French: scanned.FrenchName = displayName; break;
        }

        config.ScannedObfuscatedTranslations.Add(scanned);
        config.Save();

        log.Information($"{Class} - Scanned obfuscated translation: ID={actionId}, Obfuscated={obfuscatedName}, {clientLanguage}={displayName}");
    }

    // ----------------------------
    // Get alternative translation
    // ----------------------------
    public static string? GetAlternativeTranslation(string spellName, List<AlternativeTranslation> alternativeTranslations)
    {
        // Check for null, empty spell name or empty translations list
        if (!spellName.IsNullOrWhitespace() && alternativeTranslations != null && alternativeTranslations.Count > 0)
        {
            // Find alternative translation by spell name
            return alternativeTranslations.FindLast(translation => translation.SpellName == spellName)?.AlternativeName ?? string.Empty;
        }
        return null;
    }

    // ----------------------------
    // Log obfuscated translations
    // ----------------------------
    public void LogObfuscatedTranslations(string listName, List<ObfuscatedTranslation> translations)
    {
        // Count translations
        int count = translations?.Count ?? 0;
        log.Information($"{Class} - {listName} ({count})");

        // Check for empty list
        if (translations == null || translations.Count == 0) return;

        // Log each translation
        foreach (ObfuscatedTranslation translation in translations)
        {
            log.Debug($"{Class} - ID = {translation.Id}, Obfuscated = {translation.ObfuscatedName}, English = {translation.EnglishName}, French = {translation.FrenchName}, German = {translation.GermanName}, Japanese = {translation.JapaneseName}");
        }
    }

    // ----------------------------
    // Log alternative translations
    // ----------------------------
    public void LogAlternativeTranslations(string listName, List<AlternativeTranslation> alternativeTranslations)
    {
        // Count translations
        int count = alternativeTranslations?.Count ?? 0;
        log.Information($"{Class} - {listName} ({count})");

        // Check for empty list
        if (alternativeTranslations == null || alternativeTranslations.Count == 0) return;

        // Log each translation
        foreach (AlternativeTranslation translation in alternativeTranslations)
        {
            log.Debug($"{Class} - Spell = {translation.SpellName}, Alternative = {translation.AlternativeName}");
        }
    }

    // ----------------------------
    // Get addon
    // ----------------------------
    public AtkUnitBase* GetAddon(string addonName)
    {
        try
        {
            // Get pointer from name
            AtkUnitBasePtr addonPtr = gameGui.GetAddonByName(addonName);

            // Check for null pointer
            if (addonPtr.IsNull) return null;

            // Return addon from pointer
            return (AtkUnitBase*)addonPtr.Address;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to get {addonName} addon");
        }
        return null;
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
        try
        {
            // Check for null pointer or invalid index
            if (stringArrayData == null || index >= stringArrayData -> AtkArrayData.Size) return string.Empty;
            
            // Get memory address of the string
            nint address = new(stringArrayData -> StringArray[index]);
            if (address == IntPtr.Zero) return string.Empty;

            // Get SeString from memory address
            SeString seString = MemoryHelper.ReadSeStringNullTerminated(address);

            // Return string value from SeString
            if (seString != null) return seString.ToString().Trim();
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to read string at index {index}");
        }
        return string.Empty;
    }

    // ----------------------------
    // Write string to StringArrayData at specified index
    // ----------------------------
    public bool WriteStringToArrayData(StringArrayData* stringArrayData, int index, string translatedText)
    {
        try
        {
            // Check if translated text is null or empty
            if (string.IsNullOrWhiteSpace(translatedText)) return false;
                
            // Check for null pointer or invalid index
            if (stringArrayData == null || index >= stringArrayData -> AtkArrayData.Size) return false;
            
            // Get memory address of the string
            nint address = new(stringArrayData -> StringArray[index]);
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
            stringArrayData -> SetValue(index, bytes, false, true, false);
            return true;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to write string at index {index}");
        }
        return false;
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

    // ----------------------------
    // Remove ellipsis from text
    // ----------------------------
    public static string RemoveEllipsis(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) return text.Replace("...", "").Trim();
        else return text;
    }

    // ----------------------------
    // Remove target indicator from text
    // ----------------------------
    public string[] RemoveTargetIndicator(string text)
    {
        // Check for null or empty text
        if (string.IsNullOrWhiteSpace(text)) return [string.Empty, string.Empty];
        
        // Initialize result with original text
        string[] result = [text, string.Empty];

        // Check if text contains any target indicator
        for (int i = 0; i < targetIndicatorSymbols.Length; i++)
        {
            if (text.Contains(targetIndicatorSymbols[i]))
            {
                // Remove target indicator symbol from text and store it
                result[0] = text.Replace(targetIndicatorSymbols[i].ToString(), "").Trim();
                result[1] = targetIndicatorSymbols[i].ToString();
                break;
            }
        }
        return result;
    }

    // ----------------------------
    // Determine high quality state
    // ----------------------------
    public bool IsHighQuality(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) return text.Contains(highQualitySymbol);
        else return false;
    }

    // ----------------------------
    // Add high quality symbol
    // ----------------------------
    public string SetHighQuality(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) return text + " " + highQualitySymbol;
        else return text;
    }

    // ----------------------------
    // Remove high quality symbol
    // ----------------------------
    public string UnsetHighQuality(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) return text.Replace(highQualitySymbol.ToString(), "").Trim();
        else return text;
    }

    // ----------------------------
    // Add glamour symbol
    // ----------------------------
    public string SetGlamour(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) return glamouredSymbol + " " + text;
        else return text;
    }

    // ----------------------------
    // Remove glamour symbol
    // ----------------------------
    public string UnsetGlamour(string text)
    {
        if (!string.IsNullOrWhiteSpace(text)) return text.Replace(glamouredSymbol.ToString(), "").Trim();
        else return text;
    }

}