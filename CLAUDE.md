# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

.NET 10 WPF desktop application — a lightweight digital whiteboard optimized for interactive flat panels (希沃/SMART boards). Single-project solution using `InkCanvasForClass-Remastered.slnx`.

## Build & Test

```powershell
# Debug build
dotnet build .\InkCanvasForClass-Remastered.slnx -c Debug

# Release build + publish
dotnet publish -c Release -f net10.0-windows7.0 -r win-x64 --self-contained false -o publish

# On Linux/macOS cloud agents, add: -p:EnableWindowsTargeting=true
```

There are no dedicated test projects. `dotnet test` is a validation pass only.

## Architecture

### DI & Startup
- **`App.xaml.cs`** — Entry point. Uses `Microsoft.Extensions.Hosting` for DI and service lifecycle.
- Registers services as singletons (`SettingsService`, `PowerPointService`, `FileFolderService`, `NotificationService`), ViewModels as transient, windows as singleton/transient via `ConfigureServices()`.
- Static service locator via `IAppHost.GetService<T>()` / `TryGetService<T>()` for non-DI contexts.

### Core Layers

| Layer | Key Files | Purpose |
|-------|-----------|---------|
| **Views** | `MainWindow.xaml` (544KB), `MainWindow.xaml.cs` (220KB) | Massive single-window UI with all toolbars, ink canvas, side panels. Code-behind-heavy. |
| **ViewModels** | `MainViewModel.cs`, `RandViewModel.cs`, `NamesInputViewModel.cs`, `TrayIconMenuViewModel.cs` | MVVM via `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`). |
| **Models** | `Settings.cs`, `CommonDirectories.cs`, `NotificationEventArgs.cs` | Settings is `ObservableObject` with ~80 properties serialized as JSON. |
| **Services** | `SettingsService.cs`, `PowerPointService.cs`, `FileFolderService.cs`, `NotificationService.cs` | Settings persistence (JSON), PowerPoint COM interop lifecycle, file/folder management, in-app toast notifications. |
| **Controls** | `SettingsControl`, `FloatingBarButton`, `PPTNavigationButton/Panel`, `AutoFoldAppToggle` | Custom WPF controls for specific UI patterns. |
| **Helpers** | `MultiTouchInput.cs`, `InkRecognizeHelper.cs`, `ScreenshotHelper.cs`, `TimeMachine.cs`, `AnimationsHelper.cs`, `GZipHelper.cs`, `Hotkey.cs`, etc. | Utility classes for gestures, ink recognition, screenshot, undo/redo, animations, log compression, hotkeys. |
| **Converters** | `BoolToBrushConverter.cs`, `EnumToVisibilityConverter.cs`, etc. | Standard WPF `IValueConverter` implementations. |
| **Windows** | `CountdownTimerWindow`, `NamesInputWindow`, `RandWindow`, `YesOrNoNotificationWindow` | Secondary popup windows for specific features. |

### Key Design Decisions

- **Code-behind-heavy**: Most interaction logic lives in `MainWindow.xaml.cs` with partial helpers in `MainWindow_cs/MW_Icons.cs`. Avoid broad refactors unless explicitly requested.
- **App modes**: Two modes via `AppMode` enum — `Normal` (annotation overlay) and `WhiteBoard` (full-screen whiteboard).
- **PowerPoint integration**: `PowerPointService` wraps Microsoft.Office.Interop.PowerPoint COM — connects to running PowerPoint instance, handles slide show events (begin/end/next), and manages ink per slide.
- **Settings**: JSON file via Newtonsoft.Json. `Settings` class uses `[ObservableProperty]` for property change notifications.
- **Ink Canvas**: WPF's `System.Windows.Ink` APIs (`Stroke`, `StrokeCollection`, `InkCanvas`). Custom rendering and hit-testing.
- **Logging**: Built on `Microsoft.Extensions.Logging` — file logger with auto-compression (`GZipHelper`), colorized console in Debug builds.

## Important Caveats

- **`MainWindow.xaml` (544KB) and `MainWindow.xaml.cs` (220KB) are very large files.** Be careful editing them — prefer targeted insertions/deletions over rewrites.
- `MicrosoftOfficeCore` and `Microsoft.Office.Interop.PowerPoint` packages produce `NU1701` warning on non-Windows builds (restored via .NET Framework compatibility fallback). This is expected.
- The project targets `net10.0-windows7.0` — Windows-specific APIs throughout.
- No unit/integration test projects exist.
- Project uses single-instance mutex (`InkCanvasForClass-Remastered`).
- Key NuGet dependencies: `iNKORE.UI.WPF.Modern` (Fluent/WinUI styling), `CommunityToolkit.Mvvm`, `Newtonsoft.Json`, `Hardcodet.NotifyIcon.Wpf`.
