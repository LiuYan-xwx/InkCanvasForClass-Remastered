using CommunityToolkit.Mvvm.ComponentModel;
using InkCanvasForClass_Remastered.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
        private int _eraserType = 0; // 0 - 图标切换模式      1 - 面积擦     2 - 线条擦
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
        [ObservableProperty]
        private OptionalOperation _hyperbolaAsymptoteOption = OptionalOperation.Yes;
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
        [ObservableProperty]
        private bool _runAtStartup = false;
        [ObservableProperty]
        private bool _isEnableNibMode = false;
        [ObservableProperty]
        private bool _isFoldAtStartup = false;
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
        private bool _enableTrayIcon= true;
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
        // 0居中，+就是往上，-就是往下
        [ObservableProperty]
        private int _pPTLSButtonPosition = 0;
        // 0居中，+就是往上，-就是往下
        [ObservableProperty]
        private int _pPTRSButtonPosition = 0;
        [ObservableProperty]
        private int _pPTSButtonsOption = 221;
        [ObservableProperty]
        private int _pPTBButtonsOption = 121;
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
        private bool _isNotifyPreviousPage = false;
        [ObservableProperty]
        private bool _isNotifyHiddenPage = false;
        [ObservableProperty]
        private bool _isNotifyAutoPlayPresentation = false;
        [ObservableProperty]
        private bool _isEnableTwoFingerGestureInPresentationMode = false;
        [ObservableProperty]
        private bool _isEnableFingerGestureSlideShowControl = false;
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
        private bool _isAutoFoldInSeewoPincoTeacher = false;
        [ObservableProperty]
        private bool _isAutoFoldInHiteTouchPro = false;
        [ObservableProperty]
        private bool _isAutoFoldInHiteLightBoard = false;
        [ObservableProperty]
        private bool _isAutoFoldInHiteCamera = false;
        [ObservableProperty]
        private bool _isAutoFoldInWxBoardMain = false;
        [ObservableProperty]
        private bool _isAutoFoldInOldZyBoard = false;
        [ObservableProperty]
        private bool _isAutoFoldInMSWhiteboard = false;
        [ObservableProperty]
        private bool _isAutoFoldInAdmoxWhiteboard = false;
        [ObservableProperty]
        private bool _isAutoFoldInAdmoxBooth = false;
        [ObservableProperty]
        private bool _isAutoFoldInQPoint = false;
        [ObservableProperty]
        private bool _isAutoFoldInYiYunVisualPresenter = false;
        [ObservableProperty]
        private bool _isAutoFoldInMaxHubWhiteboard = false;
        [ObservableProperty]
        private bool _isAutoFoldInPPTSlideShow = false;
        [ObservableProperty]
        private bool _isAutoKillPptService = false;
        [ObservableProperty]
        private bool _isAutoKillEasiNote = false;
        [ObservableProperty]
        private bool _isAutoKillHiteAnnotation = false;
        [ObservableProperty]
        private bool _isAutoKillVComYouJiao = false;
        [ObservableProperty]
        private bool _isAutoKillSeewoLauncher2DesktopAnnotation = false;
        [ObservableProperty]
        private bool _isAutoKillInkCanvas = false;
        [ObservableProperty]
        private bool _isAutoKillICA = false;
        [ObservableProperty]
        private bool _isAutoKillIDT = true;
        [ObservableProperty]
        private bool _isSaveScreenshotsInDateFolders = false;
        [ObservableProperty]
        private bool _isAutoSaveStrokesAtScreenshot = true;
        [ObservableProperty]
        private bool _isAutoSaveStrokesAtClear = true;
        [ObservableProperty]
        private int _minimumAutomationStrokeNumber = 0;
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
        private bool _isEnableFullScreenHelper = false;
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
        // RandSettings
        [ObservableProperty]
        private bool _displayRandWindowNamesInputBtn = false;
        [ObservableProperty]
        private double _randWindowOnceCloseLatency = 2.5;
        [ObservableProperty]
        private int _randWindowOnceMaxStudents = 10;



        [JsonIgnore]
        public bool IsEnableAutoFold =>
            IsAutoFoldInEasiNote
            || IsAutoFoldInEasiCamera
            || IsAutoFoldInEasiNote3
            || IsAutoFoldInEasiNote3C
            || IsAutoFoldInEasiNote5C
            || IsAutoFoldInSeewoPincoTeacher
            || IsAutoFoldInHiteTouchPro
            || IsAutoFoldInHiteLightBoard
            || IsAutoFoldInHiteCamera
            || IsAutoFoldInWxBoardMain
            || IsAutoFoldInOldZyBoard
            || IsAutoFoldInMSWhiteboard
            || IsAutoFoldInAdmoxWhiteboard
            || IsAutoFoldInAdmoxBooth
            || IsAutoFoldInQPoint
            || IsAutoFoldInYiYunVisualPresenter
            || IsAutoFoldInMaxHubWhiteboard
            || IsAutoFoldInPPTSlideShow;
        [JsonIgnore]
        public bool IsEnableTwoFingerGesture => IsEnableTwoFingerZoom || IsEnableTwoFingerTranslate || IsEnableTwoFingerRotation;
        [JsonIgnore]
        public bool IsEnableTwoFingerGestureTranslateOrRotation => IsEnableTwoFingerTranslate || IsEnableTwoFingerRotation;

    }
}
