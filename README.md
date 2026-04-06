# LangSwap

A Dalamud plugin for Final Fantasy XIV that allows you to change the language of castbars and tooltips in FFXIV

## Features
- Swap castbars and tooltips language on the fly
- Customize spell alternative translations
- Some spell names are obfuscated by Square Enix to prevent early data-mining  
	-> the plugin can learn on its own or be updated remotely to resolve this issue

## Limitations
- Spell names can only be translated in some UI components, and not in chat or floating actions on enemies
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
- Select the `Target Language` : `Japanese` / `English` / `German` / `French`
- Select the UI components that will be translated : `Ally target` / `Ally focus` / `Party list` / `Enemy target` / `Enemy focus` / `Enmity list` / `Action tooltip` / `Item tooltip`
- Click the `Enable` button to activate translation

You can also :  
- Open the settings window via the slash command : `/langswap`
- Enable the toggle shortcut then select the keyboard shortcut : `Primary Key` and modifier keys (`Ctrl`, `Alt`, `Shift`)
- Press the toogle shortcut to swap language; press again to restore the original language
- Click the `Customize` button to set alternative translations that will be used when translation is activated
- New alternative translations can be imported from CSV
- Current alternative translations can be exported to CSV
- Click the `Clear cache` button to clear all translation cache

## Development / build
- Target: .NET 10
- Open the project in Visual Studio, restore packages, build
- Deploy as a Dalamud plugin following the normal deployment instructions for Dalamud plugins

## Logs & debugging
- Logs use Dalamud's `IPluginLog`; filter by plugin to view messages
- Hidden debug menu allows to :  
	-> export obfuscation resolutions to CSV  
	-> import obfuscation resolutions from CSV  
