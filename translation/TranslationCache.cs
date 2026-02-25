using Dalamud.Plugin.Services;
using System.Collections.Generic;

namespace LangSwap.translation;

// ----------------------------
// Cache for translated datas
// ----------------------------
public class TranslationCache(ExcelProvider excelProvider, IPluginLog log)
{
    // Base param cache
    private readonly Dictionary<(string, LanguageEnum, LanguageEnum), string?> baseParamCache = [];

    // Action caches
    private readonly Dictionary<(uint, LanguageEnum), string?> actionNameCache = [];
    private readonly Dictionary<(uint, LanguageEnum), string?> actionDescriptionCache = [];

    // Item caches
    private readonly Dictionary<(uint, LanguageEnum), string?> itemNameCache = [];
    private readonly Dictionary<(uint, LanguageEnum), string?> itemDescriptionCache = [];
    private readonly Dictionary<(string, LanguageEnum), uint?> itemIdByNameCache = [];

    //
    // ========== BASE PARAMS ==========
    //

    // ----------------------------
    // Get base param name
    // ----------------------------
    public string? GetBaseParamName(string paramName, LanguageEnum clientLang, LanguageEnum targetLang)
    {
        // Create cache key
        (string, LanguageEnum, LanguageEnum) key = (paramName, clientLang, targetLang);

        // Check cache
        if (baseParamCache.TryGetValue(key, out string? cachedName))
        {
            return cachedName;
        }

        // Fetch from Excel and cache it
        string? name = excelProvider.GetBaseParamName(paramName, clientLang, targetLang);
        baseParamCache[key] = name;

        // Log
        if (name != null)
        {
            log.Debug($"Cached base param name {paramName} ({clientLang} -> {targetLang}): {name}");
        }

        // Return name
        return name;
    }

    //
    // ========== ACTIONS ==========
    //

    // ----------------------------
    // Get action name
    // ----------------------------
    public string? GetActionName(uint actionId, LanguageEnum lang)
    {
        // Create cache key
        (uint, LanguageEnum) key = (actionId, lang);

        // Check cache
        if (actionNameCache.TryGetValue(key, out string? cachedName))
        {
            return cachedName;
        }

        // Fetch from Excel and cache it
        string? name = excelProvider.GetActionName(actionId, lang);
        actionNameCache[key] = name;

        // Log
        if (name != null)
        {
            log.Debug($"Cached action name {actionId} ({lang}): {name}");
        }

        // Return name
        return name;
    }

    // ----------------------------
    // Get action description
    // ----------------------------
    public string? GetActionDescription(uint actionId, LanguageEnum lang)
    {
        // Create cache key
        (uint, LanguageEnum) key = (actionId, lang);

        // Check cache
        if (actionDescriptionCache.TryGetValue(key, out string? cachedDesc))
        {
            return cachedDesc;
        }

        // Fetch from Excel and cache it
        string? description = excelProvider.GetActionDescription(actionId, lang);
        actionDescriptionCache[key] = description;

        // Log
        if (description != null)
        {
            log.Debug($"Cached action description {actionId} ({lang}): {description}");
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
    public string? GetItemDescription(uint itemId, LanguageEnum lang)
    {
        // Create cache key
        (uint, LanguageEnum) key = (itemId, lang);

        // Check cache
        if (itemDescriptionCache.TryGetValue(key, out string? cachedDesc))
        {
            return cachedDesc;
        }

        // Fetch from Excel and cache it
        string? description = excelProvider.GetItemDescription(itemId, lang);
        itemDescriptionCache[key] = description;

        // Log
        if (description != null)
        {
            log.Debug($"Cached item description {itemId} ({lang}): {description}");
        }

        // Return description
        return description;
    }

    // ----------------------------
    // Get item ID by name (reverse lookup)
    // ----------------------------
    public uint? GetItemIdByName(string itemName, LanguageEnum lang)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(itemName))
            return null;

        // Create cache key
        (string, LanguageEnum) key = (itemName.Trim(), lang);

        // Check cache
        if (itemIdByNameCache.TryGetValue(key, out uint? cachedId))
        {
            return cachedId;
        }

        // Fetch from Excel and cache it
        uint? itemId = excelProvider.GetItemIdByName(itemName, lang);
        itemIdByNameCache[key] = itemId;

        // Log
        if (itemId != null)
        {
            log.Debug($"Cached item ID lookup '{itemName}' ({lang}): {itemId}");
        }

        // Return item ID
        return itemId;
    }

    // ----------------------------
    // Clear all caches
    // ----------------------------
    public void Clear()
    {
        // Clear base param cache
        baseParamCache.Clear();
        // Clear action caches
        actionNameCache.Clear();
        actionDescriptionCache.Clear();
        // Clear item caches
        itemNameCache.Clear();
        itemDescriptionCache.Clear();
        itemIdByNameCache.Clear();
    }

}
