using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LangSwap.translation.model;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace LangSwap.windows;

// ----------------------------
// Customize Window
// ----------------------------
public class CustomizeWindow : Window, IDisposable
{
    // Log
    private const string Class = "[CustomizeWindow.cs]";

    // Service
    private static IPluginLog Log => Plugin.Log;

    // Core component
    private readonly Configuration config;

    // Alternative translations
    private readonly List<AlternativeTranslation> translations = [];

    // CSV export/import
    private string exportCSV = string.Empty;
    private string importCSV = string.Empty;
    private string importStatus = string.Empty;

    // Message
    private string message = string.Empty;

    // Focus management
    private bool focusedNewRow;
    private int focusNewRowIndex = -1;

    // Window size
    private const int WindowHeight = 478;
    private const int WindowWidth = 690;

    // ----------------------------
    // Constructor
    // ----------------------------
    public CustomizeWindow(Configuration config) : base("LangSwap - Customize###LangSwapCustomize")
    {
        // Initialize core component
        this.config = config;

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
        // Load alternative translations
        LoadAlternativeTranslations();

        // Set message
        message = $"{translations.Count} alternative translations loaded";
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

        // Alternative translations table
        DrawAlternativeTranslationsTable();

        // Export CSV
        DrawExportButton();
        DrawExportPopup();

        // Import CSV
        DrawImportButton();
        DrawImportPopup();

        // Reset button
        DrawResetButton();
        DrawResetPopup();

        // Save button
        DrawSaveButton();
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
        ImGui.TextWrapped("Define alternative translations for spell names");
    }

