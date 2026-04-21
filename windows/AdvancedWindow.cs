using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LangSwap.translation;
using LangSwap.translation.@base;
using LangSwap.translation.model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace LangSwap.windows;

// ----------------------------
// Advanced Window
//
// @author Jo44
// @version 1.7 (21/04/2026)
// @since 01/01/2026
// ----------------------------
public class AdvancedWindow : Window, IDisposable
{
    // Log
    private readonly string Class = $"[{nameof(AdvancedWindow)}]";

    // Service
    private static IPluginLog Log => Plugin.Log;

    // Core components
    private readonly Configuration config;
    private readonly ExcelProvider excelProvider;

    // Obfuscated translations
    private readonly List<ObfuscatedTranslation> translations = [];

    // Compare info for language sorting
    // TODO : clean
    private static readonly CompareInfo JapaneseCompare = CultureInfo.GetCultureInfo("ja-JP").CompareInfo;
    private static readonly CompareInfo EnglishCompare = CultureInfo.GetCultureInfo("en-US").CompareInfo;
    private static readonly CompareInfo GermanCompare = CultureInfo.GetCultureInfo("de-DE").CompareInfo;
    private static readonly CompareInfo FrenchCompare = CultureInfo.GetCultureInfo("fr-FR").CompareInfo;

    // CSV export/import
    private string exportCSV = string.Empty;
    private string importCSV = string.Empty;
    private string importStatus = string.Empty;

    // Message
    private string message = string.Empty;

    // Window size
    private const int WindowHeight = 805;
    private const int WindowWidth = 900;

