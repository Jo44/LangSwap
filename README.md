# LangSwap

A Dalamud plugin for Final Fantasy XIV — temporarily switch the HUD/tooltips language while holding a keyboard shortcut.

## Features
- Temporarily switch HUD and tooltip language while the configured key combination is held.
- Configure options through a `Settings` window.
- Slash command: `/langswap`.

## Default values (first run)
- Target language: English
- Primary key: `Y`
- Modifier: `Alt`

## Usage
1. Open the plugin manager in FFXIV.
2. Click the `Settings` button to open the configuration window.
3. Set the `Target Language`, the `Primary Key` and modifier keys (`Ctrl`, `Alt`, `Shift`).
4. Hold the configured combination to switch languages temporarily; release to restore the original language.

You can also open/close the settings window via `/langswap`.

## Development / build
- Target: .NET 10
- Open the project in Visual Studio / Rider, restore packages, build.
- Deploy as a Dalamud plugin following the normal deployment instructions for Dalamud plugins.

## Logs & debugging
- Logs use Dalamud's `IPluginLog`; filter by plugin to view messages.
