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

    // Get an item by its row ID in a specific language
    private Item? GetItem(uint itemId, LanguageEnum language)
    {
        try
        {
            // Validate item ID range
            if (itemId < 1 || itemId > configuration.MaxValidItemId)
            {
                log.Verbose($"Item ID {itemId} is out of valid range (1-{configuration.MaxValidItemId})");
                return null;
            }

            // Convert LanguageEnum to ClientLanguage
            var clientLanguage = LanguageEnumToClientLanguage(language);

            // Load the Item sheet for the specified language
            var itemSheet = dataManager.GetExcelSheet<Item>(clientLanguage);
            if (itemSheet == null)
            {
                log.Warning($"Item sheet is not available for language {language}");
                return null;
            }

            // Retrieve the item (TryGetRow is safer than GetRow)
            if (!itemSheet.TryGetRow(itemId, out var item))
            {
                log.Verbose($"Item {itemId} not found in sheet for language {language}");
                return null;
            }

            return item;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            log.Debug($"ArgumentOutOfRangeException for item {itemId} in language {language}: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Exception while getting item {itemId} in language {language}");
            return null;
        }
    }

    // Get item name in a specific language
    public string? GetItemName(uint itemId, LanguageEnum language)
    {
        try
        {
            // Get the item from the sheet in the specified language
            var item = GetItem(itemId, language);
            if (item == null) return null;

            // Access item name via Value property and convert SeString to string
            // Try Singular first (most common), fallback to Name
            string? itemName = null;
            
            try
            {
                itemName = item.Value.Singular.ToString();
            }
            catch
            {
                // If Singular doesn't exist or fails, try Name
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

    // Get item description in a specific language
    public string? GetItemDescription(uint itemId, LanguageEnum language)
    {
        try
        {
            // Get the item from the sheet in the specified language
            Item? item = GetItem(itemId, language);
            if (item == null) return null;

            // Access item description via Value property
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

}
