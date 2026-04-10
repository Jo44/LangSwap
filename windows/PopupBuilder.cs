using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace LangSwap.windows;

// ----------------------------
// Popup Builder
// ----------------------------
public static class PopupBuilder
{
    // ----------------------------
    // Draw export CSV popup
    // ----------------------------
    public static void DrawExportCSVPopup(string popup, string inputID, ref string csv, Vector2 windowSize, Action onCopy)
    {
        // Define window size
        ImGui.SetNextWindowSize(windowSize, ImGuiCond.Appearing);
        
        // Begin popup
        if (!ImGui.BeginPopupModal(popup, ImGuiWindowFlags.NoResize)) return;

        // CSV output
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(15f);
        ImGui.InputTextMultiline(inputID, ref csv, 32768, new Vector2(windowSize.X - 30f, windowSize.Y - 85f), ImGuiInputTextFlags.ReadOnly);
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        // Copy button
        ImGui.SameLine(15f);
        if (ImGui.Button("Copy", new Vector2((windowSize.X - 45f) / 2, 0f)))
        {
            // Validate CSV
            if (string.IsNullOrWhiteSpace(csv)) return;

            // Copy to clipboard
            ImGui.SetClipboardText(csv);

            // Invoke callback
            onCopy();

            // Close popup
            ImGui.CloseCurrentPopup();
        }

        // Close button
        ImGui.SameLine(0, 15f);
        if (ImGui.Button("Close", new Vector2((windowSize.X - 45f) / 2, 0f)))
        {
            // Close popup
            ImGui.CloseCurrentPopup();
        }

        // End popup
        ImGui.Spacing();
        ImGui.EndPopup();
    }

    // ----------------------------
    // Draw import CSV popup
    // ----------------------------
    public static void DrawImportCSVPopup(string popup, string inputID, ref string csv, ref string status, Vector2 windowSize, Func<string, string> onApply)
    {
        // Define window size
        ImGui.SetNextWindowSize(windowSize, ImGuiCond.Appearing);

        // Begin popup
        if (!ImGui.BeginPopupModal(popup, ImGuiWindowFlags.NoResize)) return;

        // Status message
        ImGui.Spacing();
        ImGui.Spacing();
        if (!string.IsNullOrWhiteSpace(status))
        {
            // Show status message
            ImGui.SameLine(15f);
            ImGui.TextWrapped(status);
        }
        else
        {
            // Show instruction
            ImGui.SameLine(15f);
            ImGui.TextWrapped("Paste CSV data here");
        }

        // CSV input
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(15f);

        // Set focus on input
        if (ImGui.IsWindowAppearing()) ImGui.SetKeyboardFocusHere();

        ImGui.InputTextMultiline(inputID, ref csv, 32768, new Vector2(windowSize.X - 30f, windowSize.Y - 110f));
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        // Apply button
        ImGui.SameLine(15f);
        if (ImGui.Button("Apply", new Vector2((windowSize.X - 45f) / 2, 0f)))
        {
            // Apply CSV and get status message
            string applyStatus = onApply(csv);
            if (string.IsNullOrWhiteSpace(applyStatus))
            {
                // Success - close popup
                status = string.Empty;
                ImGui.CloseCurrentPopup();
            }
            else
            {
                // Failure - show status message
                status = applyStatus;
            }
        }

        // Close button
        ImGui.SameLine(0, 15f);
        if (ImGui.Button("Close", new Vector2((windowSize.X - 45f) / 2, 0f)))
        {
            // Clear status and close popup
            status = string.Empty;
            ImGui.CloseCurrentPopup();
        }

        // End popup
        ImGui.Spacing();
        ImGui.EndPopup();
    }

    // ----------------------------
    // Draw confirmation popup
    // ----------------------------
    public static void DrawConfirmationPopup(string popup, string message, string confirmLabel, string cancelLabel, Vector2 windowSize, Action onConfirm)
    {
        // Define window size
        ImGui.SetNextWindowSize(windowSize, ImGuiCond.Appearing);

        // Begin popup
        if (!ImGui.BeginPopupModal(popup, ImGuiWindowFlags.NoResize)) return;

        // Message
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SameLine(0, 30f);
        ImGui.TextWrapped(message);
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        // Confirm button
        ImGui.SameLine(0, 15f);
        if (ImGui.Button(confirmLabel, new Vector2((windowSize.X - 60f) / 2f, 0f)))
        {
            // Invoke callback
            onConfirm();

            // Close popup
            ImGui.CloseCurrentPopup();
        }

        // Cancel button
        ImGui.SameLine(0, 15f);
        if (ImGui.Button(cancelLabel, new Vector2((windowSize.X - 60f) / 2f, 0f)))
        {
            // Close popup
            ImGui.CloseCurrentPopup();
        }

        // End popup
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.EndPopup();
    }

}