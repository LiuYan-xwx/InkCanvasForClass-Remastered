# InkCanvasForClass-Remastered Development Instructions

InkCanvasForClass-Remastered (ICC-Re) is a .NET 8 WPF classroom ink canvas/whiteboard application optimized for interactive displays and teaching environments. This is a Windows-only desktop application that provides drawing, annotation, and presentation tools for educational settings.

**ALWAYS follow these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

## Working Effectively

### Prerequisites and Environment Setup
- **CRITICAL**: This project ONLY builds and runs on Windows. Do NOT attempt to build on Linux/macOS.
- Install [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) from Microsoft
- Use Visual Studio 2022 or later (recommended) or VS Code with C# extension
- Windows 10/11 required for full WPF functionality and UI testing

### Build Commands (Windows Only)
- **Navigate to Project**: `cd InkCanvasForClass-Remastered` (from repo root) or work from solution root
- **Package Restore**: `dotnet restore` (takes ~10 seconds)
- **Debug Build**: `dotnet build` (takes ~15-30 seconds, NEVER CANCEL - set timeout to 60+ seconds)
- **Release Build**: `dotnet build -c Release` (takes ~20-40 seconds, NEVER CANCEL - set timeout to 60+ seconds)
- **Publish Single File**: `dotnet publish -c Release -f net8.0-windows -r win-x64 --self-contained false -o publish -p:PublishSingleFile=true` (takes ~30-60 seconds, NEVER CANCEL - set timeout to 120+ seconds)

### Running the Application (Windows Only)
- **Debug Mode**: `dotnet run` from project directory
- **From Build Output**: Navigate to `bin/Debug/net8.0-windows/` and run `InkCanvasForClass-Remastered.exe`
- **From Published Output**: Run `publish/InkCanvasForClass-Remastered.exe`

### Validation Requirements
- **CRITICAL**: After any UI changes, ALWAYS run the application and test basic functionality
- **NEVER rely on build success alone** - the app must be tested with user scenarios
- **ALWAYS test these core scenarios after changes**:
  1. Application startup and main window display
  2. Drawing with pen/mouse on the canvas
  3. Switching between pen, eraser, and selection tools
  4. Color picker and brush size adjustments
  5. Clearing the canvas
  6. Settings dialog accessibility and basic settings changes

## Repository Structure

### Key Directories
- `InkCanvasForClass-Remastered/` - Main project directory
  - `MainWindow.xaml/.xaml.cs` - Primary application window
  - `App.xaml/.xaml.cs` - Application entry point and dependency injection setup
  - `Services/` - Core business logic and external integrations
    - `SettingsService.cs` - Application configuration management
    - `PowerPointService.cs` - Microsoft PowerPoint integration
    - `FileFolderService.cs` - File system operations and logging
    - `Logging/` - Custom logging implementation
  - `ViewModels/` - MVVM pattern view models
  - `Windows/` - Additional UI windows (dialogs, tools)
    - `RandWindow.xaml` - Random name picker tool
    - `CountdownTimerWindow.xaml` - Classroom timer
    - `OperatingGuideWindow.xaml` - Help and tutorials
  - `Controls/` - Custom WPF controls and user controls
  - `Helpers/` - Utility classes
    - `TimeMachine.cs` - Undo/redo functionality for ink strokes
  - `Models/` - Data models and settings classes
    - `Settings.cs` - Application settings model
  - `Resources/` - Icons, cursors, styles, and other assets
  - `Converters/` - XAML value converters
  - `Enums/` - Application enumerations

### Important Files
- `InkCanvasForClass-Remastered.csproj` - Project configuration with dependencies
- `Settings.XamlStyler` - XAML formatting configuration
- `.github/workflows/build.yml` - CI/CD pipeline (Windows-only)
- `README.md` - Project documentation (Chinese)
- `CHANGELOG.md` - Version history and changes
- `Manual.md` - User manual and feature documentation

## Development Workflow

### Making Changes
- **ALWAYS build and test on Windows** - Linux/macOS builds will fail
- **Code Style**: Follow existing C# and XAML conventions in the codebase
- **XAML Formatting**: Use XamlStyler configuration in `Settings.XamlStyler`
- **Logging**: Use the custom logging service through dependency injection
- **Settings**: Access application settings via `SettingsService` singleton

