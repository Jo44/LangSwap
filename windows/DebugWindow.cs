using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.translation.@base;
using LangSwap.windows.@base;
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

    // Core components
    private readonly Configuration config;
    private readonly ExcelProvider excelProvider;
    private readonly IPluginLog log;

    // Obfuscated translations
    private readonly List<ObfuscatedTranslation> obfuscatedTranslations = [];

    // CSV export/import
    private string exportScannedCSV = string.Empty;
    private string importLocalCSV = string.Empty;
    private string importLocalStatus = string.Empty;

    // Message
    private string message = string.Empty;
    private DateTime messageTimestamp = DateTime.MinValue;
    private const int MessageDurationSeconds = 5;

    // ----------------------------
    // Constructor
    // ----------------------------
    public DebugWindow(Configuration config, ExcelProvider excelProvider, IPluginLog log) : base("LangSwap - Debug###LangSwapDebug")
    {
        // Initialize core components
        this.config = config;
        this.excelProvider = excelProvider;
        this.log = log;

        // Window settings
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize;

        // Fixed size
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(1200, 478),
            MaximumSize = new Vector2(1200, 478)
        };

        // Load obfuscated translations
        LoadObfuscatedTranslations();
    }
    
    // ----------------------------
    // On close
    // ----------------------------
    public override void OnClose()
    {
        // Reload obfuscated translations to discard unsaved changes
        LoadObfuscatedTranslations();
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

        // Check if the message has expired
        if ((DateTime.Now - messageTimestamp).TotalSeconds >= MessageDurationSeconds)
        {
            message = string.Empty;
            return;
        }

        // Calculate text position
        Vector2 textSize = ImGui.CalcTextSize(message);
        float x = ImGui.GetWindowContentRegionMax().X - textSize.X - 15f;

        // Draw message
        ImGui.SameLine();
        ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), x));
        ImGui.TextUnformatted(message);
    }

    // ----------------------------
    // Draw obfuscated translations table
    // ----------------------------
    private void DrawObfuscatedTranslationsTable()
    {
        // Table dimensions
        const float tableHeight = 374f;
        const float tableWidth = 1155f;
        const float rowHeight = 30f;

        // Draw obfuscated translations table
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY;
        if (!ImGui.BeginTable("##ObfuscatedTranslationsTable", 6, tableFlags, new Vector2(tableWidth, tableHeight))) return;
            
        // Setup columns
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Spell ID", ImGuiTableColumnFlags.WidthFixed, 65f);
        ImGui.TableSetupColumn("Obfuscation", ImGuiTableColumnFlags.WidthFixed, 320f);
        ImGui.TableSetupColumn("English", ImGuiTableColumnFlags.WidthStretch, 1.0f);
        ImGui.TableSetupColumn("French", ImGuiTableColumnFlags.WidthStretch, 1.0f);
        ImGui.TableSetupColumn("German", ImGuiTableColumnFlags.WidthStretch, 1.0f);
        ImGui.TableSetupColumn("Japanese", ImGuiTableColumnFlags.WidthStretch, 1.0f);
        ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);

        // ID
        ImGui.TableSetColumnIndex(0);
        DrawCellText("Spell ID", rowHeight);

        // Obfuscation
        ImGui.TableSetColumnIndex(1);
        DrawCellText("Obfuscation", rowHeight);

        // English
        ImGui.TableSetColumnIndex(2);
        DrawCellText("English", rowHeight);

        // French
        ImGui.TableSetColumnIndex(3);
        DrawCellText("French", rowHeight);

        // German
        ImGui.TableSetColumnIndex(4);
        DrawCellText("German", rowHeight);

        // Japanese
        ImGui.TableSetColumnIndex(5);
        DrawCellText("Japanese", rowHeight);

        // No entries
        if (obfuscatedTranslations.Count == 0)
        {
            ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);
            ImGui.TableSetColumnIndex(0);
            DrawCellText(" ", rowHeight);
            ImGui.TableSetColumnIndex(1);
            DrawCellText("No remaining obfuscated names found", rowHeight);
        }
        else
        {
            // Draw rows for each obfuscated translation
            for (int i = 0; i < obfuscatedTranslations.Count; i++)
            {
                // Get obfuscated translation
                ObfuscatedTranslation obfuscatedTranslation = obfuscatedTranslations[i];

                // ID
                ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);
                ImGui.TableSetColumnIndex(0);
                DrawCellText(obfuscatedTranslation.Id.ToString(), rowHeight);

                // Obfuscation
                ImGui.TableSetColumnIndex(1);
                DrawCellText(obfuscatedTranslation.ObfuscatedName, rowHeight);

                // English
                ImGui.TableSetColumnIndex(2);
                DrawCellText(obfuscatedTranslation.EnglishName, rowHeight);

                // French
                ImGui.TableSetColumnIndex(3);
                DrawCellText(obfuscatedTranslation.FrenchName, rowHeight);

                // German
                ImGui.TableSetColumnIndex(4);
                DrawCellText(obfuscatedTranslation.GermanName, rowHeight);

                // Japanese
                ImGui.TableSetColumnIndex(5);
                DrawCellText(obfuscatedTranslation.JapaneseName, rowHeight);
            }
        }
        ImGui.EndTable();
        ImGui.Spacing();
        ImGui.Spacing();
    }

    // ----------------------------
    // Draw cell text
    // ----------------------------
    private static void DrawCellText(string text, float rowHeight)
    {
        // Calculate text position
        Vector2 pos = ImGui.GetCursorScreenPos();
        float width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        Vector2 textSize = ImGui.CalcTextSize(text);
        Vector2 textPos = new(pos.X + (width - textSize.X) * 0.5f, pos.Y + (rowHeight - textSize.Y) * 0.5f);

        // Draw text
        ImGui.GetWindowDrawList().AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), text);
        ImGui.Dummy(new Vector2(width, rowHeight));
    }

    // ----------------------------
    // Draw export scanned button
    // ----------------------------
    private void DrawExportScannedButton()
    {
        // Draw export scanned button
        ImGui.Spacing();
        ImGui.SameLine(0, 690f);
        if (ImGui.Button("Export scanned", new Vector2(150f, 0f)))
        {
            // Export scanned obfuscated translations to CSV
            exportScannedCSV = Utilities.ExportObfuscatedTranslationsCSV(config.ScannedObfuscatedTranslations);

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
        PopupBuilder.DrawExportCSVPopup("Export scanned", "##ExportScannedCsv", ref exportScannedCSV, new Vector2(900f, 400f), false, () =>
        {
            // Count exported translations
            int count = config.ScannedObfuscatedTranslations.Count;

            // Log
            string message = $"{count} scanned obfuscated translations exported";
            log.Information($"{Class} - {message}");

            // Set message
            SetMessage(message);
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
            importLocalCSV = string.Empty;
            importLocalStatus = string.Empty;

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
        PopupBuilder.DrawImportCSVPopup("Import local", "##ImportLocalCsv", ref importLocalCSV, ref importLocalStatus, new Vector2(900f, 400f), csv =>
        {
            // Try to import obfuscated translations from CSV
            List<ObfuscatedTranslation> importedTranslations = [];
            if (Utilities.ImportObfuscatedTranslationsCSV(csv, importedTranslations, out string status))
            {
                // Replace current local translations and persist
                config.LocalObfuscatedTranslations.Clear();
                config.LocalObfuscatedTranslations.AddRange(importedTranslations);
                config.Save();

                // Count imported translations
                int count = config.LocalObfuscatedTranslations.Count;

                // Log
                string message = $"{count} local obfuscated translations imported";
                log.Information($"{Class} - {message}");

                // Set message
                SetMessage(message);

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
            string message = $"{count} local obfuscated translations cleared";
            log.Information($"{Class} - {message}");

            // Set message
            SetMessage(message);

            // Reload obfuscated translations
            LoadObfuscatedTranslations();
        });
    }

    // ----------------------------
    // Load obfuscated translations
    // ----------------------------
    private void LoadObfuscatedTranslations()
    {
        // Clear translations list
        obfuscatedTranslations.Clear();

        // Get all obfuscated actions from Excel provider
        foreach (ObfuscatedTranslation obfuscatedTranslation in excelProvider.GetAllObfuscatedActions())
        {
            obfuscatedTranslations.Add(new ObfuscatedTranslation
            {
                Id = obfuscatedTranslation.Id,
                ObfuscatedName = obfuscatedTranslation.ObfuscatedName
            });
        }

        // Merge obfuscated translations lists
        MergeObfuscatedTranslations(config.RemoteObfuscatedTranslations);
        MergeObfuscatedTranslations(config.ScannedObfuscatedTranslations);
        MergeObfuscatedTranslations(config.LocalObfuscatedTranslations);

        // Log
        log.Information($"{Class} - Loaded {obfuscatedTranslations.Count} obfuscated translations");
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
            ObfuscatedTranslation? targetTranslation = obfuscatedTranslations.Find(translation => string.Equals(translation.ObfuscatedName, sourceTranslation.ObfuscatedName, StringComparison.Ordinal));
            if (targetTranslation == null) continue;

            // Merge obfuscated translation
            targetTranslation.Id = sourceTranslation.Id;
            if (!string.IsNullOrWhiteSpace(sourceTranslation.EnglishName)) targetTranslation.EnglishName = sourceTranslation.EnglishName;
            if (!string.IsNullOrWhiteSpace(sourceTranslation.FrenchName)) targetTranslation.FrenchName = sourceTranslation.FrenchName;
            if (!string.IsNullOrWhiteSpace(sourceTranslation.GermanName)) targetTranslation.GermanName = sourceTranslation.GermanName;
            if (!string.IsNullOrWhiteSpace(sourceTranslation.JapaneseName)) targetTranslation.JapaneseName = sourceTranslation.JapaneseName;
        }
    }

    // ----------------------------
    // Set message
    // ----------------------------
    private void SetMessage(string message)
    {
        this.message = message;
        messageTimestamp = DateTime.Now;
    }

    // ----------------------------
    // Dispose
    // ----------------------------
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

}