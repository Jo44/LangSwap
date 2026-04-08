using Dalamud.Game;
using Dalamud.Plugin.Services;
using LangSwap.translation.@base;
using LangSwap.translation.model;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;

namespace LangSwap.translation;

// ----------------------------
// Excel Provider
// ----------------------------
public class ExcelProvider(Configuration config)
{
    // Log
    private const string Class = "[ExcelProvider.cs]";

    // Services
    private static IDataManager DataManager => Plugin.DataManager;
    private static IPluginLog Log => Plugin.Log;

    //
    // ========== BASE PARAMS ==========
    //

    // ----------------------------
    // Get base param by name
    // ----------------------------
    public static string? GetBaseParamName(string paramName, Language clientLang, Language targetLang)
    {
        try
        {
            // Get the BaseParam sheet for the client language
            ExcelSheet<BaseParam> clientSheet = DataManager.GetExcelSheet<BaseParam>(EnumToClientLang(clientLang));
            if (clientSheet == null)
            {
                Log.Warning($"{Class} - BaseParam sheet is not available for language {clientLang}");
                return null;
            }

            // Find the row ID matching the client language name
            uint? rowID = null;
            foreach (BaseParam row in clientSheet)
            {
                if (row.Name.ToString().Equals(paramName, StringComparison.OrdinalIgnoreCase))
                {
                    rowID = row.RowId;
                    break;
                }
            }

            // Check if row ID is found
            if (rowID == null) return null;

            // Get the BaseParam sheet for the target language
            ExcelSheet<BaseParam> targetSheet = DataManager.GetExcelSheet<BaseParam>(EnumToClientLang(targetLang));

            // Try to get the translated name using the row ID
            if (targetSheet != null && targetSheet.TryGetRow(rowID.Value, out BaseParam translatedParam)) return translatedParam.Name.ToString();

            // No match found
            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Exception while getting BaseParam {paramName} from {clientLang} to {targetLang}");
            return null;
        }
    }

    //
    // ========== ACTIONS ==========
    //

    // ----------------------------
    // Get action
    // ----------------------------
    private Lumina.Excel.Sheets.Action? GetAction(uint actionID, Language targetLang)
    {
        try
        {
            // Validate action ID range
            if (actionID < 1 || actionID > config.MaxValidActionID)
            {
                Log.Warning($"{Class} - Action ID {actionID} is out of valid range (1-{config.MaxValidActionID})");
                return null;
            }

            // Get the Action sheet for the target language
            ExcelSheet<Lumina.Excel.Sheets.Action> actionSheet = DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>(EnumToClientLang(targetLang));
            if (actionSheet == null)
            {
                Log.Warning($"{Class} - Action sheet is not available for language {targetLang}");
                return null;
            }

            // Try to get the action row
            if (!actionSheet.TryGetRow(actionID, out Lumina.Excel.Sheets.Action action))
            {
                Log.Warning($"{Class} - Action {actionID} not found in sheet for language {targetLang}");
                return null;
            }

            // Return the found action
            return action;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Exception while getting action {actionID} in language {targetLang}");
            return null;
        }
    }

    // ----------------------------
    // Get action transient (for description)
    // ----------------------------
    private Lumina.Excel.Sheets.ActionTransient? GetActionTransient(uint actionID, Language targetLang)
    {
        try
        {
            // Validate action ID range
            if (actionID < 1 || actionID > config.MaxValidActionID)
            {
                Log.Warning($"{Class} - Action transient ID {actionID} is out of valid range (1-{config.MaxValidActionID})");
                return null;
            }

            // Get the ActionTransient sheet for the target language
            ExcelSheet<ActionTransient> actionTransientSheet = DataManager.GetExcelSheet<ActionTransient>(EnumToClientLang(targetLang));
            if (actionTransientSheet == null)
            {
                Log.Warning($"{Class} - Action transient sheet is not available for language {targetLang}");
                return null;
            }

            // Try to get the action row
            if (!actionTransientSheet.TryGetRow(actionID, out Lumina.Excel.Sheets.ActionTransient action))
            {
                Log.Warning($"{Class} - Action transient {actionID} not found in sheet for language {targetLang}");
                return null;
            }

            // Return the found action
            return action;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Exception while getting action transient {actionID} in language {targetLang}");
            return null;
        }
    }

    // ----------------------------
    // Get action name
    // ----------------------------
    public string? GetActionName(uint actionID, Language targetLang)
    {
        // Get the action
        Lumina.Excel.Sheets.Action? action = GetAction(actionID, targetLang);
        if (action == null) return null;

        // Return the action name
        return action.Value.Name.ToString();
    }

    // ----------------------------
    // Get action description
    // ----------------------------
    public string? GetActionDescription(uint actionID, Language targetLang)
    {
        // Get the action transient
        ActionTransient? actionTransient = GetActionTransient(actionID, targetLang);
        if (actionTransient == null) return null;

        // Return the action description
        return actionTransient.Value.Description.ToString();
    }