    // ----------------------------
    // Constructor
    // ----------------------------
    public AdvancedWindow(Configuration config, ExcelProvider excelProvider) : base("LangSwap - Advanced###LangSwapAdvanced")
    {
        // Initialize core components
        this.config = config;
        this.excelProvider = excelProvider;

        // Window settings
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(WindowWidth, WindowHeight),
            MaximumSize = new Vector2(WindowWidth, WindowHeight)
        };
    }

    // ----------------------------
    // On open
    // ----------------------------
    public override void OnOpen()
    {
        // Load obfuscated translations
        int count = LoadObfuscatedTranslations();

        // Set message
        message = $"{count} obfuscated translations loaded";
    }

    // ----------------------------
    // Draw
    // ----------------------------
    public override void Draw()
    {
        // Information
        DrawInformation();

        // Message
        DrawMessage();

        // Obfuscated translations table
        DrawObfuscatedTranslationsTable();

        // Export scanned
        DrawExportScannedButton();
        DrawExportScannedPopup();

        // Reset scanned
        DrawResetScannedButton();
        DrawResetScannedPopup();

        // Import local
        DrawImportLocalButton();
        DrawImportLocalPopup();

        // Reset local
        DrawResetLocalButton();
        DrawResetLocalPopup();
    }

    // ----------------------------
    // Draw information
    // ----------------------------
    private static void DrawInformation()
    {
        // Draw information
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGui.TextWrapped("Some spell names are obfuscated by Square Enix to prevent early data-mining");
    }

    // ----------------------------
    // Draw message
    // ----------------------------
    private void DrawMessage()
    {
        // Check if there is a message to display
        if (string.IsNullOrWhiteSpace(message)) return;

        // Calculate text position
        Vector2 textSize = ImGui.CalcTextSize(message);
        float textX = ImGui.GetWindowContentRegionMax().X - textSize.X - 15f;

        // Draw message
        ImGui.SameLine();
        ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), textX));
        ImGui.TextUnformatted(message);
    }

    // ----------------------------
    // Draw obfuscated translations table
    // ----------------------------
    private void DrawObfuscatedTranslationsTable()
    {
        // Draw obfuscated translations table
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable;
        if (!ImGui.BeginTable("##ObfuscatedTranslationsTable", 4, tableFlags, new Vector2(WindowWidth - 45, WindowHeight - 104))) return;

        // Setup columns
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Spell ID", ImGuiTableColumnFlags.WidthFixed, 65f, 0);
        ImGui.TableSetupColumn("Obfuscated Name", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultSort, 320f, 1);
        ImGui.TableSetupColumn("Language", ImGuiTableColumnFlags.WidthFixed, 85f, 2);
        ImGui.TableSetupColumn("Spell Name", ImGuiTableColumnFlags.WidthStretch, 1.0f, 3);
        ImGui.TableHeadersRow();

        // Apply sorting
        ApplyTableSorting();

        // No entries
        if (translations.Count == 0)
        {
            ImGui.TableNextRow(ImGuiTableRowFlags.None, 30f);
            ImGui.TableSetColumnIndex(0);
            DrawTextCell(" ");
            ImGui.TableSetColumnIndex(1);
            DrawTextCell("No obfuscated names found");
        }
        else
        {
            // Draw rows for each obfuscated translation
            for (int i = 0; i < translations.Count; i++)
            {
                // Get obfuscated translation
                ObfuscatedTranslation translation = translations[i];

                // Action ID
                ImGui.TableNextRow(ImGuiTableRowFlags.None, 30f);
                ImGui.TableSetColumnIndex(0);
                DrawTextCell(translation.ActionID.ToString());

                // Obfuscated name
                ImGui.TableSetColumnIndex(1);
                DrawTextCell(translation.ObfuscatedName);

                // Language
                ImGui.TableSetColumnIndex(2);
                DrawTextCell(((Language)translation.LanguageID).ToString());

                // Deobfuscated name
                ImGui.TableSetColumnIndex(3);
                DrawTextCell(translation.DeobfuscatedName);
            }
        }
        ImGui.EndTable();
        ImGui.Spacing();
        ImGui.Spacing();
    }

    // ----------------------------
    // Draw text cell
    // ----------------------------
    private static void DrawTextCell(string text)
    {
        // Calculate text position
        Vector2 pos = ImGui.GetCursorScreenPos();
        float width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        Vector2 textSize = ImGui.CalcTextSize(text);
        Vector2 textPos = new(pos.X + (width - textSize.X) * 0.5f, pos.Y + (30f - textSize.Y) * 0.5f);

        // Draw text
        ImGui.GetWindowDrawList().AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), text);
        ImGui.Dummy(new Vector2(width, 30f));
    }

    // ----------------------------
    // Draw export scanned button
    // ----------------------------
    private void DrawExportScannedButton()
    {
        // Draw export scanned button
        ImGui.Spacing();
        ImGui.SameLine(0, 225f);
        if (ImGui.Button("Export scanned", new Vector2(150f, 0f)))
        {
            // Export scanned obfuscated translations to CSV
            exportCSV = ExportObfuscatedTranslationsCSV(config.ScannedObfuscatedTranslations);

            // Open popup
            ImGui.OpenPopup("Export scanned");
        }
    }

    // ----------------------------
    // Draw export scanned popup
    // ----------------------------
    private void DrawExportScannedPopup()
    {
        // Draw export CSV popup
        PopupBuilder.DrawExportCSVPopup("Export scanned", "##ExportScannedCsv", ref exportCSV, new Vector2(900f, 400f), () =>
        {
            // Count exported translations
            int count = config.ScannedObfuscatedTranslations.Count;

            // Log
            message = $"{count} scanned obfuscated translations exported";
            Log.Information($"{Class} - {message}");
        });
    }

    // ----------------------------
    // Draw reset scanned button
    // ----------------------------
    private static void DrawResetScannedButton()
    {
        // Draw reset scanned button
        ImGui.SameLine(0, 15f);
        if (ImGui.Button("Reset scanned", new Vector2(150f, 0f)))
        {
            // Open popup
            ImGui.OpenPopup("Reset scanned");
        }
    }

    // ----------------------------
    // Draw reset scanned popup
    // ----------------------------
    private void DrawResetScannedPopup()
    {
        // Draw confirmation popup
        PopupBuilder.DrawConfirmationPopup("Reset scanned", "This will clear all scanned obfuscated translations.    Are you sure ?", "Yes, reset all", "Cancel", new Vector2(490f, 0f), () =>
        {
            // Count removed translations
            int count = config.ScannedObfuscatedTranslations.Count;

            // Clear scanned obfuscated translations
            config.ScannedObfuscatedTranslations.Clear();

            // Save configuration
            config.Save();

            // Log
            message = $"{count} scanned obfuscated translations cleared";
            Log.Information($"{Class} - {message}");

            // Reload obfuscated translations
            LoadObfuscatedTranslations();
        });
    }

    // ----------------------------
    // Draw import local button
    // ----------------------------
    private void DrawImportLocalButton()
    {
        // Draw import local button
        ImGui.SameLine(0, 15f);
        if (ImGui.Button("Import local", new Vector2(150f, 0f)))
        {
            // Reset import local CSV and status
            importCSV = string.Empty;
            importStatus = string.Empty;

            // Open popup
            ImGui.OpenPopup("Import local");
        }
    }

    // ----------------------------
    // Draw import local popup
    // ----------------------------
    private void DrawImportLocalPopup()
    {
        // Draw import CSV popup
        PopupBuilder.DrawImportCSVPopup("Import local", "##ImportLocalCsv", ref importCSV, ref importStatus, new Vector2(900f, 400f), csv =>
        {
            // Try to import obfuscated translations from CSV
            List<ObfuscatedTranslation> importedTranslations = [];
            if (ImportObfuscatedTranslationsCSV(csv, importedTranslations, out string status))
            {
                // Replace current local translations and persist
                config.LocalObfuscatedTranslations.Clear();
                config.LocalObfuscatedTranslations.AddRange(importedTranslations);
                config.Save();

                // Count imported translations
                int count = config.LocalObfuscatedTranslations.Count;

                // Log
                message = $"{count} local obfuscated translations imported";
                Log.Information($"{Class} - {message}");

                // Reset status
                return string.Empty;
            }
            // Return status
            return status;
        });
    }

    // ----------------------------
    // Draw reset local button
    // ----------------------------
    private static void DrawResetLocalButton()
    {
        // Draw reset local button
        ImGui.SameLine(0, 15f);
        if (ImGui.Button("Reset local", new Vector2(150f, 0f)))
        {
            // Open popup
            ImGui.OpenPopup("Reset local");
        }
    }

    // ----------------------------
    // Draw reset local popup
    // ----------------------------
    private void DrawResetLocalPopup()
    {
        // Draw confirmation popup
        PopupBuilder.DrawConfirmationPopup("Reset local", "This will clear all local obfuscated translations.    Are you sure ?", "Yes, reset all", "Cancel", new Vector2(470f, 0f), () =>
        {
            // Count removed translations
            int count = config.LocalObfuscatedTranslations.Count;

            // Clear local obfuscated translations
            config.LocalObfuscatedTranslations.Clear();

            // Save configuration
            config.Save();

            // Log
            message = $"{count} local obfuscated translations cleared";
            Log.Information($"{Class} - {message}");

            // Reload obfuscated translations
            LoadObfuscatedTranslations();
        });
    }

    // ----------------------------
    // Load obfuscated translations
    // ----------------------------
    private int LoadObfuscatedTranslations()
    {
        // Clear translations list
        translations.Clear();

        // Get all obfuscated actions from Excel provider
        foreach (ObfuscatedTranslation obfuscatedTranslation in excelProvider.GetAllObfuscatedActions())
        {
            translations.Add(new ObfuscatedTranslation
            {
                ActionID = obfuscatedTranslation.ActionID,
                LanguageID = obfuscatedTranslation.LanguageID,
                ObfuscatedName = obfuscatedTranslation.ObfuscatedName
            });
        }

        // Merge obfuscated translations lists
        MergeObfuscatedTranslations(config.RemoteObfuscatedTranslations);
        MergeObfuscatedTranslations(config.ScannedObfuscatedTranslations);
        MergeObfuscatedTranslations(config.LocalObfuscatedTranslations);

        // Sort translations by obfuscated name
        translations.Sort((a, b) => EnglishCompare.Compare(a.ObfuscatedName, b.ObfuscatedName, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase));

        // Log
        Log.Information($"{Class} - Loaded {translations.Count} obfuscated translations");

        // Return count
        return translations.Count;
    }

    // ----------------------------
    // Merge obfuscated translations
    // ----------------------------
    private void MergeObfuscatedTranslations(List<ObfuscatedTranslation> sourceTranslations)
    {
        // Check if there are obfuscated translations to merge
        if (sourceTranslations == null || sourceTranslations.Count == 0) return;

        // For each source translation
        foreach (ObfuscatedTranslation sourceTranslation in sourceTranslations)
        {
            // Find matching translation in the list
            ObfuscatedTranslation targetTranslation = translations.Find(translation => string.Equals(translation.ObfuscatedName, sourceTranslation.ObfuscatedName, StringComparison.Ordinal))!;
            
            // Add if missing
            if (targetTranslation == null)
            {
                translations.Add(new ObfuscatedTranslation
                {
                    ActionID = sourceTranslation.ActionID,
                    ObfuscatedName = sourceTranslation.ObfuscatedName,
                    LanguageID = sourceTranslation.LanguageID,
                    DeobfuscatedName = sourceTranslation.DeobfuscatedName
                });
                continue;
            }

            // Merge obfuscated translation
            targetTranslation.ActionID = sourceTranslation.ActionID;
            targetTranslation.ObfuscatedName = sourceTranslation.ObfuscatedName;
            targetTranslation.LanguageID = sourceTranslation.LanguageID;
            targetTranslation.DeobfuscatedName = sourceTranslation.DeobfuscatedName;
        }
    }

    // ----------------------------
    // Apply table sorting
    // ----------------------------
    private void ApplyTableSorting()
    {
        // Get current sort specs
        ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
        if (!sortSpecs.SpecsDirty || sortSpecs.SpecsCount == 0) return;

        // Get sort specs for the first sorted column
        ImGuiTableColumnSortSpecs spec = sortSpecs.Specs[0];
        int columnID = (int)spec.ColumnUserID;
        bool ascending = spec.SortDirection != ImGuiSortDirection.Descending;

        // Sort translations based on the column and direction
        translations.Sort((left, right) =>
        {
            int primaryCompare = columnID switch
            {
                // Column 0: Sort by Action ID, then by Obfuscated Name
                0 => CompareWithSecondary(
                    left.ActionID.CompareTo(right.ActionID),
                    ascending,
                    () => EnglishCompare.Compare(left.ObfuscatedName, right.ObfuscatedName, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase)
                ),

                // Column 1: Sort by Obfuscated Name only
                1 => ascending
                    ? EnglishCompare.Compare(left.ObfuscatedName, right.ObfuscatedName, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase)
                    : EnglishCompare.Compare(right.ObfuscatedName, left.ObfuscatedName, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase),

                // Column 2: Sort by Language, then by Obfuscated Name
                2 => CompareWithSecondary(
                    left.LanguageID.CompareTo(right.LanguageID),
                    ascending,
                    () => EnglishCompare.Compare(left.ObfuscatedName, right.ObfuscatedName, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase)
                ),

                // Column 3: Sort by Spell Name (blanks always last), then by Obfuscated Name
                3 => CompareSpellNameWithSecondary(
                    left,
                    right,
                    () => EnglishCompare.Compare(left.ObfuscatedName, right.ObfuscatedName, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase)
                ),

                // Default: Sort by Obfuscated Name only
                _ => ascending
                    ? EnglishCompare.Compare(left.ObfuscatedName, right.ObfuscatedName, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase)
                    : EnglishCompare.Compare(right.ObfuscatedName, left.ObfuscatedName, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase)
            };

            return primaryCompare;
        });

        // Mark sort specs as applied
        sortSpecs.SpecsDirty = false;

        // Helper: Compare with secondary sort
        int CompareWithSecondary(int primaryResult, bool isAscending, Func<int> secondaryCompare)
        {
            int result = isAscending ? primaryResult : -primaryResult;
            return result != 0 ? result : secondaryCompare();
        }

        // Helper: Compare spell names with blanks always last
        int CompareSpellNameWithSecondary(ObfuscatedTranslation left, ObfuscatedTranslation right, Func<int> secondaryCompare)
        {
            bool leftBlank = string.IsNullOrWhiteSpace(left.DeobfuscatedName);
            bool rightBlank = string.IsNullOrWhiteSpace(right.DeobfuscatedName);

            // Blanks always go last regardless of sort direction
            if (leftBlank && !rightBlank) return 1;
            if (!leftBlank && rightBlank) return -1;
            if (leftBlank && rightBlank) return secondaryCompare();

            // Get appropriate CompareInfo based on language
            CompareInfo compareInfo = left.LanguageID switch
            {
                0 => JapaneseCompare,
                1 => EnglishCompare,
                2 => GermanCompare,
                3 => FrenchCompare,
                _ => EnglishCompare
            };

            // Compare non-blank values with sort direction
            int textCompare = compareInfo.Compare(left.DeobfuscatedName, right.DeobfuscatedName, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase);
            if (ascending)
            {
                return textCompare != 0 ? textCompare : secondaryCompare();
            }
            else
            {
                return textCompare != 0 ? -textCompare : secondaryCompare();
            }
        }
    }

    // ----------------------------
    // Export obfuscated translations CSV
    // ----------------------------
    private static string ExportObfuscatedTranslationsCSV(List<ObfuscatedTranslation> exportedTranslations)
    {
        // Check for null or empty list
        if (exportedTranslations == null || exportedTranslations.Count == 0) return string.Empty;

        // Build CSV lines
        List<string> lines = [];
        foreach (ObfuscatedTranslation exportedTranslation in exportedTranslations)
        {
            // Get fields
            string actionID = exportedTranslation.ActionID.ToString();
            string obfuscatedName = SanitizeCSVField(exportedTranslation.ObfuscatedName);
            string languageID = exportedTranslation.LanguageID.ToString();
            string deobfuscatedName = SanitizeCSVField(exportedTranslation.DeobfuscatedName);

            // Add line to CSV
            lines.Add($"{actionID};{obfuscatedName};{languageID};{deobfuscatedName}");
        }

        // Join lines
        return string.Join(Environment.NewLine, lines);
    }

    // ----------------------------
    // Import obfuscated translations CSV
    // ----------------------------
    public static bool ImportObfuscatedTranslationsCSV(string csv, List<ObfuscatedTranslation> obfuscatedTranslations, out string status)
    {
        // Initialize
        status = string.Empty;
        List<ObfuscatedTranslation> importedTranslations = [];

        // Check for null target list
        if (obfuscatedTranslations == null)
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

            // Validate line format (must contain exactly 3 ';' separators)
            string[] parts = line.Split(';');
            if (parts.Length != 4)
            {
                status = $"Invalid CSV at line {i + 1}";
                return false;
            }

            // Extract fields
            string actionIDStr = parts[0].Trim();
            string obfuscatedName = parts[1].Trim();
            string languageIDStr = parts[2].Trim();
            string deobfuscatedName = parts[3].Trim();

            // Validate required fields
            if (!int.TryParse(actionIDStr, out int actionID) || actionID < 0 || string.IsNullOrWhiteSpace(obfuscatedName) || !int.TryParse(languageIDStr, out int languageID) || languageID < 0 || languageID > 3 || string.IsNullOrWhiteSpace(deobfuscatedName))
            {
                status = $"Invalid value at line {i + 1}";
                return false;
            }

            // Add to imported list
            importedTranslations.Add(new ObfuscatedTranslation
            {
                ActionID = actionID,
                ObfuscatedName = obfuscatedName,
                LanguageID = languageID,
                DeobfuscatedName = deobfuscatedName
            });
        }

        // Replace original list with imported translations
        obfuscatedTranslations.Clear();
        obfuscatedTranslations.AddRange(importedTranslations);
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
    // Dispose
    // ----------------------------
    public void Dispose()
    {
        // Finalize
        GC.SuppressFinalize(this);
    }

}