    // ----------------------------
    // Draw message
    // ----------------------------
    private void DrawMessage()
    {
        // Check if there is an message to display
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
    // Draw alternative translations table
    // ----------------------------
    private void DrawAlternativeTranslationsTable()
    {
        // Draw alternative translations table
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY;
        if (!ImGui.BeginTable("##AlternativeTranslationsTable", 4, tableFlags, new Vector2(WindowWidth - 45, WindowHeight - 104))) return;

        // Setup columns
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("##AddOrIndex", ImGuiTableColumnFlags.WidthFixed, 40f);
        ImGui.TableSetupColumn("Spell", ImGuiTableColumnFlags.WidthStretch, 1.0f);
        ImGui.TableSetupColumn("Replacement", ImGuiTableColumnFlags.WidthStretch, 1.0f);
        ImGui.TableSetupColumn("##Remove", ImGuiTableColumnFlags.WidthFixed, 60f);
        ImGui.TableNextRow(ImGuiTableRowFlags.None, 30f);

        // Add
        ImGui.TableSetColumnIndex(0);
        bool addClicked = DrawActionCell("##AddCell", "Add");
        if (addClicked && !HasPendingEmptyRow())
        {
            translations.Add(new AlternativeTranslation());
            focusNewRowIndex = translations.Count - 1;
            focusedNewRow = true;
        }

        // Spell
        ImGui.TableSetColumnIndex(1);
        DrawTextCell("Spell");

        // Replacement
        ImGui.TableSetColumnIndex(2);
        DrawTextCell("Replacement");

        // Remove
        ImGui.TableSetColumnIndex(3);
        DrawTextCell(" ");

        // No entries
        if (translations.Count == 0)
        {
            ImGui.TableNextRow(ImGuiTableRowFlags.None, 30f);
            ImGui.TableSetColumnIndex(0);
            DrawTextCell(" ");
            ImGui.TableSetColumnIndex(1);
            DrawTextCell("No alternative translations");
        }
        else
        {
            // Initialize remove index
            int removeIndex = -1;

            // Draw rows for each alternative translation
            for (int i = 0; i < translations.Count; i++)
            {
                // Get alternative translation
                AlternativeTranslation translation = translations[i];

                // Start of row
                ImGui.PushID(i);
                ImGui.TableNextRow(ImGuiTableRowFlags.None, 30f);

                // Index
                ImGui.TableSetColumnIndex(0);
                DrawTextCell($"#{i + 1}");

                // Spell name input
                ImGui.TableSetColumnIndex(1);
                string spellName = translation.SpellName;
                bool focused = focusedNewRow && i == focusNewRowIndex;
                DrawInputCell("##Spell", ref spellName, focused);
                translation.SpellName = spellName;
                   
                // Set focus
                if (focused)
                {
                    ImGui.SetScrollHereY();
                    focusedNewRow = false;
                    focusNewRowIndex = -1;
                }

                // Alternative name input
                ImGui.TableSetColumnIndex(2);
                string alternativeName = translation.AlternativeName;
                DrawInputCell("##Replacement", ref alternativeName);
                translation.AlternativeName = alternativeName;

                // Remove cell
                ImGui.TableSetColumnIndex(3);
                if (DrawActionCell($"##RemoveCell_{i}", "Remove"))
                {
                    removeIndex = i;
                }

                // End of row
                ImGui.PopID();
            }

            // Remove entry if requested
            if (removeIndex >= 0)
            {
                translations.RemoveAt(removeIndex);
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
    // Draw input cell
    // ----------------------------
    private static void DrawInputCell(string id, ref string value, bool focused = false)
    {
        // Calculate input position
        float width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        float inputY = ImGui.GetCursorPosY();
        float offsetY = MathF.Max(0f, (30f - ImGui.GetFrameHeight()) * 0.5f);
        ImGui.SetCursorPosY(inputY + offsetY);

        // Set focus if requested
        if (focused) ImGui.SetKeyboardFocusHere();

        // Draw input text
        ImGui.SetNextItemWidth(width);
        ImGui.InputText(id, ref value, 256);
    }

    // ----------------------------
    // Draw action cell
    // ----------------------------
    private bool DrawActionCell(string id, string text)
    {
        // Calculate cell position and size
        Vector2 pos = ImGui.GetCursorScreenPos();
        Vector2 size = new(MathF.Max(1f, ImGui.GetContentRegionAvail().X), 30f);

        // Draw invisible button to capture click
        bool clicked = ImGui.InvisibleButton(id, size);

        // Determine background color
        Vector4 background = config.RedDalamud;
        if (ImGui.IsItemActive()) background = config.LighterRedDalamud;
        else if (ImGui.IsItemHovered()) background = config.LightRedDalamud;

        // Calculate background rectangle with padding
        float padX = ImGui.GetStyle().CellPadding.X;
        float padY = ImGui.GetStyle().CellPadding.Y;
        Vector2 drawMin = new(pos.X - padX + 0.5f, pos.Y - padY + 0.5f);
        Vector2 drawMax = new(pos.X + size.X + padX, pos.Y + size.Y + padY - 0.5f);

        // Draw background
        uint backgroundColor = ImGui.GetColorU32(background);
        ImGui.GetWindowDrawList().AddRectFilled(drawMin, drawMax, backgroundColor);

        // Calculate text position and size
        Vector2 textSize = ImGui.CalcTextSize(text);
        Vector2 textPos = new(drawMin.X + (drawMax.X - drawMin.X - textSize.X) * 0.5f, drawMin.Y + (drawMax.Y - drawMin.Y - textSize.Y) * 0.5f);

        // Draw text
        ImGui.GetWindowDrawList().AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), text);

        // Return whether the cell was clicked
        return clicked;
    }

    // ----------------------------
    // Draw export CSV button
    // ----------------------------
    private void DrawExportButton()
    {
        // Draw export CSV button
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        if (ImGui.Button("Export CSV", new Vector2(150f, 0f)))
        {
            // Export alternative translations to CSV
            exportCSV = ExportAlternativeTranslationsCSV(translations);

            // Open popup
            ImGui.OpenPopup("Export CSV");
        }
    }

    // ----------------------------
    // Draw export CSV popup
    // ----------------------------
    private void DrawExportPopup()
    {
        // Draw export CSV popup
        PopupBuilder.DrawExportCSVPopup("Export CSV", "##ExportCSVText", ref exportCSV, new Vector2(400f, 400f), () =>
        {
            // Count exported translations
            int count = translations.Count;

            // Log
            message = $"{count} alternative translations exported";
            Log.Information($"{Class} - {message}");
        });
    }

    // ----------------------------
    // Draw import CSV button
    // ----------------------------
    private void DrawImportButton()
    {
        // Draw import CSV button
        ImGui.SameLine(0, 15f);
        if (ImGui.Button("Import CSV", new Vector2(150f, 0f)))
        {
            // Reset import CSV and status
            importCSV = string.Empty;
            importStatus = string.Empty;

            // Open popup
            ImGui.OpenPopup("Import CSV");
        }
    }

    // ----------------------------
    // Draw import CSV popup
    // ----------------------------
    private void DrawImportPopup()
    {
        // Draw import CSV popup
        PopupBuilder.DrawImportCSVPopup("Import CSV", "##ImportCSVText", ref importCSV, ref importStatus, new Vector2(400f, 400f), csv =>
        {
            // Try to import alternative translations from CSV
            if (ImportAlternativeTranslationsCSV(csv, translations, out string status))
            {
                // Count imported translations
                int count = translations.Count;

                // Log
                message = $"{count} alternative translations imported";
                Log.Information($"{Class} - {message}");

                // Reset status
                return string.Empty;
            }
            // Return status
            return status;
        });
    }

    // ----------------------------
    // Draw reset button
    // ----------------------------
    private static void DrawResetButton()
    {
        // Draw reset button
        ImGui.SameLine(0, 15f);
        if (ImGui.Button("Reset all", new Vector2(150f, 0f)))
        {
            // Open popup
            ImGui.OpenPopup("Confirm reset");
        }
    }

    // ----------------------------
    // Draw reset popup
    // ----------------------------
    private void DrawResetPopup()
    {
        // Draw confirmation popup
        PopupBuilder.DrawConfirmationPopup("Confirm reset", "This will clear all alternative translations.    Are you sure ?", "Yes, reset all", "Cancel", new Vector2(430f, 0f), () =>
        {
            // Count removed translations
            int count = translations.Count;

            // Clear alternative translations
            translations.Clear();
            config.AlternativeTranslations.Clear();

            // Save configuration
            config.Save();

            // Log
            message = $"{count} alternative translations cleared";
            Log.Information($"{Class} - {message}");
        });
    }

    // ----------------------------
    // Draw save button
    // ----------------------------
    private void DrawSaveButton()
    {
        // Determine if changes can be saved
        bool canSave = CanSaveChanges();

        // Draw save button
        ImGui.SameLine(0, 15f);
        ImGui.BeginDisabled(!canSave);
        if (ImGui.Button("Save changes", new Vector2(150f, 0f)))
        {
            // Save changes
            SaveChanges();
        }
        ImGui.EndDisabled();
    }

    // ----------------------------
    // Load alternative translations
    // ----------------------------
    private void LoadAlternativeTranslations()
    {
        // Clear translations list
        translations.Clear();

        // Load persisted values into alternative translations list
        foreach (AlternativeTranslation alternativeTranslation in config.AlternativeTranslations)
        {
            translations.Add(new AlternativeTranslation
            {
                SpellName = alternativeTranslation.SpellName,
                AlternativeName = alternativeTranslation.AlternativeName
            });
        }

        // Log
        Log.Information($"{Class} - Loaded {translations.Count} alternative translations");
    }

    // ----------------------------
    // Export alternative translations CSV
    // ----------------------------
    private static string ExportAlternativeTranslationsCSV(List<AlternativeTranslation> exportedTranslations)
    {
        // Check for null or empty list
        if (exportedTranslations == null || exportedTranslations.Count == 0) return string.Empty;

        // Build CSV lines
        List<string> lines = [];
        foreach (AlternativeTranslation exportedTranslation in exportedTranslations)
        {
            // Get fields
            string spellName = SanitizeCSVField(exportedTranslation.SpellName);
            string alternativeName = SanitizeCSVField(exportedTranslation.AlternativeName);

            // Add line to CSV
            lines.Add($"{spellName};{alternativeName}");
        }

        // Join lines
        return string.Join(Environment.NewLine, lines);
    }

    // ----------------------------
    // Import alternative translations CSV
    // ----------------------------
    private static bool ImportAlternativeTranslationsCSV(string csv, List<AlternativeTranslation> alternativeTranslations, out string status)
    {
        // Initialize
        status = string.Empty;
        List<AlternativeTranslation> importedTranslations = [];

        // Check for null target list
        if (alternativeTranslations == null)
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
            importedTranslations.Add(new AlternativeTranslation
            {
                SpellName = spellName,
                AlternativeName = alternativeName
            });
        }

        // Replace original list with imported translations
        alternativeTranslations.Clear();
        alternativeTranslations.AddRange(importedTranslations);
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
    // Save changes
    // ----------------------------
    private void SaveChanges()
    {
        // Clear persisted list
        config.AlternativeTranslations.Clear();

        // Save alternative translations into persisted list
        foreach (AlternativeTranslation translation in translations)
        {
            config.AlternativeTranslations.Add(new AlternativeTranslation
            {
                SpellName = translation.SpellName,
                AlternativeName = translation.AlternativeName
            });
        }

        // Save configuration
        config.Save();

        // Log
        message = $"{translations.Count} alternative translations saved";
        Log.Information($"{Class} - {message}");
    }

    // ----------------------------
    // Can save changes
    // ----------------------------
    private bool CanSaveChanges()
    {
        // Check for invalid entries
        if (HasInvalidEntries()) return false;

        // Check for changes
        if (!HasChanges()) return false;

        // All checks passed, can save
        return true;
    }

    // ----------------------------
    // Has changes
    // ----------------------------
    private bool HasChanges()
    {
        // Check for count differences
        if (translations.Count != config.AlternativeTranslations.Count) return true;

        // Check for differences in memory vs persisted values
        for (int i = 0; i < translations.Count; i++)
        {
            AlternativeTranslation inMemory = translations[i];
            AlternativeTranslation persisted = config.AlternativeTranslations[i];
            if (!string.Equals(inMemory.SpellName, persisted.SpellName, StringComparison.Ordinal)) return true;
            if (!string.Equals(inMemory.AlternativeName, persisted.AlternativeName, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    // ----------------------------
    // Has invalid entries
    // ----------------------------
    private bool HasInvalidEntries()
    {
        // Check for invalid values in alternative translations
        foreach (AlternativeTranslation translation in translations)
        {
            if (IsInvalidValue(translation.SpellName) || IsInvalidValue(translation.AlternativeName)) return true;
        }
        return false;
    }

    // ----------------------------
    // Is invalid value
    // ----------------------------
    private static bool IsInvalidValue(string value)
    {
        // Check for invalid characters or empty values
        return string.IsNullOrWhiteSpace(value) || value.Contains(';');
    }

    // ----------------------------
    // Has pending empty row
    // ----------------------------
    private bool HasPendingEmptyRow()
    {
        // Check if there is already an empty row in the translations list
        foreach (AlternativeTranslation translation in translations)
        {
            if (string.IsNullOrWhiteSpace(translation.SpellName) && string.IsNullOrWhiteSpace(translation.AlternativeName)) return true;
        }
        return false;
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