# Meta Link Preset Manager

Windows preset manager for Meta/Oculus PC Link settings. Presets are stored as
JSON and converted into command files for `OculusDebugToolCLI.exe`.

## Features

- Create, edit, duplicate, and delete presets.
- Include or omit individual Common, Oculus Link, Service, HUD, and Advanced
  settings.
- Apply presets manually with optional UAC elevation.
- Apply a preset and launch a Steam URL, executable, or shortcut.
- Watch configured process names and apply presets on process start.
- Restore a configured default preset on process or application exit.
- Activate Performance, Stereo Debug, or Layer HUD overlays for PCVR
  applications running through wired Link or Air Link.
- Keep generated command files and daily logs under
  `%AppData%\MetaLinkPresetManager`.

## Build and run

```powershell
dotnet build MetaLinkPresetManager.sln
dotnet run --project src\MetaLinkPresetManager.WinForms
```

The first launch detects current and legacy default CLI locations, including:

```text
C:\Program Files\Meta Horizon\Support\oculus-diagnostics\OculusDebugToolCLI.exe
C:\Program Files\Oculus\Support\oculus-diagnostics\OculusDebugToolCLI.exe
```

## CLI compatibility

The application generates visible text files so commands can be inspected
before or after applying a preset. Meta can change CLI command support between
runtime versions. Verify commands against the installed runtime, beginning with:

```text
service set-client-fov-tan-angle-multiplier 0.80 0.70
server:asw.Off
exit
```

The application does not write directly to undocumented registry or service
internals.

Settings marked `stored only` in the editor are preserved in preset JSON but
are not emitted into command files. The installed Meta binaries expose those
settings through version-dependent registry or UI paths without a confirmed
CLI setter. This prevents the application from inventing commands that may
silently fail or alter the wrong runtime value.

HUD overlays are compositor features for running PCVR applications. They do not
appear in standalone Quest Home, and may not appear when a game is rendered
exclusively through a non-Meta compositor such as SteamVR.
