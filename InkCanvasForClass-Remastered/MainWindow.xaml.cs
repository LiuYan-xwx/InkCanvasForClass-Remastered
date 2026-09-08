using InkCanvasForClass_Remastered.Controls;
using InkCanvasForClass_Remastered.Enums;
using InkCanvasForClass_Remastered.Helpers;
using InkCanvasForClass_Remastered.Interfaces;
using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.Services;
using InkCanvasForClass_Remastered.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Office.Interop.PowerPoint;
using Microsoft.Win32;
using OSVersionExtension;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Application = System.Windows.Application;
using File = System.IO.File;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;
using Point = System.Windows.Point;

namespace InkCanvasForClass_Remastered
{
    public partial class MainWindow : Window
    {
        public readonly MainViewModel _viewModel;
        private readonly SettingsService _settingsService;
        private readonly IPowerPointService _powerPointService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<MainWindow> Logger;
        public Settings Settings => _settingsService.Settings;


        #region Window Initialization

        public MainWindow(MainViewModel viewModel,
                          SettingsService settingsService,
                          IPowerPointService powerPointService,
                          INotificationService notificationService,
                          ILogger<MainWindow> logger)
        {
            /*
                处于画板模式内：Topmost == false / _viewModel.AppMode == AppMode.WhiteBoard
                处于 PPT 放映内：_powerPointService.IsInSlideShow
            */
            InitializeComponent();

            _viewModel = viewModel;
            _settingsService = settingsService;
            _powerPointService = powerPointService;
            _notificationService = notificationService;
            Logger = logger;

            DataContext = _viewModel;

            // 挂载PPT服务事件
            _powerPointService.SlideShowBegin += PptApplication_SlideShowBegin;
            _powerPointService.SlideShowEnd += PptApplication_SlideShowEnd;
            _powerPointService.SlideShowNextSlide += PptApplication_SlideShowNextSlide;

            Settings.PropertyChanged += Settings_PropertyChanged;

            _notificationService.NotificationRequested += OnNotificationRequested;

            ViewboxFloatingBar.Margin = new Thickness((SystemParameters.WorkArea.Width - 284) / 2,
                SystemParameters.WorkArea.Height - 60, -2000, -200);
            ViewboxFloatingBarMarginAnimation(100, true);

            InitTimers();
            timeMachine.OnRedoStateChanged += TimeMachine_OnRedoStateChanged;
            timeMachine.OnUndoStateChanged += TimeMachine_OnUndoStateChanged;
            inkCanvas.Strokes.StrokesChanged += StrokesOnStrokesChanged;

            CheckColorTheme(true);
        }
        private readonly DispatcherTimer topmostRefreshTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(400)
        };
        private readonly Random sharedRandom = Random.Shared;
        private void TopmostRefreshTimer_Tick(object? sender, EventArgs e)
        {
            Topmost = false;
            Topmost = true;
        }

