using Dalamud.Plugin.Services;
using System.Collections.Generic;

namespace LangSwap.translation;

// ----------------------------
// Cache for translated datas
// ----------------------------
public class TranslationCache(ExcelProvider excelProvider, IPluginLog log)
{
    // Action caches
    private readonly Dictionary<(uint, LanguageEnum), string?> actionNameCache = [];
    private readonly Dictionary<(uint, LanguageEnum), string?> actionDescriptionCache = [];

    // Item caches
    private readonly Dictionary<(uint, LanguageEnum), string?> itemNameCache = [];
    private readonly Dictionary<(uint, LanguageEnum), string?> itemDescriptionCache = [];

    //
    // ========== ACTIONS ==========
    //

    // ----------------------------
    // Get action name
    // ----------------------------
    public string? GetActionName(uint actionId, LanguageEnum language)
    {
        // Create cache key
        (uint, LanguageEnum) key = (actionId, language);

        // Check cache
        if (actionNameCache.TryGetValue(key, out string? cachedName))
        {
            return cachedName;
        }

        // Fetch from Excel and cache it
        string? name = excelProvider.GetActionName(actionId, language);
        actionNameCache[key] = name;

        // Log
        if (name != null)
        {
            log.Debug($"Cached action name {actionId} ({language}): {name}");
        }

        // Return name
        return name;
    }

    // ----------------------------
    // Get action description
    // ----------------------------
    public string? GetActionDescription(uint actionId, LanguageEnum language)
    {
        // Create cache key
        (uint, LanguageEnum) key = (actionId, language);

        // Check cache
        if (actionDescriptionCache.TryGetValue(key, out string? cachedDesc))
        {
            return cachedDesc;
        }

        // Fetch from Excel and cache it
        string? description = excelProvider.GetActionDescription(actionId, language);
        actionDescriptionCache[key] = description;

        // Log
        if (description != null)
        {
            log.Debug($"Cached action description {actionId} ({language}): {description}");
        }

        // Return description
        return description;
    }

    //
    // ========== ITEMS ==========
    //

    // ----------------------------
    // Get item name
    // ----------------------------
    public string? GetItemName(uint itemId, LanguageEnum lang)
    {
        // Create cache key
        (uint, LanguageEnum) key = (itemId, lang);

        // Check cache
        if (itemNameCache.TryGetValue(key, out string? cachedName))
        {
            return cachedName;
        }

        // Fetch from Excel and cache it
        string? name = excelProvider.GetItemName(itemId, lang);
        itemNameCache[key] = name;

        // Log
        if (name != null)
        {
            log.Debug($"Cached item name {itemId} ({lang}): {name}");
        }

        // Return name
        return name;
    }

    // ----------------------------
    // Get item description
    // ----------------------------
    public string? GetItemDescription(uint itemId, LanguageEnum language)
    {
        // Create cache key
        (uint, LanguageEnum) key = (itemId, language);

        // Check cache
        if (itemDescriptionCache.TryGetValue(key, out string? cachedDesc))
        {
            return cachedDesc;
        }

        // Fetch from Excel and cache it
        string? description = excelProvider.GetItemDescription(itemId, language);
        itemDescriptionCache[key] = description;

        // Log
        if (description != null)
        {
            log.Debug($"Cached item description {itemId} ({language}): {description}");
        }

        // Return description
        return description;
    }

    // ----------------------------
    // Clear all caches
    // ----------------------------
    public void Clear()
    {
        // Clear action caches
        actionNameCache.Clear();
        actionDescriptionCache.Clear();
        // Clear item caches
        itemNameCache.Clear();
        itemDescriptionCache.Clear();
    }

}
