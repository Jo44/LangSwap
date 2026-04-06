# LangSwap

A Dalamud plugin for Final Fantasy XIV that allows you to change the language of tooltips and castbars in FFXIV

## Features
- Swap tooltips and castbars language on the fly
- Customize alternative translations
- Some spell names are obfuscated by Square Enix to prevent early data-mining : the plugin can learn on its own or be updated remotely to resolve this issue

## Limitations
- Spell names can only be translated in some UI components, and not in chat or floating actions on enemies
- Action tooltip only translate : name, description
- Item tooltip only translate : name, glamour, description, effects, bonuses and materias

## Default values
- Target language: `English`
- Toggle shortcut : `Enable`
- Keyboard shortcut: `Shift + Y`
- UI Components: `Ally target` / `Ally focus` / `Party list` / `Enemy target` / `Enemy focus` / `Enmity list` / `Action tooltip` / `Item tooltip`

## Usage
01. Open the plugin manager in FFXIV
02. Click the `Settings` button to open the configuration window
03. Select the `Target Language` : `Japanese` / `English` / `German` / `French`
04. Select the UI components that will be translated : `Ally target` / `Ally focus` / `Party list` / `Enemy target` / `Enemy focus` / `Enmity list` / `Action tooltip` / `Item tooltip`
05. Click the `Enable` button to activate translation

You can also :
06. Open the settings window via the slash command : `/langswap`
07. Enable the toggle shortcut then select the keyboard shortcut : `Primary Key` and modifier keys (`Ctrl`, `Alt`, `Shift`)
08. Press the toogle shortcut to swap language; press again to restore the original language
09. Click the `Customize` button to set alternative translations that will be used when translation is activated
10. New alternative translations can be imported from CSV
11. Current alternative translations can be exported to CSV
12. Click the `Clear cache` button to clear all translation cache

## Development / build
- Target: .NET 10
- Open the project in Visual Studio, restore packages, build
- Deploy as a Dalamud plugin following the normal deployment instructions for Dalamud plugins

## Logs & debugging
- Logs use Dalamud's `IPluginLog`; filter by plugin to view messages
- Hidden debug menu allows to :
	-> export obfuscation resolutions to CSV
	-> import obfuscation resolutions from CSV
