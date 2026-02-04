using Dalamud.Game;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;

namespace LangSwap.translation;

// ----------------------------
// Excel data provider for accessing game data via Lumina
// ----------------------------
public class ExcelProvider(Configuration config, IDataManager dataManager, IPluginLog log)
{
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

    //
    // ========== ITEMS ==========
    //

    // ----------------------------
    // Get item data from Excel sheet
    // ----------------------------
    public Item? GetItem(uint itemId, LanguageEnum lang)
    {
        try
        {
            // Validate item ID range
            if (itemId < 1 || itemId > config.MaxValidItemId)
            {
                log.Warning($"Item ID {itemId} is out of valid range (1-{config.MaxValidItemId})");
                return null;
            }

            // Access the Item sheet for the specified language
            ExcelSheet<Item> itemSheet = dataManager.GetExcelSheet<Item>(EnumToClientLang(lang));
            if (itemSheet == null)
            {
                log.Warning($"Item sheet is not available for language {lang}");
                return null;
            }

            // Try to get the item row
            if (!itemSheet.TryGetRow(itemId, out Item item))
            {
                log.Warning($"Item {itemId} not found in sheet for language {lang}");
                return null;
            }

            // Return the found item
            return item;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception while getting item {itemId} in language {lang}");
            return null;
        }
    }

    // ----------------------------
    // Get item name
    // ----------------------------
    public string? GetItemName(uint itemId, LanguageEnum lang)
    {
        try
        {
            // Get the item
            Item? item = GetItem(itemId, lang);
            if (item == null) return null;

            // Extract the item name
            string? name = null;
            try
            {
                name = item.Value.Name.ToString();
            }
            catch
            {
                log.Warning($"Could not extract name for item {itemId}");
                return null;
            }

            // Return the item name if valid
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception while getting item name for {itemId} in language {lang}");
            return null;
        }
    }

    // ----------------------------
    // Get item description
    // ----------------------------
    public string? GetItemDescription(uint itemId, LanguageEnum lang)
    {
        try
        {
            // Get the item
            Item? item = GetItem(itemId, lang);
            if (item == null) return null;

            // Extract the item description
            string? description = null;
            try
            {
                description = item.Value.Description.ToString();
            }
            catch
            {
                log.Warning($"Could not extract description for item {itemId}");
                return null;
            }

            // Return the item description if valid
            return string.IsNullOrWhiteSpace(description) ? null : description;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception while getting item description for {itemId} in language {lang}");
            return null;
        }
    }

    //
    // ========== ACTIONS ==========
    //

    // ----------------------------
    // Get action data from Excel sheet
    // ----------------------------
    public Lumina.Excel.Sheets.Action? GetAction(uint actionId, LanguageEnum lang)
    {
        try
        {
            // Validate action ID range
            if (actionId < 1 || actionId > config.MaxValidActionId)
            {
                log.Warning($"Action ID {actionId} is out of valid range (1-{config.MaxValidActionId})");
                return null;
            }

            // Access the Action sheet for the specified language
            ExcelSheet<Lumina.Excel.Sheets.Action> actionSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>(EnumToClientLang(lang));
            if (actionSheet == null)
            {
                log.Warning($"Action sheet is not available for language {lang}");
                return null;
            }

            // Try to get the action row
            if (!actionSheet.TryGetRow(actionId, out Lumina.Excel.Sheets.Action action))
            {
                log.Warning($"Action {actionId} not found in sheet for language {lang}");
                return null;
            }

            // Return the found action
            return action;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception while getting action {actionId} in language {lang}");
            return null;
        }
    }

    // ----------------------------
    // Get action name
    // ----------------------------
    public string? GetActionName(uint actionId, LanguageEnum lang)
    {
        try
        {
            // Get the action
            Lumina.Excel.Sheets.Action? action = GetAction(actionId, lang);
            if (action == null) return null;

            // Extract the action name
            string? actionName = null;
            try
            {
                actionName = action.Value.Name.ToString();
            }
            catch
            {
                log.Warning($"Could not extract name for action {actionId}");
                return null;
            }

            // Return the action name if valid
            return string.IsNullOrWhiteSpace(actionName) ? null : actionName;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception while getting action name for {actionId} in language {lang}");
            return null;
        }
    }

    // ----------------------------
    // Get action description
    // ----------------------------
    public string? GetActionDescription(uint actionId, LanguageEnum language)
    {
        // TODO: Implement action description retrieval
        return "<DESCRIPTION>";
    }

}
