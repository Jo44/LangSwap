using LangSwap.translation.@base;
using System;
using System.Collections.Generic;

namespace LangSwap.translation;

// ----------------------------
// Translation Cache
// ----------------------------
public class TranslationCache(ExcelProvider excelProvider)
{
    // Base param cache
    private readonly Dictionary<(string, Language, Language), string?> baseParamCache = [];

    // Action caches
    private readonly Dictionary<(uint, Language), string?> actionNameCache = [];
    private readonly Dictionary<(uint, Language), string?> actionDescriptionCache = [];
    private readonly Dictionary<(string, Language), uint?> actionIDByNameCache = [];

    // Item caches
    private readonly Dictionary<(uint, Language), string?> itemNameCache = [];
    private readonly Dictionary<(uint, Language), string?> itemDescriptionCache = [];
    private readonly Dictionary<(string, Language), uint?> itemIDByNameCache = [];

    //
    // ========== BASE PARAMS ==========
    //

    // ----------------------------
    // Get base param name
    // ----------------------------
    public string? GetBaseParamName(string paramName, Language clientLang, Language targetLang) =>
        GetOrCache(baseParamCache, (paramName, clientLang, targetLang), () => ExcelProvider.GetBaseParamName(paramName, clientLang, targetLang));

    //
    // ========== ACTIONS ==========
    //

    // ----------------------------
    // Get action name
    // ----------------------------
    public string? GetActionName(uint actionID, Language targetLang) =>
        GetOrCache(actionNameCache, (actionID, targetLang), () => excelProvider.GetActionName(actionID, targetLang));

    // ----------------------------
    // Get action description
    // ----------------------------
    public string? GetActionDescription(uint actionID, Language targetLang) =>
        GetOrCache(actionDescriptionCache, (actionID, targetLang), () => excelProvider.GetActionDescription(actionID, targetLang));

    // ----------------------------
    // Get action ID by name (reverse lookup)
    // ----------------------------
    public uint? GetActionIDByName(string actionName, Language clientLang) =>
        GetOrCache(actionIDByNameCache, (actionName, clientLang), () => ExcelProvider.GetActionIDByName(actionName, clientLang));

    //
    // ========== ITEMS ==========
    //

    // ----------------------------
    // Get item name
    // ----------------------------
    public string? GetItemName(uint itemID, Language targetLang) =>
         GetOrCache(itemNameCache, (itemID, targetLang), () => excelProvider.GetItemName(itemID, targetLang));

    // ----------------------------
    // Get item description
    // ----------------------------
    public string? GetItemDescription(uint itemID, Language targetLang) =>
        GetOrCache(itemDescriptionCache, (itemID, targetLang), () => excelProvider.GetItemDescription(itemID, targetLang));

    // ----------------------------
    // Get item ID by name (reverse lookup)
    // ----------------------------
    public uint? GetItemIDByName(string itemName, Language clientLang) =>
        GetOrCache(itemIDByNameCache, (itemName, clientLang), () => ExcelProvider.GetItemIDByName(itemName, clientLang));

    //
    // ========== GLOBAL ==========
    //

    // ----------------------------
    // Get or cache value
    // ----------------------------
    private static TValue? GetOrCache<TKey, TValue>(Dictionary<TKey, TValue?> cache, TKey key, Func<TValue?> fetch) where TKey : notnull
    {
        // Try to get from cache
        if (cache.TryGetValue(key, out TValue? value)) return value;

        // Fetch, cache, then return
        value = fetch();
        cache[key] = value;
        return value;
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
        actionIDByNameCache.Clear();

        // Clear item caches
        itemNameCache.Clear();
        itemDescriptionCache.Clear();
        itemIDByNameCache.Clear();
    }

}