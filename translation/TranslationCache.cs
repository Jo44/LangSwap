using System;
using System.Collections.Generic;

namespace LangSwap.translation;

// ----------------------------
// Cache for translated data
// ----------------------------
public class TranslationCache(ExcelProvider excelProvider)
{
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
    public string? GetBaseParamName(string paramName, LanguageEnum clientLang, LanguageEnum targetLang) =>
        GetOrCache(baseParamCache, (paramName, clientLang, targetLang), () => excelProvider.GetBaseParamName(paramName, clientLang, targetLang));

    //
    // ========== ACTIONS ==========
    //

    // ----------------------------
    // Get action name
    // ----------------------------
    public string? GetActionName(uint actionId, LanguageEnum targetLang) =>
        GetOrCache(actionNameCache, (actionId, targetLang), () => excelProvider.GetActionName(actionId, targetLang));

    // ----------------------------
    // Get action description
    // ----------------------------
    public string? GetActionDescription(uint actionId, LanguageEnum targetLang) =>
        GetOrCache(actionDescriptionCache, (actionId, targetLang), () => excelProvider.GetActionDescription(actionId, targetLang));

    // ----------------------------
    // Get action ID by name (reverse lookup)
    // ----------------------------
    public uint? GetActionIdByName(string actionName, LanguageEnum clientLang) =>
        GetOrCache(actionIdByNameCache, (actionName, clientLang), () => excelProvider.GetActionIdByName(actionName, clientLang));

    //
    // ========== ITEMS ==========
    //

    // ----------------------------
    // Get item name
    // ----------------------------
    public string? GetItemName(uint itemId, LanguageEnum targetLang) =>
         GetOrCache(itemNameCache, (itemId, targetLang), () => excelProvider.GetItemName(itemId, targetLang));

    // ----------------------------
    // Get item description
    // ----------------------------
    public string? GetItemDescription(uint itemId, LanguageEnum targetLang) =>
        GetOrCache(itemDescriptionCache, (itemId, targetLang), () => excelProvider.GetItemDescription(itemId, targetLang));

    // ----------------------------
    // Get item ID by name (reverse lookup)
    // ----------------------------
    public uint? GetItemIdByName(string itemName, LanguageEnum clientLang) =>
        GetOrCache(itemIdByNameCache, (itemName, clientLang), () => excelProvider.GetItemIdByName(itemName, clientLang));

    //
    // ========== GLOBAL ==========
    //

    // ----------------------------
    // Get or cache value helper
    // ----------------------------
    private static TValue? GetOrCache<TKey, TValue>(Dictionary<TKey, TValue?> cache, TKey key, Func<TValue?> fetch) where TKey : notnull
    {
        if (cache.TryGetValue(key, out TValue? val)) return val;
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