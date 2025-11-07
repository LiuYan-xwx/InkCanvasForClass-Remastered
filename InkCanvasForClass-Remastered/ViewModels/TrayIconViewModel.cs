using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;

namespace InkCanvasForClass_Remastered.ViewModels
{
    /// <summary>
    /// Modern MVVM ViewModel for the system tray icon and its context menu.
    /// Manages tray icon state, menu item visibility, and delegates user actions to MainWindow.
    /// This replaces the old code-behind approach with proper data binding and command pattern.
    /// </summary>
    public partial class TrayIconViewModel : ObservableObject
    {
        private readonly ILogger<TrayIconViewModel> _logger;
        private readonly SettingsService _settingsService;

        public TrayIconViewModel(ILogger<TrayIconViewModel> logger, SettingsService settingsService)
        {
            _logger = logger;
            _settingsService = settingsService;
        }

        public Settings Settings => _settingsService.Settings;

        /// <summary>
        /// Indicates whether the main window is currently hidden
        /// </summary>
        [ObservableProperty]
        private bool _isMainWindowHidden = false;

        /// <summary>
        /// Indicates whether the floating bar is in folded/收纳 mode
        /// </summary>
        [ObservableProperty]
        private bool _isFloatingBarFolded = false;

        /// <summary>
        /// Dynamic text for the fold floating bar menu item
        /// </summary>
        [ObservableProperty]
        private string _foldFloatingBarMenuText = "切换为收纳模式";

        /// <summary>
        /// Controls visibility of the eye-off icon (shown when bar is visible)
        /// </summary>
        [ObservableProperty]
        private bool _isFoldEyeOffVisible = true;

        /// <summary>
        /// Controls visibility of the eye-on icon (shown when bar is folded)
        /// </summary>
        [ObservableProperty]
        private bool _isFoldEyeOnVisible = false;

        /// <summary>
        /// Controls whether the reset floating bar position menu item is enabled
        /// </summary>
        [ObservableProperty]
        private bool _isResetPositionEnabled = true;

        /// <summary>
        /// Controls whether the fold floating bar menu item is enabled
        /// </summary>
        [ObservableProperty]
        private bool _isFoldFloatingBarEnabled = true;

        /// <summary>
        /// Controls whether the force fullscreen menu item is enabled
        /// </summary>
        [ObservableProperty]
        private bool _isForceFullScreenEnabled = true;

        /// <summary>
        /// Updates the tray icon menu state based on the main window state.
        /// This method centralizes all state management logic for the tray icon menu.
        /// </summary>
        /// <param name="isFloatingBarVisible">Whether the floating toolbar is currently visible</param>
        /// <param name="isMainWindowHidden">Whether the main application window is hidden</param>
        public void UpdateMenuState(bool isFloatingBarVisible, bool isMainWindowHidden)
        {
            IsMainWindowHidden = isMainWindowHidden;
            IsFloatingBarFolded = !isFloatingBarVisible;

            // Update fold menu text and icons
            if (!isFloatingBarVisible)
            {
                FoldFloatingBarMenuText = "退出收纳模式";
                IsFoldEyeOffVisible = false;
                IsFoldEyeOnVisible = true;
                
                if (isMainWindowHidden)
                {
                    IsResetPositionEnabled = false;
                }
            }
            else
            {
                FoldFloatingBarMenuText = "切换为收纳模式";
                IsFoldEyeOffVisible = true;
                IsFoldEyeOnVisible = false;
                
                if (!isMainWindowHidden)
                {
                    IsResetPositionEnabled = true;
                }
            }

            // Update menu item enabled states based on window visibility
            if (isMainWindowHidden)
            {
                IsResetPositionEnabled = false;
                IsFoldFloatingBarEnabled = false;
                IsForceFullScreenEnabled = false;
            }
            else
            {
                IsFoldFloatingBarEnabled = true;
                IsForceFullScreenEnabled = true;
                IsResetPositionEnabled = isFloatingBarVisible;
            }
        }

        /// <summary>
        /// Hides the main application window
        /// </summary>
        [RelayCommand]
        private void HideMainWindow()
        {
            var mainWindow = GetMainWindow();
            if (mainWindow?.IsLoaded == true)
            {
                mainWindow.Hide();
                IsMainWindowHidden = true;
                UpdateMenuState(mainWindow._viewModel.IsFloatingBarVisible, true);
                _logger.LogInformation("Main window hidden via tray icon");
            }
        }