        private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Settings.ViewboxFloatingBarScaleTransformValue):
                    if (_powerPointService.IsInSlideShow == true)
                        ViewboxFloatingBarMarginAnimation(60);
                    else
                        ViewboxFloatingBarMarginAnimation(100, true);
                    break;
                case nameof(Settings.EraserSize):
                case nameof(Settings.EraserShapeType):
                    UpdateEraserShape();
                    break;
                case nameof(Settings.IsEnableTwoFingerRotationOnSelection) or nameof(Settings.IsEnableTwoFingerRotation):
                    CheckEnableTwoFingerGestureBtnColorPrompt();
                    break;
                case nameof(Settings.FingerModeBoundsWidth) or nameof(Settings.NibModeBoundsWidth):
                    BoundsWidth = Settings.IsEnableNibMode ? Settings.NibModeBoundsWidth : Settings.FingerModeBoundsWidth;
                    break;
                case nameof(Settings.WindowMode):
                    SetWindowMode();
                    break;
                case nameof(Settings.IsEnableEdgeGestureUtil):
                    if (OSVersion.GetOperatingSystem() >= OSVersionExtension.OperatingSystem.Windows10)
                        EdgeGestureUtil.DisableEdgeGestures(new WindowInteropHelper(this).Handle, Settings.IsEnableEdgeGestureUtil);
                    break;
                case nameof(Settings.IsEnableAutoFold):
                    StartOrStoptimerCheckAutoFold();
                    break;
                case nameof(Settings.IsAutoKillPptService):
                    StartOrStopTimerKillProcess();
                    break;
                case nameof(Settings.InkStyle):
                case nameof(Settings.HighlighterWidth):
                    if (isLoaded) _settingsService.SaveSettings();
                    break;
            }
        }

        private void UpdateEraserShape()
        {
            double k = GetEraserSizeMultiplier(Settings.EraserSize, Settings.EraserShapeType);

            if (Settings.EraserShapeType == 0)
            {
                inkCanvas.EraserShape = new EllipseStylusShape(k * 90, k * 90);
            }
            else if (Settings.EraserShapeType == 1)
            {
                inkCanvas.EraserShape = new RectangleStylusShape(k * 90 * 0.6, k * 90);
            }

            if (inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint)
            {
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
            }
        }

        private static double GetEraserSizeMultiplier(int eraserSize, int eraserShapeType)
        {
            return eraserShapeType switch
            {
                0 => eraserSize switch // EllipseStylusShape
                {
                    0 => 0.5,
                    1 => 0.8,
                    3 => 1.25,
                    4 => 1.8,
                    _ => 1.0
                },
                1 => eraserSize switch // RectangleStylusShape
                {
                    0 => 0.7,
                    1 => 0.9,
                    3 => 1.2,
                    4 => 1.6,
                    _ => 1.0
                },
                _ => 1.0
            };
        }

        #endregion

        #region Ink Canvas Functions
        //private void InkCanvas_Gesture(object sender, InkCanvasGestureEventArgs e)
        //{
        //var gestures = e.GetGestureRecognitionResults();
        //try
        //{
        //    foreach (var gest in gestures)
        //        //Trace.WriteLine(string.Format("Gesture: {0}, Confidence: {1}", gest.ApplicationGesture, gest.RecognitionConfidence));
        //        if (StackPanelPPTControls.Visibility == Visibility.Visible)
        //        {
        //            if (gest.ApplicationGesture == ApplicationGesture.Left)
        //                BtnPPTSlidesDown_Click(BtnPPTSlidesDown, null);
        //            if (gest.ApplicationGesture == ApplicationGesture.Right)
        //                BtnPPTSlidesUp_Click(BtnPPTSlidesUp, null);
        //        }
        //}
        //catch { }
        //}

        private void inkCanvas_EditingModeChanged(object? sender, RoutedEventArgs? e)
        {
            //if (sender is not InkCanvas inkCanvas1)
            //{
            //    return;
            //}

            //if (Settings.IsShowCursor
            //    && inkCanvas1.EditingMode == InkCanvasEditingMode.Ink)
            //{
            //    inkCanvas1.ForceCursor = true;
            //}
            //else
            //{
            //    inkCanvas1.ForceCursor = false;
            //}
        }

        #endregion Ink Canvas

        #region Definations and Loading

        private bool isLoaded = false;

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            // 无焦点模式
            if (Settings.IsWindowNoActivate)
            {
                var handle = new WindowInteropHelper(this).Handle;
                var exstyle = GetWindowLong(handle, GWL_EXSTYLE);
                SetWindowLong(handle, GWL_EXSTYLE, new IntPtr(exstyle.ToInt32() | WS_EX_NOACTIVATE));
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetWindowMode();

            CursorFloatingBarButton_Click(null, null);

            ApplySettingsToUI();

            Logger.LogInformation("MainWindow Loaded");

            isLoaded = true;

            BlackBoardLeftSidePageListView.ItemsSource = blackBoardSidePageListViewObservableCollection;
            BlackBoardRightSidePageListView.ItemsSource = blackBoardSidePageListViewObservableCollection;

            if (Settings.IsHideFloatingBarOnStart)
            {
                _ = HideFloatingBar();
            }
            if (Settings.RefreshMainWindowTopmost)
            {
                topmostRefreshTimer.Tick += TopmostRefreshTimer_Tick;
                topmostRefreshTimer.Start();
            }
        }

        private void SetWindowMode()
        {
            switch (Settings.WindowMode)
            {
                case 0:
                    WindowState = WindowState.Maximized;
                    break;
                case 1:
                    WindowState = WindowState.Normal;
                    Left = 0.0;
                    Top = 0.0;
                    Height = SystemParameters.PrimaryScreenHeight - 1;
                    Width = SystemParameters.PrimaryScreenWidth;
                    break;
            }
        }

        private void SystemEventsOnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            if (!Settings.IsEnableResolutionChangeDetection) return;
            ShowNotification($"检测到显示器信息变化，变为{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width}x{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height}");
            new Thread(() =>
            {
                var isFloatingBarOutsideScreen = false;
                Dispatcher.Invoke(() =>
                {
                    isFloatingBarOutsideScreen = IsOutsideOfScreenHelper.IsOutsideOfScreen(ViewboxFloatingBar);
                });
                if (isFloatingBarOutsideScreen) dpiChangedDelayAction.DebounceAction(3000, null, () =>
                {
                    if (_viewModel.IsFloatingBarVisible)
                    {
                        if (_powerPointService.IsInSlideShow)
                            ViewboxFloatingBarMarginAnimation(60);
                        else
                            ViewboxFloatingBarMarginAnimation(100, true);
                    }
                });
            }).Start();
        }

        public DelayAction dpiChangedDelayAction = new DelayAction();

        private void MainWindow_OnDpiChanged(object sender, DpiChangedEventArgs e)
        {
            if (e.OldDpi.DpiScaleX != e.NewDpi.DpiScaleX && e.OldDpi.DpiScaleY != e.NewDpi.DpiScaleY && Settings.IsEnableDPIChangeDetection)
            {
                ShowNotification($"系统DPI发生变化，从 {e.OldDpi.DpiScaleX}x{e.OldDpi.DpiScaleY} 变化为 {e.NewDpi.DpiScaleX}x{e.NewDpi.DpiScaleY}");

                new Thread(() =>
                {
                    var isFloatingBarOutsideScreen = false;
                    var isInPPTPresentationMode = false;
                    Dispatcher.Invoke(() =>
                    {
                        isFloatingBarOutsideScreen = IsOutsideOfScreenHelper.IsOutsideOfScreen(ViewboxFloatingBar);
                        isInPPTPresentationMode = _powerPointService.IsInSlideShow;
                    });
                    if (isFloatingBarOutsideScreen) dpiChangedDelayAction.DebounceAction(3000, null, () =>
                    {
                        if (_viewModel.IsFloatingBarVisible)
                        {
                            if (isInPPTPresentationMode) ViewboxFloatingBarMarginAnimation(60);
                            else ViewboxFloatingBarMarginAnimation(100, true);
                        }
                    });
                }).Start();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Logger.LogInformation("MainWindow closing");
            if (!CloseIsFromButton && Settings.IsSecondConfirmWhenShutdownApp)
            {
                e.Cancel = true;
                if (MessageBox.Show("是否继续关闭 ICC-Re，这将丢失当前未保存的墨迹。", "InkCanvasForClass-Remastered", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                    e.Cancel = false;
            }

            if (e.Cancel)
            {
                Logger.LogInformation("MainWindow closing cancelled");
                return;
            }
            _settingsService.SaveSettings();
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Settings.IsEnableForceFullScreen)
            {
                if (isLoaded) ShowNotification(
                    $"检测到窗口大小变化，已自动恢复到全屏：{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width}x{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height}（缩放比例为{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / SystemParameters.PrimaryScreenWidth}x{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / SystemParameters.PrimaryScreenHeight}）");
                WindowState = WindowState.Maximized;
                MoveWindow(new WindowInteropHelper(this).Handle, 0, 0,
                    System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                    System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height, true);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SystemEvents.DisplaySettingsChanged -= SystemEventsOnDisplaySettingsChanged;
            _notificationService.NotificationRequested -= OnNotificationRequested;
            _notificationCts?.Cancel();
            _notificationCts?.Dispose();
            Logger.LogInformation("MainWindow closed");
        }

        #endregion Definations and Loading

        #region AutoFold
        private bool isFloatingBarChangingHideMode = false;

        public void HideFloatingBar_Click(object sender, RoutedEventArgs e)
        {
            _ = HideFloatingBar(true);
        }

        public async Task HideFloatingBar(bool isHideManually = false)
        {
            foldFloatingBarByUser = isHideManually;

            unfoldFloatingBarByUser = false;

            if (isFloatingBarChangingHideMode)
                return;

            isFloatingBarChangingHideMode = true;
            _viewModel.IsFloatingBarVisible = false;

            await Dispatcher.InvokeAsync(() =>
            {
                if (_viewModel.AppMode == AppMode.WhiteBoard)
                    CloseWhiteboard();
                if (_powerPointService.IsInSlideShow)
                    if (foldFloatingBarByUser && inkCanvas.Strokes.Count > 2)
                        ShowNotification("正在清空墨迹并收纳至侧边栏，可进入批注模式后通过【撤销】功能来恢复原先墨迹。");
                ClearAndMouseFloatingbarButton_Click(null, null);
            });

            await Dispatcher.InvokeAsync(() =>
            {
                ViewboxFloatingBarMarginAnimation(-60);
                HideSubPanels();
            });
            isFloatingBarChangingHideMode = false;
        }

        private async void SidePanelUnFoldButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowFloatingBar(true);
        }

        public async Task ShowFloatingBar(bool isShowManually = false)
        {
            unfoldFloatingBarByUser = isShowManually;

            foldFloatingBarByUser = false;

            if (isFloatingBarChangingHideMode)
                return;

            isFloatingBarChangingHideMode = true;
            _viewModel.IsFloatingBarVisible = true;

            await Dispatcher.InvokeAsync(() =>
            {
                if (_powerPointService.IsInSlideShow)
                    ViewboxFloatingBarMarginAnimation(60);
                else
                    ViewboxFloatingBarMarginAnimation(100, true);
            });

            isFloatingBarChangingHideMode = false;
        }
        #endregion

        #region BoardControls
        private StrokeCollection[] strokeCollections = new StrokeCollection[101];

        private TimeMachineHistory[][] TimeMachineHistories = new TimeMachineHistory[101][]; //最多99页，0用来存储非白板时的墨迹以便还原

        private void SaveStrokes(bool isBackupMain = false)
        {
            if (isBackupMain)
            {
                var timeMachineHistory = timeMachine.ExportTimeMachineHistory();
                TimeMachineHistories[0] = timeMachineHistory;
                timeMachine.ClearStrokeHistory();
            }
            else
            {
                var timeMachineHistory = timeMachine.ExportTimeMachineHistory();
                TimeMachineHistories[_viewModel.WhiteboardCurrentPage] = timeMachineHistory;
                timeMachine.ClearStrokeHistory();
            }
        }

        private void ClearStrokes(bool isErasedByCode)
        {
            _currentCommitType = CommitReason.ClearingCanvas;
            if (isErasedByCode) _currentCommitType = CommitReason.CodeInput;
            Application.Current.Dispatcher.Invoke(() =>
            {
                inkCanvas.Strokes.Clear();
            });
            _currentCommitType = CommitReason.UserInput;
        }

        private void RestoreStrokes(bool isBackupMain = false)
        {
            try
            {
                if (TimeMachineHistories[_viewModel.WhiteboardCurrentPage] == null) return; //防止白板打开后不居中
                if (isBackupMain)
                {
                    timeMachine.ImportTimeMachineHistory(TimeMachineHistories[0]);
                    foreach (var item in TimeMachineHistories[0]) ApplyHistoryToCanvas(item);
                }
                else
                {
                    timeMachine.ImportTimeMachineHistory(TimeMachineHistories[_viewModel.WhiteboardCurrentPage]);
                    foreach (var item in TimeMachineHistories[_viewModel.WhiteboardCurrentPage]) ApplyHistoryToCanvas(item);
                }
            }
            catch
            {
                // ignored
            }
        }

        private async void BtnWhiteBoardPageIndex_Click(object sender, RoutedEventArgs e)
        {
            if (sender == BtnLeftPageListWB)
            {
                if (BoardBorderLeftPageListView.Visibility == Visibility.Visible)
                {
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderLeftPageListView);
                }
                else
                {
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderRightPageListView);
                    RefreshBlackBoardSidePageListView();
                    AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardBorderLeftPageListView);
                    await Task.Delay(1);
                    ScrollViewToVerticalTop(
                        (ListViewItem)BlackBoardLeftSidePageListView.ItemContainerGenerator.ContainerFromIndex(
                            _viewModel.WhiteboardCurrentPage - 1), BlackBoardLeftSidePageListScrollViewer);
                }
            }
            else if (sender == BtnRightPageListWB)
            {
                if (BoardBorderRightPageListView.Visibility == Visibility.Visible)
                {
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderRightPageListView);
                }
                else
                {
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderLeftPageListView);
                    RefreshBlackBoardSidePageListView();
                    AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardBorderRightPageListView);
                    await Task.Delay(1);
                    ScrollViewToVerticalTop(
                        (ListViewItem)BlackBoardRightSidePageListView.ItemContainerGenerator.ContainerFromIndex(
                            _viewModel.WhiteboardCurrentPage - 1), BlackBoardRightSidePageListScrollViewer);
                }
            }

        }

        private void WhiteBoardAddPage()
        {
            if (_viewModel.WhiteboardTotalPageCount >= 99) return;
            if (Settings.IsAutoSaveStrokesAtClear &&
                inkCanvas.Strokes.Count > Settings.MinimumAutomationStrokeNumber)
                SaveScreenShot(true);
            SaveStrokes();
            ClearStrokes(true);

            _viewModel.WhiteboardTotalPageCount++;
            _viewModel.WhiteboardCurrentPage++;

            if (_viewModel.WhiteboardCurrentPage != _viewModel.WhiteboardTotalPageCount)
                for (var i = _viewModel.WhiteboardTotalPageCount; i > _viewModel.WhiteboardCurrentPage; i--)
                    TimeMachineHistories[i] = TimeMachineHistories[i - 1];

            if (BlackBoardLeftSidePageListView.Visibility == Visibility.Visible)
            {
                RefreshBlackBoardSidePageListView();
            }
        }

        private void BtnWhiteBoardSwitchPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.WhiteboardCurrentPage <= 1) return;

            SaveStrokes();

            ClearStrokes(true);
            _viewModel.WhiteboardCurrentPage--;

            RestoreStrokes();
        }

        private void BtnWhiteBoardSwitchNext_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("113223234");

            if (Settings.IsAutoSaveStrokesAtClear &&
                inkCanvas.Strokes.Count > Settings.MinimumAutomationStrokeNumber)
                SaveScreenShot(true);
            if (_viewModel.WhiteboardCurrentPage == _viewModel.WhiteboardTotalPageCount)
            {
                WhiteBoardAddPage();
                return;
            }

            SaveStrokes();
            ClearStrokes(true);
            _viewModel.WhiteboardCurrentPage++;
            RestoreStrokes();
        }
        #endregion

        #region BoardIcons
        private void BoardChangeBackgroundColorBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.UsingWhiteboard = !Settings.UsingWhiteboard;
            _settingsService.SaveSettings();
            if (Settings.UsingWhiteboard)
            {
                if (_viewModel.SelectedPenColor == 5) lastBoardInkColor = 0;
            }
            else
            {
                if (_viewModel.SelectedPenColor == 0) lastBoardInkColor = 5;
            }

            CheckColorTheme(true);
        }

        private void BoardEraserIcon_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.AppPenMode is InkCanvasEditingMode.EraseByPoint or InkCanvasEditingMode.EraseByStroke)
            {
                if (BoardEraserSizePanel.Visibility == Visibility.Collapsed)
                {
                    AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardEraserSizePanel);
                }
                else
                {
                    AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
                }
            }
            else
            {
                _viewModel.AppPenMode = InkCanvasEditingMode.EraseByPoint;
                UpdateEraserShape();
                inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                inkCanvas_EditingModeChanged(inkCanvas, null);
                CancelSingleFingerDragMode();

                HideSubPanels();
            }
        }

        private void BoardEraserIconByStrokes_Click(object sender, RoutedEventArgs e)
        {
            EraserIconByStrokes_Click(sender, e);
        }

        private void BoardSymbolIconDelete_Click(object sender, RoutedEventArgs e)
        {
            ActivatePen();
            SymbolIconDelete_Click(null, null);
        }
        private void BoardSymbolIconDeleteInkAndHistories_Click(object sender, RoutedEventArgs e)
        {
            ActivatePen();
            SymbolIconDelete_Click(null, null);
            if (Settings.ClearCanvasAndClearTimeMachine == false) timeMachine.ClearStrokeHistory();
        }

        #endregion

        #region Colors
        private void ColorSwitchCheck()
        {
            HideSubPanels();

            if (DrawingAttributesHistory.Count > 0)
            {
                timeMachine.CommitStrokeDrawingAttributesHistory(DrawingAttributesHistory);
                DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
                foreach (var item in DrawingAttributesHistoryFlag)
                {
                    item.Value.Clear();
                }
            }
            else
            {
                _viewModel.AppPenMode = InkCanvasEditingMode.Ink;
                inkCanvas.IsManipulationEnabled = true;
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                CancelSingleFingerDragMode();
                CheckColorTheme();
            }
        }

        private bool isUselightThemeColor = false, isDesktopUselightThemeColor = false;
        private int lastDesktopInkColor = 1, lastBoardInkColor = 5;

        private void CheckColorTheme(bool changeColorTheme = false)
        {
            if (changeColorTheme)
                if (_viewModel.AppMode == AppMode.WhiteBoard)
                {
                    if (Settings.UsingWhiteboard)
                    {
                        isUselightThemeColor = false;
                    }
                    else
                    {
                        isUselightThemeColor = true;
                    }
                }

            if (_viewModel.AppMode == AppMode.Normal)
            {
                isUselightThemeColor = isDesktopUselightThemeColor;
                _viewModel.SelectedPenColor = lastDesktopInkColor;
            }
            else
            {
                _viewModel.SelectedPenColor = lastBoardInkColor;
            }

            double alpha = _viewModel.InkCanvasDrawingAttributes.Color.A;

            if (!_viewModel.InkCanvasDrawingAttributes.IsHighlighter)
            {
                if (_viewModel.SelectedPenColor == 0)
                {
                    // Black
                    _viewModel.InkCanvasDrawingAttributes.Color = Color.FromArgb((byte)alpha, 0, 0, 0);
                }
                else if (_viewModel.SelectedPenColor == 5)
                {
                    // White
                    _viewModel.InkCanvasDrawingAttributes.Color = Color.FromArgb((byte)alpha, 255, 255, 255);
                }
                else if (isUselightThemeColor)
                {
                    if (_viewModel.SelectedPenColor == 1)
                        // Red
                        _viewModel.InkCanvasDrawingAttributes.Color = Color.FromArgb((byte)alpha, 239, 68, 68);
                    else if (_viewModel.SelectedPenColor == 2)
                        // Green
                        _viewModel.InkCanvasDrawingAttributes.Color = Color.FromArgb((byte)alpha, 34, 197, 94);
                    else if (_viewModel.SelectedPenColor == 3)
                        // Blue
                        _viewModel.InkCanvasDrawingAttributes.Color = Color.FromArgb((byte)alpha, 59, 130, 246);
                    else if (_viewModel.SelectedPenColor == 4)
                        // Yellow
                        _viewModel.InkCanvasDrawingAttributes.Color = Color.FromArgb((byte)alpha, 250, 204, 21);
                    else if (_viewModel.SelectedPenColor == 6)
                        // Pink
                        _viewModel.InkCanvasDrawingAttributes.Color = Color.FromArgb((byte)alpha, 236, 72, 153);
                    else if (_viewModel.SelectedPenColor == 7)
                        // Teal (亮色)
                        _viewModel.InkCanvasDrawingAttributes.Color = Color.FromArgb((byte)alpha, 20, 184, 166);
                    else if (_viewModel.SelectedPenColor == 8)
                        // Orange (亮色)
                        _viewModel.InkCanvasDrawingAttributes.Color = Color.FromArgb((byte)alpha, 249, 115, 22);
                }
                else
                {
                    if (_viewModel.SelectedPenColor == 1)
                        // Red
                        _viewModel.InkCanvasDrawingAttributes.Color = Color.FromArgb((byte)alpha, 220, 38, 38);
                    else if (_viewModel.SelectedPenColor == 2)
                        // Green
                        _viewModel.InkCanvasDrawingAttributes.Color = Color.FromArgb((byte)alpha, 22, 163, 74);
                    else if (_viewModel.SelectedPenColor == 3)
                        // Blue
                        _viewModel.InkCanvasDrawingAttributes.Color = Color.FromArgb((byte)alpha, 37, 99, 235);
                    else if (_viewModel.SelectedPenColor == 4)
                        // Yellow
                        _viewModel.InkCanvasDrawingAttributes.Color = Color.FromArgb((byte)alpha, 234, 179, 8);
                    else if (_viewModel.SelectedPenColor == 6)
                        // Pink ( Purple )
                        _viewModel.InkCanvasDrawingAttributes.Color = Color.FromArgb((byte)alpha, 147, 51, 234);
                    else if (_viewModel.SelectedPenColor == 7)
                        // Teal (暗色)
                        _viewModel.InkCanvasDrawingAttributes.Color = Color.FromArgb((byte)alpha, 13, 148, 136);
                    else if (_viewModel.SelectedPenColor == 8)
                        // Orange (暗色)
                        _viewModel.InkCanvasDrawingAttributes.Color = Color.FromArgb((byte)alpha, 234, 88, 12);
                }
            }
            else
            {
                if (_viewModel.SelectedHighlighterColor == 100)
                    // Black
                    _viewModel.InkCanvasDrawingAttributes.Color = Color.FromRgb(0, 0, 0);
                else if (_viewModel.SelectedHighlighterColor == 101)
                    // White
                    _viewModel.InkCanvasDrawingAttributes.Color = Color.FromRgb(250, 250, 250);
                else if (_viewModel.SelectedHighlighterColor == 102)
                    // Red
                    _viewModel.InkCanvasDrawingAttributes.Color = Color.FromRgb(239, 68, 68);
                else if (_viewModel.SelectedHighlighterColor == 103)
                    // Yellow
                    _viewModel.InkCanvasDrawingAttributes.Color = Color.FromRgb(253, 224, 71);
                else if (_viewModel.SelectedHighlighterColor == 104)
                    // Green
                    _viewModel.InkCanvasDrawingAttributes.Color = Color.FromRgb(74, 222, 128);
                else if (_viewModel.SelectedHighlighterColor == 105)
                    // Zinc
                    _viewModel.InkCanvasDrawingAttributes.Color = Color.FromRgb(113, 113, 122);
                else if (_viewModel.SelectedHighlighterColor == 106)
                    // Blue
                    _viewModel.InkCanvasDrawingAttributes.Color = Color.FromRgb(59, 130, 246);
                else if (_viewModel.SelectedHighlighterColor == 107)
                    // Purple
                    _viewModel.InkCanvasDrawingAttributes.Color = Color.FromRgb(168, 85, 247);
                else if (_viewModel.SelectedHighlighterColor == 108)
                    // teal
                    _viewModel.InkCanvasDrawingAttributes.Color = Color.FromRgb(45, 212, 191);
                else if (_viewModel.SelectedHighlighterColor == 109)
                    // Orange
                    _viewModel.InkCanvasDrawingAttributes.Color = Color.FromRgb(249, 115, 22);
            }

            if (isUselightThemeColor)
            {
                // 亮系
                // 亮色的红色
                BorderPenColorRed.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                BoardBorderPenColorRed.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                // 亮色的绿色
                BorderPenColorGreen.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                BoardBorderPenColorGreen.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                // 亮色的蓝色
                BorderPenColorBlue.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                BoardBorderPenColorBlue.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                // 亮色的黄色
                BorderPenColorYellow.Background = new SolidColorBrush(Color.FromRgb(250, 204, 21));
                BoardBorderPenColorYellow.Background = new SolidColorBrush(Color.FromRgb(250, 204, 21));
                // 亮色的粉色
                BorderPenColorPink.Background = new SolidColorBrush(Color.FromRgb(236, 72, 153));
                BoardBorderPenColorPink.Background = new SolidColorBrush(Color.FromRgb(236, 72, 153));
                // 亮色的Teal
                BorderPenColorTeal.Background = new SolidColorBrush(Color.FromRgb(20, 184, 166));
                BoardBorderPenColorTeal.Background = new SolidColorBrush(Color.FromRgb(20, 184, 166));
                // 亮色的Orange
                BorderPenColorOrange.Background = new SolidColorBrush(Color.FromRgb(249, 115, 22));
                BoardBorderPenColorOrange.Background = new SolidColorBrush(Color.FromRgb(249, 115, 22));

                var newImageSource = new BitmapImage();
                newImageSource.BeginInit();
                newImageSource.UriSource = new Uri("/Resources/Icons-Fluent/ic_fluent_weather_moon_24_regular.png",
                    UriKind.RelativeOrAbsolute);
                newImageSource.EndInit();
                ColorThemeSwitchIcon.Source = newImageSource;
                BoardColorThemeSwitchIcon.Source = newImageSource;

                ColorThemeSwitchTextBlock.Text = "暗系";
                BoardColorThemeSwitchTextBlock.Text = "暗系";
            }
            else
            {
                // 暗系
                // 暗色的红色
                BorderPenColorRed.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                BoardBorderPenColorRed.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                // 暗色的绿色
                BorderPenColorGreen.Background = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                BoardBorderPenColorGreen.Background = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                // 暗色的蓝色
                BorderPenColorBlue.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                BoardBorderPenColorBlue.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                // 暗色的黄色
                BorderPenColorYellow.Background = new SolidColorBrush(Color.FromRgb(234, 179, 8));
                BoardBorderPenColorYellow.Background = new SolidColorBrush(Color.FromRgb(234, 179, 8));
                // 暗色的紫色对应亮色的粉色
                BorderPenColorPink.Background = new SolidColorBrush(Color.FromRgb(147, 51, 234));
                BoardBorderPenColorPink.Background = new SolidColorBrush(Color.FromRgb(147, 51, 234));
                // 暗色的Teal
                BorderPenColorTeal.Background = new SolidColorBrush(Color.FromRgb(13, 148, 136));
                BoardBorderPenColorTeal.Background = new SolidColorBrush(Color.FromRgb(13, 148, 136));
                // 暗色的Orange
                BorderPenColorOrange.Background = new SolidColorBrush(Color.FromRgb(234, 88, 12));
                BoardBorderPenColorOrange.Background = new SolidColorBrush(Color.FromRgb(234, 88, 12));

                var newImageSource = new BitmapImage();
                newImageSource.BeginInit();
                newImageSource.UriSource = new Uri("/Resources/Icons-Fluent/ic_fluent_weather_sunny_24_regular.png",
                    UriKind.RelativeOrAbsolute);
                newImageSource.EndInit();
                ColorThemeSwitchIcon.Source = newImageSource;
                BoardColorThemeSwitchIcon.Source = newImageSource;

                ColorThemeSwitchTextBlock.Text = "亮系";
                BoardColorThemeSwitchTextBlock.Text = "亮系";
            }
        }

        private void CheckLastColor(int color, bool isHighlighter = false)
        {
            if (isHighlighter)
            {
                _viewModel.SelectedHighlighterColor = color;
            }
            else
            {
                if (_viewModel.AppMode == AppMode.Normal) lastDesktopInkColor = color;
                else lastBoardInkColor = color;
            }
        }

        private void SwitchToDefaultPen(object? sender, RoutedEventArgs? e)
        {
            SetPenType(false);
            CheckColorTheme();
        }

        private void SwitchToHighlighterPen(object sender, RoutedEventArgs e)
        {
            SetPenType(true);
            CheckColorTheme();
        }

        private void SetPenType(bool isHighlighter)
        {
            var attributes = _viewModel.InkCanvasDrawingAttributes;
            attributes.IsHighlighter = isHighlighter;
            attributes.Width = isHighlighter ? Settings.HighlighterWidth / 2 : Settings.InkWidth;
            attributes.Height = isHighlighter ? Settings.HighlighterWidth : Settings.InkWidth;
            attributes.StylusTip = isHighlighter ? StylusTip.Rectangle : StylusTip.Ellipse;
        }

        private void PenColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { CommandParameter: string value } && int.TryParse(value, out var color))
                SelectPenColor(color);
        }

        private void SelectPenColor(int color)
        {
            if (color is >= 100 and <= 109)
            {
                CheckLastColor(color, true);
                SetPenType(true);
            }
            else if (color is >= 0 and <= 8)
            {
                CheckLastColor(color);
                if (_viewModel.InkCanvasDrawingAttributes.IsHighlighter)
                    SetPenType(false);
            }
            else
            {
                return;
            }

            ColorSwitchCheck();
        }

        private Color StringToColor(string colorStr)
        {
            var argb = new byte[4];
            for (var i = 0; i < 4; i++)
            {
                var charArray = colorStr.Substring(i * 2 + 1, 2).ToCharArray();
                var b1 = toByte(charArray[0]);
                var b2 = toByte(charArray[1]);
                argb[i] = (byte)(b2 | (b1 << 4));
            }

            return Color.FromArgb(argb[0], argb[1], argb[2], argb[3]); //#FFFFFFFF
        }

        private static byte toByte(char c)
        {
            var b = (byte)"0123456789ABCDEF".IndexOf(c);
            return b;
        }
        #endregion

        #region FloatingBarIcons
        #region “手勢”按鈕

        /// <summary>
        /// 用於浮動工具欄的“手勢”按鈕和白板工具欄的“手勢”按鈕的點擊事件
        /// </summary>
        private void TwoFingerGestureBorder_Click(object sender, RoutedEventArgs e)
        {
            ToggleToolbarPanels(TwoFingerGestureBorder, BoardTwoFingerGestureBorder);
        }

        /// <summary>
        /// 用於更新浮動工具欄的“手勢”按鈕和白板工具欄的“手勢”按鈕的樣式（開啟和關閉狀態）
        /// </summary>
        private void CheckEnableTwoFingerGestureBtnColorPrompt()
        {
            if (ToggleSwitchEnableMultiTouchMode.IsOn)
            {
                TwoFingerGestureSimpleStackPanel.Opacity = 0.5;
                TwoFingerGestureSimpleStackPanel.IsHitTestVisible = false;
                EnableTwoFingerGestureBtn.IconSource =
                    new BitmapImage(new Uri("/Resources/new-icons/gesture.png", UriKind.Relative));

                BoardGesture.Background = new SolidColorBrush(Color.FromRgb(244, 244, 245));
                BoardGestureGeometry.Brush = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                BoardGestureGeometry2.Brush = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                BoardGestureLabel.Foreground = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                BoardGesture.BorderBrush = new SolidColorBrush(Color.FromRgb(161, 161, 170));
                BoardGestureGeometry.Geometry = Geometry.Parse(XamlGraphicsIconGeometries.DisabledGestureIcon);
                BoardGestureGeometry2.Geometry = Geometry.Parse("F0 M24,24z M0,0z");
            }
            else
            {
                TwoFingerGestureSimpleStackPanel.Opacity = 1;
                TwoFingerGestureSimpleStackPanel.IsHitTestVisible = true;
                if (Settings.IsEnableTwoFingerGesture)
                {
                    EnableTwoFingerGestureBtn.IconSource =
                        new BitmapImage(new Uri("/Resources/new-icons/gesture-enabled.png", UriKind.Relative));

                    BoardGesture.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                    BoardGestureGeometry.Brush = new SolidColorBrush(Colors.GhostWhite);
                    BoardGestureGeometry2.Brush = new SolidColorBrush(Colors.GhostWhite);
                    BoardGestureLabel.Foreground = new SolidColorBrush(Colors.GhostWhite);
                    BoardGesture.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                    BoardGestureGeometry.Geometry = Geometry.Parse(XamlGraphicsIconGeometries.EnabledGestureIcon);
                    BoardGestureGeometry2.Geometry = Geometry.Parse("F0 M24,24z M0,0z " + XamlGraphicsIconGeometries.EnabledGestureIconBadgeCheck);
                }
                else
                {
                    EnableTwoFingerGestureBtn.IconSource =
                        new BitmapImage(new Uri("/Resources/new-icons/gesture.png", UriKind.Relative));

                    BoardGesture.Background = new SolidColorBrush(Color.FromRgb(244, 244, 245));
                    BoardGestureGeometry.Brush = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                    BoardGestureGeometry2.Brush = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                    BoardGestureLabel.Foreground = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                    BoardGesture.BorderBrush = new SolidColorBrush(Color.FromRgb(161, 161, 170));
                    BoardGestureGeometry.Geometry = Geometry.Parse(XamlGraphicsIconGeometries.DisabledGestureIcon);
                    BoardGestureGeometry2.Geometry = Geometry.Parse("F0 M24,24z M0,0z");
                }
            }
        }

        /// <summary>
        /// 控制是否顯示浮動工具欄的“手勢”按鈕
        /// </summary>
        private void CheckEnableTwoFingerGestureBtnVisibility(bool isVisible)
        {
            if (StackPanelCanvasControls.Visibility != Visibility.Visible
                || BorderFloatingBarMainControls.Visibility != Visibility.Visible)
            {
                EnableTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
            }
            else if (isVisible == true)
            {
                if (_powerPointService.IsInSlideShow)
                    EnableTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
                else EnableTwoFingerGestureBorder.Visibility = Visibility.Visible;
            }
            else
            {
                EnableTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
            }
        }

        #endregion “手勢”按鈕

        #region 浮動工具欄的拖動實現

        private bool isDragDropInEffect = false;
        private Point pos = new();
        private Point downPos = new();
        private Point pointDesktop = new(-1, -1); //用于记录上次在桌面时的坐标
        private Point pointPPT = new(-1, -1); //用于记录上次在PPT中的坐标

        private void SymbolIconEmoji_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragDropInEffect)
            {
                var xPos = e.GetPosition(null).X - pos.X + ViewboxFloatingBar.Margin.Left;
                var yPos = e.GetPosition(null).Y - pos.Y + ViewboxFloatingBar.Margin.Top;
                ViewboxFloatingBar.Margin = new Thickness(xPos, yPos, -2000, -200);

                pos = e.GetPosition(null);
                if (_powerPointService.IsInSlideShow)
                    pointPPT = new Point(xPos, yPos);
                else
                    pointDesktop = new Point(xPos, yPos);
            }
        }

        private void SymbolIconEmoji_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isViewboxFloatingBarMarginAnimationRunning)
            {
                ViewboxFloatingBar.BeginAnimation(MarginProperty, null);
                isViewboxFloatingBarMarginAnimationRunning = false;
            }

            isDragDropInEffect = true;
            pos = e.GetPosition(null);
            downPos = e.GetPosition(null);
            GridForFloatingBarDraging.Visibility = Visibility.Visible;
        }

        private void SymbolIconEmoji_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragDropInEffect = false;

            if (e is null || (Math.Abs(downPos.X - e.GetPosition(null).X) <= 10 &&
                              Math.Abs(downPos.Y - e.GetPosition(null).Y) <= 10))
            {
                if (BorderFloatingBarMainControls.Visibility == Visibility.Visible)
                {
                    BorderFloatingBarMainControls.Visibility = Visibility.Collapsed;
                    CheckEnableTwoFingerGestureBtnVisibility(false);
                }
                else
                {
                    BorderFloatingBarMainControls.Visibility = Visibility.Visible;
                    CheckEnableTwoFingerGestureBtnVisibility(true);
                }
            }

            GridForFloatingBarDraging.Visibility = Visibility.Collapsed;
        }

        #endregion 浮動工具欄的拖動實現

        #region 子面板和浮动栏位置

        private UIElement[] ToolbarSubPanels => new UIElement[]
        {
            BorderTools,
            BoardBorderTools,
            PenPalette,
            BoardPenPalette,
            BoardEraserSizePanel,
            EraserSizePanel,
            TwoFingerGestureBorder,
            BoardTwoFingerGestureBorder,
        };

        private void ToggleToolbarPanels(UIElement primaryPanel, UIElement companionPanel)
        {
            var show = primaryPanel.Visibility != Visibility.Visible;
            foreach (var panel in ToolbarSubPanels)
            {
                if (show && (panel == primaryPanel || panel == companionPanel))
                    AnimationsHelper.ShowWithSlideFromBottomAndFade(panel);
                else
                    AnimationsHelper.HideWithSlideAndFade(panel);
            }
        }

        private void HideAllSubPanels(bool immediately = false)
        {
            var panels = ToolbarSubPanels.Concat(new UIElement[]
            {
                BoardBorderLeftPageListView,
                BoardBorderRightPageListView,
            });
            foreach (var panel in panels)
            {
                if (immediately)
                    panel.Visibility = Visibility.Collapsed;
                else
                    AnimationsHelper.HideWithSlideAndFade(panel);
            }
        }

        private void HideSubPanelsImmediately()
        {
            HideAllSubPanels(immediately: true);
        }

        private async void HideSubPanels(bool autoAlignCenter = false)
        {
            HideAllSubPanels();

            if (autoAlignCenter)
                await CenterFloatingBarAsync();

            await Task.Delay(150);
            isHidingSubPanelsWhenInking = false;
        }

        private async Task CenterFloatingBarAsync()
        {
            var useTaskbarHeight = !_powerPointService.IsInSlideShow && _viewModel.AppMode == AppMode.Normal;
            await Task.Delay(50);
            ViewboxFloatingBarMarginAnimation(useTaskbarHeight ? 100 : 60, useTaskbarHeight);
        }

        #endregion

        #region 撤銷重做按鈕
        private void SymbolIconUndo_Click(object? sender, RoutedEventArgs? e)
        {
            if (!_viewModel.CanUndo)
                return;

            //BtnUndo_Click的内容
            if (inkCanvas.GetSelectedStrokes().Count != 0)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                inkCanvas.Select(new StrokeCollection());
            }

            var item = timeMachine.Undo();
            ApplyHistoryToCanvas(item);

            HideSubPanels();
        }

        private void SymbolIconRedo_Click(object? sender, RoutedEventArgs? e)
        {
            if (!_viewModel.CanRedo)
                return;

            //BtnRedo_Click的内容
            if (inkCanvas.GetSelectedStrokes().Count != 0)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                inkCanvas.Select(new StrokeCollection());
            }

            var item = timeMachine.Redo();
            ApplyHistoryToCanvas(item);

            HideSubPanels();
        }

        #endregion

        #region 白板按鈕和退出白板模式按鈕

        private async void OpenWhiteboardFloatingBarButton_Click(object? sender, RoutedEventArgs? e)
        {
            OpenWhiteboard();
        }

        /// <summary>
        /// 打开白板模式
        /// </summary>
        private void OpenWhiteboard()
        {
            // 如果画布当前是透明的（游标模式），先显示画布
            if (GridTransparencyFakeBackground.Background == null)
            {
                ShowInkCanvas();
            }

            // 动画调整浮动工具栏位置
            new Thread(() =>
            {
                Thread.Sleep(100);
                Application.Current.Dispatcher.Invoke(() => ViewboxFloatingBarMarginAnimation(60));
            }).Start();

            HideSubPanels();

            // 自动关闭多指书写、开启双指移动
            if (Settings.AutoSwitchTwoFingerGesture)
            {
                ToggleSwitchEnableTwoFingerTranslate.IsOn = true;
                if (isInMultiTouchMode)
                    ToggleSwitchEnableMultiTouchMode.IsOn = false;
            }

            // 切换到白板模式
            SwitchToWhiteboardMode();

            SwitchToDefaultPen(null, null);
            CheckColorTheme(true);
        }

        /// <summary>
        /// 关闭白板模式
        /// </summary>
        private void CloseWhiteboard()
        {
            HideSubPanelsImmediately();

            // 动画调整浮动工具栏位置
            var targetMargin = _powerPointService.IsInSlideShow ? 60 : 100;
            var useTaskbarHeight = !_powerPointService.IsInSlideShow;

            new Thread(() =>
            {
                Thread.Sleep(300);
                Application.Current.Dispatcher.Invoke(() =>
                    ViewboxFloatingBarMarginAnimation(targetMargin, useTaskbarHeight));
            }).Start();

            // 自动启用多指书写
            if (Settings.AutoSwitchTwoFingerGesture)
            {
                ToggleSwitchEnableTwoFingerTranslate.IsOn = false;
            }

            // 切换回屏幕模式
            SwitchToScreenMode();

            CursorFloatingBarButton_Click(null, null);

            SwitchToDefaultPen(null, null);
            CheckColorTheme(true);
        }

        /// <summary>
        /// 切换到白板模式（显示黑板/白板UI）
        /// </summary>
        private void SwitchToWhiteboardMode()
        {
            _viewModel.AppMode = AppMode.WhiteBoard;

            inkCanvas.Select(new StrokeCollection());

            SaveStrokes(true);
            ClearStrokes(true);
            RestoreStrokes();

            // 根据设置选择黑板或白板颜色
            if (Settings.UsingWhiteboard)
                SelectPenColor(0);
            else
                SelectPenColor(5);
        }

        /// <summary>
        /// 切换到屏幕模式（隐藏黑板/白板UI）
        /// </summary>
        private void SwitchToScreenMode()
        {
            _viewModel.AppMode = AppMode.Normal;

            inkCanvas.Select(new StrokeCollection());

            SaveStrokes();
            ClearStrokes(true);
            RestoreStrokes(true);
        }

        /// <summary>
        /// 显示墨迹画布（从透明游标模式切换到可见画布）
        /// </summary>
        private void ShowInkCanvas()
        {
            GridTransparencyFakeBackground.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
            inkCanvas.IsHitTestVisible = true;
            inkCanvas.Visibility = Visibility.Visible;
            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region 清空畫布按鈕

        private void SymbolIconDelete_Click(object? sender, RoutedEventArgs? e)
        {
            if (inkCanvas.GetSelectedStrokes().Count > 0)
            {
                inkCanvas.Strokes.Remove(inkCanvas.GetSelectedStrokes());
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            }
            else if (inkCanvas.Strokes.Count > 0)
            {
                if (Settings.IsAutoSaveStrokesAtClear &&
                    inkCanvas.Strokes.Count > Settings.MinimumAutomationStrokeNumber)
                {
                    SaveScreenShot(true);
                }

                BtnClear_Click(null, null);
            }
        }

        #endregion

        #region 主要的工具按鈕事件

        private void SymbolIconSelect_Click(object sender, RoutedEventArgs e)
        {
            var wasSelected = _viewModel.AppPenMode == InkCanvasEditingMode.Select;
            _viewModel.AppPenMode = InkCanvasEditingMode.Select;
            inkCanvas.IsManipulationEnabled = false;
            inkCanvas.EditingMode = InkCanvasEditingMode.Select;
            if (wasSelected)
            {
                if (inkCanvas.GetSelectedStrokes().Count == inkCanvas.Strokes.Count)
                {
                    inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    inkCanvas.EditingMode = InkCanvasEditingMode.Select;
                }
                else
                {
                    var selectedStrokes = new StrokeCollection();
                    foreach (var stroke in inkCanvas.Strokes)
                        if (stroke.GetBounds().Width > 0 && stroke.GetBounds().Height > 0)
                            selectedStrokes.Add(stroke);
                    inkCanvas.Select(selectedStrokes);
                }
            }

            HideSubPanels();
        }

        #endregion

        private void ImageCountdownTimer_Click(object sender, RoutedEventArgs e)
        {
            HideToolsPanel();

            new CountdownTimerWindow().Show();
        }

        private void SymbolIconRand_Click(object sender, RoutedEventArgs e)
        {
            HideToolsPanel();

            App.GetService<RandWindow>().Show();
        }

        private void SymbolIconRandOne_Click(object sender, RoutedEventArgs e)
        {
            HideToolsPanel();

            var randWindow = App.GetService<RandWindow>();
            randWindow.IsAutoClose = true;
            randWindow.ShowDialog();
        }

        private void SymbolIconSaveStrokes_Click(object sender, RoutedEventArgs e)
        {
            HideToolsPanel();

            GridNotifications.Visibility = Visibility.Collapsed;

            SaveInkCanvasStrokes(true, true);
        }

        private void SymbolIconOpenStrokes_Click(object sender, RoutedEventArgs e)
        {
            HideToolsPanel();

            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Path.GetFullPath(CommonDirectories.AppSavesRootFolderPath),
                Title = "打开墨迹文件",
                Filter = "Ink Canvas Strokes File (*.icstk)|*.icstk"
            };
            if (openFileDialog.ShowDialog() != true) return;
            Logger.LogInformation("用户选择打开墨迹文件 {FileName}", openFileDialog.FileName);
            try
            {
                var fileStreamHasNoStroke = false;
                using (var fs = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read))
                {
                    var strokes = new StrokeCollection(fs);
                    fileStreamHasNoStroke = strokes.Count == 0;
                    if (!fileStreamHasNoStroke)
                    {
                        ClearStrokes(true);
                        timeMachine.ClearStrokeHistory();
                        inkCanvas.Strokes.Add(strokes);
                        Logger.LogInformation("墨迹文件打开成功，墨迹数 {Count}", strokes.Count);
                    }
                }

                if (fileStreamHasNoStroke)
                    using (var ms = new MemoryStream(File.ReadAllBytes(openFileDialog.FileName)))
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        var strokes = new StrokeCollection(ms);
                        ClearStrokes(true);
                        timeMachine.ClearStrokeHistory();
                        inkCanvas.Strokes.Add(strokes);
                        Logger.LogInformation("墨迹文件打开成功，墨迹数 {Count}", strokes.Count);
                    }
            }
            catch
            {
                ShowNotification("墨迹打开失败");
            }
        }

        private async void SymbolIconScreenshot_Click(object sender, RoutedEventArgs e)
        {
            HideSubPanelsImmediately();
            await Task.Delay(50);
            SaveScreenShotToDesktop();
        }



        private void ToolsFloatingBarButton_Click(object? sender, RoutedEventArgs? e)
        {
            ToggleToolbarPanels(BorderTools, BoardBorderTools);
        }

        private bool isViewboxFloatingBarMarginAnimationRunning = false;

        public async void ViewboxFloatingBarMarginAnimation(int MarginFromEdge,
            bool PosXCaculatedWithTaskbarHeight = false)
        {
            if (MarginFromEdge == 60) MarginFromEdge = 55;
            await Dispatcher.InvokeAsync(() =>
            {
                if (_viewModel.AppMode == AppMode.WhiteBoard)
                    MarginFromEdge = -60;
                else
                    ViewboxFloatingBar.Visibility = Visibility.Visible;
                isViewboxFloatingBarMarginAnimationRunning = true;

                double dpiScaleX = 1, dpiScaleY = 1;
                var source = PresentationSource.FromVisual(this);
                if (source != null)
                {
                    dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                    dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                }

                var windowHandle = new WindowInteropHelper(this).Handle;
                var screen = System.Windows.Forms.Screen.FromHandle(windowHandle);
                double screenWidth = screen.Bounds.Width / dpiScaleX, screenHeight = screen.Bounds.Height / dpiScaleY;
                var toolbarHeight = SystemParameters.PrimaryScreenHeight - SystemParameters.FullPrimaryScreenHeight -
                                    SystemParameters.WindowCaptionHeight;
                pos.X = (screenWidth - ViewboxFloatingBar.ActualWidth * ViewboxFloatingBarScaleTransform.ScaleX) / 2;

                if (PosXCaculatedWithTaskbarHeight == false)
                    pos.Y = screenHeight - MarginFromEdge * ViewboxFloatingBarScaleTransform.ScaleY;
                else if (PosXCaculatedWithTaskbarHeight == true)
                    pos.Y = screenHeight - ViewboxFloatingBar.ActualHeight * ViewboxFloatingBarScaleTransform.ScaleY -
                            toolbarHeight - ViewboxFloatingBarScaleTransform.ScaleY * 3;

                if (MarginFromEdge != -60)
                {
                    if (_powerPointService.IsInSlideShow)
                    {
                        if (pointPPT.X != -1 || pointPPT.Y != -1)
                        {
                            if (Math.Abs(pointPPT.Y - pos.Y) > 50)
                                pos = pointPPT;
                            else
                                pointPPT = pos;
                        }
                    }
                    else
                    {
                        if (pointDesktop.X != -1 || pointDesktop.Y != -1)
                        {
                            if (Math.Abs(pointDesktop.Y - pos.Y) > 50)
                                pos = pointDesktop;
                            else
                                pointDesktop = pos;
                        }
                    }
                }

                var marginAnimation = new ThicknessAnimation
                {
                    Duration = TimeSpan.FromSeconds(0.35),
                    From = ViewboxFloatingBar.Margin,
                    To = new Thickness(pos.X, pos.Y, 0, -20)
                };
                marginAnimation.EasingFunction = new CircleEase();
                ViewboxFloatingBar.BeginAnimation(MarginProperty, marginAnimation);
            });

            await Task.Delay(200);

            await Dispatcher.InvokeAsync(() =>
            {
                ViewboxFloatingBar.Margin = new Thickness(pos.X, pos.Y, -2000, -200);
                if (_viewModel.AppMode == AppMode.WhiteBoard) ViewboxFloatingBar.Visibility = Visibility.Hidden;
            });
        }

        public async void PureViewboxFloatingBarMarginAnimationInDesktopMode()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                ViewboxFloatingBar.Visibility = Visibility.Visible;
                isViewboxFloatingBarMarginAnimationRunning = true;

                double dpiScaleX = 1, dpiScaleY = 1;
                var source = PresentationSource.FromVisual(this);
                if (source != null)
                {
                    dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                    dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                }

                var windowHandle = new WindowInteropHelper(this).Handle;
                var screen = System.Windows.Forms.Screen.FromHandle(windowHandle);
                double screenWidth = screen.Bounds.Width / dpiScaleX, screenHeight = screen.Bounds.Height / dpiScaleY;
                var toolbarHeight = SystemParameters.PrimaryScreenHeight - SystemParameters.FullPrimaryScreenHeight -
                                    SystemParameters.WindowCaptionHeight;
                pos.X = (screenWidth - ViewboxFloatingBar.ActualWidth * ViewboxFloatingBarScaleTransform.ScaleX) / 2;

                pos.Y = screenHeight - ViewboxFloatingBar.ActualHeight * ViewboxFloatingBarScaleTransform.ScaleY -
                        toolbarHeight - ViewboxFloatingBarScaleTransform.ScaleY * 3;

                if (pointDesktop.X != -1 || pointDesktop.Y != -1) pointDesktop = pos;

                var marginAnimation = new ThicknessAnimation
                {
                    Duration = TimeSpan.FromSeconds(0.35),
                    From = ViewboxFloatingBar.Margin,
                    To = new Thickness(pos.X, pos.Y, 0, -20)
                };
                marginAnimation.EasingFunction = new CircleEase();
                ViewboxFloatingBar.BeginAnimation(MarginProperty, marginAnimation);
            });

            await Task.Delay(349);

            await Dispatcher.InvokeAsync(() =>
            {
                ViewboxFloatingBar.Margin = new Thickness(pos.X, pos.Y, -2000, -200);
            });
        }

        public async void PureViewboxFloatingBarMarginAnimationInPPTMode()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                ViewboxFloatingBar.Visibility = Visibility.Visible;
                isViewboxFloatingBarMarginAnimationRunning = true;

                double dpiScaleX = 1, dpiScaleY = 1;
                var source = PresentationSource.FromVisual(this);
                if (source != null)
                {
                    dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                    dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                }

                var windowHandle = new WindowInteropHelper(this).Handle;
                var screen = System.Windows.Forms.Screen.FromHandle(windowHandle);
                double screenWidth = screen.Bounds.Width / dpiScaleX, screenHeight = screen.Bounds.Height / dpiScaleY;
                var toolbarHeight = SystemParameters.PrimaryScreenHeight - SystemParameters.FullPrimaryScreenHeight -
                                    SystemParameters.WindowCaptionHeight;
                pos.X = (screenWidth - ViewboxFloatingBar.ActualWidth * ViewboxFloatingBarScaleTransform.ScaleX) / 2;

                pos.Y = screenHeight - 55 * ViewboxFloatingBarScaleTransform.ScaleY;

                if (pointPPT.X != -1 || pointPPT.Y != -1)
                {
                    pointPPT = pos;
                }

                var marginAnimation = new ThicknessAnimation
                {
                    Duration = TimeSpan.FromSeconds(0.35),
                    From = ViewboxFloatingBar.Margin,
                    To = new Thickness(pos.X, pos.Y, 0, -20)
                };
                marginAnimation.EasingFunction = new CircleEase();
                ViewboxFloatingBar.BeginAnimation(MarginProperty, marginAnimation);
            });

            await Task.Delay(349);

            await Dispatcher.InvokeAsync(() =>
            {
                ViewboxFloatingBar.Margin = new Thickness(pos.X, pos.Y, -2000, -200);
            });
        }

        private void CursorFloatingBarButton_Click(object? sender, RoutedEventArgs? e)
        {
            _viewModel.AppPenMode = InkCanvasEditingMode.None;
            // 切换前自动截图保存墨迹
            if (inkCanvas.Strokes.Count > 0 &&
                inkCanvas.Strokes.Count > Settings.MinimumAutomationStrokeNumber)
            {
                SaveScreenShot(true);
            }
            if (Settings.HideStrokeWhenSelecting)
            {
                inkCanvas.Visibility = Visibility.Collapsed;
            }
            else
            {
                inkCanvas.IsHitTestVisible = false;
                inkCanvas.Visibility = Visibility.Visible;
            }

            GridTransparencyFakeBackground.Background = null;

            // 取消选中的墨迹
            inkCanvas.Select(new StrokeCollection());
            inkCanvas.EditingMode = InkCanvasEditingMode.None;

            //GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

            //if (_viewModel.AppMode == AppMode.WhiteBoard)
            //{
            //    SaveStrokes();
            //    RestoreStrokes(true);
            //}

            CheckEnableTwoFingerGestureBtnVisibility(false);

            StackPanelCanvasControls.Visibility = Visibility.Collapsed;

            if (_viewModel.IsFloatingBarVisible)
            {
                HideSubPanels(autoAlignCenter: true);

                if (_powerPointService.IsInSlideShow)
                    ViewboxFloatingBarMarginAnimation(60);
                else
                    ViewboxFloatingBarMarginAnimation(100, true);
            }
        }

        private void PenIcon_Click(object? sender, RoutedEventArgs? e)
        {
            if (_viewModel.AppPenMode == InkCanvasEditingMode.Ink &&
                StackPanelCanvasControls.Visibility == Visibility.Visible)
            {
                ToggleToolbarPanels(PenPalette, BoardPenPalette);
                return;
            }

            ActivatePen();
        }

        private void ActivatePen()
        {
            _viewModel.AppPenMode = InkCanvasEditingMode.Ink;
            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            ShowInkCanvas();
            StackPanelCanvasControls.Visibility = Visibility.Visible;
            CheckEnableTwoFingerGestureBtnVisibility(true);
            ColorSwitchCheck();
            HideSubPanels(autoAlignCenter: true);
        }

        private void ColorThemeSwitch_Click(object sender, RoutedEventArgs e)
        {
            isUselightThemeColor = !isUselightThemeColor;
            if (_viewModel.AppMode == AppMode.Normal) isDesktopUselightThemeColor = isUselightThemeColor;
            CheckColorTheme();
        }

        private void EraserIcon_Click(object sender, RoutedEventArgs e)
        {
            var wasSelected = _viewModel.AppPenMode == InkCanvasEditingMode.EraseByPoint;
            _viewModel.AppPenMode = InkCanvasEditingMode.EraseByPoint;
            UpdateEraserShape();

            if (wasSelected)
            {
                ToggleToolbarPanels(EraserSizePanel, BoardEraserSizePanel);
            }
            else
            {
                HideSubPanels();
            }

            inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;

            inkCanvas_EditingModeChanged(inkCanvas, null);
            CancelSingleFingerDragMode();
        }

        private void EraserIconByStrokes_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.AppPenMode = InkCanvasEditingMode.EraseByStroke;
            inkCanvas.EraserShape = new EllipseStylusShape(5, 5);
            inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;

            inkCanvas_EditingModeChanged(inkCanvas, null);
            CancelSingleFingerDragMode();

            HideSubPanels();
        }

        private void ClearAndMouseFloatingbarButton_Click(object? sender, RoutedEventArgs? e)
        {
            SymbolIconDelete_Click(sender, null);
            CursorFloatingBarButton_Click(null, null);
        }

        private void CloseBordertools_Click(object sender, RoutedEventArgs e)
        {
            HideSubPanels();
        }

        #region Right Side Panel

        public static bool CloseIsFromButton = false;

        public void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            CloseIsFromButton = true;
            Application.Current.Shutdown();
        }

        public void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(System.Windows.Forms.Application.ExecutablePath, "-m");

            CloseIsFromButton = true;
            Application.Current.Shutdown();
        }

        private void SettingsOverlayClick(object sender, MouseButtonEventArgs e)
        {
            _viewModel.IsSettingsPanelVisible = false;
        }

        private void CloseSettingsPanelButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.IsSettingsPanelVisible = false;
        }

        private bool ForceEraser => inkCanvas.EditingMode is InkCanvasEditingMode.EraseByPoint
                or InkCanvasEditingMode.EraseByStroke
                or InkCanvasEditingMode.Select;

        private void BtnClear_Click(object? sender, RoutedEventArgs? e)
        {
            // 批注或白板模式下先回到画笔再清屏，鼠标模式保持穿透。
            if (_viewModel.AppPenMode != InkCanvasEditingMode.Ink &&
                (_viewModel.AppMode == AppMode.WhiteBoard || _viewModel.AppPenMode != InkCanvasEditingMode.None))
                ActivatePen();

            if (inkCanvas.Strokes.Count != 0)
            {
                var whiteboardIndex = _viewModel.WhiteboardCurrentPage;
                if (_viewModel.AppMode == AppMode.Normal) whiteboardIndex = 0;
                strokeCollections[whiteboardIndex] = inkCanvas.Strokes.Clone();
            }

            ClearStrokes(false);
            inkPreviewOverlay.Children.Clear();

            CancelSingleFingerDragMode();

            if (Settings.ClearCanvasAndClearTimeMachine) timeMachine.ClearStrokeHistory();
        }

        private void CancelSingleFingerDragMode()
        {
            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

            //isSingleFingerDragMode = false;
        }

        private int BoundsWidth = 5;

        #endregion
        #endregion

        #region Hotkeys
        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!_powerPointService.IsInSlideShow || _viewModel.AppMode == AppMode.WhiteBoard)
                return;
            if (e.Delta >= 120)
                HandlePPTPreviousPage();
            else if (e.Delta <= -120)
                HandlePPTNextPage();
        }

        private void Main_Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_powerPointService.IsInSlideShow || _viewModel.AppMode == AppMode.WhiteBoard)
                return;
            if (e.Key == Key.Down || e.Key == Key.PageDown || e.Key == Key.Right || e.Key == Key.N || e.Key == Key.Space)
                HandlePPTNextPage();
            if (e.Key == Key.Up || e.Key == Key.PageUp || e.Key == Key.Left || e.Key == Key.P)
                HandlePPTPreviousPage();
            if (e.Key == Key.Escape)
                if (_powerPointService.IsInSlideShow)
                    _powerPointService.EndSlideShow();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                if (_powerPointService.IsInSlideShow)
                    _powerPointService.EndSlideShow();
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void HotKey_Undo(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                SymbolIconUndo_Click(sender, e);
            }
            catch { }
        }

        private void HotKey_Redo(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                SymbolIconRedo_Click(sender, e);
            }
            catch { }
        }

        private void KeyExit(object sender, ExecutedRoutedEventArgs e)
        {
            if (_powerPointService.IsInSlideShow)
                _powerPointService.EndSlideShow();
        }

        #endregion

        #region Notification
        private CancellationTokenSource? _notificationCts;

        private async void OnNotificationRequested(NotificationEventArgs args)
        {
            // 取消之前的通知任务
            _notificationCts?.Cancel();
            _notificationCts = new CancellationTokenSource();
            var token = _notificationCts.Token;

            TextBlockNotice.Text = args.Message;
            AnimationsHelper.ShowWithSlideFromBottomAndFade(GridNotifications);

            try
            {
                await Task.Delay(args.DurationMs + 300, token);
                AnimationsHelper.HideWithSlideAndFade(GridNotifications);
            }
            catch (TaskCanceledException)
            {
                // 被新通知取消，不执行隐藏操作
            }
        }

        private void ShowNotification(string notice)
        {
            _notificationService.ShowNotification(notice);
        }
        #endregion

        #region PageListView
        private class PageListViewItem
        {
            public int Index { get; set; }
            public StrokeCollection? Strokes { get; set; }
        }

        ObservableCollection<PageListViewItem> blackBoardSidePageListViewObservableCollection = new ObservableCollection<PageListViewItem>();

        /// <summary>
        /// <para>刷新白板的缩略图页面列表。</para>
        /// </summary>
        private void RefreshBlackBoardSidePageListView()
        {
            if (blackBoardSidePageListViewObservableCollection.Count == _viewModel.WhiteboardTotalPageCount)
            {
                foreach (int index in Enumerable.Range(1, _viewModel.WhiteboardTotalPageCount))
                {
                    var st = ApplyHistoriesToNewStrokeCollection(TimeMachineHistories[index]);
                    st.Clip(new Rect(0, 0, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight));
                    var pitem = new PageListViewItem()
                    {
                        Index = index,
                        Strokes = st,
                    };
                    blackBoardSidePageListViewObservableCollection[index - 1] = pitem;
                }
            }
            else
            {
                blackBoardSidePageListViewObservableCollection.Clear();
                foreach (int index in Enumerable.Range(1, _viewModel.WhiteboardTotalPageCount))
                {
                    var st = ApplyHistoriesToNewStrokeCollection(TimeMachineHistories[index]);
                    st.Clip(new Rect(0, 0, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight));
                    var pitem = new PageListViewItem()
                    {
                        Index = index,
                        Strokes = st,
                    };
                    blackBoardSidePageListViewObservableCollection.Add(pitem);
                }
            }

            var _st = inkCanvas.Strokes.Clone();
            _st.Clip(new Rect(0, 0, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight));
            var _pitem = new PageListViewItem()
            {
                Index = _viewModel.WhiteboardCurrentPage,
                Strokes = _st,
            };
            blackBoardSidePageListViewObservableCollection[_viewModel.WhiteboardCurrentPage - 1] = _pitem;

            BlackBoardLeftSidePageListView.SelectedIndex = _viewModel.WhiteboardCurrentPage - 1;
            BlackBoardRightSidePageListView.SelectedIndex = _viewModel.WhiteboardCurrentPage - 1;
        }

        public static void ScrollViewToVerticalTop(FrameworkElement element, ScrollViewer scrollViewer)
        {
            var scrollViewerOffset = scrollViewer.VerticalOffset;
            var point = new Point(0, scrollViewerOffset);
            var tarPos = element.TransformToVisual(scrollViewer).Transform(point);
            scrollViewer.ScrollToVerticalOffset(tarPos.Y);
        }


        private void WhiteboardPage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { CommandParameter: PageListViewItem page }) return;
            if (page.Index < 1 || page.Index > _viewModel.WhiteboardTotalPageCount) return;

            AnimationsHelper.HideWithSlideAndFade(BoardBorderLeftPageListView);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderRightPageListView);
            if (page.Index == _viewModel.WhiteboardCurrentPage) return;

            SaveStrokes();
            ClearStrokes(true);
            _viewModel.WhiteboardCurrentPage = page.Index;
            RestoreStrokes();
            BlackBoardLeftSidePageListView.SelectedIndex = page.Index - 1;
            BlackBoardRightSidePageListView.SelectedIndex = page.Index - 1;
        }
        #endregion

        #region PPT
        private bool isEnteredSlideShowEndEvent = false;
        private int _previousSlideID = 1;
        private Dictionary<int, MemoryStream> _memoryStreams = [];
        private readonly SemaphoreSlim _pptSlideGate = new(1, 1);

        private async void PptApplication_SlideShowBegin(SlideShowWindow Wn)
        {
            if (Settings.IsAutoFoldInPPTSlideShow && _viewModel.IsFloatingBarVisible)
                await HideFloatingBar(true);
            else if (!_viewModel.IsFloatingBarVisible)
                await ShowFloatingBar(true);

            Logger.LogInformation("幻灯片放映开始");

            // 清理之前的数据
            foreach (var stream in _memoryStreams.Values)
            {
                stream?.Dispose();
            }
            _memoryStreams.Clear();

            int slidescount = _powerPointService.CurrentPresentationSlideCount;
            string? pptName = _powerPointService.CurrentPresentationName;
            //string strokePath = CommonDirectories.AutoSavePresentationStrokesFolderPath +
            //                pptName + "_" + slidescount;
            string strokePath = Path.Combine(CommonDirectories.AutoSavePresentationStrokesFolderPath,
                pptName + "_" + slidescount);

            //任何情况下都清除现有墨迹
            await Application.Current.Dispatcher.InvokeAsync(() => inkCanvas.Strokes.Clear());

            //检查是否有已有墨迹，并加载
            if (Settings.IsAutoSaveStrokesInPowerPoint && Directory.Exists(strokePath))
            {
                Logger.LogInformation("检测到已有保存的墨迹，正在加载...");
                FileInfo[] files = new DirectoryInfo(strokePath).GetFiles();
                int count = 0;
                foreach (var file in files)
                {
                    int i = 0;
                    try
                    {
                        i = int.Parse(Path.GetFileNameWithoutExtension(file.Name));
                        _memoryStreams[i] = new MemoryStream(File.ReadAllBytes(file.FullName));
                        _memoryStreams[i].Position = 0;
                        count++;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "加载第 {i} 页墨迹失败", i);
                    }
                }
                // 加载当前页墨迹到 InkCanvas
                if (_memoryStreams.TryGetValue(_powerPointService.CurrentSlidePosition, out MemoryStream? value) && value != null)
                {
                    try
                    {
                        value.Position = 0;
                        await Application.Current.Dispatcher.InvokeAsync(() => inkCanvas.Strokes.Add(new StrokeCollection(value)));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, $"加载墨迹到 InkCanvas 失败");
                    }
                }
                Logger.LogInformation("加载完成，共 {count} 页", count);
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (_viewModel.AppMode == AppMode.WhiteBoard)
                    CloseWhiteboard();

                if (Settings.IsShowCanvasAtNewSlideShow &&
                    !Settings.IsAutoFoldInPPTSlideShow &&
                    GridTransparencyFakeBackground.Background == null)
                {
                    ActivatePen();
                }
                isEnteredSlideShowEndEvent = false;
                if (_viewModel.IsFloatingBarVisible)
                {
                    ViewboxFloatingBarMarginAnimation(60);
                }
            });
            _previousSlideID = _powerPointService.CurrentSlidePosition;
        }

        private async void PptApplication_SlideShowEnd(Presentation Pres)
        {
            if (!_viewModel.IsFloatingBarVisible)
                await ShowFloatingBar(true);
            Logger.LogInformation("幻灯片放映结束");

            if (isEnteredSlideShowEndEvent)
            {
                Logger.LogInformation("检测到之前已经进入过退出事件，返回");
                return;
            }

            isEnteredSlideShowEndEvent = true;

            if (Settings.IsAutoSaveStrokesInPowerPoint)
            {
                var folderPath = Path.Combine(CommonDirectories.AutoSavePresentationStrokesFolderPath,
                    _powerPointService.CurrentPresentationName + "_" + _powerPointService.CurrentPresentationSlideCount);
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                MemoryStream ms = new();
                await Application.Current.Dispatcher.InvokeAsync(() => inkCanvas.Strokes.Save(ms));
                _memoryStreams[_powerPointService.CurrentSlidePosition] = ms;

                for (var i = 1; i <= _powerPointService.CurrentPresentationSlideCount; i++)
                {
                    if (_memoryStreams.TryGetValue(i, out MemoryStream? value) && value != null)
                    {
                        try
                        {
                            value.Position = 0;
                            byte[] allBytes = value.ToArray();
                            if (value.Length > 0)
                            {
                                File.WriteAllBytes(folderPath + @"\" + i.ToString("0000") + ".icstk", allBytes);
                                //Logger.LogTrace(
                                //    $"已为第 {i} 页保存墨迹, 大小{value.Length}, 字节数{allBytes.Length}");
                            }
                            else
                            {
                                File.Delete(folderPath + @"\" + i.ToString("0000") + ".icstk");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "为第 {i} 页保存墨迹失败", i);
                            File.Delete(folderPath + @"\" + i.ToString("0000") + ".icstk");
                        }
                    }
                }
                Logger.LogInformation("幻灯片墨迹保存完成");
                // 清理内存流资源
                foreach (var stream in _memoryStreams.Values)
                {
                    stream?.Dispose();
                }
                _memoryStreams.Clear();

            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                CursorFloatingBarButton_Click(null, null);

                inkCanvas.Strokes.Clear();

                ViewboxFloatingBarMarginAnimation(100, true);
            });
        }

        private async void PptApplication_SlideShowNextSlide(SlideShowWindow Wn)
        {
            await _pptSlideGate.WaitAsync();
            try
            {
                var currentPage = Wn.View.CurrentShowPosition;
                Logger.LogTrace("幻灯片跳转到第 {currentPage} 页", currentPage);

                if (currentPage == _previousSlideID)
                    return;
                MemoryStream ms = new();
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (inkCanvas.Strokes.Count > 0)
                        inkCanvas.Strokes.Save(ms);
                });

                if (ms.Length > 0)
                    _memoryStreams[_previousSlideID] = ms;
                else
                    _memoryStreams.Remove(_previousSlideID);

                ClearStrokes(true);
                timeMachine.ClearStrokeHistory();

                try
                {
                    if (_memoryStreams.TryGetValue(currentPage, out MemoryStream? value) && value != null)
                    {
                        value.Position = 0;
                        await Application.Current.Dispatcher.InvokeAsync(() => inkCanvas.Strokes.Add(new StrokeCollection(value)));
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "加载第 {currentPage} 页墨迹失败", currentPage);
                }
                _previousSlideID = currentPage;
            }
            finally
            {
                _pptSlideGate.Release();
            }
        }

        private void ImagePPTControlEnd_Click(object sender, RoutedEventArgs e)
        {
            _powerPointService.EndSlideShow();
        }

        #region New PPT Navigation Panel Event Handlers
        private void HandlePPTPreviousPage()
        {
            if (inkCanvas.Strokes.Count > Settings.MinimumAutomationStrokeNumber
                && Settings.IsAutoSaveScreenShotInPowerPoint)
            {
                SaveScreenShot(true);
            }
            _powerPointService.GoToPreviousSlide();
        }

        private void HandlePPTNextPage()
        {
            if (inkCanvas.Strokes.Count > Settings.MinimumAutomationStrokeNumber
                && Settings.IsAutoSaveScreenShotInPowerPoint)
            {
                SaveScreenShot(true);
            }
            _powerPointService.GoToNextSlide();
        }
        private void PPTNavigationPanel_PreviousClick(object? sender, RoutedEventArgs? e)
        {
            HandlePPTPreviousPage();
        }

        private void PPTNavigationPanel_NextClick(object? sender, RoutedEventArgs? e)
        {
            HandlePPTNextPage();
        }

        private void PPTNavigationPanel_PageClick(object sender, RoutedEventArgs e)
        {
            if (!Settings.EnablePPTButtonPageClickable)
            {
                return;
            }
            CursorFloatingBarButton_Click(null, null);
            try
            {
                _powerPointService.ActiveSlideShowWindow.SlideNavigation.Visible = true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "尝试显示幻灯片导航时失败");
            }
        }
        #endregion
        #endregion

        #region Save&OpenStrokes
        private void SaveInkCanvasStrokes(bool newNotice = true, bool saveByUser = false)
        {
            try
            {
                string savePath = saveByUser
                    ? _viewModel.AppMode == AppMode.Normal
                        ? CommonDirectories.UserSaveAnnotationStrokesFolderPath
                        : CommonDirectories.UserSaveWhiteboardStrokesFolderPath
                    : _viewModel.AppMode == AppMode.Normal
                        ? CommonDirectories.AutoSaveAnnotationStrokesFolderPath
                        : CommonDirectories.AutoSaveWhiteboardStrokesFolderPath;

                string savePathWithName = _viewModel.AppMode == AppMode.Normal
                    ? Path.Combine(savePath, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff") + ".icstk")
                    : Path.Combine(savePath,
                        DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff") + " Page-" +
                        _viewModel.WhiteboardCurrentPage + ".icstk");
                var fs = new FileStream(savePathWithName, FileMode.Create);
                inkCanvas.Strokes.Save(fs);
                if (newNotice) ShowNotification("墨迹成功保存至 " + savePathWithName);
            }
            catch (Exception ex)
            {
                ShowNotification("墨迹保存失败");
                Logger.LogError(ex, "墨迹保存失败");
            }
        }
        #endregion

        #region Screenshot
        private void SaveScreenShot(bool isHideNotification)
        {
            var filePath = ScreenshotHelper.SaveScreenshot(CommonDirectories.AutoSaveScreenshotsFolderPath);

            if (!isHideNotification)
                ShowNotification($"截图成功保存至 {filePath}");

            if (Settings.IsAutoSaveStrokesAtScreenshot)
                SaveInkCanvasStrokes(false, false);
        }

        private void SaveScreenShotToDesktop()
        {
            var filePath = ScreenshotHelper.SaveScreenshotToDesktop();
            var fileName = Path.GetFileName(filePath);

            ShowNotification($"截图成功保存至【桌面\\{fileName}】");

            if (Settings.IsAutoSaveStrokesAtScreenshot)
                SaveInkCanvasStrokes(false, false);
        }
        #endregion

        #region SelectionGestures
        #region Floating Control

        private void BorderStrokeSelectionCloneToNewBoard_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.WhiteboardTotalPageCount >= 99) return;

            var strokes = inkCanvas.GetSelectedStrokes();
            if (strokes.Count == 0) return;
            inkCanvas.Select(new StrokeCollection());
            strokes = strokes.Clone();
            WhiteBoardAddPage();
            inkCanvas.Strokes.Add(strokes);
        }

        private void GridPenWidthDecrease_Click(object sender, RoutedEventArgs e)
        {
            ChangeStrokeThickness(0.8);
        }

        private void GridPenWidthIncrease_Click(object sender, RoutedEventArgs e)
        {
            ChangeStrokeThickness(1.25);
        }

        private void ChangeStrokeThickness(double multipler)
        {
            foreach (var stroke in inkCanvas.GetSelectedStrokes())
            {
                var newWidth = stroke.DrawingAttributes.Width * multipler;
                var newHeight = stroke.DrawingAttributes.Height * multipler;
                if (!(newWidth >= DrawingAttributes.MinWidth) || !(newWidth <= DrawingAttributes.MaxWidth)
                                                              || !(newHeight >= DrawingAttributes.MinHeight) ||
                                                              !(newHeight <= DrawingAttributes.MaxHeight)) continue;
                stroke.DrawingAttributes.Width = newWidth;
                stroke.DrawingAttributes.Height = newHeight;
            }
            CommitPendingDrawingAttributes();
        }

        private void GridPenWidthRestore_Click(object sender, RoutedEventArgs e)
        {
            foreach (var stroke in inkCanvas.GetSelectedStrokes())
            {
                stroke.DrawingAttributes.Width = _viewModel.InkCanvasDrawingAttributes.Width;
                stroke.DrawingAttributes.Height = _viewModel.InkCanvasDrawingAttributes.Height;
            }
            CommitPendingDrawingAttributes();
        }

        private void ImageFlipHorizontal_Click(object sender, RoutedEventArgs e)
        {
            TransformSelectedStrokes(scaleX: -1);
        }

        private void ImageFlipVertical_Click(object sender, RoutedEventArgs e)
        {
            TransformSelectedStrokes(scaleY: -1);
        }

        private void ImageRotate45_Click(object sender, RoutedEventArgs e)
        {
            TransformSelectedStrokes(rotation: 45);
        }

        private void ImageRotate90_Click(object sender, RoutedEventArgs e)
        {
            TransformSelectedStrokes(rotation: 90);
        }

        private void TransformSelectedStrokes(double rotation = 0, double scaleX = 1, double scaleY = 1)
        {
            var strokes = inkCanvas.GetSelectedStrokes();
            if (strokes.Count == 0) return;

            var bounds = inkCanvas.GetSelectionBounds();
            var center = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
            var transform = Matrix.Identity;
            transform.ScaleAt(scaleX, scaleY, center.X, center.Y);
            transform.RotateAt(rotation, center.X, center.Y);

            foreach (var stroke in strokes) stroke.Transform(transform, false);
            CommitPendingDrawingAttributes();
        }

        private void CommitPendingDrawingAttributes()
        {
            if (DrawingAttributesHistory.Count == 0) return;

            timeMachine.CommitStrokeDrawingAttributesHistory(DrawingAttributesHistory);
            DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
            foreach (var flags in DrawingAttributesHistoryFlag.Values) flags.Clear();
        }

        #endregion

        private bool isGridInkCanvasSelectionCoverMouseDown = false;
        private StrokeCollection StrokesSelectionClone = new StrokeCollection();

        private void GridInkCanvasSelectionCover_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isGridInkCanvasSelectionCoverMouseDown = true;
        }

        private void GridInkCanvasSelectionCover_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isGridInkCanvasSelectionCoverMouseDown) return;
            isGridInkCanvasSelectionCoverMouseDown = false;
            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
        }

        private double BorderStrokeSelectionControlWidth = 490.0;
        private double BorderStrokeSelectionControlHeight = 80.0;
        private bool isProgramChangeStrokeSelection = false;

        private void inkCanvas_SelectionChanged(object sender, EventArgs e)
        {
            if (isProgramChangeStrokeSelection) return;
            if (inkCanvas.GetSelectedStrokes().Count == 0)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            }
            else
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Visible;
                _viewModel.IsSelectionCloneEnabled = false;
                updateBorderStrokeSelectionControlLocation();
            }
        }

        private void updateBorderStrokeSelectionControlLocation()
        {
            var borderLeft = (inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Right -
                              BorderStrokeSelectionControlWidth) / 2;
            var borderTop = inkCanvas.GetSelectionBounds().Bottom + 1;
            if (borderLeft < 0) borderLeft = 0;
            if (borderTop < 0) borderTop = 0;
            if (Width - borderLeft < BorderStrokeSelectionControlWidth || double.IsNaN(borderLeft))
                borderLeft = Width - BorderStrokeSelectionControlWidth;
            if (Height - borderTop < BorderStrokeSelectionControlHeight || double.IsNaN(borderTop))
                borderTop = Height - BorderStrokeSelectionControlHeight;

            if (borderTop > 60) borderTop -= 60;
            BorderStrokeSelectionControl.Margin = new Thickness(borderLeft, borderTop, 0, 0);
        }

        private void GridInkCanvasSelectionCover_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.Mode = ManipulationModes.All;
        }

        private void GridInkCanvasSelectionCover_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (StrokeManipulationHistory?.Count > 0)
            {
                timeMachine.CommitStrokeManipulationHistory(StrokeManipulationHistory);
                foreach (var item in StrokeManipulationHistory)
                {
                    StrokeInitialHistory[item.Key] = item.Value.Item2;
                }
                StrokeManipulationHistory = null;
            }
            if (DrawingAttributesHistory.Count > 0)
            {
                timeMachine.CommitStrokeDrawingAttributesHistory(DrawingAttributesHistory);
                DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
                foreach (var item in DrawingAttributesHistoryFlag)
                {
                    item.Value.Clear();
                }
            }
        }

        private void GridInkCanvasSelectionCover_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            try
            {
                if (dec.Count >= 1)
                {
                    var md = e.DeltaManipulation;
                    var trans = md.Translation; // 获得位移矢量
                    var rotate = md.Rotation; // 获得旋转角度
                    var scale = md.Scale; // 获得缩放倍数

                    var m = new Matrix();

                    // Find center of element and then transform to get current location of center
                    var fe = e.Source as FrameworkElement;
                    var center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
                    center = new Point(inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Width / 2,
                        inkCanvas.GetSelectionBounds().Top + inkCanvas.GetSelectionBounds().Height / 2);
                    center = m.Transform(center); // 转换为矩阵缩放和旋转的中心点

                    // Update matrix to reflect translation/rotation
                    m.Translate(trans.X, trans.Y); // 移动
                    m.ScaleAt(scale.X, scale.Y, center.X, center.Y); // 缩放

                    var strokes = inkCanvas.GetSelectedStrokes();
                    if (StrokesSelectionClone.Count != 0)
                        strokes = StrokesSelectionClone;
                    else if (Settings.IsEnableTwoFingerRotationOnSelection)
                        m.RotateAt(rotate, center.X, center.Y); // 旋转
                    foreach (var stroke in strokes)
                    {
                        stroke.Transform(m, false);

                        try
                        {
                            stroke.DrawingAttributes.Width *= md.Scale.X;
                            stroke.DrawingAttributes.Height *= md.Scale.Y;
                        }
                        catch { }
                    }

                    updateBorderStrokeSelectionControlLocation();
                }
            }
            catch { }
        }

        private void GridInkCanvasSelectionCover_TouchDown(object sender, TouchEventArgs e) { }

        private void GridInkCanvasSelectionCover_TouchUp(object sender, TouchEventArgs e) { }

        private Point lastTouchPointOnGridInkCanvasCover = new Point(0, 0);

        private void GridInkCanvasSelectionCover_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            dec.Add(e.TouchDevice.Id);
            //设备1个的时候，记录中心点
            if (dec.Count == 1)
            {
                var touchPoint = e.GetTouchPoint(null);
                lastTouchPointOnGridInkCanvasCover = touchPoint.Position;

                if (_viewModel.IsSelectionCloneEnabled)
                {
                    var strokes = inkCanvas.GetSelectedStrokes();
                    isProgramChangeStrokeSelection = true;
                    inkCanvas.Select(new StrokeCollection());
                    StrokesSelectionClone = strokes.Clone();
                    inkCanvas.Select(strokes);
                    isProgramChangeStrokeSelection = false;
                    inkCanvas.Strokes.Add(StrokesSelectionClone);
                }
            }
        }

        private void GridInkCanvasSelectionCover_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            dec.Remove(e.TouchDevice.Id);
            if (dec.Count >= 1) return;
            isProgramChangeStrokeSelection = false;
            if (lastTouchPointOnGridInkCanvasCover == e.GetTouchPoint(null).Position)
            {
                if (!(lastTouchPointOnGridInkCanvasCover.X < inkCanvas.GetSelectionBounds().Left) &&
                    !(lastTouchPointOnGridInkCanvasCover.Y < inkCanvas.GetSelectionBounds().Top) &&
                    !(lastTouchPointOnGridInkCanvasCover.X > inkCanvas.GetSelectionBounds().Right) &&
                    !(lastTouchPointOnGridInkCanvasCover.Y > inkCanvas.GetSelectionBounds().Bottom)) return;
                inkCanvas.Select(new StrokeCollection());
                StrokesSelectionClone = new StrokeCollection();
            }
            else if (inkCanvas.GetSelectedStrokes().Count == 0)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                StrokesSelectionClone = new StrokeCollection();
            }
            else
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Visible;
                StrokesSelectionClone = new StrokeCollection();
            }
        }
        #endregion

        #region Settings

        #region Startup

        private void ToggleSwitchEnableNibMode_Toggled(object sender, RoutedEventArgs e)
        {
            BoundsWidth = Settings.IsEnableNibMode ? Settings.NibModeBoundsWidth : Settings.FingerModeBoundsWidth;
        }

        #endregion

        #region Appearance

        private void PPTBtnLSPlusBtn_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.PPTLSButtonPosition++;
        }

        private void PPTBtnLSMinusBtn_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.PPTLSButtonPosition--;
        }

        private void PPTBtnLSSyncBtn_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.PPTRSButtonPosition = Settings.PPTLSButtonPosition;
        }

        private void PPTBtnLSResetBtn_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.PPTLSButtonPosition = 0;
        }

        private void PPTBtnRSPlusBtn_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.PPTRSButtonPosition++;
        }

        private void PPTBtnRSMinusBtn_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.PPTRSButtonPosition--;
        }

        private void PPTBtnRSSyncBtn_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.PPTLSButtonPosition = Settings.PPTRSButtonPosition;
        }

        private void PPTBtnRSResetBtn_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.PPTRSButtonPosition = 0;
        }

        private void ToggleSwitchShowCursor_Toggled(object sender, RoutedEventArgs e)
        {
            inkCanvas_EditingModeChanged(inkCanvas, null);
        }

        #endregion

        #region Canvas

        private void SwitchToCircleEraser(object sender, RoutedEventArgs e)
        {
            Settings.EraserShapeType = 0;
        }

        private void SwitchToRectangleEraser(object sender, RoutedEventArgs e)
        {
            Settings.EraserShapeType = 1;
        }

        #endregion

        #region Automation

        private void StartOrStoptimerCheckAutoFold()
        {
            if (Settings.IsEnableAutoFold)
                timerCheckAutoFold.Start();
            else
                timerCheckAutoFold.Stop();
        }

        private void StartOrStopTimerKillProcess()
        {
            if (Settings.IsAutoKillPptService)
                timerKillProcess.Start();
            else
                timerKillProcess.Stop();
        }

        private void AutoSavedStrokesLocationButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new()
            {
                Title = "选择墨迹与截图的保存文件夹",
            };
            if (openFolderDialog.ShowDialog() == true)
            {
                Settings.AutoSaveStrokesPath = openFolderDialog.FolderName;
                CommonDirectories.AppSavesRootFolderPath = Settings.AutoSaveStrokesPath;
            }
        }

        private void SetAutoSavedStrokesLocationToDiskDButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.AutoSaveStrokesPath = @"D:\ICC-Re";
            CommonDirectories.AppSavesRootFolderPath = Settings.AutoSaveStrokesPath;
        }

        private void SetAutoSavedStrokesLocationToAppFolderButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.AutoSaveStrokesPath = Path.GetFullPath(Path.Combine(CommonDirectories.AppRootFolderPath, "Saves"));
            CommonDirectories.AppSavesRootFolderPath = Settings.AutoSaveStrokesPath;
        }

        #endregion

        #region Gesture

        private void ToggleSwitchEnableTwoFingerZoom_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            if (sender == ToggleSwitchEnableTwoFingerZoom)
                BoardToggleSwitchEnableTwoFingerZoom.IsOn = ToggleSwitchEnableTwoFingerZoom.IsOn;
            else
                ToggleSwitchEnableTwoFingerZoom.IsOn = BoardToggleSwitchEnableTwoFingerZoom.IsOn;
            Settings.IsEnableTwoFingerZoom = ToggleSwitchEnableTwoFingerZoom.IsOn;
            CheckEnableTwoFingerGestureBtnColorPrompt();
            _settingsService.SaveSettings();
        }

        private void ToggleSwitchEnableMultiTouchMode_Toggled(object sender, RoutedEventArgs e)
        {
            //if (!isLoaded) return;
            if (sender == ToggleSwitchEnableMultiTouchMode)
                BoardToggleSwitchEnableMultiTouchMode.IsOn = ToggleSwitchEnableMultiTouchMode.IsOn;
            else
                ToggleSwitchEnableMultiTouchMode.IsOn = BoardToggleSwitchEnableMultiTouchMode.IsOn;
            if (ToggleSwitchEnableMultiTouchMode.IsOn)
            {
                if (!isInMultiTouchMode)
                {
                    inkCanvas.StylusDown += MainWindow_StylusDown;
                    inkCanvas.StylusMove += MainWindow_StylusMove;
                    inkCanvas.StylusUp += MainWindow_StylusUp;
                    inkCanvas.TouchDown += MainWindow_TouchDown;
                    inkCanvas.TouchDown -= Main_Grid_TouchDown;
                    inkCanvas.EditingMode = InkCanvasEditingMode.None;
                    inkCanvas.EditingMode = _viewModel.AppPenMode;
                    inkPreviewOverlay.Children.Clear();
                    isInMultiTouchMode = true;
                }
            }
            else
            {
                if (isInMultiTouchMode)
                {
                    inkCanvas.StylusDown -= MainWindow_StylusDown;
                    inkCanvas.StylusMove -= MainWindow_StylusMove;
                    inkCanvas.StylusUp -= MainWindow_StylusUp;
                    inkCanvas.TouchDown -= MainWindow_TouchDown;
                    inkCanvas.TouchDown += Main_Grid_TouchDown;
                    inkCanvas.EditingMode = InkCanvasEditingMode.None;
                    inkCanvas.EditingMode = _viewModel.AppPenMode;
                    inkPreviewOverlay.Children.Clear();
                    isInMultiTouchMode = false;
                }
            }

            Settings.IsEnableMultiTouchMode = ToggleSwitchEnableMultiTouchMode.IsOn;
            CheckEnableTwoFingerGestureBtnColorPrompt();
            _settingsService.SaveSettings();
        }

        private void ToggleSwitchEnableTwoFingerTranslate_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            if (sender == ToggleSwitchEnableTwoFingerTranslate)
                BoardToggleSwitchEnableTwoFingerTranslate.IsOn = ToggleSwitchEnableTwoFingerTranslate.IsOn;
            else
                ToggleSwitchEnableTwoFingerTranslate.IsOn = BoardToggleSwitchEnableTwoFingerTranslate.IsOn;
            Settings.IsEnableTwoFingerTranslate = ToggleSwitchEnableTwoFingerTranslate.IsOn;
            CheckEnableTwoFingerGestureBtnColorPrompt();
            _settingsService.SaveSettings();
        }

        #endregion

        #region Reset

        private void BtnResetToSuggestion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                isLoaded = false;
                _settingsService.ResetToDefaults();
                ApplySettingsToUI();
                isLoaded = true;

                ShowNotification("设置已重置为默认推荐设置~");
            }
            catch
            {

            }
        }

        #endregion

        #region Advanced

        private void BorderCalculateMultiplier_TouchDown(object sender, TouchEventArgs e)
        {
            var args = e.GetTouchPoint(null).Bounds;
            double value;
            if (!Settings.IsQuadIR) value = args.Width;
            else value = Math.Sqrt(args.Width * args.Height); //四边红外

            TextBlockShowCalculatedMultiplier.Text = (5 / (value * 1.1)).ToString();
        }

        #endregion

        #region RandSettings

        #endregion

        private void SCManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            HideSubPanels();
        }
        #endregion

        #region SettingsToLoad
        private void ApplySettingsToUI()
        {
            if (Settings.IsEnableNibMode)
            {
                BoundsWidth = Settings.NibModeBoundsWidth;
            }
            else
            {
                BoundsWidth = Settings.FingerModeBoundsWidth;
            }

            // -- new --

            // Gesture

            ToggleSwitchEnableMultiTouchMode.IsOn = Settings.IsEnableMultiTouchMode;

            ToggleSwitchEnableTwoFingerZoom.IsOn = Settings.IsEnableTwoFingerZoom;
            BoardToggleSwitchEnableTwoFingerZoom.IsOn = Settings.IsEnableTwoFingerZoom;

            ToggleSwitchEnableTwoFingerTranslate.IsOn = Settings.IsEnableTwoFingerTranslate;
            BoardToggleSwitchEnableTwoFingerTranslate.IsOn = Settings.IsEnableTwoFingerTranslate;

            if (Settings.AutoSwitchTwoFingerGesture)
            {
                if (_viewModel.AppMode == AppMode.Normal)
                {
                    ToggleSwitchEnableTwoFingerTranslate.IsOn = false;
                    BoardToggleSwitchEnableTwoFingerTranslate.IsOn = false;
                    Settings.IsEnableTwoFingerTranslate = false;
                    if (!isInMultiTouchMode) ToggleSwitchEnableMultiTouchMode.IsOn = true;
                }
                else
                {
                    ToggleSwitchEnableTwoFingerTranslate.IsOn = true;
                    BoardToggleSwitchEnableTwoFingerTranslate.IsOn = true;
                    Settings.IsEnableTwoFingerTranslate = true;
                    if (isInMultiTouchMode) ToggleSwitchEnableMultiTouchMode.IsOn = false;
                }
            }

            CheckEnableTwoFingerGestureBtnColorPrompt();

            UpdateEraserShape();

            // Advanced
            if (Settings.IsEnableEdgeGestureUtil)
            {
                if (OSVersion.GetOperatingSystem() >= OSVersionExtension.OperatingSystem.Windows10)
                    EdgeGestureUtil.DisableEdgeGestures(new WindowInteropHelper(this).Handle, true);
            }

            // Automation
            StartOrStoptimerCheckAutoFold();

            if (Settings.IsAutoKillPptService)
            {
                timerKillProcess.Start();
            }
            else
            {
                timerKillProcess.Stop();
            }

            // auto align
            if (_powerPointService.IsInSlideShow)
            {
                ViewboxFloatingBarMarginAnimation(60);
            }
            else
            {
                ViewboxFloatingBarMarginAnimation(100, true);
            }
        }
        #endregion

        #region ShapeDrawing

        private void Main_Grid_TouchUp(object sender, TouchEventArgs e)
        {

            inkCanvas.ReleaseAllTouchCaptures();
            ViewboxFloatingBar.IsHitTestVisible = true;
            WhiteboardGrid.IsHitTestVisible = true;
            PPTNavigationPanel.IsHitTestVisible = true;

            inkCanvas_MouseUp(sender, null);
        }

        private void inkCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            inkCanvas.CaptureMouse();
            ViewboxFloatingBar.IsHitTestVisible = false;
            WhiteboardGrid.IsHitTestVisible = false;
            PPTNavigationPanel.IsHitTestVisible = false;
        }

        private void inkCanvas_MouseUp(object? sender, MouseButtonEventArgs? e)
        {
            inkCanvas.ReleaseMouseCapture();
            ViewboxFloatingBar.IsHitTestVisible = true;
            WhiteboardGrid.IsHitTestVisible = true;
            PPTNavigationPanel.IsHitTestVisible = true;
            if (ReplacedStroke != null || AddedStroke != null)
            {
                timeMachine.CommitStrokeEraseHistory(ReplacedStroke, AddedStroke);
                AddedStroke = null;
                ReplacedStroke = null;
            }

            if (StrokeManipulationHistory?.Count > 0)
            {
                timeMachine.CommitStrokeManipulationHistory(StrokeManipulationHistory);
                foreach (var item in StrokeManipulationHistory)
                {
                    StrokeInitialHistory[item.Key] = item.Value.Item2;
                }
                StrokeManipulationHistory = null;
            }

            if (DrawingAttributesHistory.Count > 0)
            {
                timeMachine.CommitStrokeDrawingAttributesHistory(DrawingAttributesHistory);
                DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
                foreach (var item in DrawingAttributesHistoryFlag)
                {
                    item.Value.Clear();
                }
            }

            if (Settings.FitToCurve == true)
                _viewModel.InkCanvasDrawingAttributes.FitToCurve = true;
        }
        #endregion

        #region SimulatePressure&InkToShape
        private void inkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            //if (Settings.FitToCurve == true) _viewModel.InkCanvasDrawingAttributes.FitToCurve = false;
            try
            {
                //inkCanvas.Opacity = 1;

                var originalPoints = e.Stroke.StylusPoints;
                var count = originalPoints.Count;
                var n = count - 1;
                // 仅对签字笔进行书写优化
                if (_viewModel.InkCanvasDrawingAttributes.IsHighlighter || n <= 0) return;
                // 检查是否是压感笔书写，如果是真实的压感笔则不需要处理
                for (var i = 0; i < count; i++)
                {
                    if (originalPoints[i].PressureFactor is > 0.501f or < 0.5f and not 0f)
                        return;
                }

                var newPoints = new StylusPointCollection(count);

                switch (Settings.InkStyle)
                {
                    case 1:
                        for (var i = 0; i <= n; i++)
                        {
                            var pPrev = originalPoints[Math.Max(i - 1, 0)];
                            var pCurr = originalPoints[i];
                            var pNext = originalPoints[Math.Min(i + 1, n)];

                            var speed = (float)GetPointSpeed(pPrev, pCurr, pNext);

                            float pressureFactor;
                            if (speed >= 0.25)
                                pressureFactor = 0.5f - 0.3f * (Math.Min(speed, 1.5f) - 0.3f) / 1.2f;
                            else if (speed >= 0.05f)
                                pressureFactor = 0.5f;
                            else
                                pressureFactor = 0.5f + 0.4f * (0.05f - speed) / 0.05f;

                            newPoints.Add(new StylusPoint(originalPoints[i].X, originalPoints[i].Y, pressureFactor));
                        }

                        e.Stroke.StylusPoints = newPoints;
                        break;

                    case 0:
                        const float pressure = 0.1f;
                        const int x = 10;

                        if (n >= x)
                        {
                            for (var i = 0; i < n - x; i++)
                            {
                                newPoints.Add(new StylusPoint(originalPoints[i].X, originalPoints[i].Y, 0.5f));
                            }

                            for (var i = n - x; i <= n; i++)
                            {
                                var factor = (0.5f - pressure) * (n - i) / x + pressure;
                                newPoints.Add(new StylusPoint(originalPoints[i].X, originalPoints[i].Y, factor));
                            }
                        }
                        else
                        {
                            for (var i = 0; i <= n; i++)
                            {
                                var factor = 0.4f * (n - i) / n + pressure;
                                newPoints.Add(new StylusPoint(originalPoints[i].X, originalPoints[i].Y, factor));
                            }
                        }

                        e.Stroke.StylusPoints = newPoints;
                        break;
                }
            }
            catch { }

            //if (Settings.FitToCurve == true) _viewModel.InkCanvasDrawingAttributes.FitToCurve = true;
        }

        public double GetPointSpeed(StylusPoint point1, StylusPoint point2, StylusPoint point3)
        {
            return (Math.Sqrt((point1.X - point2.X) * (point1.X - point2.X) +
                              (point1.Y - point2.Y) * (point1.Y - point2.Y))
                    + Math.Sqrt((point3.X - point2.X) * (point3.X - point2.X) +
                                (point3.Y - point2.Y) * (point3.Y - point2.Y)))
                   / 20;
        }

        #endregion

        #region TimeMachine
        private enum CommitReason
        {
            UserInput,
            CodeInput,
            ClearingCanvas,
            Manipulation
        }

        private CommitReason _currentCommitType = CommitReason.UserInput;
        private bool IsEraseByPoint => inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint;
        private StrokeCollection? ReplacedStroke;
        private StrokeCollection? AddedStroke;
        private Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>>? StrokeManipulationHistory;

        private Dictionary<Stroke, StylusPointCollection> StrokeInitialHistory =
            new Dictionary<Stroke, StylusPointCollection>();

        private Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>> DrawingAttributesHistory =
            new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();

        private Dictionary<Guid, List<Stroke>> DrawingAttributesHistoryFlag = new() {
            { DrawingAttributeIds.Color, new List<Stroke>() },
            { DrawingAttributeIds.DrawingFlags, new List<Stroke>() },
            { DrawingAttributeIds.IsHighlighter, new List<Stroke>() },
            { DrawingAttributeIds.StylusHeight, new List<Stroke>() },
            { DrawingAttributeIds.StylusTip, new List<Stroke>() },
            { DrawingAttributeIds.StylusTipTransform, new List<Stroke>() },
            { DrawingAttributeIds.StylusWidth, new List<Stroke>() }
        };

        private TimeMachine timeMachine = new();

        private void ApplyHistoryToCanvas(TimeMachineHistory item, InkCanvas? applyCanvas = null)
        {
            _currentCommitType = CommitReason.CodeInput;
            var canvas = inkCanvas;
            if (applyCanvas != null && applyCanvas is InkCanvas)
            {
                canvas = applyCanvas;
            }

            if (item.CommitType == TimeMachineHistoryType.UserInput)
            {
                if (!item.StrokeHasBeenCleared)
                {
                    foreach (var strokes in item.CurrentStroke)
                        if (!canvas.Strokes.Contains(strokes))
                            canvas.Strokes.Add(strokes);
                }
                else
                {
                    foreach (var strokes in item.CurrentStroke)
                        if (canvas.Strokes.Contains(strokes))
                            canvas.Strokes.Remove(strokes);
                }
            }
            else if (item.CommitType == TimeMachineHistoryType.Manipulation)
            {
                if (!item.StrokeHasBeenCleared)
                {
                    foreach (var currentStroke in item.StylusPointDictionary)
                    {
                        if (canvas.Strokes.Contains(currentStroke.Key))
                        {
                            currentStroke.Key.StylusPoints = currentStroke.Value.Item2;
                        }
                    }
                }
                else
                {
                    foreach (var currentStroke in item.StylusPointDictionary)
                    {
                        if (canvas.Strokes.Contains(currentStroke.Key))
                        {
                            currentStroke.Key.StylusPoints = currentStroke.Value.Item1;
                        }
                    }
                }
            }
            else if (item.CommitType == TimeMachineHistoryType.DrawingAttributes)
            {
                if (!item.StrokeHasBeenCleared)
                {
                    foreach (var currentStroke in item.DrawingAttributes)
                    {
                        if (canvas.Strokes.Contains(currentStroke.Key))
                        {
                            currentStroke.Key.DrawingAttributes = currentStroke.Value.Item2;
                        }
                    }
                }
                else
                {
                    foreach (var currentStroke in item.DrawingAttributes)
                    {
                        if (canvas.Strokes.Contains(currentStroke.Key))
                        {
                            currentStroke.Key.DrawingAttributes = currentStroke.Value.Item1;
                        }
                    }
                }
            }
            else if (item.CommitType == TimeMachineHistoryType.Clear)
            {
                if (!item.StrokeHasBeenCleared)
                {
                    if (item.CurrentStroke != null)
                        foreach (var currentStroke in item.CurrentStroke)
                            if (!canvas.Strokes.Contains(currentStroke))
                                canvas.Strokes.Add(currentStroke);

                    if (item.ReplacedStroke != null)
                        foreach (var replacedStroke in item.ReplacedStroke)
                            if (canvas.Strokes.Contains(replacedStroke))
                                canvas.Strokes.Remove(replacedStroke);
                }
                else
                {
                    if (item.ReplacedStroke != null)
                        foreach (var replacedStroke in item.ReplacedStroke)
                            if (!canvas.Strokes.Contains(replacedStroke))
                                canvas.Strokes.Add(replacedStroke);

                    if (item.CurrentStroke != null)
                        foreach (var currentStroke in item.CurrentStroke)
                            if (canvas.Strokes.Contains(currentStroke))
                                canvas.Strokes.Remove(currentStroke);
                }
            }

            _currentCommitType = CommitReason.UserInput;
        }

        private StrokeCollection ApplyHistoriesToNewStrokeCollection(TimeMachineHistory[] items)
        {
            InkCanvas fakeInkCanv = new InkCanvas()
            {
                Width = inkCanvas.ActualWidth,
                Height = inkCanvas.ActualHeight,
                EditingMode = InkCanvasEditingMode.None,
            };

            if (items != null && items.Length > 0)
            {
                foreach (var timeMachineHistory in items)
                {
                    ApplyHistoryToCanvas(timeMachineHistory, fakeInkCanv);
                }
            }

            return fakeInkCanv.Strokes;
        }

        private void TimeMachine_OnUndoStateChanged(bool status)
        {
            _viewModel.CanUndo = status;
        }

        private void TimeMachine_OnRedoStateChanged(bool status)
        {
            _viewModel.CanRedo = status;
        }

        private void StrokesOnStrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            if (!isHidingSubPanelsWhenInking)
            {
                isHidingSubPanelsWhenInking = true;
                HideSubPanels(); // 书写时自动隐藏二级菜单
            }

            foreach (var stroke in e?.Removed)
            {
                stroke.StylusPointsChanged -= Stroke_StylusPointsChanged;
                stroke.StylusPointsReplaced -= Stroke_StylusPointsReplaced;
                stroke.DrawingAttributesChanged -= Stroke_DrawingAttributesChanged;
                StrokeInitialHistory.Remove(stroke);
            }

            foreach (var stroke in e?.Added)
            {
                stroke.StylusPointsChanged += Stroke_StylusPointsChanged;
                stroke.StylusPointsReplaced += Stroke_StylusPointsReplaced;
                stroke.DrawingAttributesChanged += Stroke_DrawingAttributesChanged;
                StrokeInitialHistory[stroke] = stroke.StylusPoints.Clone();
            }

            if (_currentCommitType == CommitReason.CodeInput)
                return;

            if ((e.Added.Count != 0 || e.Removed.Count != 0) && IsEraseByPoint)
            {
                if (AddedStroke == null) AddedStroke = new StrokeCollection();
                if (ReplacedStroke == null) ReplacedStroke = new StrokeCollection();
                AddedStroke.Add(e.Added);
                ReplacedStroke.Add(e.Removed);
                return;
            }

            if (e.Added.Count != 0)
            {
                timeMachine.CommitStrokeUserInputHistory(e.Added);
                return;
            }

            if (e.Removed.Count != 0)
            {
                if (!IsEraseByPoint || _currentCommitType == CommitReason.ClearingCanvas)
                {
                    timeMachine.CommitStrokeEraseHistory(e.Removed);
                    return;
                }
            }
        }

        private void Stroke_DrawingAttributesChanged(object sender, PropertyDataChangedEventArgs e)
        {
            var key = sender as Stroke;
            var currentValue = key.DrawingAttributes.Clone();
            DrawingAttributesHistory.TryGetValue(key, out var previousTuple);
            var previousValue = previousTuple?.Item1 ?? currentValue.Clone();
            var needUpdateValue = !DrawingAttributesHistoryFlag[e.PropertyGuid].Contains(key);
            if (needUpdateValue)
            {
                DrawingAttributesHistoryFlag[e.PropertyGuid].Add(key);
                Debug.Write(e.PreviousValue.ToString());
            }

            if (e.PropertyGuid == DrawingAttributeIds.Color && needUpdateValue)
            {
                previousValue.Color = (Color)e.PreviousValue;
            }

            if (e.PropertyGuid == DrawingAttributeIds.IsHighlighter && needUpdateValue)
            {
                previousValue.IsHighlighter = (bool)e.PreviousValue;
            }

            if (e.PropertyGuid == DrawingAttributeIds.StylusHeight && needUpdateValue)
            {
                previousValue.Height = (double)e.PreviousValue;
            }

            if (e.PropertyGuid == DrawingAttributeIds.StylusWidth && needUpdateValue)
            {
                previousValue.Width = (double)e.PreviousValue;
            }

            if (e.PropertyGuid == DrawingAttributeIds.StylusTip && needUpdateValue)
            {
                previousValue.StylusTip = (StylusTip)e.PreviousValue;
            }

            if (e.PropertyGuid == DrawingAttributeIds.StylusTipTransform && needUpdateValue)
            {
                previousValue.StylusTipTransform = (Matrix)e.PreviousValue;
            }

            if (e.PropertyGuid == DrawingAttributeIds.DrawingFlags && needUpdateValue)
            {
                previousValue.IgnorePressure = (bool)e.PreviousValue;
            }

            DrawingAttributesHistory[key] =
                new Tuple<DrawingAttributes, DrawingAttributes>(previousValue, currentValue);
        }

        private void Stroke_StylusPointsReplaced(object sender, StylusPointsReplacedEventArgs e)
        {
            StrokeInitialHistory[sender as Stroke] = e.NewStylusPoints.Clone();
        }

        private void Stroke_StylusPointsChanged(object? sender, EventArgs e)
        {
            var selectedStrokes = inkCanvas.GetSelectedStrokes();
            var count = selectedStrokes.Count;
            if (count == 0) count = inkCanvas.Strokes.Count;
            if (StrokeManipulationHistory == null)
            {
                StrokeManipulationHistory =
                    new Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>>();
            }

            StrokeManipulationHistory[sender as Stroke] =
                new Tuple<StylusPointCollection, StylusPointCollection>(StrokeInitialHistory[sender as Stroke],
                    (sender as Stroke).StylusPoints.Clone());
            if ((StrokeManipulationHistory.Count == count || sender == null) && dec.Count == 0)
            {
                timeMachine.CommitStrokeManipulationHistory(StrokeManipulationHistory);
                foreach (var item in StrokeManipulationHistory)
                {
                    StrokeInitialHistory[item.Key] = item.Value.Item2;
                }

                StrokeManipulationHistory = null;
            }
        }
        #endregion

        #region Timer
        private DispatcherTimer timerKillProcess = new DispatcherTimer();
        private DispatcherTimer timerCheckAutoFold = new DispatcherTimer();
        private bool isHidingSubPanelsWhenInking = false; // 避免书写时触发二次关闭二级菜单导致动画不连续

        private DispatcherTimer timerDisplayTime = new DispatcherTimer();
        private DispatcherTimer timerDisplayDate = new DispatcherTimer();

        private void InitTimers()
        {
            timerKillProcess.Tick += TimerKillProcess_Tick;
            timerKillProcess.Interval = TimeSpan.FromMilliseconds(2000);
            timerCheckAutoFold.Tick += timerCheckAutoFold_Tick;
            timerCheckAutoFold.Interval = TimeSpan.FromMilliseconds(500);

            timerDisplayTime.Tick += TimerDisplayTime_Tick;
            timerDisplayTime.Interval = TimeSpan.FromMilliseconds(1000);
            timerDisplayTime.Start();
            timerDisplayDate.Tick += TimerDisplayDate_Tick;
            timerDisplayDate.Interval = TimeSpan.FromMilliseconds(1000 * 60 * 60 * 1);
            timerDisplayDate.Start();
            timerKillProcess.Start();
            _viewModel.NowDate = DateTime.Now.ToShortDateString().ToString();
            _viewModel.NowTime = DateTime.Now.ToShortTimeString().ToString();
        }

        private void TimerDisplayTime_Tick(object? sender, EventArgs e)
        {
            _viewModel.NowTime = DateTime.Now.ToShortTimeString().ToString();
        }

        private void TimerDisplayDate_Tick(object? sender, EventArgs e)
        {
            _viewModel.NowDate = DateTime.Now.ToShortDateString().ToString();
        }

        private void TimerKillProcess_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (!Settings.IsAutoKillPptService)
                    return;

                var processesToKill = new List<string>();

                // 检查 PPTService 进程
                if (Process.GetProcessesByName("PPTService").Length > 0)
                {
                    processesToKill.Add("PPTService.exe");
                }

                // 检查 SeewoIwbAssistant 进程
                if (Process.GetProcessesByName("SeewoIwbAssistant").Length > 0)
                {
                    processesToKill.AddRange(new[] { "SeewoIwbAssistant.exe", "Sia.Guard.exe" });
                }

                if (processesToKill.Count > 0)
                {
                    var args = "/F " + string.Join(" ", processesToKill.Select(p => $"/IM {p}"));

                    using var process = new Process();
                    process.StartInfo = new ProcessStartInfo("taskkill", args)
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    process.Start();
                    Logger.LogInformation($"Killed processes: {string.Join(", ", processesToKill)}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to kill processes in TimerKillProcess_Tick");
            }
        }


        private bool foldFloatingBarByUser = false; // 保持收纳操作不受自动收纳的控制
        private bool unfoldFloatingBarByUser = false; // 允许用户在希沃软件内进行展开操作

        private void timerCheckAutoFold_Tick(object? sender, EventArgs e)
        {
            if (isFloatingBarChangingHideMode) return;

            try
            {
                var windowProcessName = ForegroundWindowInfo.ProcessName();
                var windowTitle = ForegroundWindowInfo.WindowTitle();
                var windowRect = ForegroundWindowInfo.WindowRect();

                // 转换 RECT 到 System.Drawing.Rectangle
                var rect = new System.Drawing.Rectangle(
                    windowRect.Left,
                    windowRect.Top,
                    windowRect.Width,
                    windowRect.Height);

                bool shouldFold = ShouldFoldForCurrentWindow(windowProcessName, windowTitle, rect);

                if (shouldFold)
                {
                    if (!unfoldFloatingBarByUser && _viewModel.IsFloatingBarVisible)
                        _ = HideFloatingBar();
                }
                else
                {
                    if (!_viewModel.IsFloatingBarVisible && !foldFloatingBarByUser)
                    {
                        _ = ShowFloatingBar();
                    }
                    unfoldFloatingBarByUser = false;
                }
            }
            catch { }
        }

        private bool ShouldFoldForCurrentWindow(string processName, string windowTitle, System.Drawing.Rectangle windowRect)
        {
            // PPT 幻灯片放映特殊处理
            if (WinTabWindowsChecker.IsWindowExisted("幻灯片放映", false))
            {
                return Settings.IsAutoFoldInPPTSlideShow;
            }

            // 检查是否为全屏应用（工作区大小减去16像素的容错）
            bool isFullScreen = windowRect.Height >= SystemParameters.WorkArea.Height - 16 &&
                               windowRect.Width >= SystemParameters.WorkArea.Width - 16;

            return processName switch
            {
                "EasiNote" => ShouldFoldEasiNote(windowTitle, windowRect),
                "EasiCamera" => Settings.IsAutoFoldInEasiCamera && isFullScreen,
                "EasiNote5C" => Settings.IsAutoFoldInEasiNote5C && isFullScreen,
                _ => false
            };
        }

        private bool ShouldFoldEasiNote(string windowTitle, System.Drawing.Rectangle windowRect)
        {
            if (ForegroundWindowInfo.ProcessPath() == "Unknown") return false;

            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(ForegroundWindowInfo.ProcessPath());
                string? version = versionInfo.FileVersion;
                string? prodName = versionInfo.ProductName;

                if (version.StartsWith("5.") && Settings.IsAutoFoldInEasiNote)
                {
                    // EasiNote5: 排除桌面标注小窗口
                    return !(windowTitle.Length == 0 && windowRect.Height < 500) ||
                           !Settings.IsAutoFoldInEasiNoteIgnoreDesktopAnno;
                }
                else if (version.StartsWith("3.") && Settings.IsAutoFoldInEasiNote3)
                {
                    return true; // EasiNote3
                }
                else if (prodName.Contains("3C") && Settings.IsAutoFoldInEasiNote3C)
                {
                    // EasiNote3C: 需要全屏
                    return windowRect.Height >= SystemParameters.WorkArea.Height - 16 &&
                           windowRect.Width >= SystemParameters.WorkArea.Width - 16;
                }
            }
            catch { }

            return false;
        }

        #endregion

        #region TouchEvents
        #region Multi-Touch

        private bool isInMultiTouchMode = false;

        private void MainWindow_TouchDown(object? sender, TouchEventArgs? e)
        {
            if (ForceEraser)
            {
                return;
            }

            if (!isHidingSubPanelsWhenInking)
            {
                isHidingSubPanelsWhenInking = true;
                HideSubPanels(); // 书写时自动隐藏二级菜单
            }

            double boundWidth = e.GetTouchPoint(null).Bounds.Width;
            double eraserMultiplier = 1.0;

            if (Settings.EraserBindTouchMultiplier && Settings.IsSpecialScreen)
                eraserMultiplier = 1 / Settings.TouchMultiplier;

            if ((Settings.TouchMultiplier != 0 && Settings.IsSpecialScreen) //启用特殊屏幕且触摸倍数为 0 时禁用橡皮
                && boundWidth > BoundsWidth * 2.5)
            {
                double k = 1;
                switch (Settings.EraserSize)
                {
                    case 0:
                        k = 0.5;
                        break;
                    case 1:
                        k = 0.8;
                        break;
                    case 3:
                        k = 1.25;
                        break;
                    case 4:
                        k = 1.8;
                        break;
                }

                inkCanvas.EraserShape = new EllipseStylusShape(boundWidth * k * eraserMultiplier * 0.25,
                    boundWidth * k * eraserMultiplier * 0.25);
                TouchDownPointsList[e.TouchDevice.Id] = InkCanvasEditingMode.EraseByPoint;
                inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
            }
            else
            {
                TouchDownPointsList[e.TouchDevice.Id] = InkCanvasEditingMode.None;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
            }
        }

        private void MainWindow_StylusDown(object sender, StylusDownEventArgs e)
        {
            //不知道为什么但注释之后是把套索选修复了
            //inkCanvas.CaptureStylus();
            ViewboxFloatingBar.IsHitTestVisible = false;
            WhiteboardGrid.IsHitTestVisible = false;
            PPTNavigationPanel.IsHitTestVisible = false;

            if (ForceEraser)
                return;

            TouchDownPointsList[e.StylusDevice.Id] = InkCanvasEditingMode.None;
        }

        private async void MainWindow_StylusUp(object sender, StylusEventArgs e)
        {
            //Logger.LogDebug("StylusUp event triggered");
            try
            {
                Stroke stroke = GetStrokeVisual(e.StylusDevice.Id).Stroke;
                inkCanvas.Strokes.Add(stroke);
                inkPreviewOverlay.Children.Remove(GetVisualCanvas(e.StylusDevice.Id));

                inkCanvas_StrokeCollected(inkCanvas,
                    new InkCanvasStrokeCollectedEventArgs(stroke));
            }
            catch
            {
                //Logger.LogWarning(ex, "Error in StylusUp event");
            }

            try
            {
                StrokeVisualList.Remove(e.StylusDevice.Id);
                VisualCanvasList.Remove(e.StylusDevice.Id);
                TouchDownPointsList.Remove(e.StylusDevice.Id);
                if (StrokeVisualList.Count == 0 || VisualCanvasList.Count == 0 || TouchDownPointsList.Count == 0)
                {
                    inkPreviewOverlay.Children.Clear();
                    StrokeVisualList.Clear();
                    VisualCanvasList.Clear();
                    TouchDownPointsList.Clear();
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error in StylusUp event");
            }

            //inkCanvas.ReleaseStylusCapture();
            ViewboxFloatingBar.IsHitTestVisible = true;
            WhiteboardGrid.IsHitTestVisible = true;
            PPTNavigationPanel.IsHitTestVisible = true;
        }

        private void MainWindow_StylusMove(object sender, StylusEventArgs e)
        {
            //Logger.LogDebug("StylusMove event triggered");
            try
            {
                if (GetTouchDownPointsList(e.StylusDevice.Id) != InkCanvasEditingMode.None) return;
                //try
                //{
                //    if (e.StylusDevice.StylusButtons[1].StylusButtonState == StylusButtonState.Down) return;
                //}
                //catch (Exception ex)
                //{
                //    Logger.LogWarning(ex, "Error checking stylus button state");
                //}

                var strokeVisual = GetStrokeVisual(e.StylusDevice.Id);
                var stylusPointCollection = e.GetStylusPoints(inkCanvas);
                strokeVisual.AddRange(stylusPointCollection);
                strokeVisual.Redraw();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error in StylusMove event");
            }
        }

        private StrokeVisual GetStrokeVisual(int id)
        {
            if (StrokeVisualList.TryGetValue(id, out var visual)) return visual;

            var strokeVisual = new StrokeVisual(_viewModel.InkCanvasDrawingAttributes.Clone());
            StrokeVisualList[id] = strokeVisual;
            var visualCanvas = new VisualCanvas(strokeVisual);
            VisualCanvasList[id] = visualCanvas;
            inkPreviewOverlay.Children.Add(visualCanvas);

            return strokeVisual;
        }

        private VisualCanvas? GetVisualCanvas(int id)
        {
            return VisualCanvasList.TryGetValue(id, out var visualCanvas) ? visualCanvas : null;
        }

        private InkCanvasEditingMode GetTouchDownPointsList(int id)
        {
            return TouchDownPointsList.TryGetValue(id, out var inkCanvasEditingMode) ? inkCanvasEditingMode : inkCanvas.EditingMode;
        }

        private Dictionary<int, InkCanvasEditingMode> TouchDownPointsList { get; } =
            new Dictionary<int, InkCanvasEditingMode>();

        private Dictionary<int, StrokeVisual> StrokeVisualList { get; } = new Dictionary<int, StrokeVisual>();
        private Dictionary<int, VisualCanvas> VisualCanvasList { get; } = new Dictionary<int, VisualCanvas>();

        #endregion

        private void Main_Grid_TouchDown(object? sender, TouchEventArgs? e)
        {
            //Logger.LogDebug("Main_Grid_touchdown");
            inkCanvas.CaptureTouch(e.TouchDevice);
            ViewboxFloatingBar.IsHitTestVisible = false;
            WhiteboardGrid.IsHitTestVisible = false;
            PPTNavigationPanel.IsHitTestVisible = false;

            if (!isHidingSubPanelsWhenInking)
            {
                isHidingSubPanelsWhenInking = true;
                HideSubPanels(); // 书写时自动隐藏二级菜单
            }

            //inkCanvas.Opacity = 1;
            //double boundsWidth = GetTouchBoundWidth(e), eraserMultiplier = 1.0;
            //if (!Settings.EraserBindTouchMultiplier && Settings.IsSpecialScreen)
            //    eraserMultiplier = 1 / Settings.TouchMultiplier;
            //if (boundsWidth > BoundsWidth)
            //{
            //    if (boundsWidth > BoundsWidth * 2.5)
            //    {
            //        double k = 1;
            //        switch (Settings.EraserSize)
            //        {
            //            case 0:
            //                k = 0.5;
            //                break;
            //            case 1:
            //                k = 0.8;
            //                break;
            //            case 3:
            //                k = 1.25;
            //                break;
            //            case 4:
            //                k = 1.8;
            //                break;
            //        }

            //        inkCanvas.EraserShape = new EllipseStylusShape(boundsWidth * k * eraserMultiplier,
            //            boundsWidth * k * eraserMultiplier);
            //        inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
            //    }
            //    else
            //    {
            //        if (_powerPointService.IsInSlideShow && inkCanvas.Strokes.Count == 0 &&
            //            Settings.IsEnableFingerGestureSlideShowControl)
            //        {
            //            inkCanvas.EditingMode = InkCanvasEditingMode.GestureOnly;
            //            inkCanvas.Opacity = 0.1;
            //        }
            //        else
            //        {
            //            inkCanvas.EraserShape = new EllipseStylusShape(5, 5);
            //            inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
            //        }
            //    }
            //}
            //else
            //{
            //    inkCanvas.EraserShape =
            //        forcePointEraser ? new EllipseStylusShape(50, 50) : new EllipseStylusShape(5, 5);
            //    if (forceEraser) return;
            //    inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            //}
        }

        private double GetTouchBoundWidth(TouchEventArgs e)
        {
            var args = e.GetTouchPoint(null).Bounds;
            double value;
            if (!Settings.IsQuadIR) value = args.Width;
            else value = Math.Sqrt(args.Width * args.Height); //四边红外
            if (Settings.IsSpecialScreen) value *= Settings.TouchMultiplier;
            return value;
        }

        //记录触摸设备ID
        private List<int> dec = [];

        private InkCanvasEditingMode lastInkCanvasEditingMode = InkCanvasEditingMode.Ink;

        private void inkCanvas_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            //Logger.LogDebug("inkCanvas_PreviewTouchDown");
            inkCanvas.CaptureTouch(e.TouchDevice);
            ViewboxFloatingBar.IsHitTestVisible = false;
            WhiteboardGrid.IsHitTestVisible = false;
            PPTNavigationPanel.IsHitTestVisible = false;

            dec.Add(e.TouchDevice.Id);
            if (dec.Count == 1)
            {
                //记录第一根手指点击时的 StrokeCollection
                //lastTouchDownStrokeCollection = inkCanvas.Strokes.Clone();
            }
            //设备两个及两个以上，将画笔功能关闭
            if (dec.Count > 1 || !Settings.IsEnableTwoFingerGesture)
            {
                if (isInMultiTouchMode || !Settings.IsEnableTwoFingerGesture) return;
                if (inkCanvas.EditingMode == InkCanvasEditingMode.None ||
                    inkCanvas.EditingMode == InkCanvasEditingMode.Select) return;
                lastInkCanvasEditingMode = inkCanvas.EditingMode;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
            }
        }

        private void inkCanvas_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            //Logger.LogDebug("inkCanvas_PreviewTouchUp");
            inkCanvas.ReleaseAllTouchCaptures();
            ViewboxFloatingBar.IsHitTestVisible = true;
            WhiteboardGrid.IsHitTestVisible = true;
            PPTNavigationPanel.IsHitTestVisible = true;

            //手势完成后切回之前的状态
            if (dec.Count > 1)
                if (inkCanvas.EditingMode == InkCanvasEditingMode.None)
                    inkCanvas.EditingMode = lastInkCanvasEditingMode;
            dec.Remove(e.TouchDevice.Id);
            inkCanvas.Opacity = 1;

        }

        private void inkCanvas_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            //Logger.LogDebug("inkCanvas_ManipulationStarting");
            e.Mode = ManipulationModes.All;
        }

        private void inkCanvas_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e) { }

        private void Main_Grid_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            //Logger.LogDebug("Main_Grid_ManipulationCompleted");
            if (e.Manipulators.Count() != 0) return;
            //if (_viewModel.AppPenMode is InkCanvasEditingMode.EraseByPoint or InkCanvasEditingMode.EraseByStroke)
            //{
            //    return;
            //}
            //inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            inkCanvas.EditingMode = _viewModel.AppPenMode;
        }

        // -- removed --
        //
        //private void inkCanvas_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        //{
        //    if (isInMultiTouchMode || !Settings.IsEnableTwoFingerGesture || inkCanvas.Strokes.Count == 0 || dec.Count() < 2) return;
        //    _currentCommitType = CommitReason.Manipulation;
        //    StrokeCollection strokes = inkCanvas.GetSelectedStrokes();
        //    if (strokes.Count != 0)
        //    {
        //        inkCanvas.Strokes.Replace(strokes, strokes.Clone());
        //    }
        //    else
        //    {
        //        var originalStrokes = inkCanvas.Strokes;
        //        var targetStrokes = originalStrokes.Clone();
        //        originalStrokes.Replace(originalStrokes, targetStrokes);
        //    }
        //    _currentCommitType = CommitReason.UserInput;
        //}

        private void Main_Grid_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (isInMultiTouchMode || !Settings.IsEnableTwoFingerGesture) return;
            if (dec.Count >= 2 && (Settings.IsEnableTwoFingerGestureInPresentationMode
                                    || !_powerPointService.IsInSlideShow
                                    || _viewModel.AppMode == AppMode.WhiteBoard))
            {
                var md = e.DeltaManipulation;
                var trans = md.Translation; // 获得位移矢量

                var m = new Matrix();

                if (Settings.IsEnableTwoFingerTranslate)
                    m.Translate(trans.X, trans.Y); // 移动

                if (Settings.IsEnableTwoFingerGestureTranslateOrRotation)
                {
                    var rotate = md.Rotation; // 获得旋转角度
                    var scale = md.Scale; // 获得缩放倍数

                    // Find center of element and then transform to get current location of center
                    var fe = e.Source as FrameworkElement;
                    var center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
                    center = m.Transform(center); // 转换为矩阵缩放和旋转的中心点

                    if (Settings.IsEnableTwoFingerRotation)
                        m.RotateAt(rotate, center.X, center.Y); // 旋转
                    if (Settings.IsEnableTwoFingerZoom)
                        m.ScaleAt(scale.X, scale.Y, center.X, center.Y); // 缩放
                }

                var strokes = inkCanvas.GetSelectedStrokes();
                if (strokes.Count != 0)
                {
                    foreach (var stroke in strokes)
                    {
                        stroke.Transform(m, false);

                        if (!Settings.IsEnableTwoFingerZoom)
                            continue;
                        try
                        {
                            stroke.DrawingAttributes.Width *= md.Scale.X;
                            stroke.DrawingAttributes.Height *= md.Scale.Y;
                        }
                        catch { }
                    }
                }
                else
                {
                    if (Settings.IsEnableTwoFingerZoom)
                    {
                        foreach (var stroke in inkCanvas.Strokes)
                        {
                            stroke.Transform(m, false);
                            try
                            {
                                stroke.DrawingAttributes.Width *= md.Scale.X;
                                stroke.DrawingAttributes.Height *= md.Scale.Y;
                            }
                            catch { }
                        }

                        ;
                    }
                    else
                    {
                        foreach (var stroke in inkCanvas.Strokes) stroke.Transform(m, false);
                        ;
                    }
                }
            }
        }
        #endregion

        #region Native Methods

        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int GWL_EXSTYLE = -20;

        public static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
        {
            return Environment.Is64BitProcess
                ? GetWindowLong64(hWnd, nIndex)
                : GetWindowLong32(hWnd, nIndex);
        }

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return Environment.Is64BitProcess
                ? SetWindowLong64(hWnd, nIndex, dwNewLong)
                : SetWindowLong32(hWnd, nIndex, dwNewLong);
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLong64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLong64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        #endregion

        private void CloseWhiteboardWhiteBoardButton_Click(object sender, RoutedEventArgs e)
        {
            CloseWhiteboard();
        }

        public void HideToolsPanel()
        {
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
        }
    }
}
