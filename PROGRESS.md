# Meta Link Preset Manager Progress

Last updated: 2026-06-11

## Status

The MVP described in the implementation plan is implemented and builds
successfully.

## Completed

- Activated Performance, Stereo Debug, and Layer HUD command generation.
- Replaced independent HUD options with one active Visible HUD selector and a
  contextual mode selector.
- Added explicit HUD reset commands so presets cannot leave stale HUD families
  active.
- Added startup migration that configures the selected default preset to disable
  HUD overlays.
- Added legacy HUD JSON inference and human-readable HUD mode labels.
- Added Common, Oculus Link, Service, HUD, and Advanced settings tabs.
- Added all requested Service fields, including a separate PC Asynchronous
  Spacewarp property.
- Added distortion curvature, video codec, sliced encoding, and dynamic bitrate
  Link fields.
- Added Visible, Performance, Stereo Debug, and Layer HUD fields.
- Added optional Lost Frame Capture storage under Advanced.
- Added confirmed installed-CLI mappings for pixels per display pixel, mipmap
  generation, mip bias, FOV stencil, adaptive GPU scale, frame drop indicator,
  and pose injection.
- Marked version-dependent settings as stored only instead of generating
  unverified commands.
- Fixed Browse availability and launch type/target consistency validation.
- Fixed enum ComboBox reload behavior that caused persisted Local Dimming values
  to display as Default after relaunch.
- Updated Apply Preset and Apply + Launch to save and validate current editor
  values before applying.
- Added Local Dimming JSON round-trip coverage for Default, Disabled, and
  Enabled.
- Created the .NET 8 solution with Core, WinForms, and test projects.
- Added JSON application settings and preset persistence.
- Added initial Default Link and AMS2 presets.
- Added detection for current Meta Horizon and legacy Oculus CLI paths.
- Added Oculus command-file generation.
- Added manual preset application with optional UAC elevation.
- Added Steam URL, executable, and shortcut launching.
- Added Apply + Launch and Launch Only workflows.
- Added process polling and automatic preset application.
- Added default-preset restoration after a watched process exits.
- Added optional default restoration when the application exits.
- Added preset creation, editing, duplication, and deletion.
- Added nullable controls for individual Oculus settings.
- Added validation, status output, generated-file access, and daily logs.
- Added serialized CLI application to prevent overlapping apply operations.
- Added deterministic .NET 8 and NuGet workspace configuration.

## Verification

- Release build succeeds with zero warnings.
- All 46 automated tests pass.
- Local Dimming was changed to Disabled through the real WinForms UI, saved,
  closed, reopened, and verified as Disabled in both JSON and the relaunched UI.
- Formatting verification passes.
- First-run JSON and log files are created successfully.
- The WinForms application opens, responds, starts its watcher, and shuts down
  cleanly.
- The installed CLI was detected at:

```text
C:\Program Files\Meta Horizon\Support\oculus-diagnostics\OculusDebugToolCLI.exe
```

## Not Yet Verified

- No preset was applied to the live Meta runtime during development.
- FOV and ASW commands should be verified manually against the installed Meta
  runtime.
- Encode bitrate, resolution width, link sharpening, and local dimming command
  names may vary between Meta runtime versions and need live confirmation.
- Game launch and external process detection should be tested with an installed
  game.
- Stored-only fields require confirmed CLI setters before they can participate
  in Apply. They currently save, load, duplicate, and round-trip through JSON.
- HUD overlays require a running PCVR application using the Meta/Oculus
  compositor through wired Link or Air Link. They are not standalone Quest
  overlays.

## Run

From the repository root:

```powershell
dotnet run --project src\MetaLinkPresetManager.WinForms
```

Or run the Release executable:

```powershell
.\src\MetaLinkPresetManager.WinForms\bin\Release\net8.0-windows\MetaLinkPresetManager.WinForms.exe
```

## Application Data

Runtime settings, presets, generated command files, and logs are stored in:

```text
%AppData%\MetaLinkPresetManager
```

## Next Steps

1. Apply a test preset containing only FOV and ASW settings.
2. Confirm the changes in Oculus Debug Tool.
3. Verify each additional CLI command individually.
4. Test Apply + Launch with AMS2.
5. Test automatic application and default restoration.
6. Add import/export and packaging after live CLI verification.
