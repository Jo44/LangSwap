using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LangSwap.tool;
using LangSwap.translation;
using LangSwap.ui.hooks.@base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LangSwap.ui.hooks;

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
    IPluginLog log) : BaseHook(config, translationCache, utilities, log)
{
    // Log
    private const string Class = "[EnemiesCastBarsHook.cs]";

    // Core components
    private readonly IAddonLifecycle addonLifecycle = addonLifecycle;
    private readonly IFramework framework = framework;
    private readonly IObjectTable objectTable = objectTable;
    private readonly ITargetManager targetManager = targetManager;

    // UI components
    private bool castBarsTarget = false;
    private bool castBarsFocus = false;
    private bool castBarsEnmityList = false;

    // Castbars addons
    private readonly AtkUnitBase* targetInfo = utilities.GetAddon(config.TargetInfoAddon, "target info");
    private readonly AtkUnitBase* targetCastBar = utilities.GetAddon(config.TargetCastBarAddon, "target castbar");
    private readonly AtkUnitBase* focusCastBar = utilities.GetAddon(config.FocusCastBarAddon, "focus castbar");
    private readonly AtkUnitBase* enemyList = utilities.GetAddon(config.EnemyListAddon, "enemy list");

    // Castbars fields
    private readonly int targetInfoField = config.TargetInfoField;
    private readonly int targetCastBarField = config.TargetCastBarField;
    private readonly int focusCastBarField = config.FocusCastBarField;
    private readonly int enemyListStartField = config.EnemyListStartField;
    private readonly int enemyListEndField = config.EnemyListEndField;
    private readonly int enemyListCastField = config.EnemyListCastField;

    // Tracking variables
    private uint _currentTargetActionId = 0;
    private ulong _currentTargetGameObjectId = 0;
    private uint _currentFocusActionId = 0;
    private ulong _currentFocusGameObjectId = 0;
    private readonly Dictionary<ulong, uint> _enemyListCasts = [];

    // ----------------------------
    // Enable the hook
    // ----------------------------
    public override void Enable()
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
            log.Debug($"{Class} - Enemies castbars hook enabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to enable enemies castbars hook");
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
        utilities.RefreshAddon(targetInfo, "target info");
        utilities.RefreshAddon(targetCastBar, "target castbar");
        utilities.RefreshAddon(focusCastBar, "focus castbar");
        utilities.RefreshAddon(enemyList, "enemy list");
    }

    // ----------------------------
    // On framework update
    // ----------------------------
    private void OnFrameworkUpdate(IFramework framework)
    {
        try
        {
            // Check if language is swapped
            if (!isLanguageSwapped)
            {
                _currentTargetActionId = 0;
                _currentTargetGameObjectId = 0;
                _currentFocusActionId = 0;
                _currentFocusGameObjectId = 0;
                _enemyListCasts.Clear();
                return;
            }

            // Get local player
            IPlayerCharacter? player = objectTable.LocalPlayer;
            if (player == null)
            {
                _currentTargetActionId = 0;
                _currentTargetGameObjectId = 0;
                _currentFocusActionId = 0;
                _currentFocusGameObjectId = 0;
                _enemyListCasts.Clear();
                return;
            }

            // Get player's target and focus
            ulong targetId = player.TargetObjectId;
            ulong focusId = targetManager.FocusTarget?.GameObjectId ?? 0;

            // Initialize tracking variables
            bool foundTarget = false;
            bool foundFocus = false;
            HashSet<ulong> currentCasting = [];

            // Iterate through all battle NPCs
            foreach (IGameObject obj in objectTable)
            {
                // Filter for battle NPCs
                if (obj == null || obj.ObjectKind != ObjectKind.BattleNpc) continue;
                if (obj is not IBattleChara battleChara) continue;

                // Check if this NPC is the current target, focus or in enmity list
                bool isTarget = battleChara.GameObjectId == targetId;
                bool isFocus = battleChara.GameObjectId == focusId;
                bool inEnmityList = IsInEnmityList(battleChara);

                // Skip if not relevant
                if (!isTarget && !isFocus && !inEnmityList) continue;

                // Check if casting
                if (battleChara.IsCasting)
                {
                    // Get action ID
                    uint actionId = (uint)battleChara.CastActionId;
                    if (actionId > 0)
                    {
                        // Add to current casting set
                        currentCasting.Add(battleChara.GameObjectId);

                        // Update target
                        if (isTarget)
                        {
                            _currentTargetActionId = actionId;
                            _currentTargetGameObjectId = battleChara.GameObjectId;
                            foundTarget = true;
                        }

                        // Update focus
                        if (isFocus)
                        {
                            _currentFocusActionId = actionId;
                            _currentFocusGameObjectId = battleChara.GameObjectId;
                            foundFocus = true;
                        }

                        // Update enemy list
                        if (inEnmityList)
                        {
                            _enemyListCasts[battleChara.GameObjectId] = actionId;
                        }
                    }
                }
            }

            // Reset if target not found
            if (!foundTarget)
            {
                _currentTargetActionId = 0;
                _currentTargetGameObjectId = 0;
            }

            // Reset if focus not found
            if (!foundFocus)
            {
                _currentFocusActionId = 0;
                _currentFocusGameObjectId = 0;
            }

            // Clean up enemy list of non-casting enemies
            List<ulong> toRemove = [.. _enemyListCasts.Keys.Where(id => !currentCasting.Contains(id))];
            foreach (ulong id in toRemove)
            {
                _enemyListCasts.Remove(id);
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error in OnFrameworkUpdate");
        }
    }

    // ----------------------------
    // Check if enemy is in enmity list
    // ----------------------------
    private static bool IsInEnmityList(IBattleChara enemy)
    {
        bool inEnmityList = false;
        try
        {
            if (enemy.StatusFlags.HasFlag(StatusFlags.InCombat)) inEnmityList = true;
        }
        catch
        {
            inEnmityList = false;
        }
        return inEnmityList;
    }

    // ----------------------------
    // On target info update
    // ----------------------------
    private void OnTargetInfoUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsTarget)
        {
            UpdateCastBarTextNode(targetInfo, _currentTargetActionId, targetInfoField, "target info");
        }
    }

    // ----------------------------
    // On target cast bar update
    // ----------------------------
    private void OnTargetCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsTarget)
        {
            UpdateCastBarTextNode(targetCastBar, _currentTargetActionId, targetCastBarField, "target castbar");
        }
    }

    // ----------------------------
    // On focus cast bar update
    // ----------------------------
    private void OnFocusCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsFocus)
        {
            UpdateCastBarTextNode(focusCastBar, _currentFocusActionId, focusCastBarField, "focus castbar");
        }
    }

    // ----------------------------
    // Update cast bar text node
    // ----------------------------
    private void UpdateCastBarTextNode(AtkUnitBase* addon, uint actionId, int fieldIndex, string addonName)
    {
        try
        {
            // Only update if language is swapped, we have a valid action ID and the addon is visible
            if (!isLanguageSwapped || actionId == 0 || addon == null || !addon -> IsVisible) return;

            // Get translated action name
            string? translatedName = translationCache.GetActionName(actionId, (LanguageEnum)config.TargetLanguage);
            if (translatedName.IsNullOrWhitespace()) return;

            // Get the text node
            AtkResNode* fieldNode = addon -> UldManager.NodeList[fieldIndex];
            if (fieldNode == null || fieldNode -> Type != NodeType.Text) return;

            // Update text
            AtkTextNode* textNode = (AtkTextNode*)fieldNode;
            if (textNode != null && textNode -> NodeText.Length > 0) textNode -> SetText(translatedName);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error updating {addonName} addon");
        }
    }

    // ----------------------------
    // On enemy list update
    // ----------------------------
    private void OnEnemyListUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsEnmityList)
        {
            try
            {
                // Only update if language is swapped, we have casts to translate and the addon is visible
                if (!isLanguageSwapped || _enemyListCasts.Count < 1 || enemyList == null || !enemyList->IsVisible) return;

                // Process each enemy slot in the list
                for (int slotIndex = enemyListStartField; slotIndex <= enemyListEndField; slotIndex++)
                {
                    ProcessEnemySlot(slotIndex);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, $"{Class} - Error updating enemy list addon");
            }
        }
    }

    // ----------------------------
    // Process enemy slot
    // ----------------------------
    private void ProcessEnemySlot(int slotIndex)
    {
        // Get the slot node
        AtkResNode* slotNode = enemyList -> UldManager.NodeList[slotIndex];
        if (slotNode == null || !slotNode -> IsVisible() || (ushort)slotNode -> Type < 1000) return;

        // Get the component node
        AtkComponentNode* componentNode = (AtkComponentNode*)slotNode;
        if (componentNode -> Component == null) return;

        // Get the uld manager
        AtkUldManager* uldManager = &componentNode -> Component -> UldManager;
        if (uldManager == null || uldManager -> NodeListCount == 0) return;

        // Get the field node
        AtkResNode* fieldNode = uldManager -> NodeList[enemyListCastField];
        if (fieldNode == null || fieldNode -> Type != NodeType.Text) return;

        // Cast to text node
        AtkTextNode* textNode = (AtkTextNode*)fieldNode;
        if (textNode == null || textNode -> NodeText.Length == 0) return;

        // Translate the cast text
        TranslateCastText(textNode);
    }

    // ----------------------------
    // Translate the cast text in the enemy list slot
    // ----------------------------
    private void TranslateCastText(AtkTextNode* textNode)
    {
        // Get the current text
        string currentText = textNode -> NodeText.ToString();
        if (string.IsNullOrWhiteSpace(currentText)) return;

        // Remove ellipsis for comparison
        currentText = Utilities.RemoveEllipsis(currentText);

        // Check if the current text contains any of the casts in the enemy list and translate it
        foreach (KeyValuePair<ulong, uint> cast in _enemyListCasts)
        {
            // Get the action ID
            uint actionId = cast.Value;

            // Get the client language action name
            string? clientActionName = translationCache.GetActionName(actionId, (LanguageEnum)config.ClientLanguage);
            if (clientActionName == null) continue;

            // If the client language action name contains the current text, translate it
            if (clientActionName.StartsWith(currentText))
            {
                // Get the translated action name
                string? translatedName = translationCache.GetActionName(actionId, (LanguageEnum)config.TargetLanguage);
                if (!translatedName.IsNullOrWhitespace())
                {
                    // Update the text node with the translated name
                    textNode -> SetText(translatedName);
                    break;
                }
            }
        }
    }

    // ----------------------------
    // Disable the hook
    // ----------------------------
    public override void Disable()
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
            log.Debug($"{Class} - Enemies castbars hook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to disable enemies castbars hook");
        }
    }

    // ----------------------------
    // Dispose the hook
    // ----------------------------
    public override void Dispose()
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
            log.Error(ex, $"{Class} - Failed to dispose enemies castbars hook");
        }

        // Finalize
        GC.SuppressFinalize(this);
    }

}