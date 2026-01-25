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
3. Set the `Target Language`, modifier keys (`Ctrl`, `Alt`, `Shift`) and the `Primary Key`.
4. Hold the configured combination to switch languages temporarily; release to restore the original language.

You can also open/close the settings window via `/langswap`.

## Development / build
- Target: .NET 10
- Open the project in Visual Studio / Rider, restore packages, build.
- Deploy as a Dalamud plugin following the normal deployment instructions for Dalamud plugins.

## Logs & debugging
- Logs use Dalamud's `IPluginLog`; filter by plugin to view messages.
- If modifier detection behaves unexpectedly, enable Debug/Verbose logs in `Plugin.cs` to inspect values returned by `IKeyState.GetRawValue(...)`.

## Customization / tips
- To show only the `Settings` button in the plugin installer, subscribe to `UiBuilder.OpenConfigUi`.  
  Note: Dalamud warns if no `OpenMainUi` callback is registered. Options:
  - Ignore the warning and register only `OpenConfigUi` (results in only the `Settings` button).
  - Also register `OpenMainUi` to remove the warning (this adds the `Open` entry back).
- Default values can be changed in `Configuration.cs`.

## Contribution
Forks & PRs welcome. See the `main` branch of the repository.
