using Dalamud.Game;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;

namespace LangSwap.translation;

// ----------------------------
// Excel data provider (for accessing game data via Lumina)
// ----------------------------
public class ExcelProvider(Configuration config, IDataManager dataManager, IPluginLog log)
{
    //
    // ========== BASE PARAMS ==========
    //

    // ----------------------------
    // Get base param name by name
    // ----------------------------
    public string? GetBaseParamName(string paramName, LanguageEnum clientLang, LanguageEnum targetLang)
    {
        try
        {
            // Access the BaseParam sheet for the client language
            ExcelSheet<BaseParam> clientSheet = dataManager.GetExcelSheet<BaseParam>(EnumToClientLang(clientLang));
            if (clientSheet == null)
            {
                log.Warning($"BaseParam sheet is not available for language {clientLang}");
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
            if (rowId == null)
            {
                return null;
            }

            // Get the translated name using the row ID in target language
            ExcelSheet<BaseParam> targetSheet = dataManager.GetExcelSheet<BaseParam>(EnumToClientLang(targetLang));
            if (targetSheet != null && targetSheet.TryGetRow(rowId.Value, out BaseParam translatedParam))
            {
                return translatedParam.Name.ToString();
            }

            return null;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception while getting BaseParam {paramName} from {clientLang} to {targetLang}");
            return null;
        }
    }

    //
    // ========== ACTIONS ==========
    //

    // ----------------------------
    // Get action data
    // ----------------------------
    private Lumina.Excel.Sheets.Action? GetAction(uint actionId, LanguageEnum targetLang)
    {
        try
        {
            // Validate action ID range
            if (actionId < 1 || actionId > config.MaxValidActionId)
            {
                log.Warning($"Action ID {actionId} is out of valid range (1-{config.MaxValidActionId})");
                return null;
            }

            // Access the action sheet for the specified language
            ExcelSheet<Lumina.Excel.Sheets.Action> actionSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>(EnumToClientLang(targetLang));
            if (actionSheet == null)
            {
                log.Warning($"Action sheet is not available for language {targetLang}");
                return null;
            }

            // Try to get the action row
            if (!actionSheet.TryGetRow(actionId, out Lumina.Excel.Sheets.Action action))
            {
                log.Warning($"Action {actionId} not found in sheet for language {targetLang}");
                return null;
            }

            // Return the found action
            return action;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception while getting action {actionId} in language {targetLang}");
            return null;
        }
    }

    // ----------------------------
    // Get action property
    // ----------------------------
    private T? GetActionProperty<T>(uint actionId, LanguageEnum lang, Func<Lumina.Excel.Sheets.Action, T?> propertySelector, string propertyName)
    {
        // Get the action
        Lumina.Excel.Sheets.Action? action = GetAction(actionId, lang);
        if (action == null) return default;

        // Extract the requested property using the provided selector
        try
        {
            return propertySelector(action.Value);
        }
        catch
        {
            log.Warning($"Could not extract {propertyName} for action {actionId}");
            return default;
        }
    }

    // ----------------------------
    // Get action name
    // ----------------------------
    public string? GetActionName(uint actionId, LanguageEnum targetLang)
        => GetActionProperty(actionId, targetLang, action => action.Name.ToString(), "name");

    // ----------------------------
    // Get action description
    // ----------------------------
    public string? GetActionDescription(uint actionId, LanguageEnum targetLang)
        => GetActionProperty(actionId, targetLang, action => action.Name.ToString(), "name");
    // TODO : get description

    // ----------------------------
    // Get action ID by name (reverse lookup)
    // ----------------------------
    public uint? GetActionIdByName(string actionName, LanguageEnum clientLang)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(actionName))
                return null;

            // Normalize the search name for comparison
            string normalizedSearchName = actionName.Trim();

            // Get the Item sheet for the specified language
            ExcelSheet<Lumina.Excel.Sheets.Action> actionSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>(EnumToClientLang(clientLang));
            if (actionSheet == null)
            {
                log.Warning($"Action sheet is not available for language {clientLang}");
                return null;
            }

