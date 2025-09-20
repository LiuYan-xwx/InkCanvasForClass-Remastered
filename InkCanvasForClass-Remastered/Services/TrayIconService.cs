using Hardcodet.Wpf.TaskbarNotification;
using iNKORE.UI.WPF.Controls;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace InkCanvasForClass_Remastered.Services
{
    public class TrayIconService
    {
        private readonly Application _application;
        private readonly ILogger<TrayIconService> _logger;
        private TaskbarIcon? _taskbarIcon;

        public TrayIconService(ILogger<TrayIconService> logger)
        {
            _application = Application.Current;
            _logger = logger;
        }

        public void Initialize()
        {
            try
            {
                _taskbarIcon = (TaskbarIcon?)_application.FindResource("TaskbarTrayIcon");
                _logger.LogInformation("TrayIconService initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize TrayIconService");
                throw;
            }
        }

        public void SysTrayMenu_Opened(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not ContextMenu contextMenu) 
                {
                    _logger.LogWarning("SysTrayMenu_Opened called with invalid sender type");
                    return;
                }

                var menuItems = GetTrayMenuItems(contextMenu);
                var mainWin = (MainWindow)Application.Current.MainWindow;
                
                if (!mainWin.IsLoaded) 
                {
                    _logger.LogDebug("MainWindow not loaded, skipping menu update");
                    return;
                }

                UpdateFloatingBarMenuState(menuItems, mainWin);
                _logger.LogDebug("Tray menu opened and updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SysTrayMenu_Opened");
            }
        }

        public void CloseAppTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWin = (MainWindow)Application.Current.MainWindow;
                if (mainWin.IsLoaded)
                {
                    _logger.LogInformation("Closing application via tray icon");
                    mainWin.BtnExit_Click(null, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing application via tray icon");
            }
        }

        public void RestartAppTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWin = (MainWindow)Application.Current.MainWindow;
                if (mainWin.IsLoaded)
                {
                    _logger.LogInformation("Restarting application via tray icon");
                    mainWin.BtnRestart_Click(null, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting application via tray icon");
            }
        }

        public void ForceFullScreenTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            var mainWin = (MainWindow)Application.Current.MainWindow;
            if (!mainWin.IsLoaded) return;

            _ = InkCanvasForClass_Remastered.MainWindow.MoveWindow(
                new WindowInteropHelper(mainWin).Handle, 0, 0,
                System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, 
                System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height, true);
            
            InkCanvasForClass_Remastered.MainWindow.ShowNewMessage(
                $"已强制全屏化：{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width}x{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height}（缩放比例为{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / SystemParameters.PrimaryScreenWidth}x{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / SystemParameters.PrimaryScreenHeight}）");
        }

        public void FoldFloatingBarTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            var mainWin = (MainWindow)Application.Current.MainWindow;
            if (!mainWin.IsLoaded) return;

            if (mainWin.isFloatingBarFolded)
                mainWin.UnFoldFloatingBar_MouseUp(new object(), null);
            else
                mainWin.FoldFloatingBar_MouseUp(new object(), null);
        }

        public void ResetFloatingBarPositionTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            var mainWin = (MainWindow)Application.Current.MainWindow;
            if (!mainWin.IsLoaded || mainWin.isFloatingBarFolded) return;

            bool isInPPTPresentationMode = false;
            _application.Dispatcher.Invoke(() =>
            {
                isInPPTPresentationMode = mainWin.BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible;
            });

            if (!isInPPTPresentationMode)
                mainWin.PureViewboxFloatingBarMarginAnimationInDesktopMode();
            else
                mainWin.PureViewboxFloatingBarMarginAnimationInPPTMode();
        }

        public void HideICCMainWindowTrayIconMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem) return;
            
            var mainWin = (MainWindow)Application.Current.MainWindow;
            if (mainWin.IsLoaded)
            {
                mainWin.Hide();
                var contextMenu = _taskbarIcon?.ContextMenu;
                if (contextMenu != null)
                {
                    var menuItems = GetTrayMenuItems(contextMenu);
                    SetMenuItemsEnabled(menuItems, false);
                }
            }
            else
            {
                menuItem.IsChecked = false;
            }
        }

        public void HideICCMainWindowTrayIconMenuItem_UnChecked(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem) return;
            
            var mainWin = (MainWindow)Application.Current.MainWindow;
            if (mainWin.IsLoaded)
            {
                mainWin.Show();
                var contextMenu = _taskbarIcon?.ContextMenu;
                if (contextMenu != null)
                {
                    var menuItems = GetTrayMenuItems(contextMenu);
                    SetMenuItemsEnabled(menuItems, true);
                }
            }
            else
            {
                menuItem.IsChecked = false;
            }
        }

        private TrayMenuItems GetTrayMenuItems(ContextMenu contextMenu)
        {
            try
            {
                // Find menu items by name instead of using hard-coded indices
                var hideICCMainWindow = FindMenuItemByName(contextMenu, "HideICCMainWindowTrayIconMenuItem");
                var foldFloatingBar = FindMenuItemByName(contextMenu, "FoldFloatingBarTrayIconMenuItem");
                var resetFloatingBarPosition = FindMenuItemByName(contextMenu, "ResetFloatingBarPositionTrayIconMenuItem");
                var forceFullScreen = FindMenuItemByName(contextMenu, "ForceFullScreenTrayIconMenuItem");

                if (hideICCMainWindow == null || foldFloatingBar == null || resetFloatingBarPosition == null || forceFullScreen == null)
                {
                    _logger.LogError("Could not find required menu items in tray context menu");
                    throw new InvalidOperationException("Could not find required menu items in tray context menu");
                }

                // Extract the specific UI elements from the FoldFloatingBarTrayIconMenuItem
                var foldIcon = foldFloatingBar.Icon as Grid;
                var foldHeader = foldFloatingBar.Header as SimpleStackPanel;

                if (foldIcon?.Children.Count < 2 || foldHeader?.Children.Count < 1)
                {
                    _logger.LogError("FoldFloatingBarTrayIconMenuItem structure is not as expected");
                    throw new InvalidOperationException("FoldFloatingBarTrayIconMenuItem structure is not as expected");
                }

                _logger.LogDebug("Successfully retrieved all tray menu items");
                return new TrayMenuItems
                {
                    FoldFloatingBarIconEyeOff = (Image)foldIcon.Children[0],
                    FoldFloatingBarIconEyeOn = (Image)foldIcon.Children[1],
                    FoldFloatingBarHeaderText = (TextBlock)foldHeader.Children[0],
                    ResetFloatingBarPosition = resetFloatingBarPosition,
                    HideICCMainWindow = hideICCMainWindow,
                    FoldFloatingBar = foldFloatingBar,
                    ForceFullScreen = forceFullScreen
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tray menu items");
                throw;
            }
        }

        private MenuItem? FindMenuItemByName(ContextMenu contextMenu, string name)
        {
            foreach (var item in contextMenu.Items)
            {
                if (item is MenuItem menuItem && menuItem.Name == name)
                {
                    return menuItem;
                }
            }
            return null;
        }

        private void UpdateFloatingBarMenuState(TrayMenuItems menuItems, MainWindow mainWin)
        {
            if (mainWin.isFloatingBarFolded)
            {
                menuItems.FoldFloatingBarIconEyeOff.Visibility = Visibility.Hidden;
                menuItems.FoldFloatingBarIconEyeOn.Visibility = Visibility.Visible;
                menuItems.FoldFloatingBarHeaderText.Text = "退出收纳模式";
                
                if (!menuItems.HideICCMainWindow.IsChecked)
                {
                    menuItems.ResetFloatingBarPosition.IsEnabled = false;
                    menuItems.ResetFloatingBarPosition.Opacity = 0.5;
                }
            }
            else
            {
                menuItems.FoldFloatingBarIconEyeOff.Visibility = Visibility.Visible;
                menuItems.FoldFloatingBarIconEyeOn.Visibility = Visibility.Hidden;
                menuItems.FoldFloatingBarHeaderText.Text = "切换为收纳模式";
                
                if (!menuItems.HideICCMainWindow.IsChecked)
                {
                    menuItems.ResetFloatingBarPosition.IsEnabled = true;
                    menuItems.ResetFloatingBarPosition.Opacity = 1;
                }
            }
        }

        private void SetMenuItemsEnabled(TrayMenuItems menuItems, bool enabled)
        {
            var opacity = enabled ? 1.0 : 0.5;
            
            menuItems.ResetFloatingBarPosition.IsEnabled = enabled;
            menuItems.FoldFloatingBar.IsEnabled = enabled;
            menuItems.ForceFullScreen.IsEnabled = enabled;
            
            menuItems.ResetFloatingBarPosition.Opacity = opacity;
            menuItems.FoldFloatingBar.Opacity = opacity;
            menuItems.ForceFullScreen.Opacity = opacity;
        }

        private class TrayMenuItems
        {
            public Image FoldFloatingBarIconEyeOff { get; init; } = null!;
            public Image FoldFloatingBarIconEyeOn { get; init; } = null!;
            public TextBlock FoldFloatingBarHeaderText { get; init; } = null!;
            public MenuItem ResetFloatingBarPosition { get; init; } = null!;
            public MenuItem HideICCMainWindow { get; init; } = null!;
            public MenuItem FoldFloatingBar { get; init; } = null!;
            public MenuItem ForceFullScreen { get; init; } = null!;
        }
    }
}