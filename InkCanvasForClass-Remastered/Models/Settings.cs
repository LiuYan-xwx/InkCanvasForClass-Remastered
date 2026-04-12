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
        public partial double InkWidth { get; set; } = 2.5;

        [ObservableProperty]
        public partial double HighlighterWidth { get; set; } = 20;

        [ObservableProperty]
        public partial double InkAlpha { get; set; } = 255;

        [ObservableProperty]
        public partial bool IsShowCursor { get; set; } = true;

        [ObservableProperty]
        public partial int InkStyle { get; set; } = 0;

        [ObservableProperty]
        public partial int EraserSize { get; set; } = 1;

        [ObservableProperty]
        public partial int EraserShapeType { get; set; } = 1; // 0 - 圆形擦  1 - 黑板擦

        [ObservableProperty]
        public partial bool HideStrokeWhenSelecting { get; set; } = false;

        [ObservableProperty]
        public partial bool FitToCurve { get; set; } = true;

        [ObservableProperty]
        public partial bool ClearCanvasAndClearTimeMachine { get; set; } = false;

        [ObservableProperty]
        public partial bool UsingWhiteboard { get; set; } = false;

        // Gesture
        [ObservableProperty]
        public partial bool IsEnableMultiTouchMode { get; set; } = true;

        [ObservableProperty]
        public partial bool IsEnableTwoFingerZoom { get; set; } = false;

        [ObservableProperty]
        public partial bool IsEnableTwoFingerTranslate { get; set; } = true;

        [ObservableProperty]
        public partial bool AutoSwitchTwoFingerGesture { get; set; } = false;

        [ObservableProperty]
        public partial bool IsEnableTwoFingerRotation { get; set; } = false;

        [ObservableProperty]
        public partial bool IsEnableTwoFingerRotationOnSelection { get; set; } = false;

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
        public partial bool IsEnableNibMode { get; set; } = false;

        [ObservableProperty]
        public partial bool IsHideFloatingBarOnStart { get; set; } = false;

        // Appearance
        [ObservableProperty]
        public partial bool IsEnableDisPlayNibModeToggler { get; set; } = false;

        [ObservableProperty]
        public partial double ViewboxFloatingBarScaleTransformValue { get; set; } = 1;

        [ObservableProperty]
        public partial int FloatingBarImg { get; set; } = 0;

        [ObservableProperty]
        public partial double ViewboxFloatingBarOpacityValue { get; set; } = 1;

        [ObservableProperty]
        public partial bool EnableTrayIcon { get; set; } = true;

        [ObservableProperty]
        public partial double ViewboxFloatingBarOpacityInPPTValue { get; set; } = 1;

        [ObservableProperty]
        public partial bool EnableViewboxBlackBoardScaleTransform { get; set; } = false;

        [ObservableProperty]
        public partial bool EnableTimeDisplayInWhiteboardMode { get; set; } = true;

        [ObservableProperty]
        public partial int UnFoldButtonImageType { get; set; } = 0;

        [ObservableProperty]
        public partial int Theme { get; set; } = 0;

        // PowerPointSettings
        [ObservableProperty]
        public partial bool ShowPPTButton { get; set; } = true;

        // 每一个数位代表一个选项，2就是开启，1就是关闭
        [ObservableProperty]
        public partial int PPTButtonsDisplayOption { get; set; } = 2222;

        [ObservableProperty]
        public partial bool IsLeftSidePPTButtonVisible { get; set; } = true;

        [ObservableProperty]
        public partial bool IsRightSidePPTButtonVisible { get; set; } = true;

        // 0居中，+就是往上，-就是往下
        [ObservableProperty]
        public partial int PPTLSButtonPosition { get; set; } = 0;

        // 0居中，+就是往上，-就是往下
        [ObservableProperty]
        public partial int PPTRSButtonPosition { get; set; } = 0;

        [ObservableProperty]
        public partial bool IsShowPPTPageNumbers { get; set; } = true;

        [ObservableProperty]
        public partial bool IsPPTButtonTranslucent { get; set; } = true;

        [ObservableProperty]
        public partial bool IsPPTButtonBlackBackground { get; set; } = false;

        [ObservableProperty]
        public partial int PPTSButtonsOption { get; set; } = 221;

        [ObservableProperty]
        public partial bool EnablePPTButtonPageClickable { get; set; } = true;

        [ObservableProperty]
        public partial bool PowerPointSupport { get; set; } = true;

        [ObservableProperty]
        public partial bool IsShowCanvasAtNewSlideShow { get; set; } = true;

        [ObservableProperty]
        public partial bool IsAutoSaveStrokesInPowerPoint { get; set; } = true;

        [ObservableProperty]
        public partial bool IsAutoSaveScreenShotInPowerPoint { get; set; } = true;

        [ObservableProperty]
        public partial bool IsEnableTwoFingerGestureInPresentationMode { get; set; } = false;

        [ObservableProperty]
        public partial bool IsEnableFingerGestureSlideShowControl { get; set; } = false;
        [ObservableProperty]
        public partial int PPTNavigationPanelWidth { get; set; } = 60;

        // Automation
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEnableAutoFold))]
        public partial bool IsAutoFoldInEasiNote { get; set; } = true;

        [ObservableProperty]
        public partial bool IsAutoFoldInEasiNoteIgnoreDesktopAnno { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEnableAutoFold))]
        public partial bool IsAutoFoldInEasiCamera { get; set; } = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEnableAutoFold))]
        public partial bool IsAutoFoldInEasiNote3 { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEnableAutoFold))]
        public partial bool IsAutoFoldInEasiNote3C { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEnableAutoFold))]
        public partial bool IsAutoFoldInEasiNote5C { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEnableAutoFold))]
        public partial bool IsAutoFoldInPPTSlideShow { get; set; } = false;

        [ObservableProperty]
        public partial bool IsAutoKillPptService { get; set; } = false;

        [ObservableProperty]
        public partial bool IsAutoSaveStrokesAtScreenshot { get; set; } = true;

        [ObservableProperty]
        public partial bool IsAutoSaveStrokesAtClear { get; set; } = true;

        [ObservableProperty]
        public partial double MinimumAutomationStrokeNumber { get; set; } = 0.3;

        [ObservableProperty]
        public partial string AutoSaveStrokesPath { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool AutoDelSavedFiles { get; set; } = true;

        [ObservableProperty]
        public partial int AutoDelSavedFilesDays { get; set; } = 15;

        // Advanced
        [ObservableProperty]
        public partial bool IsSpecialScreen { get; set; } = true;

        [ObservableProperty]
        public partial bool IsQuadIR { get; set; } = false;

        [ObservableProperty]
        public partial double TouchMultiplier { get; set; } = 0.3;

        [ObservableProperty]
        public partial int NibModeBoundsWidth { get; set; } = 5;

        [ObservableProperty]
        public partial int FingerModeBoundsWidth { get; set; } = 20;

        [ObservableProperty]
        public partial bool EraserBindTouchMultiplier { get; set; } = true;

        [ObservableProperty]
        public partial bool IsEnableEdgeGestureUtil { get; set; } = false;

        [ObservableProperty]
        public partial bool IsEnableForceFullScreen { get; set; } = false;

        [ObservableProperty]
        public partial bool IsEnableResolutionChangeDetection { get; set; } = false;

        [ObservableProperty]
        public partial bool IsEnableDPIChangeDetection { get; set; } = false;

        [ObservableProperty]
        public partial bool IsSecondConfirmWhenShutdownApp { get; set; } = false;

        [ObservableProperty]
        public partial bool IsCriticalSafeMode { get; set; } = false;

        [ObservableProperty]
        public partial int CriticalSafeModeMethod { get; set; } = 0;

        [ObservableProperty]
        public partial int WindowMode { get; set; } = 0;

        [ObservableProperty]
        public partial bool RefreshMainWindowTopmost { get; set; }

        // RandSettings
        [ObservableProperty]
        public partial bool DisplayRandWindowNamesInputBtn { get; set; } = true;

        [ObservableProperty]
        public partial double RandWindowOnceCloseLatency { get; set; } = 2.5;

        [ObservableProperty]
        public partial bool IsWindowNoActivate { get; set; } = false;

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
