using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LangSwap.translation;
using LangSwap.translation.model;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace LangSwap.windows;

// ----------------------------
// Debug Window
// ----------------------------
public class DebugWindow : Window, IDisposable
{
    // Log
    private const string Class = "[DebugWindow.cs]";

    // Service
    private static IPluginLog Log => Plugin.Log;

    // Core components
    private readonly Configuration config;
    private readonly ExcelProvider excelProvider;

    // Obfuscated translations
    private readonly List<ObfuscatedTranslation> translations = [];

    // CSV export/import
    private string exportCSV = string.Empty;
    private string importCSV = string.Empty;
    private string importStatus = string.Empty;

    // Message
    private string message = string.Empty;

    // Window size
    private const int WindowHeight = 818;
    private const int WindowWidth = 1500;

    // ----------------------------
    // Constructor
    // ----------------------------
    public DebugWindow(Configuration config, ExcelProvider excelProvider) : base("LangSwap - Debug###LangSwapDebug")
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
        ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY;
        if (!ImGui.BeginTable("##ObfuscatedTranslationsTable", 6, tableFlags, new Vector2(WindowWidth - 45, WindowHeight - 104))) return;

        // Setup columns
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Spell ID", ImGuiTableColumnFlags.WidthFixed, 65f);
        ImGui.TableSetupColumn("Obfuscation", ImGuiTableColumnFlags.WidthFixed, 320f);
        ImGui.TableSetupColumn("Japanese", ImGuiTableColumnFlags.WidthStretch, 1.0f);
        ImGui.TableSetupColumn("English", ImGuiTableColumnFlags.WidthStretch, 1.0f);
        ImGui.TableSetupColumn("German", ImGuiTableColumnFlags.WidthStretch, 1.0f);
        ImGui.TableSetupColumn("French", ImGuiTableColumnFlags.WidthStretch, 1.0f);
        ImGui.TableNextRow(ImGuiTableRowFlags.None, 30f);

        // ID
        ImGui.TableSetColumnIndex(0);
        DrawTextCell("Spell ID");

        // Obfuscation
        ImGui.TableSetColumnIndex(1);
        DrawTextCell("Obfuscation");

        // Japanese
        ImGui.TableSetColumnIndex(2);
        DrawTextCell("Japanese");

        // English
        ImGui.TableSetColumnIndex(3);
        DrawTextCell("English");

        // German
        ImGui.TableSetColumnIndex(4);
        DrawTextCell("German");

        // French
        ImGui.TableSetColumnIndex(5);
        DrawTextCell("French");

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

                // ID
                ImGui.TableNextRow(ImGuiTableRowFlags.None, 30f);
                ImGui.TableSetColumnIndex(0);
                DrawTextCell(translation.ID.ToString());

                // Obfuscation
                ImGui.TableSetColumnIndex(1);
                DrawTextCell(translation.ObfuscatedName);

                // Japanese
                ImGui.TableSetColumnIndex(2);
                DrawTextCell(translation.JapaneseName);

                // English
                ImGui.TableSetColumnIndex(3);
                DrawTextCell(translation.EnglishName);

                // German
                ImGui.TableSetColumnIndex(4);
                DrawTextCell(translation.GermanName);

                // French
                ImGui.TableSetColumnIndex(5);
                DrawTextCell(translation.FrenchName);
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
        ImGui.SameLine(0, 990f);
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
                ID = obfuscatedTranslation.ID,
                ObfuscatedName = obfuscatedTranslation.ObfuscatedName
            });
        }

        // Merge obfuscated translations lists
        MergeObfuscatedTranslations(config.RemoteObfuscatedTranslations);
        MergeObfuscatedTranslations(config.ScannedObfuscatedTranslations);
        MergeObfuscatedTranslations(config.LocalObfuscatedTranslations);

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
                    ID = sourceTranslation.ID,
                    ObfuscatedName = sourceTranslation.ObfuscatedName,
                    JapaneseName = sourceTranslation.JapaneseName,
                    EnglishName = sourceTranslation.EnglishName,
                    GermanName = sourceTranslation.GermanName,
                    FrenchName = sourceTranslation.FrenchName
                });
                continue;
            }

            // Merge obfuscated translation
            targetTranslation.ID = sourceTranslation.ID;
            if (!string.IsNullOrWhiteSpace(sourceTranslation.JapaneseName)) targetTranslation.JapaneseName = sourceTranslation.JapaneseName;
            if (!string.IsNullOrWhiteSpace(sourceTranslation.EnglishName)) targetTranslation.EnglishName = sourceTranslation.EnglishName;
            if (!string.IsNullOrWhiteSpace(sourceTranslation.GermanName)) targetTranslation.GermanName = sourceTranslation.GermanName;
            if (!string.IsNullOrWhiteSpace(sourceTranslation.FrenchName)) targetTranslation.FrenchName = sourceTranslation.FrenchName;
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
            string ID = exportedTranslation.ID.ToString();
            string obfuscatedName = SanitizeCSVField(exportedTranslation.ObfuscatedName);
            string japaneseName = SanitizeCSVField(exportedTranslation.JapaneseName);
            string englishName = SanitizeCSVField(exportedTranslation.EnglishName);
            string germanName = SanitizeCSVField(exportedTranslation.GermanName);
            string frenchName = SanitizeCSVField(exportedTranslation.FrenchName);

            // Add line to CSV
            lines.Add($"{ID};{obfuscatedName};{japaneseName};{englishName};{germanName};{frenchName}");
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
            string japaneseName = parts[2].Trim();
            string englishName = parts[3].Trim();
            string germanName = parts[4].Trim();
            string frenchName = parts[5].Trim();

            // Validate required fields
            if (!int.TryParse(idStr, out int ID) || ID < 0 || string.IsNullOrWhiteSpace(obfuscatedName))
            {
                status = $"Invalid value at line {i + 1}";
                return false;
            }

            // Add to imported list
            importedTranslations.Add(new ObfuscatedTranslation
            {
                ID = ID,
                ObfuscatedName = obfuscatedName,
                JapaneseName = japaneseName,
                EnglishName = englishName,
                GermanName = germanName,
                FrenchName = frenchName
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