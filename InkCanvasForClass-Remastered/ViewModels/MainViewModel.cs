using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkCanvasForClass_Remastered.Enums;
using InkCanvasForClass_Remastered.Interfaces;
using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.Services;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;

namespace InkCanvasForClass_Remastered.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;
        private readonly IPowerPointService _powerPointService;
        
        public MainViewModel(SettingsService settingsService, IPowerPointService powerPointService)
        {
            _settingsService = settingsService;
            _powerPointService = powerPointService;
            InkCanvasDrawingAttributes = new DrawingAttributes
            {
                Color = Colors.Red,
                Height = Settings.InkWidth,
                Width = Settings.InkWidth,
                IsHighlighter = false,
                FitToCurve = Settings.FitToCurve,
            };
            Settings.PropertyChanged += OnSettingsPropertyChanged;
        }

        public string AppVersion => App.AppVersion;
        public Settings Settings => _settingsService.Settings;
        public IPowerPointService PowerPointService => _powerPointService;

        [ObservableProperty]
        public partial AppMode AppMode { get; set; } = AppMode.Normal;

        [ObservableProperty]
        public partial InkCanvasEditingMode AppPenMode { get; set; } = InkCanvasEditingMode.None;

        [ObservableProperty]
        public partial DrawingAttributes InkCanvasDrawingAttributes { get; set; }

        [ObservableProperty]
        public partial bool ForceCursor { get; set; } = false;

        [ObservableProperty]
        public partial string NowTime { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string NowDate { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWhiteboardPreviousPageButtonEnabled))]
        public partial int WhiteboardCurrentPage { get; set; } = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWhiteboardPreviousPageButtonEnabled))]
        public partial int WhiteboardTotalPageCount { get; set; } = 1;

        public bool IsWhiteboardPreviousPageButtonEnabled => WhiteboardCurrentPage > 1;

        [ObservableProperty]
        public partial bool IsFloatingBarVisible { get; set; } = true;

        [ObservableProperty]
        public partial bool CanUndo { get; set; } = false;

        [ObservableProperty]
        public partial bool CanRedo { get; set; } = false;

        [ObservableProperty]
        public partial bool IsSettingsPanelVisible { get; set; } = false;

        [ObservableProperty]
        public partial bool ForceShowPPTNavigationPanel { get; set; } = false;

        private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Settings.FitToCurve):
                    InkCanvasDrawingAttributes.FitToCurve = Settings.FitToCurve;
                    break;
            }
        }

        [RelayCommand]
        private void OpenSettingsPanel()
        {
            IsSettingsPanelVisible = true;
            App.GetService<MainWindow>().HideToolsPanel();
        }
        [RelayCommand]
        private void CloseSettingsPanel()
        {
            IsSettingsPanelVisible = false;
        }

        [RelayCommand]
        private void CrashTest()
        {
            throw new Exception("Crash Test");
        }
    }
}
