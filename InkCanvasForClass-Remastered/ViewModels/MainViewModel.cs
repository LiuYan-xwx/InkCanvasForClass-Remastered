using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkCanvasForClass_Remastered.Enums;
using InkCanvasForClass_Remastered.Interfaces;
using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.Services;
using System.Collections.ObjectModel;
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
            PenColors = CreatePenColors();
            PenColorsById = PenColors.ToDictionary(option => option.Id);
            HighlighterColors = CreateHighlighterColors();
            HighlighterColorsById = HighlighterColors.ToDictionary(option => option.Id);
            InkCanvasDrawingAttributes = new DrawingAttributes
            {
                Color = Colors.Red,
                Height = Settings.InkWidth,
                Width = Settings.InkWidth,
                IsHighlighter = false,
                FitToCurve = Settings.FitToCurve,
            };
            UpdateColorSelection(PenColorsById, SelectedPenColor);
            UpdateColorSelection(HighlighterColorsById, SelectedHighlighterColor);
            Settings.PropertyChanged += OnSettingsPropertyChanged;
        }

        public string AppVersion => App.AppVersion;
        public Settings Settings => _settingsService.Settings;
        public IPowerPointService PowerPointService => _powerPointService;

        [ObservableProperty]
        public partial AppMode AppMode { get; set; } = AppMode.Normal;

        // 用户选定的工具；触摸擦除和手势中的临时 EditingMode 不改变它。
        [ObservableProperty]
        public partial InkCanvasEditingMode AppPenMode { get; set; } = InkCanvasEditingMode.None;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(InkAlpha))]
        public partial DrawingAttributes InkCanvasDrawingAttributes { get; set; }

        public byte InkAlpha
        {
            get => InkCanvasDrawingAttributes.Color.A;
            set
            {
                var color = InkCanvasDrawingAttributes.Color;
                if (color.A == value) return;
                color.A = value;
                InkCanvasDrawingAttributes.Color = color;
            }
        }

        partial void OnInkCanvasDrawingAttributesChanged(DrawingAttributes oldValue, DrawingAttributes newValue)
        {
            if (oldValue != null) oldValue.AttributeChanged -= OnDrawingAttributeChanged;
            newValue.AttributeChanged += OnDrawingAttributeChanged;
        }

        private void OnDrawingAttributeChanged(object? sender, PropertyDataChangedEventArgs e)
        {
            if (e.PropertyGuid == DrawingAttributeIds.Color)
                OnPropertyChanged(nameof(InkAlpha));
        }

        [ObservableProperty]
        public partial int SelectedPenColor { get; set; } = 1;

        [ObservableProperty]
        public partial int SelectedHighlighterColor { get; set; } = 102;

        public IReadOnlyList<PenColorOption> PenColors { get; }

        public IReadOnlyDictionary<int, PenColorOption> PenColorsById { get; }

        public IReadOnlyList<PenColorOption> HighlighterColors { get; }

        public IReadOnlyDictionary<int, PenColorOption> HighlighterColorsById { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ColorThemeLabel))]
        [NotifyPropertyChangedFor(nameof(ColorThemeIconPath))]
        public partial bool UseLightPenPalette { get; set; }

        public string ColorThemeLabel => UseLightPenPalette ? "暗系" : "亮系";

        public string ColorThemeIconPath => UseLightPenPalette
            ? "/Resources/Icons-Fluent/ic_fluent_weather_moon_24_regular.png"
            : "/Resources/Icons-Fluent/ic_fluent_weather_sunny_24_regular.png";

        [ObservableProperty]
        public partial bool ForceCursor { get; set; } = false;

        [ObservableProperty]
        public partial string NowTime { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string NowDate { get; set; } = string.Empty;

        public const int MaxWhiteboardPageCount = 99;

        public ObservableCollection<WhiteboardPage> WhiteboardPages { get; } = [new WhiteboardPage(1)];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWhiteboardPreviousPageButtonEnabled))]
        [NotifyPropertyChangedFor(nameof(IsWhiteboardNextPageButtonEnabled))]
        public partial int WhiteboardCurrentPage { get; set; } = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWhiteboardNextPageButtonEnabled))]
        public partial int WhiteboardTotalPageCount { get; set; } = 1;

        public bool IsWhiteboardPreviousPageButtonEnabled => WhiteboardCurrentPage > 1;

        public bool IsWhiteboardNextPageButtonEnabled =>
            WhiteboardCurrentPage < WhiteboardTotalPageCount || WhiteboardTotalPageCount < MaxWhiteboardPageCount;

        partial void OnWhiteboardTotalPageCountChanged(int value)
        {
            while (WhiteboardPages.Count < value)
                WhiteboardPages.Add(new WhiteboardPage(WhiteboardPages.Count + 1));
            while (WhiteboardPages.Count > value)
                WhiteboardPages.RemoveAt(WhiteboardPages.Count - 1);
        }

        [ObservableProperty]
        public partial bool IsFloatingBarVisible { get; set; } = true;

        [ObservableProperty]
        public partial bool CanUndo { get; set; } = false;

        [ObservableProperty]
        public partial bool CanRedo { get; set; } = false;

        [ObservableProperty]
        public partial bool IsSelectionCloneEnabled { get; set; } = false;

        [ObservableProperty]
        public partial bool IsSettingsPanelVisible { get; set; } = false;

        [ObservableProperty]
        public partial bool ForceShowPPTNavigationPanel { get; set; } = false;

        private bool _desktopUsesLightPenPalette;
        private int _lastDesktopPenColor = 1;
        private int _lastBoardPenColor = 5;

        // 颜色状态更新后，由窗口处理面板、画布交互和待提交的墨迹历史。
        public event EventHandler? PenColorSelected;

        partial void OnSelectedPenColorChanged(int value)
        {
            if (PenColorsById != null)
                UpdateColorSelection(PenColorsById, value);

            if (InkCanvasDrawingAttributes != null && !InkCanvasDrawingAttributes.IsHighlighter)
                ApplyCurrentPenColor();
        }

        partial void OnSelectedHighlighterColorChanged(int value)
        {
            if (HighlighterColorsById != null)
                UpdateColorSelection(HighlighterColorsById, value);

            if (InkCanvasDrawingAttributes != null && InkCanvasDrawingAttributes.IsHighlighter)
                ApplyCurrentPenColor();
        }

        partial void OnUseLightPenPaletteChanged(bool value)
        {
            if (PenColors == null) return;

            foreach (var option in PenColors)
                option.SetPalette(value);

            if (InkCanvasDrawingAttributes != null && !InkCanvasDrawingAttributes.IsHighlighter)
                ApplyCurrentPenColor();
        }

        public void SyncPenColorState()
        {
            if (AppMode == AppMode.Normal)
            {
                UseLightPenPalette = _desktopUsesLightPenPalette;
                SelectedPenColor = _lastDesktopPenColor;
            }
            else
            {
                SelectedPenColor = _lastBoardPenColor;
            }

            ApplyCurrentPenColor();
        }

        public void ApplyWhiteboardPalette()
        {
            UseLightPenPalette = !Settings.UsingWhiteboard;
            SyncPenColorState();
        }

        [RelayCommand]
        private void TogglePenPalette()
        {
            UseLightPenPalette = !UseLightPenPalette;
            if (AppMode == AppMode.Normal)
                _desktopUsesLightPenPalette = UseLightPenPalette;

            ApplyCurrentPenColor();
        }

        [RelayCommand]
        public void SelectPenColor(int color)
        {
            var isHighlighter = HighlighterColorsById.ContainsKey(color);
            if (!isHighlighter && !PenColorsById.ContainsKey(color)) return;

            if (isHighlighter)
                SelectedHighlighterColor = color;
            else if (AppMode == AppMode.Normal)
                _lastDesktopPenColor = color;
            else
                _lastBoardPenColor = color;

            if (isHighlighter || InkCanvasDrawingAttributes.IsHighlighter)
                SetPenType(isHighlighter);

            SyncPenColorState();
            PenColorSelected?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        public void SwitchToDefaultPen()
        {
            SetPenType(false);
            SyncPenColorState();
        }

        [RelayCommand]
        private void SwitchToHighlighterPen()
        {
            SetPenType(true);
            SyncPenColorState();
        }

        private void SetPenType(bool isHighlighter)
        {
            var attributes = InkCanvasDrawingAttributes;
            attributes.IsHighlighter = isHighlighter;
            attributes.Width = isHighlighter ? Settings.HighlighterWidth / 2 : Settings.InkWidth;
            attributes.Height = isHighlighter ? Settings.HighlighterWidth : Settings.InkWidth;
            attributes.StylusTip = isHighlighter ? StylusTip.Rectangle : StylusTip.Ellipse;
        }

        [RelayCommand]
        private void ToggleWhiteboardBackground()
        {
            Settings.UsingWhiteboard = !Settings.UsingWhiteboard;
            if (Settings.UsingWhiteboard && SelectedPenColor == 5)
                _lastBoardPenColor = 0;
            else if (!Settings.UsingWhiteboard && SelectedPenColor == 0)
                _lastBoardPenColor = 5;

            ApplyWhiteboardPalette();
            _settingsService.SaveSettings();
        }

        [RelayCommand]
        private void SwitchToCircleEraser() => Settings.EraserShapeType = 0;

        [RelayCommand]
        private void SwitchToRectangleEraser() => Settings.EraserShapeType = 1;

        private void ApplyCurrentPenColor()
        {
            var options = InkCanvasDrawingAttributes.IsHighlighter
                ? HighlighterColorsById
                : PenColorsById;
            var colorIndex = InkCanvasDrawingAttributes.IsHighlighter
                ? SelectedHighlighterColor
                : SelectedPenColor;

            if (!options.TryGetValue(colorIndex, out var option)) return;

            var color = option.CurrentColor;
            InkCanvasDrawingAttributes.Color = InkCanvasDrawingAttributes.IsHighlighter
                ? color
                : Color.FromArgb(InkCanvasDrawingAttributes.Color.A, color.R, color.G, color.B);
        }

        private static void UpdateColorSelection(
            IReadOnlyDictionary<int, PenColorOption> options,
            int selectedColor)
        {
            foreach (var option in options.Values)
                option.IsSelected = option.Id == selectedColor;
        }

        private static PenColorOption[] CreatePenColors() =>
        [
            new(0, "黑色", Color.FromRgb(0, 0, 0)),
            new(1, "红色", Color.FromRgb(239, 68, 68), Color.FromRgb(220, 38, 38)),
            new(2, "绿色", Color.FromRgb(34, 197, 94), Color.FromRgb(22, 163, 74)),
            new(3, "蓝色", Color.FromRgb(59, 130, 246), Color.FromRgb(37, 99, 235)),
            new(4, "黄色", Color.FromRgb(250, 204, 21), Color.FromRgb(234, 179, 8)),
            new(5, "白色", Color.FromRgb(255, 255, 255), useDarkCheckMark: true),
            new(6, "粉色", Color.FromRgb(236, 72, 153), Color.FromRgb(147, 51, 234)),
            new(7, "青色", Color.FromRgb(20, 184, 166), Color.FromRgb(13, 148, 136)),
            new(8, "橙色", Color.FromRgb(249, 115, 22), Color.FromRgb(234, 88, 12))
        ];

        private static PenColorOption[] CreateHighlighterColors() =>
        [
            new(100, "荧光笔 · 黑色", Color.FromRgb(0, 0, 0)),
            new(101, "荧光笔 · 白色", Color.FromRgb(250, 250, 250), useDarkCheckMark: true),
            new(102, "荧光笔 · 红色", Color.FromRgb(239, 68, 68)),
            new(103, "荧光笔 · 黄色", Color.FromRgb(253, 224, 71), useDarkCheckMark: true),
            new(104, "荧光笔 · 绿色", Color.FromRgb(74, 222, 128), useDarkCheckMark: true),
            new(105, "荧光笔 · 灰色", Color.FromRgb(113, 113, 122)),
            new(106, "荧光笔 · 蓝色", Color.FromRgb(59, 130, 246)),
            new(107, "荧光笔 · 紫色", Color.FromRgb(168, 85, 247)),
            new(108, "荧光笔 · 青色", Color.FromRgb(45, 212, 191), useDarkCheckMark: true),
            new(109, "荧光笔 · 橙色", Color.FromRgb(249, 115, 22))
        ];

        private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Settings.FitToCurve):
                    InkCanvasDrawingAttributes.FitToCurve = Settings.FitToCurve;
                    break;
                case nameof(Settings.InkWidth) when !InkCanvasDrawingAttributes.IsHighlighter:
                    InkCanvasDrawingAttributes.Width = Settings.InkWidth;
                    InkCanvasDrawingAttributes.Height = Settings.InkWidth;
                    break;
                case nameof(Settings.HighlighterWidth) when InkCanvasDrawingAttributes.IsHighlighter:
                    InkCanvasDrawingAttributes.Width = Settings.HighlighterWidth / 2;
                    InkCanvasDrawingAttributes.Height = Settings.HighlighterWidth;
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