### Testing Changes
- **NEVER skip manual testing** - this project has no automated tests
- **UI Changes**: Start the app and verify visual elements render correctly
- **Drawing Features**: Test pen, eraser, shapes, and color changes
- **File Operations**: Test save/load functionality if modified
- **PowerPoint Integration**: Test with actual PowerPoint if you modify `PowerPointService.cs`

### Common Modification Areas
- **UI Adjustments**: Modify `.xaml` files for layout and appearance
- **Drawing Logic**: Update `MainWindow_cs/` files and `Helpers/TimeMachine.cs`
- **Settings/Preferences**: Edit `Models/Settings.cs` and `Services/SettingsService.cs`
- **Tool Windows**: Modify files in `Windows/` directory
- **Application Behavior**: Update `App.xaml.cs` and service classes

### Architecture Notes
- **Dependency Injection**: Uses Microsoft.Extensions.Hosting for service registration
- **MVVM Pattern**: ViewModels are in `ViewModels/` directory
- **Modern UI**: Uses iNKORE.UI.WPF.Modern for modern Windows 11-style UI
- **Logging**: Custom file-based logging system with log compression
- **Settings**: JSON-based settings with automatic save/load

## CI/CD Pipeline
- **GitHub Actions**: `.github/workflows/build.yml` builds on `windows-latest`
- **Trigger**: Runs on every push and manual workflow dispatch
- **Build Command**: Uses exact same publish command as above with `--self-contained false`
- **Artifacts**: Produces single-file executable `InkCanvasForClass-Remastered.exe` as build artifact named `ICC-Re`
- **Runtime**: Targets `win-x64` with .NET 8 Desktop Runtime dependency
- **Build Time**: CI builds typically take 2-5 minutes, NEVER CANCEL - full pipeline timeout should be 10+ minutes
- **Download Artifacts**: Available from GitHub Actions for testing published builds

## Important Dependencies
- **WPF Framework**: Core Windows desktop UI framework
- **Microsoft Office Interop**: For PowerPoint integration features
- **CommunityToolkit.MVVM**: MVVM pattern implementation
- **iNKORE.UI.WPF.Modern**: Modern Windows UI styling
- **Hardcodet.NotifyIcon.Wpf**: System tray functionality
- **Newtonsoft.Json**: Settings serialization

## Troubleshooting
- **Build Errors on Linux/macOS**: Expected - move to Windows environment
- **Missing WindowsDesktop SDK**: Install Visual Studio with .NET desktop development workload
- **PowerPoint Integration Issues**: Requires Microsoft Office installation
- **UI Rendering Problems**: Verify Windows version compatibility and graphics drivers
- **Performance Issues**: Check hardware acceleration and reduce canvas complexity

## Limitations
- **Windows Only**: Cannot build, run, or test on non-Windows platforms
- **No Unit Tests**: Manual testing required for all changes
- **Office Dependency**: Some features require Microsoft Office installation
- **Hardware Requirements**: Optimized for touch displays and interactive whiteboards

## Quick Reference Commands

```bash
# Windows only - these will fail on Linux/macOS
# Can run from solution root or project directory
dotnet restore                           # Restore packages (~10 sec)
dotnet build                            # Debug build (~30 sec)
dotnet build -c Release                 # Release build (~40 sec) 
dotnet run                              # Run in debug mode (from project dir only)
dotnet publish -c Release -f net8.0-windows -r win-x64 --self-contained false -o publish -p:PublishSingleFile=true  # Publish (~60 sec)

# Alternative: run from project directory
cd InkCanvasForClass-Remastered
dotnet run                              # Start application directly
```

## Common Task Reference

### Repository Root Structure
```
InkCanvasForClass-Remastered/
├── .github/workflows/build.yml         # CI/CD pipeline
├── InkCanvasForClass-Remastered/        # Main project
├── InkCanvasForClass-Remastered.sln     # Solution file
├── README.md                           # Project documentation
├── CHANGELOG.md                        # Version history
├── Manual.md                          # User manual
└── Settings.XamlStyler               # XAML formatting rules
```

### Project File Key Dependencies
- **Target Framework**: net8.0-windows (WPF application)
- **UI Framework**: iNKORE.UI.WPF.Modern for modern styling
- **MVVM**: CommunityToolkit.Mvvm for view model patterns
- **Office Integration**: Microsoft.Office.Interop.PowerPoint
- **System Tray**: Hardcodet.NotifyIcon.Wpf
- **Logging**: Microsoft.Extensions.Hosting with custom file logger

Remember: **ALWAYS test your changes by running the application on Windows and exercising the affected functionality manually.**