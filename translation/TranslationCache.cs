using Dalamud.Plugin.Services;
using System.Collections.Generic;

namespace LangSwap.translation;

// Cache for translated item names and descriptions
public class TranslationCache(ExcelProvider excelProvider, IPluginLog log)
{
    // References
    private readonly ExcelProvider excelProvider = excelProvider;
    private readonly IPluginLog log = log;
    private readonly Dictionary<(uint, LanguageEnum), string?> nameCache = [];
    private readonly Dictionary<(uint, LanguageEnum), string?> descriptionCache = [];

    // Get item name
    public string? GetItemName(uint itemId, LanguageEnum language)
    {
        var key = (itemId, language);
        
        if (nameCache.TryGetValue(key, out var cachedName))
        {
            return cachedName;
        }

        // Fetch from excel provider
        var name = excelProvider.GetItemName(itemId, language);
        nameCache[key] = name;
        
        if (name != null)
        {
            log.Verbose($"Cached item name {itemId} ({language}): {name}");
        }
        
        return name;
    }

    // Get item description
    public string? GetItemDescription(uint itemId, LanguageEnum language)
    {
        var key = (itemId, language);
        
        if (descriptionCache.TryGetValue(key, out var cachedDesc))
        {
            return cachedDesc;
        }

        // Fetch from excel provider
        var description = excelProvider.GetItemDescription(itemId, language);
        descriptionCache[key] = description;
        
        if (description != null)
        {
            log.Verbose($"Cached item description {itemId} ({language}): {description}");
        }
        
        return description;
    }

    // Clear the cache
    public void Clear()
    {
        nameCache.Clear();
        descriptionCache.Clear();
        log.Debug("Translation cache cleared");
    }
}
