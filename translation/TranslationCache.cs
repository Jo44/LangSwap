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
    private readonly Dictionary<(string, Language), uint?> actionIdByNameCache = [];

    // Item caches
    private readonly Dictionary<(uint, Language), string?> itemNameCache = [];
    private readonly Dictionary<(uint, Language), string?> itemDescriptionCache = [];
    private readonly Dictionary<(string, Language), uint?> itemIdByNameCache = [];

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
    public string? GetActionName(uint actionId, Language targetLang) =>
        GetOrCache(actionNameCache, (actionId, targetLang), () => excelProvider.GetActionName(actionId, targetLang));

    // ----------------------------
    // Get action description
    // ----------------------------
    public string? GetActionDescription(uint actionId, Language targetLang) =>
        GetOrCache(actionDescriptionCache, (actionId, targetLang), () => excelProvider.GetActionDescription(actionId, targetLang));

    // ----------------------------
    // Get action ID by name (reverse lookup)
    // ----------------------------
    public uint? GetActionIdByName(string actionName, Language clientLang) =>
        GetOrCache(actionIdByNameCache, (actionName, clientLang), () => ExcelProvider.GetActionIdByName(actionName, clientLang));

    //
    // ========== ITEMS ==========
    //

    // ----------------------------
    // Get item name
    // ----------------------------
    public string? GetItemName(uint itemId, Language targetLang) =>
         GetOrCache(itemNameCache, (itemId, targetLang), () => excelProvider.GetItemName(itemId, targetLang));

    // ----------------------------
    // Get item description
    // ----------------------------
    public string? GetItemDescription(uint itemId, Language targetLang) =>
        GetOrCache(itemDescriptionCache, (itemId, targetLang), () => excelProvider.GetItemDescription(itemId, targetLang));

    // ----------------------------
    // Get item ID by name (reverse lookup)
    // ----------------------------
    public uint? GetItemIdByName(string itemName, Language clientLang) =>
        GetOrCache(itemIdByNameCache, (itemName, clientLang), () => ExcelProvider.GetItemIdByName(itemName, clientLang));

    //
    // ========== GLOBAL ==========
    //

    // ----------------------------
    // Get or cache value
    // ----------------------------
    private static TValue? GetOrCache<TKey, TValue>(Dictionary<TKey, TValue?> cache, TKey key, Func<TValue?> fetch) where TKey : notnull
    {
        // Try to get from cache
        if (cache.TryGetValue(key, out TValue? val)) return val;

        // Fetch, cache, then return
        val = fetch();
        cache[key] = val;
        return val;
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