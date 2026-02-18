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
    // ========== ACTIONS ==========
    //

    // ----------------------------
    // Get action data
    // ----------------------------
    private Lumina.Excel.Sheets.Action? GetAction(uint actionId, LanguageEnum lang)
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
    public string? GetActionName(uint actionId, LanguageEnum lang)
        => GetActionProperty(actionId, lang, action => action.Name.ToString(), "name");

    // ----------------------------
    // Get action description
    // ----------------------------
    public string? GetActionDescription(uint actionId, LanguageEnum lang)
        => GetActionProperty(actionId, lang, action => action.Name.ToString(), "name");
    // TODO : get description

    //
    // ========== ITEMS ==========
    //

    // ----------------------------
    // Get item data
    // ----------------------------
    private Item? GetItem(uint itemId, LanguageEnum lang)
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
    public string? GetItemName(uint itemId, LanguageEnum lang)
        => GetItemProperty(itemId, lang, item => item.Name.ToString(), "name");

    // ----------------------------
    // Get item description
    // ----------------------------
    public string? GetItemDescription(uint itemId, LanguageEnum lang)
        => GetItemProperty(itemId, lang, item => item.Description.ToString(), "description");

}
