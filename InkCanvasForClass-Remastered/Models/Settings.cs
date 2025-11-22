using CommunityToolkit.Mvvm.ComponentModel;
using InkCanvasForClass_Remastered.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using WindowsShortcutFactory;

namespace InkCanvasForClass_Remastered.Models
{
    public partial class Settings : ObservableObject
    {
        // Canvas
        [ObservableProperty]
        private double _inkWidth = 2.5;
        [ObservableProperty]
        private double _highlighterWidth = 20;
        [ObservableProperty]
        private double _inkAlpha = 255;
        [ObservableProperty]
        private bool _isShowCursor = true;
        [ObservableProperty]
        private int _inkStyle = 0;
        [ObservableProperty]
        private int _eraserSize = 1;
        [ObservableProperty]
        private int _eraserShapeType = 1; // 0 - 圆形擦  1 - 黑板擦
        [ObservableProperty]
        private bool _hideStrokeWhenSelecting = false;
        [ObservableProperty]
        private bool _fitToCurve = true;
        [ObservableProperty]
        private bool _clearCanvasAndClearTimeMachine = false;
        [ObservableProperty]
        private bool _usingWhiteboard = false;
        // Gesture
        [ObservableProperty]
        private bool _isEnableMultiTouchMode = true;
        [ObservableProperty]
        private bool _isEnableTwoFingerZoom = false;
        [ObservableProperty]
        private bool _isEnableTwoFingerTranslate = true;
        [ObservableProperty]
        private bool _autoSwitchTwoFingerGesture = false;
        [ObservableProperty]
        private bool _isEnableTwoFingerRotation = false;
        [ObservableProperty]
        private bool _isEnableTwoFingerRotationOnSelection = false;
        // Startup
        [JsonIgnore]
        public bool IsAutoStartEnabled
        {
            get => File.Exists(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "ICC-Re.lnk"));
            set
            {
                string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "ICC-Re.lnk");
                try
                {
                    if (value)
                    {
                        using WindowsShortcut shortcut = new()
                        {
                            Path = Environment.ProcessPath,
                            WorkingDirectory = Environment.CurrentDirectory
                        };
                        shortcut.Save(path);
                    }
                    else
                    {
                        File.Delete(path);
                    }
                    OnPropertyChanged();
                }
                catch (Exception ex)
                {
                    App.GetService<ILogger<Settings>>().LogError(ex, "无法创建开机自启动快捷方式。");
                }
            }
        }
        [ObservableProperty]
        private bool _isEnableNibMode = false;
        [ObservableProperty]
        private bool _isHideFloatingBarOnStart = false;
        // Appearance
        [ObservableProperty]
        private bool _isEnableDisPlayNibModeToggler = false;
        [ObservableProperty]
        private double _viewboxFloatingBarScaleTransformValue = 1;
        [ObservableProperty]
        private int _floatingBarImg = 0;
        [ObservableProperty]
        private double _viewboxFloatingBarOpacityValue = 1;
        [ObservableProperty]
        private bool _enableTrayIcon = true;
        [ObservableProperty]
        private double _viewboxFloatingBarOpacityInPPTValue = 1;
        [ObservableProperty]
        private bool _enableViewboxBlackBoardScaleTransform = false;
        [ObservableProperty]
        private bool _enableTimeDisplayInWhiteboardMode = true;
        [ObservableProperty]
        private int _unFoldButtonImageType = 0;
        [ObservableProperty]
        private int _theme = 0;
        // PowerPointSettings
        [ObservableProperty]
        private bool _showPPTButton = true;
        // 每一个数位代表一个选项，2就是开启，1就是关闭
        [ObservableProperty]
        private int _pPTButtonsDisplayOption = 2222;
        [ObservableProperty]
        private bool _isLeftSidePPTButtonVisible = true;
        [ObservableProperty]
        private bool _isRightSidePPTButtonVisible = true;
        // 0居中，+就是往上，-就是往下
        [ObservableProperty]
        private int _pPTLSButtonPosition = 0;
        // 0居中，+就是往上，-就是往下
        [ObservableProperty]
        private int _pPTRSButtonPosition = 0;
        [ObservableProperty]
        private bool _isShowPPTPageNumbers = true;
        [ObservableProperty]
        private bool _isPPTButtonTranslucent = true;
        [ObservableProperty]
        private bool _isPPTButtonBlackBackground = false;
        [ObservableProperty]
        private int _pPTSButtonsOption = 221;
        [ObservableProperty]
        private bool _enablePPTButtonPageClickable = true;
        [ObservableProperty]
        private bool _powerPointSupport = true;
        [ObservableProperty]
        private bool _isShowCanvasAtNewSlideShow = true;
        [ObservableProperty]
        private bool _isAutoSaveStrokesInPowerPoint = true;
        [ObservableProperty]
        private bool _isAutoSaveScreenShotInPowerPoint = true;
        [ObservableProperty]
        private bool _isEnableTwoFingerGestureInPresentationMode = false;
        [ObservableProperty]
        private bool _isEnableFingerGestureSlideShowControl = false;
        [ObservableProperty]
        private int _pPTNavigationPanelWidth = 60;
        // Automation
        [ObservableProperty]
        private bool _isAutoFoldInEasiNote = true;
        [ObservableProperty]
        private bool _isAutoFoldInEasiNoteIgnoreDesktopAnno = false;
        [ObservableProperty]
        private bool _isAutoFoldInEasiCamera = true;
        [ObservableProperty]
        private bool _isAutoFoldInEasiNote3 = false;
        [ObservableProperty]
        private bool _isAutoFoldInEasiNote3C = false;
        [ObservableProperty]
        private bool _isAutoFoldInEasiNote5C = false;
        [ObservableProperty]
        private bool _isAutoFoldInPPTSlideShow = false;
        [ObservableProperty]
        private bool _isAutoKillPptService = false;
        [ObservableProperty]
        private bool _isSaveScreenshotsInDateFolders = false;
        [ObservableProperty]
        private bool _isAutoSaveStrokesAtScreenshot = true;
        [ObservableProperty]
        private bool _isAutoSaveStrokesAtClear = true;
        [ObservableProperty]
        private double _minimumAutomationStrokeNumber = 0.3;
        [ObservableProperty]
        private string _autoSaveStrokesPath = @"D:\ICC-Re";
        [ObservableProperty]
        private bool _autoDelSavedFiles = true;
        [ObservableProperty]
        private int _autoDelSavedFilesDays = 15;
        // Advanced
        [ObservableProperty]
        private bool _isSpecialScreen = true;
        [ObservableProperty]
        private bool _isQuadIR = false;
        [ObservableProperty]
        private double _touchMultiplier = 0.3;
        [ObservableProperty]
        private int _nibModeBoundsWidth = 5;
        [ObservableProperty]
        private int _fingerModeBoundsWidth = 20;
        [ObservableProperty]
        private bool _eraserBindTouchMultiplier = true;
        [ObservableProperty]
        private bool _isEnableEdgeGestureUtil = false;
        [ObservableProperty]
        private bool _isEnableForceFullScreen = false;
        [ObservableProperty]
        private bool _isEnableResolutionChangeDetection = false;
        [ObservableProperty]
        private bool _isEnableDPIChangeDetection = false;
        [ObservableProperty]
        private bool _isSecondConfirmWhenShutdownApp = false;
        [ObservableProperty]
        private bool _isCriticalSafeMode = false;
        [ObservableProperty]
        private int _criticalSafeModeMethod = 0;
        [ObservableProperty]
        private int _windowMode = 0;
        // RandSettings
        [ObservableProperty]
        private bool _displayRandWindowNamesInputBtn = true;
        [ObservableProperty]
        private double _randWindowOnceCloseLatency = 2.5;

        [ObservableProperty]
        private bool _isWindowNoActivate = false;

        [JsonIgnore]
        public bool IsEnableAutoFold =>
            IsAutoFoldInEasiNote
            || IsAutoFoldInEasiCamera
            || IsAutoFoldInEasiNote3
            || IsAutoFoldInEasiNote3C
            || IsAutoFoldInEasiNote5C
            || IsAutoFoldInPPTSlideShow;
        [JsonIgnore]
        public bool IsEnableTwoFingerGesture => IsEnableTwoFingerZoom || IsEnableTwoFingerTranslate || IsEnableTwoFingerRotation;
        [JsonIgnore]
        public bool IsEnableTwoFingerGestureTranslateOrRotation => IsEnableTwoFingerTranslate || IsEnableTwoFingerRotation;

    }
}