    // ----------------------------
    // Get action ID by name (reverse lookup)
    // ----------------------------
    public static uint? GetActionIDByName(string actionName, Language clientLang)
        => GetIDByName<Lumina.Excel.Sheets.Action>(actionName, clientLang, action => action.Name.ToString());

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
            ExcelSheet<Lumina.Excel.Sheets.Action> actionSheet = DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>(EnumToClientLang(Language.English));
            if (actionSheet != null)
            {
                // Loop through actions to find obfuscated ones
                foreach (Lumina.Excel.Sheets.Action action in actionSheet)
                {
                    // Get action ID
                    int actionID = (int)action.RowId;

                    // Get action name
                    string actionName = action.Name.ToString();

                    // Check if the name is obfuscated (starts with "_rsv_")
                    if (!string.IsNullOrWhiteSpace(actionName) && actionName.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal))
                    {
                        // Add to the set of obfuscated translations
                        obfuscatedTranslations.Add(new ObfuscatedTranslation { ID = actionID, ObfuscatedName = actionName });
                    }
                }
            }

            // Convert to list and sort by ID
            List<ObfuscatedTranslation> list = [.. obfuscatedTranslations];
            list.Sort((a, b) => a.ID.CompareTo(b.ID));

            // Return obfuscated translations
            return list;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Exception while getting all obfuscated actions");
            return [];
        }
    }

    //
    // ========== ITEMS ==========
    //

    // ----------------------------
    // Get item
    // ----------------------------
    private Item? GetItem(uint itemID, Language targetLang)
    {
        try
        {
            // Validate item ID range
            if (itemID < 1 || itemID > config.MaxValidItemID)
            {
                Log.Warning($"{Class} - Item ID {itemID} is out of valid range (1-{config.MaxValidItemID})");
                return null;
            }

            // // Get the Item sheet for the target language
            ExcelSheet<Item> itemSheet = DataManager.GetExcelSheet<Item>(EnumToClientLang(targetLang));
            if (itemSheet == null)
            {
                Log.Warning($"{Class} - Item sheet is not available for language {targetLang}");
                return null;
            }

            // Try to get the item row
            if (!itemSheet.TryGetRow(itemID, out Item item))
            {
                Log.Warning($"{Class} - Item {itemID} not found in sheet for language {targetLang}");
                return null;
            }

            // Return the found item
            return item;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Exception while getting item {itemID} in language {targetLang}");
            return null;
        }
    }

    // ----------------------------
    // Get item name
    // ----------------------------
    public string? GetItemName(uint itemID, Language targetLang)
    {
        // Get item
        Item? item = GetItem(itemID, targetLang);
        if (item == null) return null;

        // Return item name
        return item.Value.Name.ToString();
    }

    // ----------------------------
    // Get item description
    // ----------------------------
    public string? GetItemDescription(uint itemID, Language targetLang)
    {
        // Get item
        Item? item = GetItem(itemID, targetLang);
        if (item == null) return null;

        // Return item description
        return item.Value.Description.ToString();
    }

    // ----------------------------
    // Get item ID by name (reverse lookup)
    // ----------------------------
    public static uint? GetItemIDByName(string itemName, Language clientLang)
        => GetIDByName<Item>(itemName, clientLang, item => item.Name.ToString());

    //
    // ========== GLOBAL ==========
    //

    // ----------------------------
    // Get row ID by name (reverse lookup)
    // ---------------------------
    private static uint? GetIDByName<TSheet>(string name, Language clientLang, Func<TSheet, string> getName)
        where TSheet : struct, IExcelRow<TSheet>
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(name)) return null;

            // Get the sheet for the specified language
            ExcelSheet<TSheet> sheet = DataManager.GetExcelSheet<TSheet>(EnumToClientLang(clientLang));
            if (sheet == null)
            {
                Log.Warning($"{Class} - Sheet {typeof(TSheet).Name} is not available for language {clientLang}");
                return null;
            }

            // Search through rows for matching name
            foreach (TSheet row in sheet)
            {
                // Skip row 0 (invalid)
                if (row.RowId == 0) continue;

                // Compare names (case-insensitive)
                if (string.Equals(name.Trim(), getName(row).Trim(), StringComparison.OrdinalIgnoreCase)) return row.RowId;
            }

            // No match found
            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Failed to get {typeof(TSheet).Name} ID for name {name} in language {clientLang}");
            return null;
        }
    }

    // ----------------------------
    // Convert Language to ClientLanguage
    // ----------------------------
    private static ClientLanguage EnumToClientLang(Language lang)
    {
        // Map Language to ClientLanguage
        return lang switch
        {
            Language.Japanese => ClientLanguage.Japanese,
            Language.English => ClientLanguage.English,
            Language.German => ClientLanguage.German,
            Language.French => ClientLanguage.French,
            _ => ClientLanguage.English
        };
    }

}