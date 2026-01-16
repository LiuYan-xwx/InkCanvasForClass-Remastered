using Hardcodet.Wpf.TaskbarNotification;
using iNKORE.UI.WPF.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace InkCanvasForClass_Remastered
{
    public partial class App : Application
    {

        private void SysTrayMenu_Opened(object sender, RoutedEventArgs e)
        {
            ContextMenu s = (ContextMenu)sender;
            Image FoldFloatingBarTrayIconMenuItemIconEyeOff =
                (Image)((Grid)((MenuItem)s.Items[^5]).Icon).Children[0];
            Image FoldFloatingBarTrayIconMenuItemIconEyeOn =
                (Image)((Grid)((MenuItem)s.Items[s.Items.Count - 5]).Icon).Children[1];
            TextBlock FoldFloatingBarTrayIconMenuItemHeaderText =
                (TextBlock)((SimpleStackPanel)((MenuItem)s.Items[s.Items.Count - 5]).Header).Children[0];
            MenuItem ResetFloatingBarPositionTrayIconMenuItem = (MenuItem)s.Items[s.Items.Count - 4];
            MenuItem HideICCMainWindowTrayIconMenuItem = (MenuItem)s.Items[s.Items.Count - 9];
            MainWindow mainWin = (MainWindow)Application.Current.MainWindow;
            if (mainWin.IsLoaded)
            {
                // 判斷是否在收納模式中
                if (!mainWin._viewModel.IsFloatingBarVisible)
                {
                    FoldFloatingBarTrayIconMenuItemIconEyeOff.Visibility = Visibility.Hidden;
                    FoldFloatingBarTrayIconMenuItemIconEyeOn.Visibility = Visibility.Visible;
                    FoldFloatingBarTrayIconMenuItemHeaderText.Text = "退出收纳模式";
                    if (!HideICCMainWindowTrayIconMenuItem.IsChecked)
                    {
                        ResetFloatingBarPositionTrayIconMenuItem.IsEnabled = false;
                        ResetFloatingBarPositionTrayIconMenuItem.Opacity = 0.5;
                    }
                }
                else
                {
                    FoldFloatingBarTrayIconMenuItemIconEyeOff.Visibility = Visibility.Visible;
                    FoldFloatingBarTrayIconMenuItemIconEyeOn.Visibility = Visibility.Hidden;
                    FoldFloatingBarTrayIconMenuItemHeaderText.Text = "切换为收纳模式";
                    if (!HideICCMainWindowTrayIconMenuItem.IsChecked)
                    {
                        ResetFloatingBarPositionTrayIconMenuItem.IsEnabled = true;
                        ResetFloatingBarPositionTrayIconMenuItem.Opacity = 1;
                    }

                }
            }
        }

        private void CloseAppTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            MainWindow mainWin = (MainWindow)Application.Current.MainWindow;
            if (mainWin.IsLoaded)
                mainWin.BtnExit_Click(null, null);
        }

        private void RestartAppTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            MainWindow mainWin = (MainWindow)Application.Current.MainWindow;
            if (mainWin.IsLoaded)
                mainWin.BtnRestart_Click(null, null);
        }

        private void ForceFullScreenTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            MainWindow mainWin = (MainWindow)Application.Current.MainWindow;
            if (mainWin.IsLoaded)
            {
                _ = InkCanvasForClass_Remastered.MainWindow.MoveWindow(new WindowInteropHelper(mainWin).Handle, 0, 0,
                    System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height, true);
                _notificationService.ShowNotification($"已强制全屏化：{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width}x{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height}（缩放比例为{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / SystemParameters.PrimaryScreenWidth}x{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / SystemParameters.PrimaryScreenHeight}）");
            }
        }

        private void FoldFloatingBarTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            MainWindow mainWin = (MainWindow)Application.Current.MainWindow;
            if (mainWin.IsLoaded)
            {
                if (mainWin._viewModel.IsFloatingBarVisible)
                    _ = mainWin.HideFloatingBar(true);
                else
                    _ = mainWin.ShowFloatingBar(true);
            }
        }

        private void ResetFloatingBarPositionTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            MainWindow mainWin = (MainWindow)Application.Current.MainWindow;
            if (mainWin.IsLoaded)
            {
                bool isInPPTPresentationMode = false;
                Dispatcher.Invoke(() =>
                {
                    isInPPTPresentationMode = mainWin.BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible;
                });
                if (mainWin._viewModel.IsFloatingBarVisible)
                {
                    if (!isInPPTPresentationMode)
                        mainWin.PureViewboxFloatingBarMarginAnimationInDesktopMode();
                    else
                        mainWin.PureViewboxFloatingBarMarginAnimationInPPTMode();
                }
            }
        }

        private void HideICCMainWindowTrayIconMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            MainWindow mainWin = (MainWindow)Application.Current.MainWindow;
            if (mainWin.IsLoaded)
            {
                mainWin.Hide();
                ContextMenu s = ((TaskbarIcon)Application.Current.Resources["TaskbarTrayIcon"]).ContextMenu;
                MenuItem ResetFloatingBarPositionTrayIconMenuItem = (MenuItem)s.Items[s.Items.Count - 4];
                MenuItem FoldFloatingBarTrayIconMenuItem = (MenuItem)s.Items[s.Items.Count - 5];
                MenuItem ForceFullScreenTrayIconMenuItem = (MenuItem)s.Items[s.Items.Count - 6];
                ResetFloatingBarPositionTrayIconMenuItem.IsEnabled = false;
                FoldFloatingBarTrayIconMenuItem.IsEnabled = false;
                ForceFullScreenTrayIconMenuItem.IsEnabled = false;
                ResetFloatingBarPositionTrayIconMenuItem.Opacity = 0.5;
                FoldFloatingBarTrayIconMenuItem.Opacity = 0.5;
                ForceFullScreenTrayIconMenuItem.Opacity = 0.5;
            }
            else
            {
                mi.IsChecked = false;
            }

        }

        private void HideICCMainWindowTrayIconMenuItem_UnChecked(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            MainWindow mainWin = (MainWindow)Application.Current.MainWindow;
            if (mainWin.IsLoaded)
            {
                mainWin.Show();
                ContextMenu s = ((TaskbarIcon)Application.Current.Resources["TaskbarTrayIcon"]).ContextMenu;
                MenuItem ResetFloatingBarPositionTrayIconMenuItem = (MenuItem)s.Items[s.Items.Count - 4];
                MenuItem FoldFloatingBarTrayIconMenuItem = (MenuItem)s.Items[s.Items.Count - 5];
                MenuItem ForceFullScreenTrayIconMenuItem = (MenuItem)s.Items[s.Items.Count - 6];
                ResetFloatingBarPositionTrayIconMenuItem.IsEnabled = true;
                FoldFloatingBarTrayIconMenuItem.IsEnabled = true;
                ForceFullScreenTrayIconMenuItem.IsEnabled = true;
                ResetFloatingBarPositionTrayIconMenuItem.Opacity = 1;
                FoldFloatingBarTrayIconMenuItem.Opacity = 1;
                ForceFullScreenTrayIconMenuItem.Opacity = 1;
            }
            else
            {
                mi.IsChecked = false;
            }
        }

    }
}
