using Dalamud.IoC;
using Dalamud.Plugin.Services;
using LangSwap.translation.model;
using System;
using System.Collections.Generic;

namespace LangSwap.tool;

// ----------------------------
// Utilities
// ----------------------------
public class Utilities()
{
    // Log
    private const string Class = "[Utilities.cs]";

    // Service
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

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
    // Log obfuscated translations
    // ----------------------------
    public static void LogObfuscatedTranslations(string listName, List<ObfuscatedTranslation> translations)
    {
        // Count translations
        int count = translations?.Count ?? 0;
        Log.Information($"{Class} - {listName} ({count})");

        // Check for empty list
        if (translations == null || translations.Count == 0) return;

        // Log each translation
        foreach (ObfuscatedTranslation translation in translations)
        {
            Log.Debug($"{Class} - ID = {translation.Id}, Obfuscated = {translation.ObfuscatedName}, English = {translation.EnglishName}, French = {translation.FrenchName}, German = {translation.GermanName}, Japanese = {translation.JapaneseName}");
        }
    }

    // ----------------------------
    // Log alternative translations
    // ----------------------------
    public static void LogAlternativeTranslations(string listName, List<AlternativeTranslation> alternativeTranslations)
    {
        // Count translations
        int count = alternativeTranslations?.Count ?? 0;
        Log.Information($"{Class} - {listName} ({count})");

        // Check for empty list
        if (alternativeTranslations == null || alternativeTranslations.Count == 0) return;

        // Log each translation
        foreach (AlternativeTranslation translation in alternativeTranslations)
        {
            Log.Debug($"{Class} - Spell = {translation.SpellName}, Alternative = {translation.AlternativeName}");
        }
    }

}