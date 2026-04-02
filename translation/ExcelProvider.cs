using Dalamud.Plugin.Services;
using LangSwap.tool;
using LangSwap.translation.@base;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;

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
    // Get action transient (for description)
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
            ExcelSheet<ActionTransient> actionTransientSheet = dataManager.GetExcelSheet<ActionTransient>(Utilities.EnumToClientLang(targetLang));
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
    // Get all obfuscated actions
    // ----------------------------
    public List<ObfuscatedTranslation> GetAllObfuscatedActions()
    {
        try
        {
            // Initialize
            HashSet<ObfuscatedTranslation> obfuscatedTranslations = [];

            // Get the english action sheet
            ExcelSheet<Lumina.Excel.Sheets.Action> actionSheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>(Utilities.EnumToClientLang(LanguageEnum.English));
            if (actionSheet != null)
            {
                // Loop through actions to find obfuscated ones
                foreach (Lumina.Excel.Sheets.Action action in actionSheet)
                {
                    // Get action ID
                    int actionId = (int)action.RowId;

                    // Get action name
                    string actionName = action.Name.ToString();

                    // Check if the name is obfuscated (starts with "_rsv_")
                    if (!string.IsNullOrWhiteSpace(actionName) && actionName.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal))
                    {
                        // Add to the set of obfuscated translations
                        obfuscatedTranslations.Add(new ObfuscatedTranslation { Id = actionId, ObfuscatedName = actionName });
                    }
                }
            }

            // Convert to list and sort by ID
            List<ObfuscatedTranslation> result = [.. obfuscatedTranslations];
            result.Sort((a, b) => a.Id.CompareTo(b.Id));

            // Return obfuscated translations
            return result;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Exception while getting all obfuscated actions");
            return [];
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
        ActionTransient? actionTransient = GetActionTransient(actionId, targetLang);
        if (actionTransient == null) return default;

        // Return the action description
        return actionTransient.Value.Description.ToString();
    }

    // ----------------------------
    // Get action ID by name (reverse lookup)
    // ----------------------------
    public uint? GetActionIdByName(string actionName, LanguageEnum clientLang)
        => GetIdByName<Lumina.Excel.Sheets.Action>(actionName, clientLang, action => action.Name.ToString());

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
        => GetIdByName<Item>(itemName, clientLang, item => item.Name.ToString());

    //
    // ========== GLOBAL ==========
    //

    // ----------------------------
    // Get row ID by name (reverse lookup)
    // ---------------------------
    private uint? GetIdByName<TSheet>(string name, LanguageEnum clientLang, Func<TSheet, string> getName)
        where TSheet : struct, IExcelRow<TSheet>
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(name)) return null;

            // Normalize the search name for comparison
            string normalizedName = name.Trim();

            // Get the sheet for the specified language
            ExcelSheet<TSheet> sheet = dataManager.GetExcelSheet<TSheet>(Utilities.EnumToClientLang(clientLang));
            if (sheet == null)
            {
                log.Warning($"{Class} - Sheet {typeof(TSheet).Name} is not available for language {clientLang}");
                return null;
            }

            // Search through rows for matching name
            foreach (TSheet row in sheet)
            {
                // Skip row 0 (invalid)
                if (row.RowId == 0) continue;

                // Compare names (case-insensitive)
                if (string.Equals(normalizedName, getName(row).Trim(), StringComparison.OrdinalIgnoreCase)) return row.RowId;
            }

            // No match found
            return null;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to get {typeof(TSheet).Name} ID for name {name} in language {clientLang}");
            return null;
        }
    }

}