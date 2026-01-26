using System.Collections.Generic;
using Dalamud.Plugin.Services;

namespace LangSwap.translation;

// Cache for storing item translations
public class TranslationCache(ExcelProvider excelProvider, IPluginLog log)
{
    // References
    private readonly Dictionary<uint, Dictionary<LanguageEnum, string?>> cache = [];
    private readonly ExcelProvider excelProvider = excelProvider;
    private readonly IPluginLog log = log;

    // Get item name in target language, using cache if available
    public string? GetItemName(uint itemId, LanguageEnum targetLanguage)
    {
        // Check cache first
        if (cache.TryGetValue(itemId, out Dictionary<LanguageEnum, string?>? languageCache))
        {
            if (languageCache.TryGetValue(targetLanguage, out string? cachedName))
            {
                return cachedName;
            }
        }

        // Get from Excel provider
        string? name = excelProvider.GetItemName(itemId, targetLanguage);

        // Initialize language cache (if not present)
        if (!cache.TryGetValue(itemId, out Dictionary<LanguageEnum, string?>? value))
        {
            value = [];
            cache[itemId] = value;
        }

        // Update the cache with the new translation
        value[targetLanguage] = name;

        return name;
    }

    // Clear the cache
    public void Clear()
    {
        cache.Clear();
        log.Debug("Translation cache cleared");
    }

    // Get cache statistics
    public int GetCacheSize()
    {
        return cache.Count;
    }
}
