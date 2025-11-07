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
    /// ViewModel for the system tray icon and its context menu
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

        [ObservableProperty]
        private bool _isMainWindowHidden = false;

        [ObservableProperty]
        private bool _isFloatingBarFolded = false;

        [ObservableProperty]
        private string _foldFloatingBarMenuText = "切换为收纳模式";

        [ObservableProperty]
        private bool _isFoldEyeOffVisible = true;

        [ObservableProperty]
        private bool _isFoldEyeOnVisible = false;

        [ObservableProperty]
        private bool _isResetPositionEnabled = true;

        [ObservableProperty]
        private bool _isFoldFloatingBarEnabled = true;

        [ObservableProperty]
        private bool _isForceFullScreenEnabled = true;

        /// <summary>
        /// Updates the tray icon menu state based on the main window state
        /// </summary>
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

        [RelayCommand]
        private void ContextMenuOpened()
        {
            var mainWindow = GetMainWindow();
            if (mainWindow?.IsLoaded == true)
            {
                UpdateMenuState(mainWindow._viewModel.IsFloatingBarVisible, IsMainWindowHidden);
            }
        }

        private MainWindow? GetMainWindow()
        {
            return Application.Current.MainWindow as MainWindow;
        }
    }
}