        /// <summary>
        /// Shows the main application window
        /// </summary>
        [RelayCommand]
        private void ShowMainWindow()
        {
            var mainWindow = GetMainWindow();
            if (mainWindow?.IsLoaded == true)
            {
                mainWindow.Show();
                IsMainWindowHidden = false;
                UpdateMenuState(mainWindow._viewModel.IsFloatingBarVisible, false);
                _logger.LogInformation("Main window shown via tray icon");
            }
        }

        /// <summary>
        /// Forces the main window to fullscreen mode covering the entire primary screen
        /// </summary>
        [RelayCommand]
        private void ForceFullScreen()
        {
            var mainWindow = GetMainWindow();
            if (mainWindow?.IsLoaded == true)
            {
                _ = MainWindow.MoveWindow(
                    new WindowInteropHelper(mainWindow).Handle,
                    0, 0,
                    System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                    System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height,
                    true);

                MainWindow.ShowNewMessage(
                    $"已强制全屏化：{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width}x{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height}" +
                    $"（缩放比例为{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / SystemParameters.PrimaryScreenWidth}x" +
                    $"{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / SystemParameters.PrimaryScreenHeight}）");

                _logger.LogInformation("Forced full screen via tray icon");
            }
        }

        /// <summary>
        /// Toggles the floating toolbar between visible and folded/收纳 states
        /// </summary>
        [RelayCommand]
        private void FoldFloatingBar()
        {
            var mainWindow = GetMainWindow();
            if (mainWindow?.IsLoaded == true)
            {
                if (mainWindow._viewModel.IsFloatingBarVisible)
                {
                    _ = mainWindow.HideFloatingBar(new object());
                    _logger.LogInformation("Floating bar hidden via tray icon");
                }
                else
                {
                    _ = mainWindow.ShowFloatingBar(new object());
                    _logger.LogInformation("Floating bar shown via tray icon");
                }
                UpdateMenuState(mainWindow._viewModel.IsFloatingBarVisible, IsMainWindowHidden);
            }
        }

        /// <summary>
        /// Resets the floating toolbar to its default position
        /// </summary>
        [RelayCommand]
        private void ResetFloatingBarPosition()
        {
            var mainWindow = GetMainWindow();
            if (mainWindow?.IsLoaded == true && mainWindow._viewModel.IsFloatingBarVisible)
            {
                bool isInPPTPresentationMode = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    isInPPTPresentationMode = mainWindow.BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible;
                });

                if (!isInPPTPresentationMode)
                {
                    mainWindow.PureViewboxFloatingBarMarginAnimationInDesktopMode();
                }
                else
                {
                    mainWindow.PureViewboxFloatingBarMarginAnimationInPPTMode();
                }

                _logger.LogInformation("Floating bar position reset via tray icon");
            }
        }

        /// <summary>
        /// Restarts the application
        /// </summary>
        [RelayCommand]
        private void RestartApp()
        {
            var mainWindow = GetMainWindow();
            if (mainWindow?.IsLoaded == true)
            {
                _logger.LogInformation("Application restart requested via tray icon");
                mainWindow.BtnRestart_Click(null, null);
            }
        }

        /// <summary>
        /// Closes the application
        /// </summary>
        [RelayCommand]
        private void CloseApp()
        {
            var mainWindow = GetMainWindow();
            if (mainWindow?.IsLoaded == true)
            {
                _logger.LogInformation("Application close requested via tray icon");
                mainWindow.BtnExit_Click(null, null);
            }
        }

        /// <summary>
        /// Called when the context menu is opened. Updates menu state to reflect current application state.
        /// </summary>
        [RelayCommand]
        private void ContextMenuOpened()
        {
            var mainWindow = GetMainWindow();
            if (mainWindow?.IsLoaded == true)
            {
                UpdateMenuState(mainWindow._viewModel.IsFloatingBarVisible, IsMainWindowHidden);
            }
        }

        /// <summary>
        /// Gets the main application window instance
        /// </summary>
        /// <returns>MainWindow instance or null if not available</returns>
        private MainWindow? GetMainWindow()
        {
            return Application.Current.MainWindow as MainWindow;
        }
    }
}
