using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.hook.@base;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.translation.@base;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LangSwap.hook;

// ----------------------------
// Enemies CastBars Hook
// ----------------------------
public unsafe class EnemiesCastBarsHook(
    IAddonLifecycle addonLifecycle,
    Configuration config,
    IFramework framework,
    IObjectTable objectTable,
    ITargetManager targetManager,
    TranslationCache translationCache,
    Utilities utilities,
    IPluginLog log) : CastBarsBaseHook(addonLifecycle, config, framework, objectTable, targetManager, translationCache, utilities, log)
{
    // Log
    private const string Class = "[EnemiesCastBarsHook.cs]";

    // UI components
    private bool castBarsTarget = false;
    private bool castBarsFocus = false;
    private bool castBarsEnmityList = false;

    // Castbars fields
    private readonly int targetInfoField = config.TargetInfoField;
    private readonly int targetCastBarField = config.TargetCastBarField;
    private readonly int focusCastBarField = config.FocusCastBarField;
    private readonly int enemyListStartField = config.EnemyListStartField;
    private readonly int enemyListEndField = config.EnemyListEndField;
    private readonly int enemyListCastField = config.EnemyListCastField;

    // Action IDs
    private uint currentEnemyTargetActionId = 0;
    private uint currentEnemyFocusActionId = 0;

    // ----------------------------
    // Enable the hook
    // ----------------------------
    public override void Enable(string hookName)
    {
        // Prevent multiple enables
        if (isEnabled) return;

        try
        {
            // Subscribe to framework update
            framework.Update += OnFrameworkUpdate;

            // Register addon lifecycle listeners
            addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.EnemyListAddon, OnEnemyListUpdate);

            // Set enabled flag
            isEnabled = true;

            // Log
            log.Information($"{Class} - {hookName} hook enabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to enable {hookName} hook");
        }
    }

    // ----------------------------
    // On language swap
    // ----------------------------
    protected override void OnLanguageSwap()
    {
        // Initialize UI components
        castBarsTarget = config.EnemiesCastBarsTarget;
        castBarsFocus = config.EnemiesCastBarsFocus;
        castBarsEnmityList = config.EnemiesCastBarsEnmityList;

        // Refresh addons
        utilities.RefreshAddon(utilities.GetAddon(config.TargetInfoAddon), "target info");
        utilities.RefreshAddon(utilities.GetAddon(config.TargetCastBarAddon), "target castbar");
        utilities.RefreshAddon(utilities.GetAddon(config.FocusCastBarAddon), "focus castbar");
        utilities.RefreshAddon(utilities.GetAddon(config.EnemyListAddon), "enemy list");
    }

    // ----------------------------
    // On framework update
    // ----------------------------
    protected override void OnFrameworkUpdate(IFramework framework)
    {
        try
        {
            // Check if language is swapped
            if (!isLanguageSwapped)
            {
                currentEnemyTargetActionId = 0;
                currentEnemyFocusActionId = 0;
                listCasts.Clear();
                listCastsExpiry.Clear();
                return;
            }

            // Get local player
            IPlayerCharacter? player = objectTable.LocalPlayer;
            if (player == null)
            {
                currentEnemyTargetActionId = 0;
                currentEnemyFocusActionId = 0;
                listCasts.Clear();
                listCastsExpiry.Clear();
                return;
            }

            // Get player's ID
            ulong playerId = player.GameObjectId;

            // Get player's target ID
            ulong targetId = player.TargetObjectId;

            // Get player's focus ID
            ulong focusId = targetManager.FocusTarget?.GameObjectId ?? 0;

            // Clean expired list casts
            CleanExpiredListCasts();

            // Iterate through all battle NPCs
            foreach (IGameObject obj in objectTable)
            {
                // Filter for battle NPCs
                if (obj == null || obj.ObjectKind != ObjectKind.BattleNpc || obj is IPlayerCharacter || obj is not IBattleChara battleChara) continue;

                // Check if this character is the current player's target
                bool isTarget = battleChara.GameObjectId == targetId;

                // Check if this character is the current player's focus
                bool isFocus = battleChara.GameObjectId == focusId;

                // Check if this character is in the current player's enmity list
                bool inEnmityList = IsInList(battleChara, StatusFlags.InCombat);

                // Skip if not relevant
                if (!isTarget && !isFocus && !inEnmityList) continue;

                // Check if casting
                if (battleChara.IsCasting)
                {
                    // Get action ID
                    uint actionId = (uint)battleChara.CastActionId;
                    if (actionId > 0)
                    {
                        // Update target
                        if (isTarget) currentEnemyTargetActionId = actionId;

                        // Update focus
                        if (isFocus) currentEnemyFocusActionId = actionId;

                        // Update enemy list
                        if (inEnmityList)
                        {
                            listCasts[battleChara.GameObjectId] = actionId;
                            listCastsExpiry[battleChara.GameObjectId] = Stopwatch.GetTimestamp() * 10_000_000L / Stopwatch.Frequency;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error in OnFrameworkUpdate");
        }
    }

    // ----------------------------
    // On target info update
    // ----------------------------
    private void OnTargetInfoUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsTarget)
        {
            UpdateCastBar(utilities.GetAddon(config.TargetInfoAddon), currentEnemyTargetActionId, targetInfoField, "target info");
        }
    }

    // ----------------------------
    // On target cast bar update
    // ----------------------------
    private void OnTargetCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsTarget)
        {
            UpdateCastBar(utilities.GetAddon(config.TargetCastBarAddon), currentEnemyTargetActionId, targetCastBarField, "target castbar");
        }
    }

    // ----------------------------
    // On focus cast bar update
    // ----------------------------
    private void OnFocusCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsFocus)
        {
            UpdateCastBar(utilities.GetAddon(config.FocusCastBarAddon), currentEnemyFocusActionId, focusCastBarField, "focus castbar");
        }
    }

    // ----------------------------
    // On enemy list update
    // ----------------------------
    private void OnEnemyListUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsEnmityList)
        {
            UpdateList(utilities.GetAddon(config.EnemyListAddon), enemyListStartField, enemyListEndField, enemyListCastField);
        }
    }

    // ----------------------------
    // Process slot
    // ----------------------------
    protected override void ProcessSlot(AtkTextNode* textNode)
    {
        // Get the current text
        string currentText = textNode -> NodeText.ToString();
        if (string.IsNullOrWhiteSpace(currentText)) return;

        // Remove ellipsis for comparison
        currentText = Utilities.RemoveEllipsis(currentText);

        // Track a single unresolved obfuscated action for UI-based detection
        uint unresolvedObfuscatedActionId = 0;
        bool hasMultipleUnresolvedObfuscatedActions = false;

        // Check if the current text contains any of the casts in the enmity list and translate it
        foreach (KeyValuePair<ulong, uint> cast in listCasts)
        {
            // Get the action ID
            uint actionId = cast.Value;

            // Get the client language action name
            string? clientActionName = translationCache.GetActionName(actionId, config.ClientLanguage);
            if (clientActionName == null) continue;

            // Resolve obfuscated client action name from known mappings
            bool isObfuscatedClientActionName = clientActionName.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal);
            if (isObfuscatedClientActionName)
            {
                string? knownClientActionName = GetObfuscatedTranslationName(clientActionName, config.ClientLanguage);
                if (!knownClientActionName.IsNullOrWhitespace()) clientActionName = knownClientActionName;
            }

            // If the client language action name contains the current text, translate it
            if (clientActionName.StartsWith(currentText))
            {
                // Get the display action name
                string? displayName = GetDisplayActionName(actionId, currentText);
                if (!displayName.IsNullOrWhitespace())
                {
                    // Update the text node with the display name
                    textNode -> SetText(displayName);
                    break;
                }
            }

            // If the action is still unresolved and the UI shows a visible name, keep a single safe fallback candidate
            if (isObfuscatedClientActionName &&
                clientActionName.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal) &&
                !currentText.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal))
            {
                if (unresolvedObfuscatedActionId == 0 || unresolvedObfuscatedActionId == actionId)
                {
                    unresolvedObfuscatedActionId = actionId;
                }
                else
                {
                    hasMultipleUnresolvedObfuscatedActions = true;
                }
            }
        }

        // If exactly one unresolved obfuscated action remains, use the visible UI text to trigger detection and optional translation
        if (unresolvedObfuscatedActionId > 0 && !hasMultipleUnresolvedObfuscatedActions)
        {
            string? displayName = GetDisplayActionName(unresolvedObfuscatedActionId, currentText);
            if (!displayName.IsNullOrWhitespace())
            {
                // Update the text node with the display name
                textNode -> SetText(displayName);
            }
        }
    }

    // ----------------------------
    // Update cast bar
    // ----------------------------
    protected override void UpdateCastBar(AtkUnitBase* addon, uint actionId, int fieldIndex, string addonName)
    {
        try
        {
            // Extract visible name for scanning if addon is accessible
            string visibleName = "";
            if (addon != null && addon->IsVisible)
            {
                AtkResNode* fieldNode = addon->UldManager.NodeList[fieldIndex];
                if (fieldNode != null && fieldNode->Type == NodeType.Text)
                {
                    AtkTextNode* textNode = (AtkTextNode*)fieldNode;
                    if (textNode != null && textNode->NodeText.Length > 0)
                    {
                        visibleName = textNode->NodeText.ToString();
                    }
                }
            }

            // Always scan for obfuscated actions, regardless of language swap state
            if (actionId > 0)
            {
                ScanForObfuscatedAction(actionId, visibleName);
            }

            // Only update if language is swapped, we have a valid action ID and the addon is visible
            if (!isLanguageSwapped || actionId == 0 || addon == null || !addon->IsVisible) return;

            // Get the text node
            AtkResNode* fieldNode2 = addon->UldManager.NodeList[fieldIndex];
            if (fieldNode2 == null || fieldNode2->Type != NodeType.Text) return;

            // Update text
            AtkTextNode* textNode2 = (AtkTextNode*)fieldNode2;
            if (textNode2 == null || textNode2->NodeText.Length == 0) return;

            // Get action visible name
            string updateVisibleName = textNode2->NodeText.ToString();

            // Get action display name
            string? displayName = GetDisplayActionName(actionId, updateVisibleName);
            if (displayName.IsNullOrWhitespace()) return;

            // Update the text node with the display name
            textNode2->SetText(displayName);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error updating {addonName} addon");
        }
    }

    // ----------------------------
    // Update list
    // ----------------------------
    protected override void UpdateList(AtkUnitBase* addon, int listStartField, int listEndField, int castField)
    {
        try
        {
            // Scan all active casts for obfuscated actions, regardless of language swap state
            foreach (KeyValuePair<ulong, uint> cast in listCasts)
            {
                if (cast.Value > 0)
                {
                    ScanForObfuscatedAction(cast.Value);
                }
            }

            // Only update if language is swapped, we have casts to translate and the addon is visible
            if (!isLanguageSwapped || (listCasts.Count < 1) || addon == null || !addon->IsVisible) return;

            // Process each slot in the list
            for (int slotIndex = listStartField; slotIndex <= listEndField; slotIndex++)
            {
                ProcessList(addon, slotIndex, castField);
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error updating list addon");
        }
    }

    // ----------------------------
    // Process list
    // ----------------------------
    protected override void ProcessList(AtkUnitBase* addon, int slotIndex, int castField)
    {
        // Get the slot node
        AtkResNode* slotNode = addon->UldManager.NodeList[slotIndex];
        if (slotNode == null || !slotNode->IsVisible() || (ushort)slotNode->Type < 1000) return;

        // Get the component node
        AtkComponentNode* componentNode = (AtkComponentNode*)slotNode;
        if (componentNode->Component == null) return;

        // Get the uld manager
        AtkUldManager* uldManager = &componentNode->Component->UldManager;
        if (uldManager == null || uldManager->NodeListCount == 0) return;

        // Get the field node
        AtkResNode* fieldNode = uldManager->NodeList[castField];
        if (fieldNode == null || fieldNode->Type != NodeType.Text) return;

        // Cast to text node
        AtkTextNode* textNode = (AtkTextNode*)fieldNode;
        if (textNode == null || textNode->NodeText.Length == 0) return;

        // Always scan the visible text for obfuscated actions from list
        string visibleText = textNode->NodeText.ToString();
        foreach (KeyValuePair<ulong, uint> cast in listCasts)
        {
            if (cast.Value > 0 && !visibleText.IsNullOrWhitespace())
            {
                ScanForObfuscatedAction(cast.Value, visibleText);
            }
        }

        // Translate the cast text
        ProcessSlot(textNode);
    }

    // ----------------------------
    // Scan for obfuscated action
    // ----------------------------
    protected void ScanForObfuscatedAction(uint actionId, string visibleName = "")
    {
        try
        {
            if (actionId == 0) return;

            // Get the client action name
            string? clientActionName = translationCache.GetActionName(actionId, config.ClientLanguage);
            if (clientActionName.IsNullOrWhitespace()) return;

            // Check for obfuscated name
            if (clientActionName.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal))
            {
                log.Information($"{Class} - Obfuscated action detected (always-on scan): ID={actionId}, ObfuscatedName={clientActionName}");

                // If we have the visible name from UI, save the association
                if (!visibleName.IsNullOrWhitespace())
                {
                    SaveScannedObfuscatedTranslation((int)actionId, clientActionName, visibleName);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error scanning obfuscated action {actionId}");
        }
    }

    // ----------------------------
    // Get display action name
    // ----------------------------
    private string? GetDisplayActionName(uint actionId, string visibleAddonName = "")
    {
        try
        {
            // Check for valid action ID
            if (actionId == 0) return null;

            // Get the client action name
            string? clientActionName = translationCache.GetActionName(actionId, config.ClientLanguage);
            if (clientActionName.IsNullOrWhitespace()) return null;

            string originalClientActionName = clientActionName;

            // Check for obfuscated name
            if (clientActionName.StartsWith(config.ObfuscatedPrefix, StringComparison.Ordinal))
            {
                log.Information($"{Class} - Obfuscated cast detected for action {actionId}: {clientActionName} | Visible: {visibleAddonName}");
                SaveScannedObfuscatedTranslation((int)actionId, clientActionName, visibleAddonName);

                string? resolvedClientActionName = GetObfuscatedTranslationName(clientActionName, config.ClientLanguage);
                if (!resolvedClientActionName.IsNullOrWhitespace()) clientActionName = resolvedClientActionName;
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

                return string.Equals(clientActionName, originalClientActionName, StringComparison.Ordinal) ? null : clientActionName;
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
                log.Information($"{Class} - Scanned obfuscated cast saved: ID={actionId}, ObfuscatedName={obfuscatedName}, ClientLanguage={config.ClientLanguage}, ClientName={scannedName}");
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
        string? translation = FindObfuscatedTranslation(config.RemoteObfuscatedTranslations, obfuscatedName, language);
        if (!translation.IsNullOrWhitespace()) return translation;

        translation = FindObfuscatedTranslation(config.ScannedObfuscatedTranslations, obfuscatedName, language);
        if (!translation.IsNullOrWhitespace()) return translation;

        return FindObfuscatedTranslation(config.LocalObfuscatedTranslations, obfuscatedName, language);
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
    // Disable the hook
    // ----------------------------
    public override void Disable(string hookName)
    {
        // Prevent multiple disables
        if (!isEnabled) return;

        try
        {
            // Unsubscribe from framework update
            framework.Update -= OnFrameworkUpdate;

            // Unregister addon lifecycle listeners
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.EnemyListAddon, OnEnemyListUpdate);

            // Set disabled flag
            isEnabled = false;
            log.Information($"{Class} - {hookName} hook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to disable {hookName} hook");
        }
    }

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    public override void Dispose(string hookName)
    {
        try
        {
            // Unsubscribe from framework update
            framework.Update -= OnFrameworkUpdate;

            // Unregister addon lifecycle listeners
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.EnemyListAddon, OnEnemyListUpdate);

            // Set disabled flag
            isEnabled = false;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to dispose {hookName} hook");
        }
    }

}