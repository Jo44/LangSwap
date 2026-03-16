using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
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

    // Language : 0 = Japanese, 1 = English, 2 = German, 3 = French
    public byte ClientLanguage { get; set; } = 1;
    public byte TargetLanguage { get; set; } = 1;

    // Primary key
    public int PrimaryKey { get; set; } = (int)VirtualKey.Y;

    // Modifier keys
    public bool Ctrl { get; set; } = false;
    public bool Alt { get; set; } = false;
    public bool Shift { get; set; } = true;

    // UI components
    public bool ActionTooltip { get; set; } = true;
    public bool ItemTooltip { get; set; } = true;
    public bool AlliesCastBars { get; set; } = true;
    public bool EnemiesCastBars { get; set; } = true;

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

    // Allies CastBar Addons
    public string CastBarAddon { get; } = "_CastBar";
    public string PartyListAddon { get; } = "_PartyList";

    // Enemies CastBar Addons
    public string TargetCastBarAddon { get; } = "_TargetInfoCastBar";
    public string FocusCastBarAddon { get; } = "_FocusTargetInfo";
    public string EnemyListAddon { get; } = "_EnemyList";

    // Miscellaneous
    public int MaxValidActionId { get; } = 100000;
    public int MaxValidItemId { get; } = 100000;
    public char GlamouredSymbol { get; } = '\uE03B'; // Mirage symbol
    public char HighQualitySymbol { get; } = '\uE03C'; // HQ symbol

    // Save configuration
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }

}