using Dalamud.Plugin.Services;
using LangSwap.tool;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;

namespace LangSwap.translation;

// ----------------------------
// Excel data provider (accessing game data via Lumina)
// ----------------------------
public class ExcelProvider(Configuration config, IDataManager dataManager, IPluginLog log)
{
    // Log
    private const string Class = "[ExcelProvider.cs]";

    //
    // ========== BASE PARAMS ==========
    //

    // ----------------------------
    // Get base param by name
    // ----------------------------
    public string? GetBaseParamName(string paramName, LanguageEnum clientLang, LanguageEnum targetLang)
    {
        try
        {
            // Access the BaseParam sheet for the client language
            ExcelSheet<BaseParam> clientSheet = dataManager.GetExcelSheet<BaseParam>(Utilities.EnumToClientLang(clientLang));
            if (clientSheet == null)
            {
                log.Warning($"{Class} - BaseParam sheet is not available for language {clientLang}");
                return null;
            }

            // Find the row ID matching the client language name
            uint? rowId = null;
            foreach (BaseParam row in clientSheet)
            {
                if (row.Name.ToString().Equals(paramName, StringComparison.OrdinalIgnoreCase))
                {
                    rowId = row.RowId;
                    break;
                }
            }

            // Check if row ID is found
            if (rowId == null) return null;

            // Get the translated name using the row ID in target language
            ExcelSheet<BaseParam> targetSheet = dataManager.GetExcelSheet<BaseParam>(Utilities.EnumToClientLang(targetLang));
            if (targetSheet != null && targetSheet.TryGetRow(rowId.Value, out BaseParam translatedParam)) return translatedParam.Name.ToString();

            // No match found
            return null;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Exception while getting BaseParam {paramName} from {clientLang} to {targetLang}");
            return null;
        }
    }

    //
    // ========== ACTIONS ==========
    //

    // ----------------------------
    // Get action
    // ----------------------------
    private Lumina.Excel.Sheets.Action? GetAction(uint actionId, LanguageEnum targetLang)
    {
        try
        {
            // Validate action ID range
            if (actionId < 1 || actionId > config.MaxValidActionId)
            {
                log.Warning($"{Class} - Action ID {actionId} is out of valid range (1-{config.MaxValidActionId})");
                return null;
            }

            // Access the action sheet for the specified language
            ExcelSheet<Lumina.Excel.Sheets.Action> actionSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>(Utilities.EnumToClientLang(targetLang));
            if (actionSheet == null)
            {
                log.Warning($"{Class} - Action sheet is not available for language {targetLang}");
                return null;
            }

            // Try to get the action row
            if (!actionSheet.TryGetRow(actionId, out Lumina.Excel.Sheets.Action action))
            {
                log.Warning($"{Class} - Action {actionId} not found in sheet for language {targetLang}");
                return null;
            }

            // Return the found action
            return action;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Exception while getting action {actionId} in language {targetLang}");
            return null;
        }
    }

    // ----------------------------
    // Get action transient
    // ----------------------------
    private Lumina.Excel.Sheets.ActionTransient? GetActionTransient(uint actionId, LanguageEnum targetLang)
    {
        try
        {
            // Validate action transient ID range
            if (actionId < 1 || actionId > config.MaxValidActionId)
            {
                log.Warning($"{Class} - Action transient ID {actionId} is out of valid range (1-{config.MaxValidActionId})");
                return null;
            }

            // Access the action transient sheet for the specified language
            ExcelSheet<Lumina.Excel.Sheets.ActionTransient> actionTransientSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.ActionTransient>(Utilities.EnumToClientLang(targetLang));
            if (actionTransientSheet == null)
            {
                log.Warning($"{Class} - Action transient sheet is not available for language {targetLang}");
                return null;
            }

            // Try to get the action transient row
            if (!actionTransientSheet.TryGetRow(actionId, out Lumina.Excel.Sheets.ActionTransient actionTransient))
            {
                log.Warning($"{Class} - Action transient {actionId} not found in sheet for language {targetLang}");
                return null;
            }

            // Return the found action transient
            return actionTransient;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Exception while getting action transient {actionId} in language {targetLang}");
            return null;
        }
    }

    // ----------------------------
    // Get action name
    // ----------------------------
    public string? GetActionName(uint actionId, LanguageEnum targetLang)
    {
        // Get the action
        Lumina.Excel.Sheets.Action? action = GetAction(actionId, targetLang);
        if (action == null) return default;

        // Return the action name
        return action.Value.Name.ToString();
    }

