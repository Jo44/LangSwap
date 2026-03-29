using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.tool;
using LangSwap.translation;
using System;
using System.Collections.Generic;

namespace LangSwap.hook.@base;

// ----------------------------
// Base class for all castbars hooks
// ----------------------------
public unsafe abstract class CastBarsBaseHook(
    IAddonLifecycle addonLifecycle,
    Configuration config,
    IFramework framework,
    IObjectTable objectTable,
    ITargetManager targetManager,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : BaseHook(config, translationCache, utilities, log)
{
    // Log
    private const string Class = "[CastBarsBaseHook.cs]";

    // Core components
    protected IAddonLifecycle addonLifecycle = addonLifecycle;
    protected IFramework framework = framework;
    protected IObjectTable objectTable = objectTable;
    protected ITargetManager targetManager = targetManager;

    // Lists of casts
    protected readonly Dictionary<ulong, uint> listCasts = [];
    protected readonly Dictionary<ulong, long> listCastsExpiry = [];
    protected const long ListCastExpiryTicks = 30L * 10_000_000L; // 30 seconds

    // ----------------------------
    // Update cast bar
    // ----------------------------
    protected void UpdateCastBar(AtkUnitBase* addon, uint actionId, int fieldIndex, string addonName)
    {
        try
        {
            // Only update if language is swapped, we have a valid action ID and the addon is visible
            if (!isLanguageSwapped || actionId == 0 || addon == null || !addon -> IsVisible) return;

            // Get the text node
            AtkResNode* fieldNode = addon -> UldManager.NodeList[fieldIndex];
            if (fieldNode == null || fieldNode -> Type != NodeType.Text) return;

            // Update text
            AtkTextNode* textNode = (AtkTextNode*)fieldNode;
            if (textNode == null || textNode -> NodeText.Length == 0) return;

            // Get action visible name
            string visibleName = textNode -> NodeText.ToString();

            // Get action display name
            string? displayName = GetDisplayActionName(actionId, visibleName);
            if (displayName.IsNullOrWhitespace()) return;

            // Update the text node with the display name
            textNode -> SetText(displayName);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error updating {addonName} addon");
        }
    }

    // ----------------------------
    // Update list
    // ----------------------------
    protected void UpdateList(AtkUnitBase* addon, int listStartField, int listEndField, int castField)
    {
        try
        {
            // Only update if language is swapped, we have casts to translate and the addon is visible
            if (!isLanguageSwapped || (listCasts.Count < 1 && listCasts.Count < 1) || addon == null || !addon -> IsVisible) return;

            // Process each slot in the list
            for (int slotIndex = listStartField; slotIndex <= listEndField; slotIndex++)
            {
                ProcessSlot(addon, slotIndex, castField);
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error updating list addon");
        }
    }

    // ----------------------------
    // Process slot
    // ----------------------------
    protected void ProcessSlot(AtkUnitBase* addon, int slotIndex, int castField)
    {
        // Get the slot node
        AtkResNode* slotNode = addon -> UldManager.NodeList[slotIndex];
        if (slotNode == null || !slotNode -> IsVisible() || (ushort)slotNode -> Type < 1000) return;

        // Get the component node
        AtkComponentNode* componentNode = (AtkComponentNode*)slotNode;
        if (componentNode -> Component == null) return;

        // Get the uld manager
        AtkUldManager* uldManager = &componentNode -> Component -> UldManager;
        if (uldManager == null || uldManager -> NodeListCount == 0) return;

        // Get the field node
        AtkResNode* fieldNode = uldManager -> NodeList[castField];
        if (fieldNode == null || fieldNode -> Type != NodeType.Text) return;

        // Cast to text node
        AtkTextNode* textNode = (AtkTextNode*)fieldNode;
        if (textNode == null || textNode -> NodeText.Length == 0) return;

        // Translate the cast text
        TranslateCastText(textNode);
    }

    // ----------------------------
    // Check if member is in list
    // ----------------------------
    protected static bool IsInList(IBattleChara character, StatusFlags flag)
    {
        bool inList = false;
        try
        {
            if (character != null && character.StatusFlags.HasFlag(flag)) inList = true;
        }
        catch
        {
            inList = false;
        }
        return inList;
    }

    // ----------------------------
    // Get display action name
    // ----------------------------
    protected string? GetDisplayActionName(uint actionId, string visibleAddonName = "")
    {
        try
        {
            // Check for valid action ID
            if (actionId == 0) return null;

            // Get the client action name
            string? clientActionName = translationCache.GetActionName(actionId, config.ClientLanguage);
            if (clientActionName.IsNullOrWhitespace()) return null;

            // Check for obfuscated name
            if (clientActionName.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal))
            {
                // TODO : à voir si utile
                log.Information($"{Class} - Obfuscated cast detected for action {actionId}: {clientActionName} | Visible: {visibleAddonName}");
                SaveScannedObfuscatedTranslation((int)actionId, clientActionName, visibleAddonName);
            }

            // If target language is same as client language
            if (config.TargetLanguage == config.ClientLanguage)
            {
                // Check for alternative translation
                string? alternativeName = Utilities.GetAlternativeTranslation(clientActionName, config.AlternativeTranslations);
                if (!alternativeName.IsNullOrWhitespace() && !string.Equals(alternativeName, clientActionName, StringComparison.Ordinal))
                {
                    return alternativeName;
                }
                return null;
            }

            // Get the translated action name
            string? translatedName = translationCache.GetActionName(actionId, config.TargetLanguage);
            if (translatedName.IsNullOrWhitespace()) return null;

            // Resolve obfuscated translated name using known mappings
            if (translatedName.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal))
            {
                translatedName = GetObfuscatedTranslationName(translatedName, config.TargetLanguage);
                if (translatedName.IsNullOrWhitespace()) return null;
            }

            // Check for alternative translation
            string? alternativeTranslatedName = Utilities.GetAlternativeTranslation(translatedName, config.AlternativeTranslations);
            if (!alternativeTranslatedName.IsNullOrWhitespace() && !string.Equals(alternativeTranslatedName, translatedName, StringComparison.Ordinal))
            {
                return alternativeTranslatedName;
            }

            // Fallback to translated action name
            return translatedName;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error resolving cast display name");
            return null;
        }
    }

    // ----------------------------
    // Save scanned obfuscated translation
    // ----------------------------
    private void SaveScannedObfuscatedTranslation(int actionId, string obfuscatedName, string visibleAddonName)
    {
        try
        {
            if (actionId <= 0 || obfuscatedName.IsNullOrWhitespace() || !obfuscatedName.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal)) return;

            string[] textParts = utilities.RemoveTargetIndicator(visibleAddonName ?? string.Empty);
            string scannedName = Utilities.RemoveEllipsis(textParts[0]).Trim();
            if (scannedName.IsNullOrWhitespace() || scannedName.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal)) return;

            ObfuscatedTranslation? scannedTranslation = config.ScannedObfuscatedTranslations.FindLast(translation =>
                translation.Id == actionId &&
                string.Equals(translation.ObfuscatedName, obfuscatedName, StringComparison.Ordinal));

            bool created = false;
            if (scannedTranslation == null)
            {
                scannedTranslation = new ObfuscatedTranslation
                {
                    Id = actionId,
                    ObfuscatedName = obfuscatedName
                };
                config.ScannedObfuscatedTranslations.Add(scannedTranslation);
                created = true;
            }

            bool changed = false;
            switch (config.ClientLanguage)
            {
                case LanguageEnum.English:
                    if (!string.Equals(scannedTranslation.EnglishName, scannedName, StringComparison.Ordinal))
                    {
                        scannedTranslation.EnglishName = scannedName;
                        changed = true;
                    }
                    break;

                case LanguageEnum.French:
                    if (!string.Equals(scannedTranslation.FrenchName, scannedName, StringComparison.Ordinal))
                    {
                        scannedTranslation.FrenchName = scannedName;
                        changed = true;
                    }
                    break;

                case LanguageEnum.German:
                    if (!string.Equals(scannedTranslation.GermanName, scannedName, StringComparison.Ordinal))
                    {
                        scannedTranslation.GermanName = scannedName;
                        changed = true;
                    }
                    break;

                case LanguageEnum.Japanese:
                    if (!string.Equals(scannedTranslation.JapaneseName, scannedName, StringComparison.Ordinal))
                    {
                        scannedTranslation.JapaneseName = scannedName;
                        changed = true;
                    }
                    break;
            }

            if (created || changed)
            {
                config.Save();
                log.Information($"{Class} - Scanned obfuscated cast saved for action {actionId}");
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to save scanned obfuscated cast");
        }
    }

    // ----------------------------
    // Get obfuscated translation name for a language
    // ----------------------------
    protected string? GetObfuscatedTranslationName(string obfuscatedName, LanguageEnum language)
    {
        string? translation = FindObfuscatedTranslation(config.LocalObfuscatedTranslations, obfuscatedName, language);
        if (!translation.IsNullOrWhitespace()) return translation;

        translation = FindObfuscatedTranslation(config.ScannedObfuscatedTranslations, obfuscatedName, language);
        if (!translation.IsNullOrWhitespace()) return translation;

        return FindObfuscatedTranslation(config.RemoteObfuscatedTranslations, obfuscatedName, language);
    }

    // ----------------------------
    // Find obfuscated translation in a list
    // ----------------------------
    private static string? FindObfuscatedTranslation(List<ObfuscatedTranslation> translations, string obfuscatedName, LanguageEnum language)
    {
        if (translations == null || translations.Count == 0 || obfuscatedName.IsNullOrWhitespace()) return null;

        ObfuscatedTranslation? translation = translations.FindLast(entry =>
            !entry.ObfuscatedName.IsNullOrWhitespace() &&
            string.Equals(entry.ObfuscatedName, obfuscatedName, StringComparison.Ordinal));
        if (translation == null) return null;

        return language switch
        {
            LanguageEnum.English => translation.EnglishName,
            LanguageEnum.French => translation.FrenchName,
            LanguageEnum.German => translation.GermanName,
            LanguageEnum.Japanese => translation.JapaneseName,
            _ => null
        };
    }

    // ----------------------------
    // Translate cast text in the list slot
    // ----------------------------
    protected abstract void TranslateCastText(AtkTextNode* textNode);

}