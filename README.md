# LangSwap

A Dalamud plugin for Final Fantasy XIV that allows you to change the language of castbars and tooltips in FFXIV

## Features
- Swap castbars and tooltips language on the fly (no game restart required)
- Customize spell alternative names
- Some spell names are obfuscated by Square Enix to prevent early data-mining  
	-> The plugin can learn on its own or be updated remotely to resolve this issue

## Limitations
- Spell names can only be translated in some UI components, and not in chat or floating actions on enemies
- Spell names can only be translated when character is in combat
- Action tooltip only translate : `Name` / `Description`
- Item tooltip only translate : `Name` / `Glamour` / `Description` / `Effects` / `Bonuses` / `Materias`

## Default values
- Target language : `English`
- Toggle shortcut : `Enable`
- Keyboard shortcut : `Shift + Y`
- UI Components : `Ally target` / `Ally focus` / `Party list` / `Enemy target` / `Enemy focus` / `Enmity list` / `Action tooltip` / `Item tooltip`

## Usage
- Open the plugin manager in FFXIV
- Click the `Settings` button to open the configuration window
- Select the target language : `Japanese` / `English` / `German` / `French`
- Select the UI components that will be translated : `Ally target` / `Ally focus` / `Party list` / `Enemy target` / `Enemy focus` / `Enmity list` / `Action tooltip` / `Item tooltip`
- Click the `Enable` button to activate translation

You can also :  
- Open the settings window via the slash command : `/langswap`
- Enable the toggle shortcut then select the keyboard shortcut : Primary key and modifier keys (`Ctrl`, `Alt`, `Shift`)
- Press the toogle shortcut to swap language; press again to restore the original language
- Click the `Customize` button to set alternative translations that will be used when translation is activated
- New alternative translations can be imported from CSV
- Current alternative translations can be exported to CSV
- Click the `Clear cache` button to clear all translation cache

## Updates
- **v1.0** : 
  - Initial release
- **v1.1** : 
  - Added support for keyboard modifiers (Ctrl, Alt, Shift) next to the primary key
  - Added options to toggle specific UI components (Target, Focus, Party List, Hate List)
- **v1.2** : 
  - Added "Alternative Translations" configuration allowing users to set custom spell names
  - Improved caching for translation queries
- **v1.3** : 
  - Added support for obfuscated action names to handle Square Enix data-mining protections
  - Automatic resolution of obfuscated translations via remote CSV data
- **v1.4** : 
  - Important CPU and memory optimizations
  - Added the ability to scan, import, export, and reset local obfuscated translations via CSV
  - Overall UI polish and stability improvements
- **v1.5** : 
  - Added the ability to reset scanned obfuscated translations via CSV

## Development / build
- Target: .NET 10
- Open the project in Visual Studio, restore packages, build
- Deploy as a Dalamud plugin following the normal deployment instructions for Dalamud plugins

## Logs & debugging
- Logs use Dalamud's `IPluginLog`; filter by plugin to view messages
- Hidden debug menu allows to :  
	-> Import obfuscation resolutions from CSV  
	-> Export obfuscation resolutions to CSV  
