using InkCanvasForClass_Remastered.ViewModels;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace InkCanvasForClass_Remastered
{
    public partial class CountdownTimerWindow : Window
    {
        private readonly CountdownTimerViewModel _viewModel = new();
        private readonly SoundPlayer _player = new();
        private bool _isCompact;

        public CountdownTimerWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
            _viewModel.Completed += Countdown_Completed;
        }

        private void Countdown_Completed(object? sender, EventArgs e)
        {
            _player.Stream = Properties.Resources.TimerDownNotice;
            _player.Play();
        }

        private void FullscreenButton_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void CompactButton_Click(object sender, RoutedEventArgs e)
        {
            _isCompact = !_isCompact;
            if (_isCompact && WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;

            Width = _isCompact ? 400 : 1100;
            Height = _isCompact ? 250 : 700;
            BigViewController.Visibility = _isCompact ? Visibility.Collapsed : Visibility.Visible;
            CompactTimeButton.Visibility = _isCompact ? Visibility.Visible : Visibility.Collapsed;

            if (!_isCompact) CenterWindowOnCurrentScreen();
        }

        private void CenterWindowOnCurrentScreen()
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            var screen = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);
            Left = screen.Bounds.Left / dpi.DpiScaleX + (screen.Bounds.Width / dpi.DpiScaleX - Width) / 2;
            Top = screen.Bounds.Top / dpi.DpiScaleY + (screen.Bounds.Height / dpi.DpiScaleY - Height) / 2;
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Completed -= Countdown_Completed;
            _viewModel.Dispose();
            _player.Stop();
            _player.Dispose();
            base.OnClosed(e);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void WindowDragMove(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
