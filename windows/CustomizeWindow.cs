using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LangSwap.translation;
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
    private readonly List<AlternativeTranslation> workingTranslations = [];
    private bool focusNewRowSpell;
    private int focusNewRowIndex = -1;

    // Action cell colors
    private static readonly Vector4 ActionCellColor = new(0.37f, 0.15f, 0.14f, 1.00f);
    private static readonly Vector4 ActionCellHoverColor = new(0.42f, 0.23f, 0.23f, 1.00f);
    private static readonly Vector4 ActionCellActiveColor = new(0.35f, 0.08f, 0.08f, 1.00f);

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

        // Load persisted values
        LoadFromConfiguration();
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

        // Alternative translations list
        DrawTranslationList();

        // Export button
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        if (ImGui.Button("Export CSV", new Vector2(150f, 0f)))
        {
            // TODO: Implement export functionality
        }

        // Import button
        ImGui.SameLine(0, 15f);
        if (ImGui.Button("Import CSV", new Vector2(150f, 0f)))
        {
            // TODO: Implement import functionality
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

        // Draw reset confirmation popup
        DrawResetPopup();
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
        if (workingTranslations.Count != config.AlternativeTranslations.Count) return true;

        // Check for differences in spell names or alternative names
        for (int i = 0; i < workingTranslations.Count; i++)
        {
            AlternativeTranslation working = workingTranslations[i];
            AlternativeTranslation saved = config.AlternativeTranslations[i];
            if (!string.Equals(working.SpellName, saved.SpellName, StringComparison.Ordinal)) return true;
            if (!string.Equals(working.AlternativeName, saved.AlternativeName, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    // ----------------------------
    // Has invalid entries
    // ----------------------------
    private bool HasInvalidEntries()
    {
        // Check for invalid values in working translations
        foreach (AlternativeTranslation item in workingTranslations)
        {
            if (IsInvalidValue(item.SpellName) || IsInvalidValue(item.AlternativeName)) return true;
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
    // Draw translation list
    // ----------------------------
    private void DrawTranslationList()
    {
        // Constants for layout
        const float listHeight = 374f;
        const float tableWidth = 645f;
        const float rowHeight = 30f;

        // Alternative translations table
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 15f);
        ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY;
        if (ImGui.BeginTable("##AlternativeTranslationsTable", 4, tableFlags, new Vector2(tableWidth, listHeight)))
        {
            // Setup columns
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("##AddOrIndex", ImGuiTableColumnFlags.WidthFixed, 40f);
            ImGui.TableSetupColumn("Spell", ImGuiTableColumnFlags.WidthStretch, 1.0f);
            ImGui.TableSetupColumn("Replacement", ImGuiTableColumnFlags.WidthStretch, 1.0f);
            ImGui.TableSetupColumn("##Remove", ImGuiTableColumnFlags.WidthFixed, 60f);
            ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);

            // Add cell
            ImGui.TableSetColumnIndex(0);
            bool addClicked = DrawActionCell("##AddCell", "Add", rowHeight);
            if (addClicked && !HasPendingEmptyRow())
            {
                workingTranslations.Add(new AlternativeTranslation());
                focusNewRowIndex = workingTranslations.Count - 1;
                focusNewRowSpell = true;
            }

            // Spell
            ImGui.TableSetColumnIndex(1);
            DrawCenteredCellText("Spell", rowHeight);

            // Replacement
            ImGui.TableSetColumnIndex(2);
            DrawCenteredCellText("Replacement", rowHeight);

            // Remove
            ImGui.TableSetColumnIndex(3);
            DrawCenteredCellText(" ", rowHeight);

            // No entries message
            if (workingTranslations.Count == 0)
            {
                ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);
                ImGui.TableSetColumnIndex(0);
                DrawCenteredCellText(" ", rowHeight);
                ImGui.TableSetColumnIndex(1);
                DrawCenteredCellText("No alternative translations yet", rowHeight);
            }
            else
            {
                // Index to remove
                int removeIndex = -1;

                // Draw rows for each alternative translation
                for (int i = 0; i < workingTranslations.Count; i++)
                {
                    // Get current entry
                    AlternativeTranslation entry = workingTranslations[i];

                    // Start of row
                    ImGui.PushID(i);
                    ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);

                    // Index
                    ImGui.TableSetColumnIndex(0);
                    DrawCenteredCellText($"#{i + 1}", rowHeight);

                    // Spell name input
                    ImGui.TableSetColumnIndex(1);
                    string spellName = entry.SpellName;
                    bool setFocus = focusNewRowSpell && i == focusNewRowIndex;
                    DrawCenteredInputText("##Spell", ref spellName, 256, rowHeight, setFocus);
                    entry.SpellName = spellName;
                    if (setFocus)
                    {
                        ImGui.SetScrollHereY();
                        focusNewRowSpell = false;
                        focusNewRowIndex = -1;
                    }

                    // Alternative name input
                    ImGui.TableSetColumnIndex(2);
                    string alternativeName = entry.AlternativeName;
                    DrawCenteredInputText("##Replacement", ref alternativeName, 256, rowHeight);
                    entry.AlternativeName = alternativeName;

                    // Remove cell
                    ImGui.TableSetColumnIndex(3);
                    if (DrawActionCell($"##RemoveCell_{i}", "Remove", rowHeight))
                    {
                        removeIndex = i;
                    }

                    // End of row
                    ImGui.PopID();
                }

                if (removeIndex >= 0)
                {
                    workingTranslations.RemoveAt(removeIndex);
                }
            }

            ImGui.EndTable();
        }

        ImGui.Spacing();
        ImGui.Spacing();
    }

    // ----------------------------
    // Draw centered cell text
    // ----------------------------
    private static void DrawCenteredCellText(string text, float rowHeight)
    {
        Vector2 pos = ImGui.GetCursorScreenPos();
        float width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        Vector2 textSize = ImGui.CalcTextSize(text);
        Vector2 textPos = new(pos.X + (width - textSize.X) * 0.5f, pos.Y + (rowHeight - textSize.Y) * 0.5f);
        ImGui.GetWindowDrawList().AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), text);
        ImGui.Dummy(new Vector2(width, rowHeight));
    }

    // ----------------------------
    // Draw centered input text
    // ----------------------------
    private static void DrawCenteredInputText(string id, ref string value, int maxLength, float rowHeight, bool setFocus = false)
    {
        float width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        float y = ImGui.GetCursorPosY();
        float offsetY = MathF.Max(0f, (rowHeight - ImGui.GetFrameHeight()) * 0.5f);
        ImGui.SetCursorPosY(y + offsetY);
        if (setFocus) ImGui.SetKeyboardFocusHere();
        ImGui.SetNextItemWidth(width);
        ImGui.InputText(id, ref value, maxLength);
    }

    // ----------------------------
    // Draw action cell
    // ----------------------------
    private static bool DrawActionCell(string id, string label, float rowHeight)
    {
        Vector2 pos = ImGui.GetCursorScreenPos();
        Vector2 size = new(MathF.Max(1f, ImGui.GetContentRegionAvail().X), rowHeight);

        bool clicked = ImGui.InvisibleButton(id, size);

        Vector4 bg = ActionCellColor;
        if (ImGui.IsItemActive()) bg = ActionCellActiveColor;
        else if (ImGui.IsItemHovered()) bg = ActionCellHoverColor;

        float padX = ImGui.GetStyle().CellPadding.X;
        float padY = ImGui.GetStyle().CellPadding.Y;
        const float borderInset = 0.5f;
        Vector2 drawMin = new(pos.X - padX + borderInset, pos.Y - padY + borderInset);
        Vector2 drawMax = new(pos.X + size.X + padX, pos.Y + size.Y + padY - borderInset);

        uint bgColor = ImGui.GetColorU32(bg);
        ImGui.GetWindowDrawList().AddRectFilled(drawMin, drawMax, bgColor);

        Vector2 textSize = ImGui.CalcTextSize(label);
        Vector2 textPos = new(drawMin.X + (drawMax.X - drawMin.X - textSize.X) * 0.5f, drawMin.Y + (drawMax.Y - drawMin.Y - textSize.Y) * 0.5f);
        ImGui.GetWindowDrawList().AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), label);

        return clicked;
    }

    // ----------------------------
    // Draw reset popup
    // ----------------------------
    private void DrawResetPopup()
    {
        // Popup for reset confirmation
        if (!ImGui.BeginPopupModal("Confirm reset", ImGuiWindowFlags.AlwaysAutoResize)) return;

        // Message
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 30f);
        ImGui.TextWrapped("This will clear all alternative translations.   Are you sure ?");
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        // Confirm button
        if (ImGui.Button("Yes, reset all", new Vector2(200f, 0f)))
        {
            // Clear working values
            workingTranslations.Clear();

            // Clear persisted values
            config.AlternativeTranslations.Clear();

            // Save configuration
            config.Save();
            log.Information($"{Class} - All alternative translations cleared");

            // Close popup
            ImGui.CloseCurrentPopup();
        }

        // Cancel button
        ImGui.SameLine(0, 10f);
        if (ImGui.Button("Cancel", new Vector2(200f, 0f)))
        {
            // Close popup
            ImGui.CloseCurrentPopup();
        }

        // End popup
        ImGui.EndPopup();
    }

    // ----------------------------
    // Save changes
    // ----------------------------
    private void SaveChanges()
    {
        // Persist working values
        config.AlternativeTranslations.Clear();

        // Save working values into persisted list
        foreach (AlternativeTranslation item in workingTranslations)
        {
            config.AlternativeTranslations.Add(new AlternativeTranslation
            {
                SpellName = item.SpellName,
                AlternativeName = item.AlternativeName
            });
        }

        // Save configuration
        config.Save();

        // Log
        log.Information($"{Class} - Saved {workingTranslations.Count} alternative translations");
    }

    // ----------------------------
    // Has pending empty row
    // ----------------------------
    private bool HasPendingEmptyRow()
    {
        // Check if there is already an empty row in the working translations
        foreach (AlternativeTranslation item in workingTranslations)
        {
            if (string.IsNullOrWhiteSpace(item.SpellName) && string.IsNullOrWhiteSpace(item.AlternativeName)) return true;
        }
        return false;
    }

    // ----------------------------
    // Load from configuration
    // ----------------------------
    private void LoadFromConfiguration()
    {
        // Clear working list
        workingTranslations.Clear();

        // Load persisted values into working list
        foreach (AlternativeTranslation item in config.AlternativeTranslations)
        {
            workingTranslations.Add(new AlternativeTranslation
            {
                SpellName = item.SpellName,
                AlternativeName = item.AlternativeName
            });
        }

        // Log
        log.Information($"{Class} - Loaded {workingTranslations.Count} persisted alternative translations");
    }

    // ----------------------------
    // Dispose
    // ----------------------------
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

}