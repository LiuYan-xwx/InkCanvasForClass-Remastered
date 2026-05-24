# Copilot cloud agent onboarding (InkCanvasForClass-Remastered)

## Project at a glance
- Single-project **.NET 10 WPF** desktop app targeting `net10.0-windows7.0`.
- Solution file: `InkCanvasForClass-Remastered.slnx`.
- Main project: `InkCanvasForClass-Remastered/InkCanvasForClass-Remastered.csproj`.
- App entry and DI setup: `InkCanvasForClass-Remastered/App.xaml.cs`.
- Main UI logic is mostly in `InkCanvasForClass-Remastered/MainWindow.xaml.cs` with partial files under `InkCanvasForClass-Remastered/MainWindow_cs/`.

## Important folders
- `InkCanvasForClass-Remastered/Controls`, `Converters`, `Helpers`, `Services`, `ViewModels`, `Windows`, `Models`: core app code.
- `InkCanvasForClass-Remastered/Resources`: icons/cursors/images and other UI assets.
- `.github/workflows/build.yml`: CI build workflow (Windows runner, `dotnet publish`).

## Build and validation workflow
Use repository-root commands:

1. Baseline build (Linux cloud agent):
   - `dotnet build ./InkCanvasForClass-Remastered.slnx -c Release -p:EnableWindowsTargeting=true`
2. CI-equivalent publish command (from workflow):
   - `dotnet publish -c Release -f net10.0-windows7.0 -r win-x64 --self-contained false -o publish`
3. Test check:
   - `dotnet test ./InkCanvasForClass-Remastered.slnx -c Release -p:EnableWindowsTargeting=true`
   - There are currently no dedicated test projects; this is mostly a validation pass.

## Errors encountered in cloud-agent environment and workarounds
1. **Error:** `NETSDK1100: To build a project targeting Windows on this operating system, set the EnableWindowsTargeting property to true.`
   - **When seen:** Running `dotnet build ./InkCanvasForClass-Remastered.slnx -c Release` on Linux.
   - **Workaround:** Add `-p:EnableWindowsTargeting=true` to build/test commands.

2. **Warning:** `NU1701` for package `MicrosoftOfficeCore 15.0.0` (restored using .NET Framework TFM compatibility fallback).
   - **When seen:** Build/test restore on .NET 10 target.
   - **Workaround used:** No immediate change required for onboarding; treat as known compatibility warning unless task is specifically dependency modernization.

## Editing guidance for future agents
- Keep changes scoped: this is a large WPF code-behind-heavy codebase; avoid broad refactors unless explicitly requested.
- Preserve existing architecture patterns:
  - DI and startup wiring in `App.xaml.cs`
  - Settings persisted through `Services/SettingsService.cs` and `Models/Settings.cs`
  - Core interaction logic in `MainWindow.xaml(.cs)` and `MainWindow_cs/*`
- Do not commit `bin/` or `obj/` artifacts.
- Prefer small, verifiable increments and run the Linux-compatible build command above after code changes.
