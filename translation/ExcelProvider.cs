using Dalamud.Game;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;

namespace LangSwap.translation;

// Excel data provider for accessing game data (via Lumina)
public class ExcelProvider(IDataManager dataManager, IPluginLog log)
{
    // References
    private readonly IDataManager dataManager = dataManager;
    private readonly IPluginLog log = log;

    // Get item name in a specific language
    public string? GetItemName(uint itemId, LanguageEnum language)
    {
        try
        {
            // Get the item from the sheet in the specified language
            Item? item = GetItem(itemId, language);
            if (item == null) return null;

            // Get the Singular name
            string itemName = item.Value.Singular.ToString();
            
            // If Singular is empty, try Name as fallback
            if (string.IsNullOrWhiteSpace(itemName))
            {
                itemName = item.Value.Name.ToString();
            }

            return string.IsNullOrWhiteSpace(itemName) ? null : itemName;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception while getting item name for {itemId} in language {language}");
            return null;
        }
    }

    // Get an item by its row ID in a specific language
    private Item? GetItem(uint itemId, LanguageEnum language)
    {
        try
        {
            // Convert LanguageEnum to ClientLanguage
            ClientLanguage targetLanguage = LanguageEnumToClientLanguage(language);

            // Load the Item sheet for the specified language
            ExcelSheet<Item>? itemSheet = dataManager.GetExcelSheet<Item>(targetLanguage);
            if (itemSheet == null)
            {
                log.Warning($"Item sheet is not available for language {language}");
                return null;
            }

            // Retrieve the item
            Item item = itemSheet.GetRow(itemId);
            return item;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception while getting item {itemId} in language {language}");
            return null;
        }
    }

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

}
