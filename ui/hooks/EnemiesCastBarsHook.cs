using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.NativeWrapper;
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
public unsafe class EnemiesCastBarsHook : BaseHook
{
    // Log
    private const string Class = "[EnemiesCastBarsHook.cs]";

    // Core components
    private readonly IAddonLifecycle addonLifecycle;
    private readonly IFramework framework;
    private readonly IObjectTable objectTable;
    private readonly ITargetManager targetManager;

    // Castbars addons
    private readonly AtkUnitBase* targetInfo;
    private readonly AtkUnitBase* targetCastBar;
    private readonly AtkUnitBase* focusCastBar;
    private readonly AtkUnitBase* enemyList;

    // Castbars fields
    private readonly int targetInfoField;
    private readonly int targetCastBarField;
    private readonly int focusCastBarField;
    private readonly int enemyListField;

    // Tracking variables
    private uint _currentTargetActionId;
    private ulong _currentTargetGameObjectId;
    private uint _currentFocusActionId;
    private ulong _currentFocusGameObjectId;
    private readonly Dictionary<ulong, uint> _enemyListCasts;

    // ----------------------------
    // Constructor
    // ----------------------------
    public EnemiesCastBarsHook(
        IAddonLifecycle addonLifecycle,
        Configuration config,
        IFramework framework,
        IGameGui gameGui,
        IGameInteropProvider gameInterop,
        IObjectTable objectTable,
        ITargetManager targetManager,
        TranslationCache translationCache,
        Utilities utilities,
        IPluginLog log) : base(config, gameGui, gameInterop, translationCache, utilities, log)
    {
        // Assign core components
        this.addonLifecycle = addonLifecycle;
        this.framework = framework;
        this.objectTable = objectTable;
        this.targetManager = targetManager;

        // Get castbars addons
        targetInfo = GetTargetInfoAddon();
        targetCastBar = GetTargetCastBarAddon();
        focusCastBar = GetFocusCastBarAddon();
        enemyList = GetEnemyListAddon();

        // Get castbar fields
        targetInfoField = config.TargetInfoField;
        targetCastBarField = config.TargetCastBarField;
        focusCastBarField = config.FocusCastBarField;
        enemyListField = config.EnemyListField;

        // Initialize tracking variables
        _currentTargetActionId = 0;
        _currentTargetGameObjectId = 0;
        _currentFocusActionId = 0;
        _currentFocusGameObjectId = 0;
        _enemyListCasts = [];
    }

