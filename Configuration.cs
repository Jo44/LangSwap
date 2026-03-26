using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using LangSwap.translation;
using System;

namespace LangSwap;

// ----------------------------
// Plugin Configuration
// ----------------------------
[Serializable]
public class Configuration : IPluginConfiguration
{
    // Version
    public int Version { get; set; } = 1;

    // Language
    public LanguageEnum ClientLanguage { get; set; } = LanguageEnum.English;
    public LanguageEnum TargetLanguage { get; set; } = LanguageEnum.English;

    // Startup behavior
    public bool AutoStartup { get; set; } = false;

    // Shortcut
    public bool ShortcutEnabled { get; set; } = true;

    // Primary key
    public int PrimaryKey { get; set; } = (int)VirtualKey.Y;

    // Modifier keys
    public bool Ctrl { get; set; } = false;
    public bool Alt { get; set; } = false;
    public bool Shift { get; set; } = true;

    // UI components
    public bool ActionTooltip { get; set; } = true;
    public bool ItemTooltip { get; set; } = true;
    public bool AlliesCastBarsTarget { get; set; } = true;
    public bool AlliesCastBarsFocus { get; set; } = true;
    public bool AlliesCastBarsPartyList { get; set; } = true;
    public bool EnemiesCastBarsTarget { get; set; } = true;
    public bool EnemiesCastBarsFocus { get; set; } = true;
    public bool EnemiesCastBarsEnmityList { get; set; } = true;

    // Memory signatures
    public string ActionTooltipSig { get; } = "E8 ?? ?? ?? ?? 48 8B 43 28 48 8B AF";
    public string ItemTooltipSig { get; } = "48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 83 EC ?? 48 8B 42 ?? 4C 8B EA";

    // Action Detail Addon
    public string ActionDetailAddon { get; } = "ActionDetail";
    public int ActionNameField { get; } = 0;
    public int ActionDescriptionField { get; } = 13;

    // Item Detail Addon
    public string ItemDetailAddon { get; } = "ItemDetail";
    public int ItemNameField { get; } = 0;
    public int GlamourNameField { get; } = 1;
    public int ItemDescriptionField { get; } = 13;
    public int ItemEffectsField { get; } = 16;
    public int ItemBonusesStartField { get; } = 37;
    public int ItemBonusesEndField { get; } = 49;
    public int ItemMateriaNameStartField { get; } = 53;
    public int ItemMateriaNameEndField { get; } = 57;
    public int ItemMateriaStatStartField { get; } = 58;
    public int ItemMateriaStatEndField { get; } = 62;

    // Common CastBar Addons
    public string TargetInfoAddon { get; } = "_TargetInfo";
    public int TargetInfoField { get; } = 44;
    public string TargetCastBarAddon { get; } = "_TargetInfoCastBar";
    public int TargetCastBarField { get; } = 5;
    public string FocusCastBarAddon { get; } = "_FocusTargetInfo";
    public int FocusCastBarField { get; } = 16;

    // Allies CastBar Addons
    public string CastBarAddon { get; } = "_CastBar";
    public int CastBarField { get; } = 11;
    public string PartyListAddon { get; } = "_PartyList";
    public int PartyListStartField { get; } = 16;
    public int PartyListEndField { get; } = 23;
    public int PartyListCastField { get; } = 30;

    // Enemies CastBar Addons
    public string EnemyListAddon { get; } = "_EnemyList";
    public int EnemyListStartField { get; } = 4;
    public int EnemyListEndField { get; } = 11;
    public int EnemyListCastField { get; } = 16;

    // Miscellaneous
    public int MaxValidActionId { get; } = 100000;
    public int MaxValidItemId { get; } = 100000;

    // Symbols
    public char GlamouredSymbol { get; } = '\uE03B'; // Mirage symbol
    public char HighQualitySymbol { get; } = '\uE03C'; // HQ symbol
    public char[] TargetIndicatorSymbols { get; } = ['\uE071', '\uE072', '\uE073', '\uE074', '\uE075', '\uE076', '\uE077', '\uE078',
        '\uE079', '\uE07A', '\uE07B', '\uE07C', '\uE07D', '\uE07E', '\uE07F', '\uE080', '\uE081', '\uE082', '\uE083', '\uE084',
        '\uE085', '\uE086', '\uE087', '\uE088', '\uE089', '\uE08A', '\uE08F', '\uE090', '\uE091', '\uE092', '\uE093', '\uE094',
        '\uE095', '\uE096', '\uE097', '\uE098', '\uE099', '\uE09A', '\uE09B', '\uE09C', '\uE09D', '\uE09E', '\uE09F', '\uE0A0',
        '\uE0A1', '\uE0A2', '\uE0A3', '\uE0A4', '\uE0A5', '\uE0A6', '\uE0A7', '\uE0A8', '\uE0A9', '\uE0AA', '\uE0AB', '\uE0AC',
        '\uE0AD', '\uE0AE', '\uE0C1', '\uE0C2', '\uE0C3', '\uE0C4', '\uE0C5', '\uE0C6', '\uE0E0', '\uE0E1', '\uE0E2', '\uE0E3',
         '\uE0E4', '\uE0E5', '\uE0E6', '\uE0E7', '\uE0E8', '\uE0E9',]; // Target symbols

    // Save configuration
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }

}