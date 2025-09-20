using Hardcodet.Wpf.TaskbarNotification;
using iNKORE.UI.WPF.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace InkCanvasForClass_Remastered.Services
{
    public class TrayIconService
    {
        private readonly Application _application;
        private TaskbarIcon? _taskbarIcon;

        public TrayIconService()
        {
            _application = Application.Current;
        }

        public void Initialize()
        {
            _taskbarIcon = (TaskbarIcon?)_application.FindResource("TaskbarTrayIcon");
        }

        public void SysTrayMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is not ContextMenu contextMenu) return;

            var menuItems = GetTrayMenuItems(contextMenu);
            var mainWin = (MainWindow)Application.Current.MainWindow;
            
            if (!mainWin.IsLoaded) return;

            UpdateFloatingBarMenuState(menuItems, mainWin);
        }

        public void CloseAppTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            var mainWin = (MainWindow)Application.Current.MainWindow;
            if (mainWin.IsLoaded)
                mainWin.BtnExit_Click(null, null);
        }

        public void RestartAppTrayIconMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            var mainWin = (MainWindow)Application.Current.MainWindow;
            if (mainWin.IsLoaded)
                mainWin.BtnRestart_Click(null, null);
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
            return new TrayMenuItems
            {
                FoldFloatingBarIconEyeOff = (Image)((Grid)((MenuItem)contextMenu.Items[^5]).Icon).Children[0],
                FoldFloatingBarIconEyeOn = (Image)((Grid)((MenuItem)contextMenu.Items[contextMenu.Items.Count - 5]).Icon).Children[1],
                FoldFloatingBarHeaderText = (TextBlock)((SimpleStackPanel)((MenuItem)contextMenu.Items[contextMenu.Items.Count - 5]).Header).Children[0],
                ResetFloatingBarPosition = (MenuItem)contextMenu.Items[contextMenu.Items.Count - 4],
                HideICCMainWindow = (MenuItem)contextMenu.Items[contextMenu.Items.Count - 9],
                FoldFloatingBar = (MenuItem)contextMenu.Items[contextMenu.Items.Count - 5],
                ForceFullScreen = (MenuItem)contextMenu.Items[contextMenu.Items.Count - 6]
            };
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