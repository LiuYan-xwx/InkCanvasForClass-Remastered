# Tray Icon Implementation - MVVM Architecture

## Overview

The tray icon implementation has been modernized to follow MVVM (Model-View-ViewModel) architectural pattern using CommunityToolkit.Mvvm. This provides better separation of concerns, testability, and maintainability.

## Architecture

### TrayIconViewModel (`ViewModels/TrayIconViewModel.cs`)

The ViewModel encapsulates all tray icon state and behavior:

**Observable Properties:**
- `IsMainWindowHidden` - Tracks if the main window is hidden
- `IsFloatingBarFolded` - Tracks if the floating bar is in folded/收纳 mode
- `FoldFloatingBarMenuText` - Dynamic text for the fold menu item ("切换为收纳模式" / "退出收纳模式")
- `IsFoldEyeOffVisible` / `IsFoldEyeOnVisible` - Controls which eye icon is visible
- `IsResetPositionEnabled` - Controls if the reset position menu item is enabled
- `IsFoldFloatingBarEnabled` - Controls if the fold floating bar menu item is enabled  
- `IsForceFullScreenEnabled` - Controls if the force fullscreen menu item is enabled

**RelayCommands:**
- `HideMainWindowCommand` - Hides the main application window
- `ShowMainWindowCommand` - Shows the main application window
- `ForceFullScreenCommand` - Forces the application to fullscreen mode
- `FoldFloatingBarCommand` - Toggles the floating bar fold state
- `ResetFloatingBarPositionCommand` - Resets floating bar to default position
- `RestartAppCommand` - Restarts the application
- `CloseAppCommand` - Closes the application
- `ContextMenuOpenedCommand` - Updates menu state when context menu opens

**Methods:**
- `UpdateMenuState(bool isFloatingBarVisible, bool isMainWindowHidden)` - Centralized state management logic

### View Layer (`App.xaml`)

The XAML now uses data binding instead of manipulating UI elements in code-behind:

```xaml
<MenuItem IsEnabled="{Binding IsForceFullScreenEnabled}"
          Opacity="{Binding IsForceFullScreenEnabled, Converter={StaticResource IsEnabledToOpacityConverter}}">
    ...
</MenuItem>

<MenuItem>
    <MenuItem.Header>
        <TextBlock Text="{Binding FoldFloatingBarMenuText}" />
    </MenuItem.Header>
    <MenuItem.Icon>
        <Grid>
            <Image Visibility="{Binding IsFoldEyeOffVisible, Converter={StaticResource BooleanToVisibilityConverter}}" ... />
            <Image Visibility="{Binding IsFoldEyeOnVisible, Converter={StaticResource BooleanToVisibilityConverter}}" ... />
        </Grid>
    </MenuItem.Icon>
</MenuItem>
```

### Event Adapter Layer (`MainWindow_cs/MW_TrayIcon.cs`)

Event handlers act as adapters that delegate to ViewModel commands:

```csharp
private void ForceFullScreenTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
{
    var trayViewModel = GetService<TrayIconViewModel>();
    trayViewModel?.ForceFullScreenCommand.Execute(null);
}
```

This approach provides:
- **Backward compatibility** - Existing XAML event handlers still work
- **Clean migration path** - Can gradually remove event handlers in favor of pure command bindings
- **Testability** - ViewModel logic can be unit tested without UI dependencies

## Dependency Injection

The `TrayIconViewModel` is registered as a singleton in `App.xaml.cs`:

```csharp
services.AddSingleton<TrayIconViewModel>();
```

And initialized with the TaskbarIcon's DataContext:

```csharp
var taskbar = (TaskbarIcon)FindResource("TaskbarTrayIcon");
var trayIconViewModel = GetService<TrayIconViewModel>();
taskbar.DataContext = trayIconViewModel;
```

## Benefits of MVVM Approach

1. **Separation of Concerns**: UI logic is separate from business logic
2. **Testability**: ViewModel can be unit tested independently of the UI
3. **Maintainability**: State management is centralized in one place
4. **Declarative**: XAML bindings are more readable than imperative code-behind
5. **Type Safety**: Properties are strongly typed instead of cast-based UI element access
6. **No Fragile Indexing**: Eliminates `s.Items[s.Items.Count - 5]` type of brittle code
7. **Observable**: Changes to properties automatically update UI through INotifyPropertyChanged

## Migration from Old Implementation

### Old Approach (Code-Behind)
```csharp
// Brittle index-based access
Image icon = (Image)((Grid)((MenuItem)s.Items[^5]).Icon).Children[0];
icon.Visibility = Visibility.Hidden;
TextBlock text = (TextBlock)((SimpleStackPanel)((MenuItem)s.Items[^5]).Header).Children[0];
text.Text = "退出收纳模式";
```

### New Approach (MVVM)
```csharp
// ViewModel
IsFoldEyeOffVisible = false;
FoldFloatingBarMenuText = "退出收纳模式";

// XAML binding automatically updates UI
<Image Visibility="{Binding IsFoldEyeOffVisible, Converter={StaticResource BooleanToVisibilityConverter}}" />
<TextBlock Text="{Binding FoldFloatingBarMenuText}" />
```

## Future Improvements

1. **Full Command Binding**: Replace event handlers with direct command bindings in XAML:
   ```xaml
   <MenuItem Command="{Binding ForceFullScreenCommand}" />
   ```

2. **Unit Tests**: Add unit tests for TrayIconViewModel logic

3. **ItemsSource Binding**: Consider using ItemsSource for dynamic menu generation

4. **Separate Menu Resource**: Move context menu to separate ResourceDictionary for better organization

## Logging

The ViewModel includes logging for all user actions via dependency injection:

```csharp
_logger.LogInformation("Main window hidden via tray icon");
```

This provides better debugging and audit trail capabilities.
