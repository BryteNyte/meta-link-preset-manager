# Meta Link Preset Manager

Windows preset manager for Meta/Oculus PC Link settings. Presets are stored as
JSON and converted into command files for `OculusDebugToolCLI.exe`.

## Features

- Create, edit, duplicate, and delete presets.
- Include or omit individual Common, Oculus Link, Service, HUD, and Advanced
  settings.
- Apply presets manually with optional UAC elevation.
- Select an executable or running process, then apply presets automatically
  when that process starts.
- Restore a configured default preset on process or application exit.
- Apply H.264, H.265, or System Default through Meta's per-user Video Codec
  registry override.
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

Video Codec is not exposed by the installed CLI. The application applies that
one setting through Meta's per-user value:

```text
HKEY_CURRENT_USER\Software\Oculus\RemoteHeadset\HEVC
```

H.264 writes `0`, H.265 writes `1`, and System Default deletes the override.
The previous value is restored if the CLI portion of a mixed preset fails or is
cancelled. Codec changes may require reconnecting Link or restarting the PCVR
application.

Settings marked `stored only` in the editor are preserved in preset JSON but
are not emitted into command files. The installed Meta binaries expose those
settings through version-dependent registry or UI paths without a confirmed
CLI setter. This prevents the application from inventing commands that may
silently fail or alter the wrong runtime value.

HUD overlays are compositor features for running PCVR applications. They do not
appear in standalone Quest Home, and may not appear when a game is rendered
exclusively through a non-Meta compositor such as SteamVR.

Game launching is intentionally outside the application. Launch Steam, Xbox,
Game Pass, or other games through their platform and use the process watcher to
apply the matching preset automatically.

The preset editor can populate Process Name from an `.exe` file or from a list
of currently running processes. Only the executable filename is stored; the
selection is used for watching and never launches the application.
