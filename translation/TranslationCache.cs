using Dalamud.Plugin.Services;
using System.Collections.Generic;

namespace LangSwap.translation;

// ----------------------------
// Cache for translated datas
// ----------------------------
public class TranslationCache(ExcelProvider excelProvider, IPluginLog log)
{    
    // Item caches
    private readonly Dictionary<(uint, LanguageEnum), string?> itemNameCache = [];
    private readonly Dictionary<(uint, LanguageEnum), string?> itemEffectsCache = [];
    private readonly Dictionary<(uint, LanguageEnum), string?> itemDescriptionCache = [];
    
    // Action caches
    private readonly Dictionary<(uint, LanguageEnum), string?> actionNameCache = [];
    private readonly Dictionary<(uint, LanguageEnum), string?> actionDescriptionCache = [];

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
    // Get item effects
    // ----------------------------
    public string? GetItemEffects(uint itemId, LanguageEnum language)
    {
        // Create cache key
        (uint, LanguageEnum) key = (itemId, language);

        // Check cache
        if (itemEffectsCache.TryGetValue(key, out string? cachedEffects))
        {
            return cachedEffects;
        }

        // Fetch from Excel and cache it
        string? effects = excelProvider.GetItemEffects(itemId, language);
        itemEffectsCache[key] = effects;

        // Log
        if (effects != null)
        {
            log.Debug($"Cached item effects {itemId} ({language}): {effects}");
        }

        // Return effects
        return effects;
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

    // ----------------------------
    // Clear all caches
    // ----------------------------
    public void Clear()
    {
        itemNameCache.Clear();
        itemDescriptionCache.Clear();
        actionNameCache.Clear();
        actionDescriptionCache.Clear();
    }

}
