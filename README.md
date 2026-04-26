# LangSwap

A Dalamud plugin for Final Fantasy XIV that allows you to change the language of castbars and tooltips

## Features
- Swap castbars and tooltips language on the fly (no game restart required)
- Customize spell alternative names

## Limitations
- Spell names can only be translated in some UI components, and not in chat or floating actions on enemies
- Spell names can only be translated when character is in combat
- Some spell names are obfuscated by Square Enix to prevent early data-mining 
  - The plugin can learn on its own or be updated remotely to resolve obfuscated translations 
  - Users can automatically upload new scanned obfuscated translations (if feature is authorized) 
- Action tooltip only translate : `Name` / `Description`
- Item tooltip only translate : `Name` / `Glamour` / `Description` / `Effects` / `Bonuses` / `Materias`

## Default values
- Target language : `English`
- Automatically swap at startup : `Disable`
- Automatically upload scanned data : `Enable`
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
- Enable / disable the automatically swap at startup
- Enable / disable the automatically upload scanned data
- Enable the toggle shortcut then select the keyboard shortcut : Primary key and modifier keys (`Ctrl`, `Alt`, `Shift`)
- Press the toogle shortcut to swap language; press again to restore the original language
- Click the `Customize` button to set alternative translations that will be used when translation is activated 
  - Current alternative translations can be exported to CSV 
  - New alternative translations can be imported from CSV 
- Click the `Advanced` button to display obfuscated translations list 
  - Scanned obfuscated translations can be exported to CSV 
  - Local obfuscated translations can be imported from CSV (for debug)
- Click the `Clear cache` button to clear all translation caches

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
  - Added the ability to reset scanned obfuscated translations
- **v1.6** : 
  - Fixed fallback match via cached actions (truncated)
  - Added sorting into Customize and Advanced UI tables
- **v1.7** : 
  - Added automated sync service that fetches remote translations and uploads scanned translations
  - Improved global UI

## WebService
A webservice is associated to collect scanned obfuscated translations automatically uploaded by users of this plugin

LangSwap-WS project : https://github.com/Jo44/LangSwap-WS

## Development / build
- Target: .NET 10
- Open the project in Visual Studio, restore packages, build
- Deploy as a Dalamud plugin following the normal deployment instructions for Dalamud plugins

## Logs & debugging
- Logs use Dalamud's `IPluginLog`; filter by plugin to view messages 
