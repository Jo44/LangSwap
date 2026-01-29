using Dalamud.Plugin.Services;
using System.Collections.Generic;

namespace LangSwap.translation;

// Cache for translated item names, descriptions and action data
public class TranslationCache(ExcelProvider excelProvider, IPluginLog log)
{
    // References
    private readonly ExcelProvider excelProvider = excelProvider;
    private readonly IPluginLog log = log;
    
    // Item caches
    private readonly Dictionary<(uint, LanguageEnum), string?> itemNameCache = [];
    private readonly Dictionary<(uint, LanguageEnum), string?> itemDescriptionCache = [];
    
    // Action caches
    private readonly Dictionary<(uint, LanguageEnum), string?> actionNameCache = [];
    private readonly Dictionary<(uint, LanguageEnum), string?> actionDescriptionCache = [];

    // ========== ITEMS ==========

    // Get item name
    public string? GetItemName(uint itemId, LanguageEnum language)
    {
        var key = (itemId, language);
        
        if (itemNameCache.TryGetValue(key, out var cachedName))
        {
            return cachedName;
        }

        var name = excelProvider.GetItemName(itemId, language);
        itemNameCache[key] = name;
        
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
        
        if (itemDescriptionCache.TryGetValue(key, out var cachedDesc))
        {
            return cachedDesc;
        }

        var description = excelProvider.GetItemDescription(itemId, language);
        itemDescriptionCache[key] = description;
        
        if (description != null)
        {
            log.Verbose($"Cached item description {itemId} ({language}): {description}");
        }
        
        return description;
    }

    // ========== ACTIONS ==========

    // Get action name
    public string? GetActionName(uint actionId, LanguageEnum language)
    {
        var key = (actionId, language);
        
        if (actionNameCache.TryGetValue(key, out var cachedName))
        {
            return cachedName;
        }

        var name = excelProvider.GetActionName(actionId, language);
        actionNameCache[key] = name;
        
        if (name != null)
        {
            log.Verbose($"Cached action name {actionId} ({language}): {name}");
        }
        
        return name;
    }

    // Get action description
    public string? GetActionDescription(uint actionId, LanguageEnum language)
    {
        var key = (actionId, language);
        
        if (actionDescriptionCache.TryGetValue(key, out var cachedDesc))
        {
            return cachedDesc;
        }

        var description = excelProvider.GetActionDescription(actionId, language);
        actionDescriptionCache[key] = description;
        
        if (description != null)
        {
            log.Verbose($"Cached action description {actionId} ({language}): {description}");
        }
        
        return description;
    }

    // Clear all caches
    public void Clear()
    {
        itemNameCache.Clear();
        itemDescriptionCache.Clear();
        actionNameCache.Clear();
        actionDescriptionCache.Clear();
        log.Debug("Translation cache cleared");
    }
}