    // ----------------------------
    // Get target info addon
    // ----------------------------
    private AtkUnitBase* GetTargetInfoAddon()
    {
        // Initialize
        AtkUnitBase* targetInfo = null;
        try
        {
            // Get pointer from name
            AtkUnitBasePtr targetInfoPtr = gameGui.GetAddonByName(config.TargetInfoAddon);
            if (!targetInfoPtr.IsNull)
            {
                // Get addon from pointer
                targetInfo = (AtkUnitBase*)targetInfoPtr.Address;
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to get target info addon");
        }
        return targetInfo;
    }

    // ----------------------------
    // Get target castbar addon
    // ----------------------------
    private AtkUnitBase* GetTargetCastBarAddon()
    {
        // Initialize
        AtkUnitBase* targetCastBar = null;
        try
        {
            // Get pointer from name
            AtkUnitBasePtr targetCastBarPtr = gameGui.GetAddonByName(config.TargetCastBarAddon);
            if (!targetCastBarPtr.IsNull)
            {
                // Get addon from pointer
                targetCastBar = (AtkUnitBase*)targetCastBarPtr.Address;
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to get target castbar addon");
        }
        return targetCastBar;
    }

    // ----------------------------
    // Get focus castbar addon
    // ----------------------------
    private AtkUnitBase* GetFocusCastBarAddon()
    {
        // Initialize
        AtkUnitBase* focusCastBar = null;
        try
        {
            // Get pointer from name
            AtkUnitBasePtr focusCastBarPtr = gameGui.GetAddonByName(config.FocusCastBarAddon);
            if (!focusCastBarPtr.IsNull)
            {
                // Get addon from pointer
                focusCastBar = (AtkUnitBase*)focusCastBarPtr.Address;
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to get focus castbar addon");
        }
        return focusCastBar;
    }

    // ----------------------------
    // Get enemy list addon
    // ----------------------------
    private AtkUnitBase* GetEnemyListAddon()
    {
        // Initialize
        AtkUnitBase* enemyList = null;
        try
        {
            // Get pointer from name
            AtkUnitBasePtr enemyListPtr = gameGui.GetAddonByName(config.EnemyListAddon);
            if (!enemyListPtr.IsNull)
            {
                // Get addon from pointer
                enemyList = (AtkUnitBase*)enemyListPtr.Address;
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to get enemy list addon");
        }
        return enemyList;
    }

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

            // Subscribe to addon lifecycle
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
        // Refresh target (compact & split mode), focus & enemy list addons
        try
        {
            // Only refresh if the addon is currently visible
            if (targetInfo != null && targetInfo -> IsVisible)
            {
                targetInfo -> Hide(true, false, 0);
                targetInfo -> Show(true, 0);
            }

            // Only refresh if the addon is currently visible
            if (targetCastBar != null && targetCastBar -> IsVisible)
            {
                targetCastBar -> Hide(true, false, 0);
                targetCastBar -> Show(true, 0);
            }

            // Only refresh if the addon is currently visible
            if (focusCastBar != null && focusCastBar -> IsVisible)
            {
                focusCastBar -> Hide(true, false, 0);
                focusCastBar -> Show(true, 0);
            }

            // Only refresh if the addon is currently visible
            if (enemyList != null && enemyList -> IsVisible)
            {
                enemyList -> Hide(true, false, 0);
                enemyList -> Show(true, 0);
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to refresh enemies castbars addons");
        }
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
            var currentCasting = new HashSet<ulong>();

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
        try
        {
            // Only update if language is swapped, we have a valid action ID and the addon is visible
            if (!isLanguageSwapped || _currentTargetActionId == 0 || targetInfo == null || !targetInfo -> IsVisible) return;

            // Get translated action name
            string? translatedName = translationCache.GetActionName(_currentTargetActionId, (LanguageEnum)config.TargetLanguage);
            if (translatedName.IsNullOrWhitespace()) return;

            // Get the text node
            var node = targetInfo -> UldManager.NodeList[targetInfoField];
            if (node == null || node -> Type != NodeType.Text) return;

            // Update text
            AtkTextNode* textNode = (AtkTextNode*)node;
            if (textNode != null && textNode -> NodeText.Length > 0) textNode -> SetText(translatedName);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error updating target info addon");
        }
    }

    // ----------------------------
    // On target cast bar update
    // ----------------------------
    private void OnTargetCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        try
        {
            // Only update if language is swapped, we have a valid action ID and the addon is visible
            if (!isLanguageSwapped || _currentTargetActionId == 0 || targetCastBar == null || !targetCastBar -> IsVisible) return;

            // Get translated action name
            string? translatedName = translationCache.GetActionName(_currentTargetActionId, (LanguageEnum)config.TargetLanguage);
            if (translatedName.IsNullOrWhitespace()) return;

            // Get the text node
            var node = targetCastBar -> UldManager.NodeList[targetCastBarField];
            if (node == null || node -> Type != NodeType.Text) return;

            // Update text
            AtkTextNode* textNode = (AtkTextNode*)node;
            if (textNode != null && textNode -> NodeText.Length > 0) textNode -> SetText(translatedName);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error updating target castbar addon");
        }
    }

    // ----------------------------
    // On focus cast bar update
    // ----------------------------
    private void OnFocusCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        try
        {
            // Only update if language is swapped, we have a valid action ID and the addon is visible
            if (!isLanguageSwapped || _currentFocusActionId == 0 || focusCastBar == null || !focusCastBar -> IsVisible) return;

            // Get translated action name
            string? translatedName = translationCache.GetActionName(_currentFocusActionId, (LanguageEnum)config.TargetLanguage);
            if (translatedName.IsNullOrWhitespace()) return;

            // Get the text node
            AtkResNode* node = focusCastBar -> UldManager.NodeList[focusCastBarField];
            if (node == null || node -> Type != NodeType.Text) return;

            // Update text
            AtkTextNode* textNode = (AtkTextNode*)node;
            if (textNode != null && textNode -> NodeText.Length > 0) textNode -> SetText(translatedName);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error updating focus castbar addon");
        }
    }

    // ----------------------------
    // On enemy list update
    // ----------------------------
    private void OnEnemyListUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        try
        {
            // Only update if language is swapped, we have at least one enemy casting and the addon is visible
            if (!isLanguageSwapped || _enemyListCasts.Count < 1 || enemyList == null || !enemyList -> IsVisible) return;

            // TODO: Parcourir les slots de l'enemy list et matcher avec _enemyListCasts
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error updating enemy list addon");
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

            // Unsubscribe from addon lifecycle
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

            // Unsubscribe from addon lifecycle
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.EnemyListAddon, OnEnemyListUpdate);

            // Set disabled flag
            isEnabled = false;
            log.Debug($"{Class} - Enemies castbars hook disposed");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to dispose enemies castbars hook");
        }

        // Finalize
        GC.SuppressFinalize(this);
    }

}