using Dalamud.Game;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;

namespace LangSwap.translation;

// Excel data provider for accessing game data via Lumina
public class ExcelProvider(Configuration configuration, IDataManager dataManager, IPluginLog log)
{
    // References
    private readonly IDataManager dataManager = dataManager;
    private readonly IPluginLog log = log;
    private readonly Configuration configuration = configuration;

    // Convert LanguageEnum to ClientLanguage
    private static ClientLanguage LanguageEnumToClientLanguage(LanguageEnum language)
    {
        return language switch
        {
            LanguageEnum.Japanese => ClientLanguage.Japanese,
            LanguageEnum.English => ClientLanguage.English,
            LanguageEnum.German => ClientLanguage.German,
            LanguageEnum.French => ClientLanguage.French,
            _ => ClientLanguage.English
        };
    }

    // ========== ITEMS ==========

    public Item? GetItem(uint itemId, LanguageEnum language)
    {
        try
        {
            if (itemId < 1 || itemId > configuration.MaxValidItemId)
            {
                log.Verbose($"Item ID {itemId} is out of valid range (1-{configuration.MaxValidItemId})");
                return null;
            }

            var clientLanguage = LanguageEnumToClientLanguage(language);
            var itemSheet = dataManager.GetExcelSheet<Item>(clientLanguage);
            if (itemSheet == null)
            {
                log.Warning($"Item sheet is not available for language {language}");
                return null;
            }

            if (!itemSheet.TryGetRow(itemId, out var item))
            {
                log.Verbose($"Item {itemId} not found in sheet for language {language}");
                return null;
            }

            return item;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception while getting item {itemId} in language {language}");
            return null;
        }
    }

    public string? GetItemName(uint itemId, LanguageEnum language)
    {
        try
        {
            var item = GetItem(itemId, language);
            if (item == null) return null;

            string? itemName = null;
            try
            {
                itemName = item.Value.Singular.ToString();
            }
            catch
            {
                try
                {
                    itemName = item.Value.Name.ToString();
                }
                catch
                {
                    log.Verbose($"Could not extract name for item {itemId}");
                    return null;
                }
            }

            return string.IsNullOrWhiteSpace(itemName) ? null : itemName;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception while getting item name for {itemId} in language {language}");
            return null;
        }
    }

    public string? GetItemDescription(uint itemId, LanguageEnum language)
    {
        try
        {
            var item = GetItem(itemId, language);
            if (item == null) return null;

            string? itemDescription = null;
            try
            {
                itemDescription = item.Value.Description.ToString();
            }
            catch
            {
                log.Verbose($"Could not extract description for item {itemId}");
                return null;
            }

            return string.IsNullOrWhiteSpace(itemDescription) ? null : itemDescription;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception while getting item description for {itemId} in language {language}");
            return null;
        }
    }

    // ========== ACTIONS ==========

    public Lumina.Excel.Sheets.Action? GetAction(uint actionId, LanguageEnum language)
    {
        try
        {
            if (actionId < 1 || actionId > configuration.MaxValidActionId)
            {
                log.Verbose($"Action ID {actionId} is out of valid range (1-{configuration.MaxValidActionId})");
                return null;
            }

            var clientLanguage = LanguageEnumToClientLanguage(language);
            var actionSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>(clientLanguage);
            if (actionSheet == null)
            {
                log.Warning($"Action sheet is not available for language {language}");
                return null;
            }

            if (!actionSheet.TryGetRow(actionId, out var action))
            {
                log.Verbose($"Action {actionId} not found in sheet for language {language}");
                return null;
            }

            return action;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception while getting action {actionId} in language {language}");
            return null;
        }
    }

    public string? GetActionName(uint actionId, LanguageEnum language)
    {
        try
        {
            var action = GetAction(actionId, language);
            if (action == null) return null;

            string? actionName = null;
            try
            {
                actionName = action.Value.Name.ToString();
            }
            catch (Exception ex)
            {
                log.Verbose($"Could not extract name for action {actionId}: {ex.Message}");
                return null;
            }

            return string.IsNullOrWhiteSpace(actionName) ? null : actionName;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception while getting action name for {actionId} in language {language}");
            return null;
        }
    }

    // Get action description in a specific language
    public string? GetActionDescription(uint actionId, LanguageEnum language)
    {
        try
        {
            // ActionTransient is a direct row type, not a RowRef wrapper
            var clientLanguage = LanguageEnumToClientLanguage(language);
            var actionTransientSheet = dataManager.GetExcelSheet<ActionTransient>(clientLanguage);
            
            if (actionTransientSheet == null)
            {
                log.Debug($"ActionTransient sheet is not available for language {language}");
                return null;
            }

            if (!actionTransientSheet.TryGetRow(actionId, out var actionTransient))
            {
                log.Verbose($"ActionTransient {actionId} not found in sheet for language {language}");
                return null;
            }

            try
            {
                // ActionTransient doesn't have .Value - access Description directly
                var description = actionTransient.Description.ToString();
                if (!string.IsNullOrWhiteSpace(description))
                {
                    return description;
                }
            }
            catch (Exception ex)
            {
                log.Debug($"Could not extract Description from ActionTransient for action {actionId}: {ex.Message}");
            }

            log.Verbose($"No description found for action {actionId}");
            return null;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception while getting action description for {actionId} in language {language}");
            return null;
        }
    }
}
