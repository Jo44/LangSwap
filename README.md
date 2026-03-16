# LangSwap

A Dalamud plugin for Final Fantasy XIV that allows you to change the language of castbars and tooltips in FFXIV by pressing a keyboard shortcut

## Features
- Swap castbars and tooltips language on the fly with a keyboard shortcut
- Configure options through a `Settings` window
- Slash command: `/langswap`

## Limitations
- Action tooltip only translate : name, description
- Item tooltip only translate : name, glamour, description, bonuses and materias

## Default values
- Target language: `English`
- Keyboard shortcut: `Alt + Y`
- UI Components: `Action tooltip` / `Item tooltip` / `AlliesCastBars` / `EnemiesCastBars`

## Usage
1. Open the plugin manager in FFXIV
2. Click the `Settings` button to open the configuration window
3. Set the `Target Language`
4. Set the keyboard shortcut : `Primary Key` and modifier keys (`Ctrl`, `Alt`, `Shift`)
5. Set the UI components that will be activated : `Action tooltip` / `Item tooltip` / `AlliesCastBars` / `EnemiesCastBars`
6. Press the configured combination to switch languages; press again to restore the original language

You can also open/close the settings window via `/langswap`

## Development / build
- Target: .NET 10
- Open the project in Visual Studio, restore packages, build
- Deploy as a Dalamud plugin following the normal deployment instructions for Dalamud plugins

## Logs & debugging
- Logs use Dalamud's `IPluginLog`; filter by plugin to view messages
