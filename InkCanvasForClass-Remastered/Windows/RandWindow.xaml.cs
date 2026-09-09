using InkCanvasForClass_Remastered.Helpers;
using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace InkCanvasForClass_Remastered
{
    public partial class RandWindow : Window
    {
        private readonly RandViewModel ViewModel;
        private readonly string _namesFilePath = Path.Combine(CommonDirectories.AppRootFolderPath, "Names.txt");
        private readonly DispatcherTimer _autoCloseTimer = new();
        private bool _isClosed;

        public bool IsAutoClose
        {
            get => ViewModel.IsAutoClose;
            set => ViewModel.IsAutoClose = value;
        }

        public RandWindow(RandViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;
            _autoCloseTimer.Tick += AutoCloseTimer_Tick;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AnimationsHelper.ShowWithSlideFromBottomAndFade(this, 0.25);
            ReloadNamesFromFile();
            if (ViewModel.NameCount == 0)
            {
                MessageBox.Show("名单为空，请先导入名单！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                OpenNamesEditor();
                if (ViewModel.NameCount == 0)
                {
                    IsAutoClose = false;
                    return;
                }
            }

            if (IsAutoClose) await DrawOnceAndCloseAsync();
        }

        private async Task DrawOnceAndCloseAsync()
        {
            if (!ViewModel.DrawCommand.CanExecute(null)) return;
            if (!ViewModel.HasEnoughNames)
            {
                ShowInsufficientNamesMessage();
                return;
            }

            await ViewModel.DrawCommand.ExecuteAsync(null);
            if (_isClosed) return;

            _autoCloseTimer.Interval = TimeSpan.FromSeconds(ViewModel.Settings.RandWindowOnceCloseLatency);
            _autoCloseTimer.Start();
        }

        private void ReloadNamesFromFile()
        {
            ViewModel.SetNames(File.Exists(_namesFilePath) ? File.ReadAllLines(_namesFilePath) : []);
        }

        private void OpenNamesEditor()
        {
            App.GetService<NamesInputWindow>().ShowDialog();
            ReloadNamesFromFile();
        }

        private void ImportNames_Click(object sender, RoutedEventArgs e) => OpenNamesEditor();
        private void DrawButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.HasEnoughNames)
                ShowInsufficientNamesMessage();
        }

        private static void ShowInsufficientNamesMessage() => MessageBox.Show(
            "没有足够的未被抽过的人！\n请减少抽取人数、重新导入名单，或关闭“不重复抽取”。",
            "提示",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        private void AutoCloseTimer_Tick(object? sender, EventArgs e) => Close();

        protected override void OnClosed(EventArgs e)
        {
            _isClosed = true;
            _autoCloseTimer.Stop();
            ViewModel.DrawCommand.Cancel();
            base.OnClosed(e);
        }
    }
}
