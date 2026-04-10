using LangSwap.translation.@base;
using System;
using System.Collections.Generic;

namespace LangSwap.translation;

// ----------------------------
// Translation Cache
// ----------------------------
public class TranslationCache(Configuration config, ExcelProvider excelProvider)
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
    private TValue? GetOrCache<TKey, TValue>(Dictionary<TKey, TValue?> cache, TKey key, Func<TValue?> fetch) where TKey : notnull
    {
        // Try to get from cache
        if (cache.TryGetValue(key, out TValue? value)) return value;

        // Fetch
        value = fetch();

        // Cache only if non-obfuscated
        if (value is string strValue && !strValue.StartsWith(config.ObfuscatedPrefix)) 
        {
            // Cache
            cache[key] = value;
        }

        // Return value
        return value;
    }

    // ----------------------------
    // Clear all caches
    // ----------------------------
    public uint ClearAll()
    {
        // Count cleared entries
        int count = 0;

        // Clear base param cache
        count += baseParamCache.Count;
        baseParamCache.Clear();

        // Clear action caches
        count += actionNameCache.Count;
        actionNameCache.Clear();
        count += actionDescriptionCache.Count;
        actionDescriptionCache.Clear();
        count += actionIDByNameCache.Count;
        actionIDByNameCache.Clear();

        // Clear item caches
        count += itemNameCache.Count;
        itemNameCache.Clear();
        count += itemDescriptionCache.Count;
        itemDescriptionCache.Clear();
        count += itemIDByNameCache.Count;
        itemIDByNameCache.Clear();

        // Return total cleared entries
        return (uint)count;
    }

}