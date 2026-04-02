using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LangSwap.tool;
using LangSwap.translation.@base;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace LangSwap.Windows;

// ----------------------------
// Customize Window
// ----------------------------
public class CustomizeWindow : Window, IDisposable
{
    // Log
    private const string Class = "[CustomizeWindow.cs]";

    // Core components
    private readonly Configuration config;
    private readonly IPluginLog log;

    // Alternative translations
    private readonly List<AlternativeTranslation> alternativeTranslations = [];

    // CSV export/import
    private string exportCSV = string.Empty;
    private string importCSV = string.Empty;
    private string importStatus = string.Empty;

    // Information message
    private string information = string.Empty;
    private DateTime informationTimestamp = DateTime.MinValue;
    private const int InformationDurationSeconds = 5;

    // Focus management
    private bool focusedNewRow;
    private int focusNewRowIndex = -1;

    // ----------------------------
    // Constructor
    // ----------------------------
    public CustomizeWindow(Configuration config, IPluginLog log) : base("LangSwap - Customize###LangSwapCustomize")
    {
        // Initialize core components
        this.config = config;
        this.log = log;

        // Window settings
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize;

        // Fixed size
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(690, 478),
            MaximumSize = new Vector2(690, 478)
        };

        // Load alternative translations
        LoadAlternativeTranslations();
    }

    // ----------------------------
    // On close
    // ----------------------------
    public override void OnClose()
    {
        // Reload alternative translations to discard unsaved changes
        LoadAlternativeTranslations();
    }

    // ----------------------------
    // Draw
    // ----------------------------
    public override void Draw()
    {
        // Information
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGui.TextWrapped("Define alternative translations for spell names");

        // Draw information message
        DrawInformationMessage();

        // Draw alternative translations table
        DrawAlternativeTranslationsTable();

        // Export button
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        if (ImGui.Button("Export CSV", new Vector2(150f, 0f)))
        {
            exportCSV = Utilities.ExportAlternativeTranslationsCSV(alternativeTranslations);
            ImGui.OpenPopup("Export CSV");
        }

        // Import button
        ImGui.SameLine(0, 15f);
        if (ImGui.Button("Import CSV", new Vector2(150f, 0f)))
        {
            importCSV = string.Empty;
            importStatus = string.Empty;
            ImGui.OpenPopup("Import CSV");
        }

        // Reset button
        ImGui.SameLine(0, 15f);
        if (ImGui.Button("Reset all", new Vector2(150f, 0f)))
        {
            ImGui.OpenPopup("Confirm reset");
        }

        // Save button
        bool canSave = CanSaveChanges();
        ImGui.SameLine(0, 15f);
        ImGui.BeginDisabled(!canSave);
        if (ImGui.Button("Save changes", new Vector2(150f, 0f)))
        {
            SaveChanges();
        }
        ImGui.EndDisabled();

        // Draw popups
        DrawExportPopup();
        DrawImportPopup();
        DrawResetPopup();
    }

    // ----------------------------
    // Draw information message
    // ----------------------------
    private void DrawInformationMessage()
    {
        // Check if there is an information message to display
        if (string.IsNullOrWhiteSpace(information)) return;

        // Check if the information message has expired
        if ((DateTime.Now - informationTimestamp).TotalSeconds >= InformationDurationSeconds)
        {
            information = string.Empty;
            return;
        }

        // Calculate text position
        Vector2 textSize = ImGui.CalcTextSize(information);
        float x = ImGui.GetWindowContentRegionMax().X - textSize.X - 15f;

        // Draw information message
        ImGui.SameLine();
        ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), x));
        ImGui.TextUnformatted(information);
    }

    // ----------------------------
    // Draw alternative translations table
    // ----------------------------
    private void DrawAlternativeTranslationsTable()
    {
        // Table dimensions
        const float tableHeight = 374f;
        const float tableWidth = 645f;
        const float rowHeight = 30f;

        // Alternative translations table
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY;
        if (!ImGui.BeginTable("##AlternativeTranslationsTable", 4, tableFlags, new Vector2(tableWidth, tableHeight))) return;

        // Setup columns
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("##AddOrIndex", ImGuiTableColumnFlags.WidthFixed, 40f);
        ImGui.TableSetupColumn("Spell", ImGuiTableColumnFlags.WidthStretch, 1.0f);
        ImGui.TableSetupColumn("Replacement", ImGuiTableColumnFlags.WidthStretch, 1.0f);
        ImGui.TableSetupColumn("##Remove", ImGuiTableColumnFlags.WidthFixed, 60f);
        ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);

        // Add
        ImGui.TableSetColumnIndex(0);
        bool addClicked = DrawActionCell("##AddCell", "Add", rowHeight);
        if (addClicked && !HasPendingEmptyRow())
        {
            alternativeTranslations.Add(new AlternativeTranslation());
            focusNewRowIndex = alternativeTranslations.Count - 1;
            focusedNewRow = true;
        }

        // Spell
        ImGui.TableSetColumnIndex(1);
        DrawCellText("Spell", rowHeight);

        // Replacement
        ImGui.TableSetColumnIndex(2);
        DrawCellText("Replacement", rowHeight);

        // Remove
        ImGui.TableSetColumnIndex(3);
        DrawCellText(" ", rowHeight);

        // No entries
        if (alternativeTranslations.Count == 0)
        {
            ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);
            ImGui.TableSetColumnIndex(0);
            DrawCellText(" ", rowHeight);
            ImGui.TableSetColumnIndex(1);
            DrawCellText("No alternative translations", rowHeight);
        }
        else
        {
            // Initialize index
            int removeIndex = -1;

            // Draw rows for each alternative translation
            for (int i = 0; i < alternativeTranslations.Count; i++)
            {
                // Get current translation
                AlternativeTranslation alternativeTranslation = alternativeTranslations[i];

                // Start of row
                ImGui.PushID(i);
                ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);

                // Index
                ImGui.TableSetColumnIndex(0);
                DrawCellText($"#{i + 1}", rowHeight);

                // Spell name input
                ImGui.TableSetColumnIndex(1);
                string spellName = alternativeTranslation.SpellName;
                bool setFocus = focusedNewRow && i == focusNewRowIndex;
                DrawInputText("##Spell", ref spellName, 256, rowHeight, setFocus);
                alternativeTranslation.SpellName = spellName;
                   
                // Set focus
                if (setFocus)
                {
                    ImGui.SetScrollHereY();
                    focusedNewRow = false;
                    focusNewRowIndex = -1;
                }

                // Alternative name input
                ImGui.TableSetColumnIndex(2);
                string alternativeName = alternativeTranslation.AlternativeName;
                DrawInputText("##Replacement", ref alternativeName, 256, rowHeight);
                alternativeTranslation.AlternativeName = alternativeName;

                // Remove cell
                ImGui.TableSetColumnIndex(3);
                if (DrawActionCell($"##RemoveCell_{i}", "Remove", rowHeight))
                {
                    removeIndex = i;
                }

                // End of row
                ImGui.PopID();
            }

            // Remove entry if requested
            if (removeIndex >= 0)
            {
                alternativeTranslations.RemoveAt(removeIndex);
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
    // Draw input text
    // ----------------------------
    private static void DrawInputText(string id, ref string value, int maxLength, float rowHeight, bool setFocus = false)
    {
        // Calculate input position and width
        float width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        float y = ImGui.GetCursorPosY();
        float offsetY = MathF.Max(0f, (rowHeight - ImGui.GetFrameHeight()) * 0.5f);
        ImGui.SetCursorPosY(y + offsetY);

        // Set focus if requested
        if (setFocus) ImGui.SetKeyboardFocusHere();

        // Draw input text
        ImGui.SetNextItemWidth(width);
        ImGui.InputText(id, ref value, maxLength);
    }

    // ----------------------------
    // Draw action cell
    // ----------------------------
    private bool DrawActionCell(string id, string label, float rowHeight)
    {
        // Calculate cell position and size
        Vector2 pos = ImGui.GetCursorScreenPos();
        Vector2 size = new(MathF.Max(1f, ImGui.GetContentRegionAvail().X), rowHeight);

        // Draw invisible button to capture clicks and hover state
        bool clicked = ImGui.InvisibleButton(id, size);

        // Determine background color
        Vector4 bg = config.RedDalamud;
        if (ImGui.IsItemActive()) bg = config.LighterRedDalamud;
        else if (ImGui.IsItemHovered()) bg = config.LightRedDalamud;

        // Calculate background rectangle with padding
        float padX = ImGui.GetStyle().CellPadding.X;
        float padY = ImGui.GetStyle().CellPadding.Y;
        const float borderInset = 0.5f;
        Vector2 drawMin = new(pos.X - padX + borderInset, pos.Y - padY + borderInset);
        Vector2 drawMax = new(pos.X + size.X + padX, pos.Y + size.Y + padY - borderInset);

        // Draw background
        uint bgColor = ImGui.GetColorU32(bg);
        ImGui.GetWindowDrawList().AddRectFilled(drawMin, drawMax, bgColor);

        // Calculate text position to center it
        Vector2 textSize = ImGui.CalcTextSize(label);
        Vector2 textPos = new(drawMin.X + (drawMax.X - drawMin.X - textSize.X) * 0.5f, drawMin.Y + (drawMax.Y - drawMin.Y - textSize.Y) * 0.5f);

        // Draw text
        ImGui.GetWindowDrawList().AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), label);
        return clicked;
    }

    // ----------------------------
    // Draw export CSV popup
    // ----------------------------
    private void DrawExportPopup()
    {
        // Draw export CSV popup
        PopupHelper.DrawExportCSVPopup("Export CSV", "##ExportCSVText", ref exportCSV, new Vector2(400f, 400f), true, () =>
        {
            // Count exported translations
            int count = alternativeTranslations.Count;

            // Log
            string message = $"{count} alternative translations exported";
            SetInformation(message);
            log.Information($"{Class} - {message}");
        });
    }

    // ----------------------------
    // Draw import CSV popup
    // ----------------------------
    private void DrawImportPopup()
    {
        // Draw import CSV popup
        PopupHelper.DrawImportCSVPopup("Import CSV", "##ImportCSVText", ref importCSV, ref importStatus, new Vector2(400f, 400f), csv =>
        {
            // Try to import alternative translations from CSV
            if (Utilities.ImportAlternativeTranslationsCSV(csv, alternativeTranslations, out string status))
            {
                // Count imported translations
                int count = alternativeTranslations.Count;

                // Log
                string message = $"{count} alternative translations imported";
                SetInformation(message);
                log.Information($"{Class} - {message}");

                // Reset status
                return string.Empty;
            }
            // Reset status
            return status;
        });
    }

    // ----------------------------
    // Draw reset popup
    // ----------------------------
    private void DrawResetPopup()
    {
        // Draw confirmation popup
        PopupHelper.DrawConfirmationPopup("Confirm reset", "This will clear all alternative translations.    Are you sure ?", "Yes, reset all", "Cancel", new Vector2(430f, 0f), () =>
        {
            // Count removed translations
            int count = alternativeTranslations.Count;

            // Clear alternative translations
            alternativeTranslations.Clear();
            config.AlternativeTranslations.Clear();

            // Save configuration
            config.Save();

            // Log
            string message = $"{count} alternative translations cleared";
            SetInformation(message);
            log.Information($"{Class} - {message}");
        });
    }

    // ----------------------------
    // Load alternative translations
    // ----------------------------
    private void LoadAlternativeTranslations()
    {
        // Clear translation list
        alternativeTranslations.Clear();

        // Load persisted values into translation list
        foreach (AlternativeTranslation alternativeTranslation in config.AlternativeTranslations)
        {
            alternativeTranslations.Add(new AlternativeTranslation
            {
                SpellName = alternativeTranslation.SpellName,
                AlternativeName = alternativeTranslation.AlternativeName
            });
        }

        // Log
        log.Information($"{Class} - Loaded {alternativeTranslations.Count} alternative translations");
    }

    // ----------------------------
    // Save changes
    // ----------------------------
    private void SaveChanges()
    {
        // Clear persisted list
        config.AlternativeTranslations.Clear();

        // Save alternative translations into persisted list
        foreach (AlternativeTranslation alternativeTranslation in alternativeTranslations)
        {
            config.AlternativeTranslations.Add(new AlternativeTranslation
            {
                SpellName = alternativeTranslation.SpellName,
                AlternativeName = alternativeTranslation.AlternativeName
            });
        }

        // Save configuration
        config.Save();

        // Log
        string message = $"{alternativeTranslations.Count} alternative translations saved";
        SetInformation(message);
        log.Information($"{Class} - {message}");
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
        if (alternativeTranslations.Count != config.AlternativeTranslations.Count) return true;

        // Check for differences in memory vs persisted values
        for (int i = 0; i < alternativeTranslations.Count; i++)
        {
            AlternativeTranslation inMemory = alternativeTranslations[i];
            AlternativeTranslation saved = config.AlternativeTranslations[i];
            if (!string.Equals(inMemory.SpellName, saved.SpellName, StringComparison.Ordinal)) return true;
            if (!string.Equals(inMemory.AlternativeName, saved.AlternativeName, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    // ----------------------------
    // Has invalid entries
    // ----------------------------
    private bool HasInvalidEntries()
    {
        // Check for invalid values in alternative translations
        foreach (AlternativeTranslation alternativeTranslation in alternativeTranslations)
        {
            if (IsInvalidValue(alternativeTranslation.SpellName) || IsInvalidValue(alternativeTranslation.AlternativeName)) return true;
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
        foreach (AlternativeTranslation alternativeTranslation in alternativeTranslations)
        {
            if (string.IsNullOrWhiteSpace(alternativeTranslation.SpellName) && string.IsNullOrWhiteSpace(alternativeTranslation.AlternativeName)) return true;
        }
        return false;
    }

    // ----------------------------
    // Set information message
    // ----------------------------
    private void SetInformation(string message)
    {
        information = message;
        informationTimestamp = DateTime.Now;
    }

    // ----------------------------
    // Dispose
    // ----------------------------
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

}