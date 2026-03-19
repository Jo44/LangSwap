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
// Allies CastBars Hook
// ----------------------------
public unsafe class AlliesCastBarsHook : BaseHook
{
    // Log
    private const string Class = "[AlliesCastBarsHook.cs]";

    // Core components
    private readonly IAddonLifecycle addonLifecycle;
    private readonly IFramework framework;
    private readonly IObjectTable objectTable;
    private readonly ITargetManager targetManager;

    // UI components
    private readonly bool castBarsTarget;
    private readonly bool castBarsFocus;
    private readonly bool castBarsPartyList;

    // Castbars addons
    private AtkUnitBase* castBar;
    private AtkUnitBase* targetInfo;
    private AtkUnitBase* targetCastBar;
    private AtkUnitBase* focusCastBar;
    private AtkUnitBase* partyList;

    // Castbars fields
    private readonly int castBarField;
    private readonly int targetInfoField;
    private readonly int targetCastBarField;
    private readonly int focusCastBarField;
    private readonly int partyListStartField;
    private readonly int partyListEndField;
    private readonly int partyListCastField;

    // Tracking variables
    private uint _currentActionId;
    private uint _currentTargetActionId;
    private ulong _currentTargetGameObjectId;
    private uint _currentFocusActionId;
    private ulong _currentFocusGameObjectId;
    private readonly Dictionary<ulong, uint> _partyListCasts;

    // ----------------------------
    // Constructor
    // ----------------------------
    public AlliesCastBarsHook(
        IAddonLifecycle addonLifecycle,
        Configuration config,
        IFramework framework,
        IObjectTable objectTable,
        ITargetManager targetManager,
        TranslationCache translationCache,
        Utilities utilities,
        IPluginLog log) : base(config, translationCache, utilities, log)
    {
        // Initialize core components
        this.addonLifecycle = addonLifecycle;
        this.framework = framework;
        this.objectTable = objectTable;
        this.targetManager = targetManager;

        // Initialize UI components
        castBarsTarget = config.AlliesCastBarsTarget;
        castBarsFocus = config.AlliesCastBarsFocus;
        castBarsPartyList = config.AlliesCastBarsPartyList;

        // Initialize castbars addons
        InitializeAddons();

        // Initialize castbars fields
        castBarField = config.CastBarField;
        targetInfoField = config.TargetInfoField;
        targetCastBarField = config.TargetCastBarField;
        focusCastBarField = config.FocusCastBarField;
        partyListStartField = config.PartyListStartField;
        partyListEndField = config.PartyListEndField;
        partyListCastField = config.PartyListCastField;

        // Initialize tracking variables
        _currentActionId = 0;
        _currentTargetActionId = 0;
        _currentTargetGameObjectId = 0;
        _currentFocusActionId = 0;
        _currentFocusGameObjectId = 0;
        _partyListCasts = [];
    }

    // ----------------------------
    // Initialize addons
    // ----------------------------
    private void InitializeAddons()
    {
        if (castBarsTarget || castBarsFocus || castBarsPartyList)
        {
            castBar = utilities.GetAddon(config.CastBarAddon, "castbar");
        }
        if (castBarsTarget)
        {
            targetInfo = utilities.GetAddon(config.TargetInfoAddon, "target info");
            targetCastBar = utilities.GetAddon(config.TargetCastBarAddon, "target castbar");
        }
        if (castBarsFocus)
        {
            focusCastBar = utilities.GetAddon(config.FocusCastBarAddon, "focus castbar");
        }
        if (castBarsPartyList)
        {
            partyList = utilities.GetAddon(config.PartyListAddon, "party list");
        }
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

            // Register addon lifecycle listeners for relevant addons
            if (castBarsTarget || castBarsFocus || castBarsPartyList)
            {
                addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.CastBarAddon, OnCastBarUpdate);
            }
            if (castBarsTarget)
            {
                addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
                addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            }
            if (castBarsFocus)
            {
                addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            }
            if (castBarsPartyList)
            {
                addonLifecycle.RegisterListener(AddonEvent.PostUpdate, config.PartyListAddon, OnPartyListUpdate);
            }