    // ----------------------------
    // Get action description
    // ----------------------------
    public string? GetActionDescription(uint actionId, LanguageEnum targetLang)
    {
        // Get the action transient
        Lumina.Excel.Sheets.ActionTransient? actionTransient = GetActionTransient(actionId, targetLang);
        if (actionTransient == null) return default;

        // Return the action description
        return actionTransient.Value.Description.ToString();
    }

    // ----------------------------
    // Get action ID by name (reverse lookup)
    // ----------------------------
    public uint? GetActionIdByName(string actionName, LanguageEnum clientLang)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(actionName)) return null;

            // Normalize the search name for comparison
            string normalizedSearchName = actionName.Trim();

            // Get the Item sheet for the specified language
            ExcelSheet<Lumina.Excel.Sheets.Action> actionSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>(Utilities.EnumToClientLang(clientLang));
            if (actionSheet == null)
            {
                log.Warning($"{Class} - Action sheet is not available for language {clientLang}");
                return null;
            }

            // Search through items for matching name
            foreach (Lumina.Excel.Sheets.Action action in actionSheet)
            {
                // Skip invalid items
                if (action.RowId == 0) continue;

                // Get action name in the target language
                string actionNameInLang = action.Name.ToString() ?? string.Empty;

                // Compare names (case-insensitive)
                if (string.Equals(normalizedSearchName, actionNameInLang.Trim(), StringComparison.OrdinalIgnoreCase)) return action.RowId;
            }

            // No match found
            return null;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to get action ID for name {actionName} in language {clientLang}");
            return null;
        }
    }

    //
    // ========== ITEMS ==========
    //

    // ----------------------------
    // Get item
    // ----------------------------
    private Item? GetItem(uint itemId, LanguageEnum targetLang)
    {
        try
        {
            // Validate item ID range
            if (itemId < 1 || itemId > config.MaxValidItemId)
            {
                log.Warning($"{Class} - Item ID {itemId} is out of valid range (1-{config.MaxValidItemId})");
                return null;
            }

            // Access the item sheet for the specified language
            ExcelSheet<Item> itemSheet = dataManager.GetExcelSheet<Item>(Utilities.EnumToClientLang(targetLang));
            if (itemSheet == null)
            {
                log.Warning($"{Class} - Item sheet is not available for language {targetLang}");
                return null;
            }

            // Try to get the item row
            if (!itemSheet.TryGetRow(itemId, out Item item))
            {
                log.Warning($"{Class} - Item {itemId} not found in sheet for language {targetLang}");
                return null;
            }

            // Return the found item
            return item;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Exception while getting item {itemId} in language {targetLang}");
            return null;
        }
    }

    // ----------------------------
    // Get item name
    // ----------------------------
    public string? GetItemName(uint itemId, LanguageEnum targetLang)
    {
        // Get item
        Item? item = GetItem(itemId, targetLang);
        if (item == null) return null;

        // Return item name
        return item.Value.Name.ToString();
    }

    // ----------------------------
    // Get item description
    // ----------------------------
    public string? GetItemDescription(uint itemId, LanguageEnum targetLang)
    {
        // Get item
        Item? item = GetItem(itemId, targetLang);
        if (item == null) return null;

        // Return item description
        return item.Value.Description.ToString();
    }

    // ----------------------------
    // Get item ID by name (reverse lookup)
    // ----------------------------
    public uint? GetItemIdByName(string itemName, LanguageEnum clientLang)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(itemName)) return null;

            // Normalize the search name for comparison
            string normalizedSearchName = itemName.Trim();

            // Get the Item sheet for the specified language
            ExcelSheet<Item> itemSheet = dataManager.GetExcelSheet<Item>(Utilities.EnumToClientLang(clientLang));
            if (itemSheet == null)
            {
                log.Warning($"{Class} - Item sheet is not available for language {clientLang}");
                return null;
            }

            // Search through items for matching name
            foreach (Item item in itemSheet)
            {
                // Skip invalid items
                if (item.RowId == 0) continue;

                // Get item name in the target language
                string itemNameInLang = item.Name.ToString() ?? string.Empty;

                // Compare names (case-insensitive)
                if (string.Equals(normalizedSearchName, itemNameInLang.Trim(), StringComparison.OrdinalIgnoreCase)) return item.RowId;
            }

            // No match found
            return null;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to get item ID for name {itemName} in language {clientLang}");
            return null;
        }
    }

}