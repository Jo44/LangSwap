# LangSwap

A Dalamud plugin for Final Fantasy XIV that allows you to change the language of castbars and tooltips in FFXIV by pressing a keyboard shortcut.

## Features
- Swap castbars and tooltips language on the fly with a keyboard shortcut.
- Configure options through a `Settings` window.
- Slash command: `/langswap`.

## Default values (first run)
- Target language: English
- Primary key: `Y`
- Modifier: `Alt`

## Usage
1. Open the plugin manager in FFXIV.
2. Click the `Settings` button to open the configuration window.
3. Set the `Target Language`.
4. Set the shortcut : `Primary Key` and modifier keys (`Ctrl`, `Alt`, `Shift`).
5. Set the components who will be affected : `Castbars`, `Item details` and `Skill details`.
6. Press the configured combination to switch languages; press again to restore the original language.

You can also open/close the settings window via `/langswap`.

## Development / build
- Target: .NET 10
- Open the project in Visual Studio / Rider, restore packages, build.
- Deploy as a Dalamud plugin following the normal deployment instructions for Dalamud plugins.

## Logs & debugging
- Logs use Dalamud's `IPluginLog`; filter by plugin to view messages.