            // Set enabled flag
            isEnabled = true;

            // Log
            log.Debug($"{Class} - Allies castbars hook enabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to enable allies castbars hook");
        }
    }

    // ----------------------------
    // On language swap
    // ----------------------------
    protected override void OnLanguageSwap()
    {
        // Refresh castbar addon
        utilities.RefreshAddon(castBar, "castbar");
        // Refresh target info addon
        utilities.RefreshAddon(targetInfo, "target info");
        // Refresh target castbar addon
        utilities.RefreshAddon(targetCastBar, "target castbar");
        // Refresh focus castbar addon
        utilities.RefreshAddon(focusCastBar, "focus castbar");
        // Refresh party list addon
        utilities.RefreshAddon(partyList, "party list");
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
                _currentActionId = 0;
                _currentTargetActionId = 0;
                _currentTargetGameObjectId = 0;
                _currentFocusActionId = 0;
                _currentFocusGameObjectId = 0;
                _partyListCasts.Clear();
                return;
            }

            // Get local player
            IPlayerCharacter? player = objectTable.LocalPlayer;
            if (player == null)
            {
                _currentActionId = 0;
                _currentTargetActionId = 0;
                _currentTargetGameObjectId = 0;
                _currentFocusActionId = 0;
                _currentFocusGameObjectId = 0;
                _partyListCasts.Clear();
                return;
            }

            // Get player's target and focus
            ulong targetId = player.TargetObjectId;
            ulong focusId = targetManager.FocusTarget?.GameObjectId ?? 0;

            // Initialize tracking variables
            bool foundPlayer = false;
            bool foundTarget = false;
            bool foundFocus = false;
            HashSet<ulong> currentCasting = [];

            // Iterate through all battle NPCs
            foreach (IGameObject obj in objectTable)
            {
                // Filter for players
                if (obj == null || obj.ObjectKind != ObjectKind.Player) continue;
                if (obj is not IBattleChara battleChara) continue;

                // Check if this character is the current player
                bool isCharacter = battleChara.GameObjectId == player.GameObjectId;
                
                // Check if this character is the current player's target
                bool isTarget = battleChara.GameObjectId == targetId;

                // Check if this character is the current player's focus
                bool isFocus = battleChara.GameObjectId == focusId;

                // Check if this character is in the current player's party list (or player himself)
                bool inPartyList = IsInPartyList(battleChara, isCharacter);

                // Skip if not relevant
                if (!isCharacter && !isTarget && !isFocus && !inPartyList) continue;

                // Check if casting
                if (battleChara.IsCasting)
                {
                    // Get action ID
                    uint actionId = (uint)battleChara.CastActionId;
                    if (actionId > 0)
                    {
                        // Add to current casting set
                        currentCasting.Add(battleChara.GameObjectId);

                        // Update player
                        if (isCharacter)
                        {
                            _currentActionId = actionId;
                            foundPlayer = true;
                        }

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

                        // Update party list
                        if (inPartyList)
                        {
                            _partyListCasts[battleChara.GameObjectId] = actionId;
                        }
                    }
                }
            }

            // Reset if player not found
            if (!foundPlayer)
            {
                _currentActionId = 0;
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

            // Clean up party list of non-casting members
            List<ulong> toRemove = [.. _partyListCasts.Keys.Where(id => !currentCasting.Contains(id))];
            foreach (ulong id in toRemove)
            {
                _partyListCasts.Remove(id);
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Error in OnFrameworkUpdate");
        }
    }

    // ----------------------------
    // Check if member is in party list
    // ----------------------------
    private static bool IsInPartyList(IBattleChara ally, bool isCharacter)
    {
        bool inPartyList = false;
        try
        {
            if (ally.StatusFlags.HasFlag(StatusFlags.PartyMember) || isCharacter) inPartyList = true;
        }
        catch
        {
            inPartyList = false;
        }
        return inPartyList;
    }

    // ----------------------------
    // On cast bar update
    // ----------------------------
    private void OnCastBarUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsTarget || castBarsFocus || castBarsPartyList)
        {
            UpdateCastBarTextNode(castBar, _currentActionId, castBarField, "castbar");
        }
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
    // On party list update
    // ----------------------------
    private void OnPartyListUpdate(AddonEvent addonEvent, AddonArgs addonArgs)
    {
        if (castBarsPartyList)
        {
            try
            {
                // Only update if language is swapped, we have casts to translate and the addon is visible
                if (!isLanguageSwapped || _partyListCasts.Count < 1 || partyList == null || !partyList->IsVisible) return;

                // Process each ally slot in the list
                for (int slotIndex = partyListStartField; slotIndex <= partyListEndField; slotIndex++)
                {
                    ProcessAllySlot(slotIndex);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, $"{Class} - Error updating party list addon");
            }
        }
    }

    // ----------------------------
    // Process ally slot
    // ----------------------------
    private void ProcessAllySlot(int slotIndex)
    {
        // Get the slot node
        AtkResNode* slotNode = partyList -> UldManager.NodeList[slotIndex];
        if (slotNode == null || !slotNode -> IsVisible() || (ushort)slotNode -> Type < 1000) return;

        // Get the component node
        AtkComponentNode* componentNode = (AtkComponentNode*)slotNode;
        if (componentNode -> Component == null) return;

        // Get the uld manager
        AtkUldManager* uldManager = &componentNode -> Component -> UldManager;
        if (uldManager == null || uldManager -> NodeListCount == 0) return;

        // Get the field node
        AtkResNode* fieldNode = uldManager -> NodeList[partyListCastField];
        if (fieldNode == null || fieldNode -> Type != NodeType.Text) return;

        // Cast to text node
        AtkTextNode* textNode = (AtkTextNode*)fieldNode;
        if (textNode == null || textNode -> NodeText.Length == 0) return;

        // Translate the cast text
        TranslateCastText(textNode);
    }

    // ----------------------------
    // Translate the cast text in the party list slot
    // ----------------------------
    private void TranslateCastText(AtkTextNode* textNode)
    {
        // Get the current text
        string currentText = textNode -> NodeText.ToString();
        if (string.IsNullOrWhiteSpace(currentText)) return;

        // Remove target indicator for comparison
        string[] textParts = utilities.RemoveTargetIndicator(currentText);
        string textWithoutIndicator = Utilities.RemoveEllipsis(textParts[0]);
        string targetIndicator = textParts[1];

        // Check if the current text contains any of the casts in the party list and translate it
        foreach (KeyValuePair<ulong, uint> cast in _partyListCasts)
        {
            // Get the action ID
            uint actionId = cast.Value;

            // Get the client language action name
            string? clientActionName = translationCache.GetActionName(actionId, (LanguageEnum)config.ClientLanguage);
            if (clientActionName == null) continue;

            // If the client language action name contains the current text, translate it
            if (clientActionName.StartsWith(textWithoutIndicator))
            {
                // Get the translated action name
                string? translatedName = translationCache.GetActionName(actionId, (LanguageEnum)config.TargetLanguage);
                if (!translatedName.IsNullOrWhitespace())
                {
                    if (!targetIndicator.IsNullOrWhitespace())
                    {
                        // Update the text node with the translated name and target indicator
                        textNode -> SetText(translatedName + " " + targetIndicator);
                        break;
                    }
                    else
                    {
                        // Update the text node with the translated name
                        textNode -> SetText(translatedName);
                        break;
                    }
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
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.CastBarAddon, OnCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.PartyListAddon, OnPartyListUpdate);

            // Set disabled flag
            isEnabled = false;
            log.Debug($"{Class} - Allies castbars hook disabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to disable allies castbars hook");
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
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.CastBarAddon, OnCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetInfoAddon, OnTargetInfoUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.TargetCastBarAddon, OnTargetCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.FocusCastBarAddon, OnFocusCastBarUpdate);
            addonLifecycle.UnregisterListener(AddonEvent.PostUpdate, config.PartyListAddon, OnPartyListUpdate);

            // Set disabled flag
            isEnabled = false;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"{Class} - Failed to dispose allies castbars hook");
        }

        // Finalize
        GC.SuppressFinalize(this);
    }

}