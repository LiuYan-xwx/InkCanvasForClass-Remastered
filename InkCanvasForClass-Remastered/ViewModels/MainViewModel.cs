using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkCanvasForClass_Remastered.Enums;
using InkCanvasForClass_Remastered.Interfaces;
using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.Services;
using System.ComponentModel;
using System.Windows.Ink;
using System.Windows.Media;

namespace InkCanvasForClass_Remastered.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public const int MaxWhiteboardPageCount = 99;

        private readonly SettingsService _settingsService;
        private readonly IPowerPointService _powerPointService;

        // The view owns InkCanvas/history/animation details. These events carry only user intent
        // so those details do not leak into the view model while the migration is incremental.
        public event Action? EnterWhiteboardRequested;
        public event Action? ExitWhiteboardRequested;
        public event Action? WhiteboardPreviousPageRequested;
        public event Action? WhiteboardNextPageRequested;
        public event Action<int>? WhiteboardPageSelectionRequested;
        
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
        [NotifyPropertyChangedFor(nameof(IsDesktopAnnotationMode))]
        [NotifyPropertyChangedFor(nameof(IsWhiteboardMode))]
        [NotifyPropertyChangedFor(nameof(IsPresentationMode))]
        [NotifyCanExecuteChangedFor(nameof(RequestEnterWhiteboardCommand))]
        [NotifyCanExecuteChangedFor(nameof(RequestExitWhiteboardCommand))]
        [NotifyCanExecuteChangedFor(nameof(RequestWhiteboardPreviousPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(RequestWhiteboardNextPageCommand))]
        public partial WorkspaceMode CurrentWorkspaceMode { get; set; } = WorkspaceMode.DesktopAnnotation;

        public bool IsDesktopAnnotationMode => CurrentWorkspaceMode == WorkspaceMode.DesktopAnnotation;
        public bool IsWhiteboardMode => CurrentWorkspaceMode == WorkspaceMode.Whiteboard;
        public bool IsPresentationMode => CurrentWorkspaceMode == WorkspaceMode.Presentation;

        [ObservableProperty]
        public partial InkTool ActiveInkTool { get; set; } = InkTool.Cursor;

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
        [NotifyCanExecuteChangedFor(nameof(RequestWhiteboardPreviousPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(RequestWhiteboardNextPageCommand))]
        public partial int WhiteboardCurrentPage { get; set; } = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWhiteboardPreviousPageButtonEnabled))]
        [NotifyCanExecuteChangedFor(nameof(RequestWhiteboardNextPageCommand))]
        public partial int WhiteboardTotalPageCount { get; set; } = 1;

        public bool IsWhiteboardPreviousPageButtonEnabled => WhiteboardCurrentPage > 1;

        [RelayCommand(CanExecute = nameof(CanRequestEnterWhiteboard))]
        private void RequestEnterWhiteboard()
        {
            EnterWhiteboardRequested?.Invoke();
        }

        private bool CanRequestEnterWhiteboard() => !IsWhiteboardMode;

        [RelayCommand(CanExecute = nameof(CanRequestExitWhiteboard))]
        private void RequestExitWhiteboard()
        {
            ExitWhiteboardRequested?.Invoke();
        }

        private bool CanRequestExitWhiteboard() => IsWhiteboardMode;

        [RelayCommand(CanExecute = nameof(CanRequestPreviousWhiteboardPage))]
        private void RequestWhiteboardPreviousPage()
        {
            WhiteboardPreviousPageRequested?.Invoke();
        }

        private bool CanRequestPreviousWhiteboardPage() =>
            IsWhiteboardMode && WhiteboardCurrentPage > 1;

        [RelayCommand(CanExecute = nameof(CanRequestNextWhiteboardPage))]
        private void RequestWhiteboardNextPage()
        {
            WhiteboardNextPageRequested?.Invoke();
        }

        private bool CanRequestNextWhiteboardPage() =>
            IsWhiteboardMode &&
            (WhiteboardCurrentPage < WhiteboardTotalPageCount ||
             WhiteboardTotalPageCount < MaxWhiteboardPageCount);

        [RelayCommand]
        private void RequestWhiteboardPageSelection(int page)
        {
            if (!IsWhiteboardMode ||
                page < 1 ||
                page > WhiteboardTotalPageCount)
            {
                return;
            }

            WhiteboardPageSelectionRequested?.Invoke(page);
        }

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
