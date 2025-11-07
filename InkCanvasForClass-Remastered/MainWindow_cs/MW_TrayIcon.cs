using Hardcodet.Wpf.TaskbarNotification;
using iNKORE.UI.WPF.Controls;
using InkCanvasForClass_Remastered.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace InkCanvasForClass_Remastered
{
    public partial class App : Application
    {
        // Modern MVVM-based tray icon implementation
        // Event handlers delegate to TrayIconViewModel

        private void SysTrayMenu_Opened(object sender, RoutedEventArgs e)
        {
            var trayViewModel = GetService<TrayIconViewModel>();
            trayViewModel?.ContextMenuOpenedCommand.Execute(null);
        }

        private void CloseAppTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            var trayViewModel = GetService<TrayIconViewModel>();
            trayViewModel?.CloseAppCommand.Execute(null);
        }

        private void RestartAppTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            var trayViewModel = GetService<TrayIconViewModel>();
            trayViewModel?.RestartAppCommand.Execute(null);
        }

        private void ForceFullScreenTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            var trayViewModel = GetService<TrayIconViewModel>();
            trayViewModel?.ForceFullScreenCommand.Execute(null);
        }

        private void FoldFloatingBarTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            var trayViewModel = GetService<TrayIconViewModel>();
            trayViewModel?.FoldFloatingBarCommand.Execute(null);
        }

        private void ResetFloatingBarPositionTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            var trayViewModel = GetService<TrayIconViewModel>();
            trayViewModel?.ResetFloatingBarPositionCommand.Execute(null);
        }

        private void HideICCMainWindowTrayIconMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            var trayViewModel = GetService<TrayIconViewModel>();
            trayViewModel?.HideMainWindowCommand.Execute(null);
        }

        private void HideICCMainWindowTrayIconMenuItem_UnChecked(object sender, RoutedEventArgs e)
        {
            var trayViewModel = GetService<TrayIconViewModel>();
            trayViewModel?.ShowMainWindowCommand.Execute(null);
        }
    }
}