            // Search through items for matching name
            foreach (Lumina.Excel.Sheets.Action action in actionSheet)
            {
                // Skip invalid items
                if (action.RowId == 0)
                    continue;

                // Get action name in the target language
                string actionNameInLang = action.Name.ToString() ?? string.Empty;

                // Compare names (case-insensitive)
                if (string.Equals(normalizedSearchName, actionNameInLang.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return action.RowId;
                }
            }

            // No match found
            return null;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to get action ID for name {actionName} in language {clientLang}");
            return null;
        }
    }

    //
    // ========== ITEMS ==========
    //

    // ----------------------------
    // Get item data
    // ----------------------------
    private Item? GetItem(uint itemId, LanguageEnum targetLang)
    {
        try
        {
            // Validate item ID range
            if (itemId < 1 || itemId > config.MaxValidItemId)
            {
                log.Warning($"Item ID {itemId} is out of valid range (1-{config.MaxValidItemId})");
                return null;
            }

            // Access the item sheet for the specified language
            ExcelSheet<Item> itemSheet = dataManager.GetExcelSheet<Item>(EnumToClientLang(targetLang));
            if (itemSheet == null)
            {
                log.Warning($"Item sheet is not available for language {targetLang}");
                return null;
            }

            // Try to get the item row
            if (!itemSheet.TryGetRow(itemId, out Item item))
            {
                log.Warning($"Item {itemId} not found in sheet for language {targetLang}");
                return null;
            }

            // Return the found item
            return item;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception while getting item {itemId} in language {targetLang}");
            return null;
        }
    }

    // ----------------------------
    // Get item property
    // ----------------------------
    private T? GetItemProperty<T>(uint itemId, LanguageEnum lang, Func<Item, T?> propertySelector, string propertyName)
    {
        // Get the item
        Item? item = GetItem(itemId, lang);
        if (item == null) return default;

        // Extract the requested property using the provided selector
        try
        {
            return propertySelector(item.Value);
        }
        catch
        {
            log.Warning($"Could not extract {propertyName} for item {itemId}");
            return default;
        }
    }

    // ----------------------------
    // Get item name
    // ----------------------------
    public string? GetItemName(uint itemId, LanguageEnum targetLang)
        => GetItemProperty(itemId, targetLang, item => item.Name.ToString(), "name");

    // ----------------------------
    // Get item description
    // ----------------------------
    public string? GetItemDescription(uint itemId, LanguageEnum targetLang)
        => GetItemProperty(itemId, targetLang, item => item.Description.ToString(), "description");

    // ----------------------------
    // Get item ID by name (reverse lookup)
    // ----------------------------
    public uint? GetItemIdByName(string itemName, LanguageEnum clientLang)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(itemName))
                return null;

            // Normalize the search name for comparison
            string normalizedSearchName = itemName.Trim();

            // Get the Item sheet for the specified language
            ExcelSheet<Item> itemSheet = dataManager.GetExcelSheet<Item>(EnumToClientLang(clientLang));
            if (itemSheet == null)
            {
                log.Warning($"Item sheet is not available for language {clientLang}");
                return null;
            }

            // Search through items for matching name
            foreach (Item item in itemSheet)
            {
                // Skip invalid items
                if (item.RowId == 0)
                    continue;

                // Get item name in the target language
                string itemNameInLang = item.Name.ToString() ?? string.Empty;

                // Compare names (case-insensitive)
                if (string.Equals(normalizedSearchName, itemNameInLang.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return item.RowId;
                }
            }

            // No match found
            return null;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to get item ID for name {itemName} in language {clientLang}");
            return null;
        }
    }

    // ----------------------------
    // Convert LanguageEnum to ClientLanguage
    // ----------------------------
    private static ClientLanguage EnumToClientLang(LanguageEnum lang)
    {
        // Map LanguageEnum to ClientLanguage
        return lang switch
        {
            LanguageEnum.Japanese => ClientLanguage.Japanese,
            LanguageEnum.English => ClientLanguage.English,
            LanguageEnum.German => ClientLanguage.German,
            LanguageEnum.French => ClientLanguage.French,
            _ => ClientLanguage.English
        };
    }
}
