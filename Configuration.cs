using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using LangSwap.translation.@base;
using LangSwap.translation.model;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace LangSwap;

// ----------------------------
// Configuration
//
// @author Jo44
// @version 1.7 (21/04/2026)
// @since 01/01/2026
// ----------------------------
[Serializable]
public class Configuration : IPluginConfiguration
{
    // Version
    public int Version { get; set; } = 3;

    // Language
    public Language ClientLanguage { get; set; } = Language.English;
    public Language TargetLanguage { get; set; } = Language.English;

    // Colors
    public Vector4 DarkGreen { get; } = new Vector4(0.00f, 0.80f, 0.13f, 1.00f); // Dark Green
    public Vector4 LightRed { get; } = new Vector4(0.57f, 0.13f, 0.13f, 1.00f); // Light Red
    public Vector4 RedDalamud { get; } = new Vector4(0.35f, 0.08f, 0.08f, 1.00f); // Red Dalamud
    public Vector4 LightRedDalamud { get; } = new Vector4(0.37f, 0.15f, 0.14f, 1.00f); // Light Red Dalamud
    public Vector4 LighterRedDalamud { get; } = new Vector4(0.42f, 0.23f, 0.23f, 1.00f); // Lighter Red Dalamud

    // Obfuscated translations
    public string ObfuscatedPrefix { get; } = "_rsv_";
    public string RemoteUrl { get; } = "https://raw.githubusercontent.com/Jo44/LangSwap/refs/heads/main/data/obfuscated_translations.csv";
    public List<ObfuscatedTranslation> RemoteObfuscatedTranslations { get; set; } = [];
    public List<ObfuscatedTranslation> ScannedObfuscatedTranslations { get; set; } = [];
    public List<ObfuscatedTranslation> LocalObfuscatedTranslations { get; set; } = [];

    // Alternative translations
    public List<AlternativeTranslation> AlternativeTranslations { get; set; } = [];

    // Auto swap language
    public bool AutoSwapLanguage { get; set; } = false;

    // Auto send scanned data
    public bool AutoSendScannedData { get; set; } = true;

    // Shortcut
    public bool ShortcutEnabled { get; set; } = true;

    // Primary key
    public int PrimaryKey { get; set; } = (int)VirtualKey.Y;

    // Modifier keys
    public bool Ctrl { get; set; } = false;
    public bool Alt { get; set; } = false;
    public bool Shift { get; set; } = true;

    // UI components
    public bool AlliesCastBarsTarget { get; set; } = true;
    public bool AlliesCastBarsFocus { get; set; } = true;
    public bool AlliesCastBarsPartyList { get; set; } = true;
    public bool EnemiesCastBarsTarget { get; set; } = true;
    public bool EnemiesCastBarsFocus { get; set; } = true;
    public bool EnemiesCastBarsHateList { get; set; } = true;
    public bool ActionTooltip { get; set; } = true;
    public bool ItemTooltip { get; set; } = true;

    // Target & Focus CastBar Addons
    public string TargetInfoAddon { get; } = "_TargetInfo";
    public string TargetInfoName { get; } = "target info";
    public int TargetInfoField { get; } = 44;
    public string TargetCastBarAddon { get; } = "_TargetInfoCastBar";
    public string TargetCastBarName { get; } = "target castbar";
    public int TargetCastBarField { get; } = 5;
    public string FocusCastBarAddon { get; } = "_FocusTargetInfo";
    public string FocusCastBarName { get; } = "focus castbar";
    public int FocusCastBarField { get; } = 16;

    // Player & Allies CastBar Addons
    public string CastBarAddon { get; } = "_CastBar";
    public string CastBarName { get; } = "castbar";
    public int CastBarField { get; } = 11;
    public string PartyListAddon { get; } = "_PartyList";
    public string PartyListName { get; } = "party list";
    public int PartyListStartField { get; } = 16;
    public int PartyListEndField { get; } = 23;
    public int PartyListCastField { get; } = 30;

    // Enemies CastBar Addons
    public string HateListAddon { get; } = "_EnemyList";
    public string HateListName { get; } = "hate list";
    public int HateListStartField { get; } = 4;
    public int HateListEndField { get; } = 11;
    public int HateListCastField { get; } = 16;

    // Action Detail Addon
    public string ActionTooltipSignature { get; } = "E8 ?? ?? ?? ?? 48 8B 43 28 48 8B AF";
    public string ActionDetailAddon { get; } = "ActionDetail";
    public string ActionDetailName { get; } = "action detail";
    public int ActionNameField { get; } = 0;
    public int ActionDescriptionField { get; } = 13;

    // Item Detail Addon
    public string ItemTooltipSignature { get; } = "48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 83 EC ?? 48 8B 42 ?? 4C 8B EA";
    public string ItemDetailAddon { get; } = "ItemDetail";
    public string ItemDetailName { get; } = "item detail";
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

    // Miscellaneous
    public int MaxValidActionID { get; } = 100000;
    public int MaxValidItemID { get; } = 100000;

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
        Plugin.DalamudPluginInterface.SavePluginConfig(this);
    }

}