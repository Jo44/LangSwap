using Dalamud.Plugin.Services;
using System.Collections.Generic;

namespace LangSwap.translation;

// ----------------------------
// Cache for translated data
// ----------------------------
public class TranslationCache(ExcelProvider excelProvider, IPluginLog log)
{
    // Log
    private const string Class = "[TranslationCache.cs]";

    // Base param cache
    private readonly Dictionary<(string, LanguageEnum, LanguageEnum), string?> baseParamCache = [];

    // Action caches
    private readonly Dictionary<(uint, LanguageEnum), string?> actionNameCache = [];
    private readonly Dictionary<(uint, LanguageEnum), string?> actionDescriptionCache = [];
    private readonly Dictionary<(string, LanguageEnum), uint?> actionIdByNameCache = [];

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
        if (baseParamCache.TryGetValue(key, out string? cachedName)) return cachedName;

        // Fetch from Excel and cache it
        string? name = excelProvider.GetBaseParamName(paramName, clientLang, targetLang);
        baseParamCache[key] = name;

        // Log
        if (name != null) log.Debug($"{Class} - Cached base param name {paramName} ({clientLang} -> {targetLang}): {name}");

        // Return name
        return name;
    }

    //
    // ========== ACTIONS ==========
    //

    // ----------------------------
    // Get action name
    // ----------------------------
    public string? GetActionName(uint actionId, LanguageEnum targetLang)
    {
        // Create cache key
        (uint, LanguageEnum) key = (actionId, targetLang);

        // Check cache
        if (actionNameCache.TryGetValue(key, out string? cachedName)) return cachedName;

        // Fetch from Excel and cache it
        string? name = excelProvider.GetActionName(actionId, targetLang);
        actionNameCache[key] = name;

        // Log
        if (name != null) log.Debug($"{Class} - Cached action name {actionId} ({targetLang}): {name}");

        // Return name
        return name;
    }

    // ----------------------------
    // Get action description
    // ----------------------------
    public string? GetActionDescription(uint actionId, LanguageEnum targetLang)
    {
        // Create cache key
        (uint, LanguageEnum) key = (actionId, targetLang);

        // Check cache
        if (actionDescriptionCache.TryGetValue(key, out string? cachedDesc)) return cachedDesc;

        // Fetch from Excel and cache it
        string? description = excelProvider.GetActionDescription(actionId, targetLang);
        actionDescriptionCache[key] = description;

        // Log
        if (description != null) log.Debug($"{Class} - Cached action description {actionId} ({targetLang}): {description}");

        // Return description
        return description;
    }

    // ----------------------------
    // Get action ID by name (reverse lookup)
    // ----------------------------
    public uint? GetActionIdByName(string actionName, LanguageEnum clientLang)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(actionName)) return null;

        // Create cache key
        (string, LanguageEnum) key = (actionName.Trim(), clientLang);

        // Check cache
        if (actionIdByNameCache.TryGetValue(key, out uint? cachedId)) return cachedId;

        // Fetch from Excel and cache it
        uint? id = excelProvider.GetActionIdByName(actionName, clientLang);
        actionIdByNameCache[key] = id;

        // Log
        if (id != null) log.Debug($"{Class} - Cached action ID lookup '{actionName}' ({clientLang}): {id}");

        // Return ID
        return id;
    }

    //
    // ========== ITEMS ==========
    //

    // ----------------------------
    // Get item name
    // ----------------------------
    public string? GetItemName(uint itemId, LanguageEnum targetLang)
    {
        // Create cache key
        (uint, LanguageEnum) key = (itemId, targetLang);

        // Check cache
        if (itemNameCache.TryGetValue(key, out string? cachedName)) return cachedName;

        // Fetch from Excel and cache it
        string? name = excelProvider.GetItemName(itemId, targetLang);
        itemNameCache[key] = name;

        // Log
        if (name != null) log.Debug($"{Class} - Cached item name {itemId} ({targetLang}): {name}");

        // Return name
        return name;
    }

    // ----------------------------
    // Get item description
    // ----------------------------
    public string? GetItemDescription(uint itemId, LanguageEnum targetLang)
    {
        // Create cache key
        (uint, LanguageEnum) key = (itemId, targetLang);

        // Check cache
        if (itemDescriptionCache.TryGetValue(key, out string? cachedDesc)) return cachedDesc;

        // Fetch from Excel and cache it
        string? description = excelProvider.GetItemDescription(itemId, targetLang);
        itemDescriptionCache[key] = description;

        // Log
        if (description != null) log.Debug($"{Class} - Cached item description {itemId} ({targetLang}): {description}");

        // Return description
        return description;
    }

    // ----------------------------
    // Get item ID by name (reverse lookup)
    // ----------------------------
    public uint? GetItemIdByName(string itemName, LanguageEnum clientLang)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(itemName)) return null;

        // Create cache key
        (string, LanguageEnum) key = (itemName.Trim(), clientLang);

        // Check cache
        if (itemIdByNameCache.TryGetValue(key, out uint? cachedId)) return cachedId;

        // Fetch from Excel and cache it
        uint? id = excelProvider.GetItemIdByName(itemName, clientLang);
        itemIdByNameCache[key] = id;

        // Log
        if (id != null) log.Debug($"{Class} - Cached item ID lookup '{itemName}' ({clientLang}): {id}");

        // Return ID
        return id;
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
        actionIdByNameCache.Clear();

        // Clear item caches
        itemNameCache.Clear();
        itemDescriptionCache.Clear();
        itemIdByNameCache.Clear();
    }

}