using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Windows;

namespace InkCanvasForClass_Remastered.ViewModels
{
    public partial class TrayIconMenuViewModel(MainWindow mainWindow) : ObservableObject
    {
        private readonly MainWindow MainWindow = mainWindow;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(FoldFloatingBarCommand))]
        [NotifyCanExecuteChangedFor(nameof(ResetFloatingBarPositionCommand))]
        private bool _showMainWindowChecked = true;

        [RelayCommand]
        private void HideOrShowMainWindow()
        {
            if (ShowMainWindowChecked)
            {
                MainWindow.Show();
                MainWindow.Activate();
            }
            else
            {
                MainWindow.Hide();
            }
        }

        [RelayCommand(CanExecute = nameof(ShowMainWindowChecked))]
        private void FoldFloatingBar()
        {
            if (MainWindow._viewModel.IsFloatingBarVisible)
                _ = MainWindow.HideFloatingBar(true);
            else
                _ = MainWindow.ShowFloatingBar(true);
        }

        [RelayCommand(CanExecute = nameof(ShowMainWindowChecked))]
        private void ResetFloatingBarPosition()
        {
            if (MainWindow._viewModel.IsPresentationMode)
            {
                MainWindow.PureViewboxFloatingBarMarginAnimationInPPTMode();
            }
            else
            {
                MainWindow.PureViewboxFloatingBarMarginAnimationInDesktopMode();
            }
        }

        [RelayCommand]
        private void RestartApp()
        {
            Process.Start(System.Windows.Forms.Application.ExecutablePath, "-m");
            MainWindow.CloseIsFromButton = true;
            Application.Current.Shutdown();
        }

        [RelayCommand]
        private void CloseApp()
        {
            MainWindow.CloseIsFromButton = true;
            Application.Current.Shutdown();
        }
    }
}
