using InkCanvasForClass_Remastered.Enums;
using InkCanvasForClass_Remastered.Helpers;
using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.Services;
using InkCanvasForClass_Remastered.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Office.Interop.PowerPoint;
using Microsoft.Win32;
using OSVersionExtension;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
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
        private readonly ILogger<MainWindow> Logger;
        public Settings Settings => _settingsService.Settings;


        #region Window Initialization

        public MainWindow(MainViewModel viewModel, SettingsService settingsService, IPowerPointService powerPointService, ILogger<MainWindow> logger)
        {
            /*
                处于画板模式内：Topmost == false / _viewModel.AppMode == AppMode.WhiteBoard
                处于 PPT 放映内：_powerPointService.IsInSlideShow
            */
            InitializeComponent();

            _viewModel = viewModel;
            _settingsService = settingsService;
            _powerPointService = powerPointService;
            Logger = logger;

            DataContext = _viewModel;

            // 挂载PPT服务事件
            _powerPointService.PresentationClose += PptApplication_PresentationClose;
            _powerPointService.SlideShowBegin += PptApplication_SlideShowBegin;
            _powerPointService.SlideShowEnd += PptApplication_SlideShowEnd;
            _powerPointService.SlideShowNextSlide += PptApplication_SlideShowNextSlide;

            Settings.PropertyChanged += Settings_PropertyChanged;

            BlackboardLeftSide.Visibility = Visibility.Collapsed;
            BlackboardCenterSide.Visibility = Visibility.Collapsed;
            BlackboardRightSide.Visibility = Visibility.Collapsed;
            BorderTools.Visibility = Visibility.Collapsed;
            TwoFingerGestureBorder.Visibility = Visibility.Collapsed;
            BoardTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            ViewboxFloatingBar.Margin = new Thickness((SystemParameters.WorkArea.Width - 284) / 2,
                SystemParameters.WorkArea.Height - 60, -2000, -200);
            ViewboxFloatingBarMarginAnimation(100, true);

            InitTimers();
            timeMachine.OnRedoStateChanged += TimeMachine_OnRedoStateChanged;
            timeMachine.OnUndoStateChanged += TimeMachine_OnUndoStateChanged;
            inkCanvas.Strokes.StrokesChanged += StrokesOnStrokesChanged;

            CheckColorTheme(true);
            CheckPenTypeUIState();
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
                case nameof(Settings.FitToCurve):
                    drawingAttributes.FitToCurve = Settings.FitToCurve;
                    break;
                case nameof(Settings.EraserSize):
                    UpdateEraserShape();
                    break;
                case nameof(Settings.IsEnableTwoFingerRotationOnSelection) or nameof(Settings.IsEnableTwoFingerRotation):
                    CheckEnableTwoFingerGestureBtnColorPrompt();
                    break;
                case nameof(Settings.FingerModeBoundsWidth) or nameof(Settings.NibModeBoundsWidth):
                    BoundsWidth = Settings.IsEnableNibMode ? Settings.NibModeBoundsWidth : Settings.FingerModeBoundsWidth;
                    break;
                case nameof(Settings.PPTLSButtonPosition) or nameof(Settings.PPTRSButtonPosition):
                    UpdatePPTBtnPreview();
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

            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
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

        private System.Windows.Media.Color Ink_DefaultColor = Colors.Red;

        private DrawingAttributes drawingAttributes;

        private void loadPenCanvas()
        {
            try
            {
                //drawingAttributes = new DrawingAttributes();
                drawingAttributes = inkCanvas.DefaultDrawingAttributes;
                drawingAttributes.Color = Ink_DefaultColor;


                drawingAttributes.Height = 2.5;
                drawingAttributes.Width = 2.5;
                drawingAttributes.IsHighlighter = false;
                drawingAttributes.FitToCurve = Settings.FitToCurve;

                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                inkCanvas.Gesture += InkCanvas_Gesture;
            }
            catch { }
        }

        //ApplicationGesture lastApplicationGesture = ApplicationGesture.AllGestures;
        private DateTime lastGestureTime = DateTime.Now;

        private void InkCanvas_Gesture(object sender, InkCanvasGestureEventArgs e)
        {
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
        }

        private void inkCanvas_EditingModeChanged(object? sender, RoutedEventArgs? e)
        {
            var inkCanvas1 = sender as InkCanvas;
            if (inkCanvas1 == null) return;
            if (Settings.IsShowCursor)
            {
                if (inkCanvas1.EditingMode == InkCanvasEditingMode.Ink)
                    inkCanvas1.ForceCursor = true;
                else
                    inkCanvas1.ForceCursor = false;
            }
            else
            {
                inkCanvas1.ForceCursor = false;
            }

            if (inkCanvas1.EditingMode == InkCanvasEditingMode.Ink) forcePointEraser = !forcePointEraser;
        }

        #endregion Ink Canvas

        #region Definations and Loading

        private bool isLoaded = false;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loadPenCanvas();
            AppVersionTextBlock.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            CursorIcon_Click(null, null);

            if (Settings.IsFoldAtStartup)
            {
                _ = HideFloatingBar(Fold_Icon);
            }

            ApplySettingsToUI();

            Logger.LogInformation("MainWindow Loaded");

            isLoaded = true;

            BlackBoardLeftSidePageListView.ItemsSource = blackBoardSidePageListViewObservableCollection;
            BlackBoardRightSidePageListView.ItemsSource = blackBoardSidePageListViewObservableCollection;

            BorderInkReplayToolBox.Visibility = Visibility.Collapsed;

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
            Logger.LogInformation("MainWindow closed");
        }

        #endregion Definations and Loading

        #region AutoFold
        private bool isFloatingBarChangingHideMode = false;

        public async void HideFloatingBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            await HideFloatingBar(sender);
        }

        public async Task HideFloatingBar(object? sender)
        {
            var isShouldRejectAction = false;

            await Dispatcher.InvokeAsync(() =>
            {
                if (lastBorderMouseDownObject != null && lastBorderMouseDownObject is Panel)
                    ((Panel)lastBorderMouseDownObject).Background = new SolidColorBrush(Colors.Transparent);
                if (sender == Fold_Icon && lastBorderMouseDownObject != Fold_Icon)
                    isShouldRejectAction = true;
            });

            if (isShouldRejectAction)
                return;

            if (sender == null)
                foldFloatingBarByUser = false;
            else
                foldFloatingBarByUser = true;
            unfoldFloatingBarByUser = false;

            if (isFloatingBarChangingHideMode)
                return;

            isFloatingBarChangingHideMode = true;
            _viewModel.IsFloatingBarVisible = false;

            await Dispatcher.InvokeAsync(() =>
            {
                InkCanvasForInkReplay.Visibility = Visibility.Collapsed;
                InkCanvasGridForInkReplay.Visibility = Visibility.Visible;
                InkCanvasGridForInkReplay.IsHitTestVisible = true;
                FloatingbarUIForInkReplay.Visibility = Visibility.Visible;
                FloatingbarUIForInkReplay.IsHitTestVisible = true;
                BlackboardUIGridForInkReplay.Visibility = Visibility.Visible;
                BlackboardUIGridForInkReplay.IsHitTestVisible = true;
                AnimationsHelper.HideWithFadeOut(BorderInkReplayToolBox);
                isStopInkReplay = true;
            });

            await Dispatcher.InvokeAsync(() =>
            {
                if (_viewModel.AppMode == AppMode.WhiteBoard)
                    CloseWhiteboard();
                if (_powerPointService.IsInSlideShow)
                    if (foldFloatingBarByUser && inkCanvas.Strokes.Count > 2)
                        ShowNotification("正在清空墨迹并收纳至侧边栏，可进入批注模式后通过【撤销】功能来恢复原先墨迹。");
                CursorWithDelIcon_Click(null, null);
            });

            await Dispatcher.InvokeAsync(() =>
            {
                ViewboxFloatingBarMarginAnimation(-60);
                HideSubPanels("cursor");
            });
            isFloatingBarChangingHideMode = false;
        }

        private async void SidePanelUnFoldButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            await ShowFloatingBar(sender);
        }

        public async Task ShowFloatingBar(object? sender)
        {
            unfoldFloatingBarByUser = sender != null;

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

        private async void BtnWhiteBoardPageIndex_Click(object sender, EventArgs e)
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

        private void BtnWhiteBoardSwitchPrevious_Click(object sender, EventArgs e)
        {
            if (_viewModel.WhiteboardCurrentPage <= 1) return;

            SaveStrokes();

            ClearStrokes(true);
            _viewModel.WhiteboardCurrentPage--;

            RestoreStrokes();
        }

        private void BtnWhiteBoardSwitchNext_Click(object sender, EventArgs e)
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
        private void BoardChangeBackgroundColorBtn_MouseUp(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.UsingWhiteboard = !Settings.UsingWhiteboard;
            _settingsService.SaveSettings();
            if (Settings.UsingWhiteboard)
            {
                if (inkColor == 5) lastBoardInkColor = 0;
            }
            else
            {
                if (inkColor == 0) lastBoardInkColor = 5;
            }

            CheckColorTheme(true);
        }

        private void BoardEraserIcon_Click(object sender, RoutedEventArgs e)
        {
            if (inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint ||
                inkCanvas.EditingMode == InkCanvasEditingMode.EraseByStroke)
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
                forceEraser = true;
                forcePointEraser = true;
                UpdateEraserShape();

                inkCanvas_EditingModeChanged(inkCanvas, null);
                CancelSingleFingerDragMode();

                HideSubPanels("eraser");
            }
        }

        private void BoardEraserIconByStrokes_Click(object sender, RoutedEventArgs e)
        {
            //if (BoardEraserByStrokes.Background.ToString() == "#FF679CF4") {
            //    AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardDeleteIcon);
            //}
            //else {
            forceEraser = true;
            forcePointEraser = false;

            inkCanvas.EraserShape = new EllipseStylusShape(5, 5);
            inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;

            inkCanvas_EditingModeChanged(inkCanvas, null);
            CancelSingleFingerDragMode();

            HideSubPanels("eraserByStrokes");
            //}
        }

        private void BoardSymbolIconDelete_MouseUp(object sender, RoutedEventArgs e)
        {
            PenIcon_Click(null, null);
            SymbolIconDelete_MouseUp(null, null);
        }
        private void BoardSymbolIconDeleteInkAndHistories_MouseUp(object sender, RoutedEventArgs e)
        {
            PenIcon_Click(null, null);
            SymbolIconDelete_MouseUp(null, null);
            if (Settings.ClearCanvasAndClearTimeMachine == false) timeMachine.ClearStrokeHistory();
        }

        #endregion

        #region Colors
        private int inkColor = 1;

        private void ColorSwitchCheck()
        {
            HideSubPanels("color");
            if (GridTransparencyFakeBackground.Background == null)
            {
                if (_viewModel.AppMode == AppMode.WhiteBoard)
                {
                    _viewModel.AppMode = AppMode.Normal;
                    GridBackgroundCover.Visibility = Visibility.Collapsed;
                    AnimationsHelper.HideWithSlideAndFade(BlackboardLeftSide);
                    AnimationsHelper.HideWithSlideAndFade(BlackboardCenterSide);
                    AnimationsHelper.HideWithSlideAndFade(BlackboardRightSide);
                }

                BtnHideInkCanvas_Click(null, null);
            }

            var strokes = inkCanvas.GetSelectedStrokes();
            if (strokes.Count != 0)
            {
                foreach (var stroke in strokes)
                    try
                    {
                        stroke.DrawingAttributes.Color = inkCanvas.DefaultDrawingAttributes.Color;
                    }
                    catch
                    {
                        // ignored
                    }
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
            else
            {
                inkCanvas.IsManipulationEnabled = true;
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                CancelSingleFingerDragMode();
                forceEraser = false;
                CheckColorTheme();
            }
        }

        private bool isUselightThemeColor = false, isDesktopUselightThemeColor = false;
        private int penType = 0; // 0是签字笔，1是荧光笔
        private int lastDesktopInkColor = 1, lastBoardInkColor = 5;
        private int highlighterColor = 102;

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
                inkColor = lastDesktopInkColor;
            }
            else
            {
                inkColor = lastBoardInkColor;
            }

            double alpha = inkCanvas.DefaultDrawingAttributes.Color.A;

            if (penType == 0)
            {
                if (inkColor == 0)
                {
                    // Black
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 0, 0, 0);
                }
                else if (inkColor == 5)
                {
                    // White
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 255, 255, 255);
                }
                else if (isUselightThemeColor)
                {
                    if (inkColor == 1)
                        // Red
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 239, 68, 68);
                    else if (inkColor == 2)
                        // Green
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 34, 197, 94);
                    else if (inkColor == 3)
                        // Blue
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 59, 130, 246);
                    else if (inkColor == 4)
                        // Yellow
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 250, 204, 21);
                    else if (inkColor == 6)
                        // Pink
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 236, 72, 153);
                    else if (inkColor == 7)
                        // Teal (亮色)
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 20, 184, 166);
                    else if (inkColor == 8)
                        // Orange (亮色)
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 249, 115, 22);
                }
                else
                {
                    if (inkColor == 1)
                        // Red
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 220, 38, 38);
                    else if (inkColor == 2)
                        // Green
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 22, 163, 74);
                    else if (inkColor == 3)
                        // Blue
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 37, 99, 235);
                    else if (inkColor == 4)
                        // Yellow
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 234, 179, 8);
                    else if (inkColor == 6)
                        // Pink ( Purple )
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 147, 51, 234);
                    else if (inkColor == 7)
                        // Teal (暗色)
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 13, 148, 136);
                    else if (inkColor == 8)
                        // Orange (暗色)
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 234, 88, 12);
                }
            }
            else if (penType == 1)
            {
                if (highlighterColor == 100)
                    // Black
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(0, 0, 0);
                else if (highlighterColor == 101)
                    // White
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(250, 250, 250);
                else if (highlighterColor == 102)
                    // Red
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(239, 68, 68);
                else if (highlighterColor == 103)
                    // Yellow
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(253, 224, 71);
                else if (highlighterColor == 104)
                    // Green
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(74, 222, 128);
                else if (highlighterColor == 105)
                    // Zinc
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(113, 113, 122);
                else if (highlighterColor == 106)
                    // Blue
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(59, 130, 246);
                else if (highlighterColor == 107)
                    // Purple
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(168, 85, 247);
                else if (highlighterColor == 108)
                    // teal
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(45, 212, 191);
                else if (highlighterColor == 109)
                    // Orange
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(249, 115, 22);
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

            // 改变选中提示
            ViewboxBtnColorBlackContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorBlueContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorGreenContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorRedContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorYellowContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorWhiteContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorPinkContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorTealContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorOrangeContent.Visibility = Visibility.Collapsed;

            BoardViewboxBtnColorBlackContent.Visibility = Visibility.Collapsed;
            BoardViewboxBtnColorBlueContent.Visibility = Visibility.Collapsed;
            BoardViewboxBtnColorGreenContent.Visibility = Visibility.Collapsed;
            BoardViewboxBtnColorRedContent.Visibility = Visibility.Collapsed;
            BoardViewboxBtnColorYellowContent.Visibility = Visibility.Collapsed;
            BoardViewboxBtnColorWhiteContent.Visibility = Visibility.Collapsed;
            BoardViewboxBtnColorPinkContent.Visibility = Visibility.Collapsed;
            BoardViewboxBtnColorTealContent.Visibility = Visibility.Collapsed;
            BoardViewboxBtnColorOrangeContent.Visibility = Visibility.Collapsed;

            HighlighterPenViewboxBtnColorBlackContent.Visibility = Visibility.Collapsed;
            HighlighterPenViewboxBtnColorBlueContent.Visibility = Visibility.Collapsed;
            HighlighterPenViewboxBtnColorGreenContent.Visibility = Visibility.Collapsed;
            HighlighterPenViewboxBtnColorOrangeContent.Visibility = Visibility.Collapsed;
            HighlighterPenViewboxBtnColorPurpleContent.Visibility = Visibility.Collapsed;
            HighlighterPenViewboxBtnColorRedContent.Visibility = Visibility.Collapsed;
            HighlighterPenViewboxBtnColorTealContent.Visibility = Visibility.Collapsed;
            HighlighterPenViewboxBtnColorWhiteContent.Visibility = Visibility.Collapsed;
            HighlighterPenViewboxBtnColorYellowContent.Visibility = Visibility.Collapsed;
            HighlighterPenViewboxBtnColorZincContent.Visibility = Visibility.Collapsed;

            BoardHighlighterPenViewboxBtnColorBlackContent.Visibility = Visibility.Collapsed;
            BoardHighlighterPenViewboxBtnColorBlueContent.Visibility = Visibility.Collapsed;
            BoardHighlighterPenViewboxBtnColorGreenContent.Visibility = Visibility.Collapsed;
            BoardHighlighterPenViewboxBtnColorOrangeContent.Visibility = Visibility.Collapsed;
            BoardHighlighterPenViewboxBtnColorPurpleContent.Visibility = Visibility.Collapsed;
            BoardHighlighterPenViewboxBtnColorRedContent.Visibility = Visibility.Collapsed;
            BoardHighlighterPenViewboxBtnColorTealContent.Visibility = Visibility.Collapsed;
            BoardHighlighterPenViewboxBtnColorWhiteContent.Visibility = Visibility.Collapsed;
            BoardHighlighterPenViewboxBtnColorYellowContent.Visibility = Visibility.Collapsed;
            BoardHighlighterPenViewboxBtnColorZincContent.Visibility = Visibility.Collapsed;

            switch (inkColor)
            {
                case 0:
                    ViewboxBtnColorBlackContent.Visibility = Visibility.Visible;
                    BoardViewboxBtnColorBlackContent.Visibility = Visibility.Visible;
                    break;
                case 1:
                    ViewboxBtnColorRedContent.Visibility = Visibility.Visible;
                    BoardViewboxBtnColorRedContent.Visibility = Visibility.Visible;
                    break;
                case 2:
                    ViewboxBtnColorGreenContent.Visibility = Visibility.Visible;
                    BoardViewboxBtnColorGreenContent.Visibility = Visibility.Visible;
                    break;
                case 3:
                    ViewboxBtnColorBlueContent.Visibility = Visibility.Visible;
                    BoardViewboxBtnColorBlueContent.Visibility = Visibility.Visible;
                    break;
                case 4:
                    ViewboxBtnColorYellowContent.Visibility = Visibility.Visible;
                    BoardViewboxBtnColorYellowContent.Visibility = Visibility.Visible;
                    break;
                case 5:
                    ViewboxBtnColorWhiteContent.Visibility = Visibility.Visible;
                    BoardViewboxBtnColorWhiteContent.Visibility = Visibility.Visible;
                    break;
                case 6:
                    ViewboxBtnColorPinkContent.Visibility = Visibility.Visible;
                    BoardViewboxBtnColorPinkContent.Visibility = Visibility.Visible;
                    break;
                case 7:
                    ViewboxBtnColorTealContent.Visibility = Visibility.Visible;
                    break;
                case 8:
                    ViewboxBtnColorOrangeContent.Visibility = Visibility.Visible;
                    break;
            }

            switch (highlighterColor)
            {
                case 100:
                    HighlighterPenViewboxBtnColorBlackContent.Visibility = Visibility.Visible;
                    BoardHighlighterPenViewboxBtnColorBlackContent.Visibility = Visibility.Visible;
                    break;
                case 101:
                    HighlighterPenViewboxBtnColorWhiteContent.Visibility = Visibility.Visible;
                    BoardHighlighterPenViewboxBtnColorWhiteContent.Visibility = Visibility.Visible;
                    break;
                case 102:
                    HighlighterPenViewboxBtnColorRedContent.Visibility = Visibility.Visible;
                    BoardHighlighterPenViewboxBtnColorRedContent.Visibility = Visibility.Visible;
                    break;
                case 103:
                    HighlighterPenViewboxBtnColorYellowContent.Visibility = Visibility.Visible;
                    BoardHighlighterPenViewboxBtnColorYellowContent.Visibility = Visibility.Visible;
                    break;
                case 104:
                    HighlighterPenViewboxBtnColorGreenContent.Visibility = Visibility.Visible;
                    BoardHighlighterPenViewboxBtnColorGreenContent.Visibility = Visibility.Visible;
                    break;
                case 105:
                    HighlighterPenViewboxBtnColorZincContent.Visibility = Visibility.Visible;
                    BoardHighlighterPenViewboxBtnColorZincContent.Visibility = Visibility.Visible;
                    break;
                case 106:
                    HighlighterPenViewboxBtnColorBlueContent.Visibility = Visibility.Visible;
                    BoardHighlighterPenViewboxBtnColorBlueContent.Visibility = Visibility.Visible;
                    break;
                case 107:
                    HighlighterPenViewboxBtnColorPurpleContent.Visibility = Visibility.Visible;
                    BoardHighlighterPenViewboxBtnColorPurpleContent.Visibility = Visibility.Visible;
                    break;
                case 108:
                    HighlighterPenViewboxBtnColorTealContent.Visibility = Visibility.Visible;
                    BoardHighlighterPenViewboxBtnColorTealContent.Visibility = Visibility.Visible;
                    break;
                case 109:
                    HighlighterPenViewboxBtnColorOrangeContent.Visibility = Visibility.Visible;
                    BoardHighlighterPenViewboxBtnColorOrangeContent.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void CheckLastColor(int inkColor, bool isHighlighter = false)
        {
            if (isHighlighter == true)
            {
                highlighterColor = inkColor;
            }
            else
            {
                if (_viewModel.AppMode == AppMode.Normal) lastDesktopInkColor = inkColor;
                else lastBoardInkColor = inkColor;
            }
        }

        private async void CheckPenTypeUIState()
        {
            if (penType == 0)
            {
                DefaultPenPropsPanel.Visibility = Visibility.Visible;
                DefaultPenColorsPanel.Visibility = Visibility.Visible;
                HighlighterPenColorsPanel.Visibility = Visibility.Collapsed;
                HighlighterPenPropsPanel.Visibility = Visibility.Collapsed;
                DefaultPenTabButton.Opacity = 1;
                DefaultPenTabButtonText.FontWeight = FontWeights.Bold;
                DefaultPenTabButtonText.Margin = new Thickness(2, 0.5, 0, 0);
                DefaultPenTabButtonText.FontSize = 9.5;
                DefaultPenTabButton.Background = new SolidColorBrush(Color.FromArgb(72, 219, 234, 254));
                DefaultPenTabButtonIndicator.Visibility = Visibility.Visible;
                HighlightPenTabButton.Opacity = 0.9;
                HighlightPenTabButtonText.FontWeight = FontWeights.Normal;
                HighlightPenTabButtonText.FontSize = 9;
                HighlightPenTabButtonText.Margin = new Thickness(2, 1, 0, 0);
                HighlightPenTabButton.Background = new SolidColorBrush(Colors.Transparent);
                HighlightPenTabButtonIndicator.Visibility = Visibility.Collapsed;

                BoardDefaultPenPropsPanel.Visibility = Visibility.Visible;
                BoardDefaultPenColorsPanel.Visibility = Visibility.Visible;
                BoardHighlighterPenColorsPanel.Visibility = Visibility.Collapsed;
                BoardHighlighterPenPropsPanel.Visibility = Visibility.Collapsed;
                BoardDefaultPenTabButton.Opacity = 1;
                BoardDefaultPenTabButtonText.FontWeight = FontWeights.Bold;
                BoardDefaultPenTabButtonText.Margin = new Thickness(2, 0.5, 0, 0);
                BoardDefaultPenTabButtonText.FontSize = 9.5;
                BoardDefaultPenTabButton.Background = new SolidColorBrush(Color.FromArgb(72, 219, 234, 254));
                BoardDefaultPenTabButtonIndicator.Visibility = Visibility.Visible;
                BoardHighlightPenTabButton.Opacity = 0.9;
                BoardHighlightPenTabButtonText.FontWeight = FontWeights.Normal;
                BoardHighlightPenTabButtonText.FontSize = 9;
                BoardHighlightPenTabButtonText.Margin = new Thickness(2, 1, 0, 0);
                BoardHighlightPenTabButton.Background = new SolidColorBrush(Colors.Transparent);
                BoardHighlightPenTabButtonIndicator.Visibility = Visibility.Collapsed;

                // PenPalette.Margin = new Thickness(-160, -200, -33, 32);
                await Dispatcher.InvokeAsync(() =>
                {
                    var marginAnimation = new ThicknessAnimation
                    {
                        Duration = TimeSpan.FromSeconds(0.1),
                        From = PenPalette.Margin,
                        To = new Thickness(-160, -200, -33, 32),
                        EasingFunction = new CubicEase()
                    };
                    PenPalette.BeginAnimation(MarginProperty, marginAnimation);
                });

                await Dispatcher.InvokeAsync(() =>
                {
                    var marginAnimation = new ThicknessAnimation
                    {
                        Duration = TimeSpan.FromSeconds(0.1),
                        From = PenPalette.Margin,
                        To = new Thickness(-160, -200, -33, 50),
                        EasingFunction = new CubicEase()
                    };
                    BoardPenPaletteGrid.BeginAnimation(MarginProperty, marginAnimation);
                });


                await Task.Delay(100);

                await Dispatcher.InvokeAsync(() => { PenPalette.Margin = new Thickness(-160, -200, -33, 32); });

                await Dispatcher.InvokeAsync(() => { BoardPenPaletteGrid.Margin = new Thickness(-160, -200, -33, 50); });
            }
            else if (penType == 1)
            {
                DefaultPenPropsPanel.Visibility = Visibility.Collapsed;
                DefaultPenColorsPanel.Visibility = Visibility.Collapsed;
                HighlighterPenColorsPanel.Visibility = Visibility.Visible;
                HighlighterPenPropsPanel.Visibility = Visibility.Visible;
                DefaultPenTabButton.Opacity = 0.9;
                DefaultPenTabButtonText.FontWeight = FontWeights.Normal;
                DefaultPenTabButtonText.FontSize = 9;
                DefaultPenTabButtonText.Margin = new Thickness(2, 1, 0, 0);
                DefaultPenTabButton.Background = new SolidColorBrush(Colors.Transparent);
                DefaultPenTabButtonIndicator.Visibility = Visibility.Collapsed;
                HighlightPenTabButton.Opacity = 1;
                HighlightPenTabButtonText.FontWeight = FontWeights.Bold;
                HighlightPenTabButtonText.FontSize = 9.5;
                HighlightPenTabButtonText.Margin = new Thickness(2, 0.5, 0, 0);
                HighlightPenTabButton.Background = new SolidColorBrush(Color.FromArgb(72, 219, 234, 254));
                HighlightPenTabButtonIndicator.Visibility = Visibility.Visible;

                BoardDefaultPenPropsPanel.Visibility = Visibility.Collapsed;
                BoardDefaultPenColorsPanel.Visibility = Visibility.Collapsed;
                BoardHighlighterPenColorsPanel.Visibility = Visibility.Visible;
                BoardHighlighterPenPropsPanel.Visibility = Visibility.Visible;
                BoardDefaultPenTabButton.Opacity = 0.9;
                BoardDefaultPenTabButtonText.FontWeight = FontWeights.Normal;
                BoardDefaultPenTabButtonText.FontSize = 9;
                BoardDefaultPenTabButtonText.Margin = new Thickness(2, 1, 0, 0);
                BoardDefaultPenTabButton.Background = new SolidColorBrush(Colors.Transparent);
                BoardDefaultPenTabButtonIndicator.Visibility = Visibility.Collapsed;
                BoardHighlightPenTabButton.Opacity = 1;
                BoardHighlightPenTabButtonText.FontWeight = FontWeights.Bold;
                BoardHighlightPenTabButtonText.FontSize = 9.5;
                BoardHighlightPenTabButtonText.Margin = new Thickness(2, 0.5, 0, 0);
                BoardHighlightPenTabButton.Background = new SolidColorBrush(Color.FromArgb(72, 219, 234, 254));
                BoardHighlightPenTabButtonIndicator.Visibility = Visibility.Visible;

                // PenPalette.Margin = new Thickness(-160, -157, -33, 32);
                await Dispatcher.InvokeAsync(() =>
                {
                    var marginAnimation = new ThicknessAnimation
                    {
                        Duration = TimeSpan.FromSeconds(0.1),
                        From = PenPalette.Margin,
                        To = new Thickness(-160, -157, -33, 32),
                        EasingFunction = new CubicEase()
                    };
                    PenPalette.BeginAnimation(MarginProperty, marginAnimation);
                });

                await Dispatcher.InvokeAsync(() =>
                {
                    var marginAnimation = new ThicknessAnimation
                    {
                        Duration = TimeSpan.FromSeconds(0.1),
                        From = PenPalette.Margin,
                        To = new Thickness(-160, -154, -33, 50),
                        EasingFunction = new CubicEase()
                    };
                    BoardPenPaletteGrid.BeginAnimation(MarginProperty, marginAnimation);
                });

                await Task.Delay(100);

                await Dispatcher.InvokeAsync(() => { PenPalette.Margin = new Thickness(-160, -157, -33, 32); });

                await Dispatcher.InvokeAsync(() => { BoardPenPaletteGrid.Margin = new Thickness(-160, -154, -33, 50); });
            }
        }

        private void SwitchToDefaultPen(object? sender, MouseButtonEventArgs? e)
        {
            penType = 0;
            CheckPenTypeUIState();
            CheckColorTheme();
            drawingAttributes.Width = Settings.InkWidth;
            drawingAttributes.Height = Settings.InkWidth;
            drawingAttributes.StylusTip = StylusTip.Ellipse;
            drawingAttributes.IsHighlighter = false;
        }

        private void SwitchToHighlighterPen(object sender, MouseButtonEventArgs e)
        {
            penType = 1;
            CheckPenTypeUIState();
            CheckColorTheme();
            drawingAttributes.Width = Settings.HighlighterWidth / 2;
            drawingAttributes.Height = Settings.HighlighterWidth;
            drawingAttributes.StylusTip = StylusTip.Rectangle;
            drawingAttributes.IsHighlighter = true;
        }

        private void BtnColorBlack_Click(object? sender, RoutedEventArgs? e)
        {
            CheckLastColor(0);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnColorRed_Click(object sender, RoutedEventArgs e)
        {
            CheckLastColor(1);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnColorGreen_Click(object sender, RoutedEventArgs e)
        {
            CheckLastColor(2);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnColorBlue_Click(object sender, RoutedEventArgs e)
        {
            CheckLastColor(3);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnColorYellow_Click(object sender, RoutedEventArgs e)
        {
            CheckLastColor(4);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnColorWhite_Click(object? sender, RoutedEventArgs? e)
        {
            CheckLastColor(5);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnColorPink_Click(object sender, RoutedEventArgs e)
        {
            CheckLastColor(6);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnColorOrange_Click(object sender, RoutedEventArgs e)
        {
            CheckLastColor(8);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnColorTeal_Click(object sender, RoutedEventArgs e)
        {
            CheckLastColor(7);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorBlack_Click(object sender, RoutedEventArgs e)
        {
            CheckLastColor(100, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorWhite_Click(object sender, RoutedEventArgs e)
        {
            CheckLastColor(101, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorRed_Click(object sender, RoutedEventArgs e)
        {
            CheckLastColor(102, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorYellow_Click(object sender, RoutedEventArgs e)
        {
            CheckLastColor(103, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorGreen_Click(object sender, RoutedEventArgs e)
        {
            CheckLastColor(104, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorZinc_Click(object sender, RoutedEventArgs e)
        {
            CheckLastColor(105, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorBlue_Click(object sender, RoutedEventArgs e)
        {
            CheckLastColor(106, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorPurple_Click(object sender, RoutedEventArgs e)
        {
            CheckLastColor(107, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorTeal_Click(object sender, RoutedEventArgs e)
        {
            CheckLastColor(108, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorOrange_Click(object sender, RoutedEventArgs e)
        {
            CheckLastColor(109, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
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
        private void TwoFingerGestureBorder_MouseUp(object sender, RoutedEventArgs e)
        {
            if (TwoFingerGestureBorder.Visibility == Visibility.Visible)
            {
                AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
                AnimationsHelper.HideWithSlideAndFade(BorderTools);
                AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                AnimationsHelper.HideWithSlideAndFade(PenPalette);
                AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
                AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
                AnimationsHelper.HideWithSlideAndFade(BorderTools);
                AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
                AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
            }
            else
            {
                AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
                AnimationsHelper.HideWithSlideAndFade(BorderTools);
                AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                AnimationsHelper.HideWithSlideAndFade(PenPalette);
                AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
                AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
                AnimationsHelper.HideWithSlideAndFade(BorderTools);
                AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                AnimationsHelper.ShowWithSlideFromBottomAndFade(TwoFingerGestureBorder);
                AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardTwoFingerGestureBorder);
            }
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
                EnableTwoFingerGestureBtn.Source =
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
                    EnableTwoFingerGestureBtn.Source =
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
                    EnableTwoFingerGestureBtn.Source =
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
        private Point pos = new Point();
        private Point downPos = new Point();
        private Point pointDesktop = new Point(-1, -1); //用于记录上次在桌面时的坐标
        private Point pointPPT = new Point(-1, -1); //用于记录上次在PPT中的坐标

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

        #region 隱藏子面板和按鈕背景高亮

        /// <summary>
        ///     <c>HideSubPanels</c>的青春版。目前需要修改<c>BorderSettings</c>的關閉機制（改為動畫關閉）。
        /// </summary>
        private void HideSubPanelsImmediately()
        {
            BorderTools.Visibility = Visibility.Collapsed;
            BoardBorderTools.Visibility = Visibility.Collapsed;
            PenPalette.Visibility = Visibility.Collapsed;
            BoardPenPalette.Visibility = Visibility.Collapsed;
            BoardEraserSizePanel.Visibility = Visibility.Collapsed;
            EraserSizePanel.Visibility = Visibility.Collapsed;
            BoardBorderLeftPageListView.Visibility = Visibility.Collapsed;
            BoardBorderRightPageListView.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        ///     <para>
        ///         易嚴定真，這個多功能函數包括了以下的內容：
        ///     </para>
        ///     <list type="number">
        ///         <item>
        ///             隱藏浮動工具欄和白板模式下的“更多功能”面板
        ///         </item>
        ///         <item>
        ///             隱藏白板模式下和浮動工具欄的畫筆調色盤
        ///         </item>
        ///         <item>
        ///             隱藏白板模式下的“清屏”按鈕（已作廢）
        ///         </item>
        ///         <item>
        ///             負責給Settings設置面板做隱藏動畫
        ///         </item>
        ///         <item>
        ///             隱藏白板模式下和浮動工具欄的“手勢”面板
        ///         </item>
        ///         <item>
        ///             當<c>ToggleSwitchDrawShapeBorderAutoHide</c>開啟時，會自動隱藏白板模式下和浮動工具欄的“形狀”面板
        ///         </item>
        ///         <item>
        ///             按需高亮指定的浮動工具欄和白板工具欄中的按鈕，通過param：<paramref name="mode"/> 來指定
        ///         </item>
        ///         <item>
        ///             將浮動工具欄自動居中，通過param：<paramref name="autoAlignCenter"/>
        ///         </item>
        ///     </list>
        /// </summary>
        /// <param name="mode">
        ///     <para>
        ///         按需高亮指定的浮動工具欄和白板工具欄中的按鈕，有下面幾種情況：
        ///     </para>
        ///     <list type="number">
        ///         <item>
        ///             當<c><paramref name="mode"/>==null</c>時，不會執行任何有關操作
        ///         </item>
        ///         <item>
        ///             當<c><paramref name="mode"/>!="clear"</c>時，會先取消高亮所有工具欄按鈕，然後根據下面的情況進行高亮處理
        ///         </item>
        ///         <item>
        ///             當<c><paramref name="mode"/>=="color" || <paramref name="mode"/>=="pen"</c>時，會高亮浮動工具欄和白板工具欄中的“批註”，“筆”按鈕
        ///         </item>
        ///         <item>
        ///             當<c><paramref name="mode"/>=="eraser"</c>時，會高亮白板工具欄中的“橡皮”和浮動工具欄中的“面積擦”按鈕
        ///         </item>
        ///         <item>
        ///             當<c><paramref name="mode"/>=="eraserByStrokes"</c>時，會高亮白板工具欄中的“橡皮”和浮動工具欄中的“墨跡擦”按鈕
        ///         </item>
        ///         <item>
        ///             當<c><paramref name="mode"/>=="select"</c>時，會高亮浮動工具欄和白板工具欄中的“選擇”，“套索選”按鈕
        ///         </item>
        ///     </list>
        /// </param>
        /// <param name="autoAlignCenter">
        ///     是否自動居中浮動工具欄
        /// </param>
        private async void HideSubPanels(string? mode = null, bool autoAlignCenter = false)
        {
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
            AnimationsHelper.HideWithSlideAndFade(PenPalette);
            AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
            AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
            AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderLeftPageListView);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderRightPageListView);

            AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
            AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
            AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);

            if (mode != null)
            {
                if (mode != "clear")
                {
                    CursorIconGeometry.Brush = new SolidColorBrush(Color.FromRgb(27, 27, 27));
                    CursorIconGeometry.Geometry = Geometry.Parse(XamlGraphicsIconGeometries.LinedCursorIcon);
                    PenIconGeometry.Brush = new SolidColorBrush(Color.FromRgb(27, 27, 27));
                    PenIconGeometry.Geometry = Geometry.Parse(XamlGraphicsIconGeometries.LinedPenIcon);
                    StrokeEraserIconGeometry.Brush = new SolidColorBrush(Color.FromRgb(27, 27, 27));
                    StrokeEraserIconGeometry.Geometry =
                        Geometry.Parse(XamlGraphicsIconGeometries.LinedEraserStrokeIcon);
                    CircleEraserIconGeometry.Brush = new SolidColorBrush(Color.FromRgb(27, 27, 27));
                    CircleEraserIconGeometry.Geometry =
                        Geometry.Parse(XamlGraphicsIconGeometries.LinedEraserCircleIcon);
                    LassoSelectIconGeometry.Brush = new SolidColorBrush(Color.FromRgb(27, 27, 27));
                    LassoSelectIconGeometry.Geometry = Geometry.Parse(XamlGraphicsIconGeometries.LinedLassoSelectIcon);

                    BoardPen.Background = new SolidColorBrush(Color.FromRgb(244, 244, 245));
                    BoardSelect.Background = new SolidColorBrush(Color.FromRgb(244, 244, 245));
                    BoardEraser.Background = new SolidColorBrush(Color.FromRgb(244, 244, 245));
                    BoardSelectGeometry.Brush = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                    BoardPenGeometry.Brush = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                    BoardEraserGeometry.Brush = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                    BoardPenLabel.Foreground = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                    BoardSelectLabel.Foreground = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                    BoardEraserLabel.Foreground = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                    BoardSelect.BorderBrush = new SolidColorBrush(Color.FromRgb(161, 161, 170));
                    BoardEraser.BorderBrush = new SolidColorBrush(Color.FromRgb(161, 161, 170));
                    BoardPen.BorderBrush = new SolidColorBrush(Color.FromRgb(161, 161, 170));

                    FloatingbarSelectionBG.Visibility = Visibility.Hidden;
                    System.Windows.Controls.Canvas.SetLeft(FloatingbarSelectionBG, 0);
                }

                switch (mode)
                {
                    case "pen":
                    case "color":
                        {
                            PenIconGeometry.Brush = new SolidColorBrush(Color.FromRgb(30, 58, 138));
                            PenIconGeometry.Geometry = Geometry.Parse(XamlGraphicsIconGeometries.SolidPenIcon);
                            BoardPen.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                            BoardPen.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                            BoardPenGeometry.Brush = new SolidColorBrush(Colors.GhostWhite);
                            BoardPenLabel.Foreground = new SolidColorBrush(Colors.GhostWhite);

                            FloatingbarSelectionBG.Visibility = Visibility.Visible;
                            System.Windows.Controls.Canvas.SetLeft(FloatingbarSelectionBG, 28);
                            break;
                        }
                    case "eraser":
                        {
                            CircleEraserIconGeometry.Brush = new SolidColorBrush(Color.FromRgb(30, 58, 138));
                            CircleEraserIconGeometry.Geometry =
                                Geometry.Parse(XamlGraphicsIconGeometries.SolidEraserCircleIcon);
                            BoardEraser.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                            BoardEraser.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                            BoardEraserGeometry.Brush = new SolidColorBrush(Colors.GhostWhite);
                            BoardEraserLabel.Foreground = new SolidColorBrush(Colors.GhostWhite);

                            FloatingbarSelectionBG.Visibility = Visibility.Visible;
                            System.Windows.Controls.Canvas.SetLeft(FloatingbarSelectionBG, 28 * 3);
                            break;
                        }
                    case "eraserByStrokes":
                        {
                            StrokeEraserIconGeometry.Brush = new SolidColorBrush(Color.FromRgb(30, 58, 138));
                            StrokeEraserIconGeometry.Geometry =
                                Geometry.Parse(XamlGraphicsIconGeometries.SolidEraserStrokeIcon);
                            BoardEraser.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                            BoardEraser.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                            BoardEraserGeometry.Brush = new SolidColorBrush(Colors.GhostWhite);
                            BoardEraserLabel.Foreground = new SolidColorBrush(Colors.GhostWhite);

                            FloatingbarSelectionBG.Visibility = Visibility.Visible;
                            System.Windows.Controls.Canvas.SetLeft(FloatingbarSelectionBG, 28 * 4);
                            break;
                        }
                    case "select":
                        {
                            LassoSelectIconGeometry.Brush = new SolidColorBrush(Color.FromRgb(30, 58, 138));
                            LassoSelectIconGeometry.Geometry =
                                Geometry.Parse(XamlGraphicsIconGeometries.SolidLassoSelectIcon);
                            BoardSelect.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                            BoardSelect.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                            BoardSelectGeometry.Brush = new SolidColorBrush(Colors.GhostWhite);
                            BoardSelectLabel.Foreground = new SolidColorBrush(Colors.GhostWhite);

                            FloatingbarSelectionBG.Visibility = Visibility.Visible;
                            System.Windows.Controls.Canvas.SetLeft(FloatingbarSelectionBG, 28 * 5);
                            break;
                        }
                }


                if (autoAlignCenter) // 控制居中
                {
                    if (_powerPointService.IsInSlideShow)
                    {
                        await Task.Delay(50);
                        ViewboxFloatingBarMarginAnimation(60);
                    }
                    else if (Topmost == true) //非黑板
                    {
                        await Task.Delay(50);
                        ViewboxFloatingBarMarginAnimation(100, true);
                    }
                    else //黑板
                    {
                        await Task.Delay(50);
                        ViewboxFloatingBarMarginAnimation(60);
                    }
                }
            }

            await Task.Delay(150);
            isHidingSubPanelsWhenInking = false;
        }

        #endregion

        #region 撤銷重做按鈕
        private void SymbolIconUndo_MouseUp(object? sender, MouseButtonEventArgs? e)
        {
            //if (lastBorderMouseDownObject != sender) return;

            if (lastBorderMouseDownObject != null && lastBorderMouseDownObject is Panel)
                ((Panel)lastBorderMouseDownObject).Background = new SolidColorBrush(Colors.Transparent);
            if (sender == SymbolIconUndo && lastBorderMouseDownObject != SymbolIconUndo) return;

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

        private void SymbolIconRedo_MouseUp(object? sender, MouseButtonEventArgs? e)
        {
            //if (lastBorderMouseDownObject != sender) return;

            if (lastBorderMouseDownObject != null && lastBorderMouseDownObject is Panel)
                ((Panel)lastBorderMouseDownObject).Background = new SolidColorBrush(Colors.Transparent);
            if (sender == SymbolIconRedo && lastBorderMouseDownObject != SymbolIconRedo) return;

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

        private bool isDisplayingOrHidingBlackboard = false;

        private void ImageBlackboard_MouseUp(object? sender, MouseButtonEventArgs? e)
        {
            if (lastBorderMouseDownObject != null && lastBorderMouseDownObject is Panel)
                ((Panel)lastBorderMouseDownObject).Background = new SolidColorBrush(Colors.Transparent);
            if (sender == WhiteboardFloatingBarBtn && lastBorderMouseDownObject != WhiteboardFloatingBarBtn) return;

            if (isDisplayingOrHidingBlackboard) return;
            isDisplayingOrHidingBlackboard = true;

            if (_viewModel.AppMode == AppMode.Normal)
            {
                OpenWhiteboard();
            }
            else
            {
                CloseWhiteboard();
            }

            new Thread(() =>
            {
                Thread.Sleep(200);
                Application.Current.Dispatcher.Invoke(() => { isDisplayingOrHidingBlackboard = false; });
            }).Start();
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
            
            CursorIcon_Click(null, null);

            SwitchToDefaultPen(null, null);
            CheckColorTheme(true);
        }

        /// <summary>
        /// 切换到白板模式（显示黑板/白板UI）
        /// </summary>
        private void SwitchToWhiteboardMode()
        {
            _viewModel.AppMode = AppMode.WhiteBoard;

            GridBackgroundCover.Visibility = Visibility.Visible;
            AnimationsHelper.ShowWithSlideFromBottomAndFade(BlackboardLeftSide);
            AnimationsHelper.ShowWithSlideFromBottomAndFade(BlackboardCenterSide);
            AnimationsHelper.ShowWithSlideFromBottomAndFade(BlackboardRightSide);

            SaveStrokes(true);
            ClearStrokes(true);
            RestoreStrokes();

            // 根据设置选择黑板或白板颜色
            if (Settings.UsingWhiteboard)
                BtnColorBlack_Click(null, null);
            else
                BtnColorWhite_Click(null, null);

            Topmost = false;
        }

        /// <summary>
        /// 切换到屏幕模式（隐藏黑板/白板UI）
        /// </summary>
        private void SwitchToScreenMode()
        {
            _viewModel.AppMode = AppMode.Normal;

            GridBackgroundCover.Visibility = Visibility.Collapsed;
            AnimationsHelper.HideWithSlideAndFade(BlackboardLeftSide);
            AnimationsHelper.HideWithSlideAndFade(BlackboardCenterSide);
            AnimationsHelper.HideWithSlideAndFade(BlackboardRightSide);

            SaveStrokes();
            ClearStrokes(true);
            RestoreStrokes(true);

            Topmost = true;
        }

        /// <summary>
        /// 显示墨迹画布（从透明游标模式切换到可见画布）
        /// </summary>
        private void ShowInkCanvas()
        {
            GridTransparencyFakeBackground.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
            inkCanvas.IsHitTestVisible = true;
            inkCanvas.Visibility = Visibility.Visible;
            GridBackgroundCoverHolder.Visibility = Visibility.Visible;
            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 隐藏墨迹画布（切换到透明游标模式）
        /// </summary>
        private void HideInkCanvas()
        {
            inkCanvas.IsHitTestVisible = true;
            inkCanvas.Visibility = Visibility.Visible;
            GridTransparencyFakeBackground.Background = null;
            GridBackgroundCoverHolder.Visibility = Visibility.Collapsed;

            if (_viewModel.AppMode == AppMode.WhiteBoard)
            {
                SaveStrokes();
                RestoreStrokes(true);
            }
        }

        #endregion

        #region 清空畫布按鈕

        private void SymbolIconDelete_MouseUp(object? sender, MouseButtonEventArgs? e)
        {

            if (lastBorderMouseDownObject != null && lastBorderMouseDownObject is Panel)
                ((Panel)lastBorderMouseDownObject).Background = new SolidColorBrush(Colors.Transparent);
            if (sender == SymbolIconDelete && lastBorderMouseDownObject != SymbolIconDelete) return;

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
                    if (_powerPointService.IsInSlideShow)
                        SaveScreenShot(true, $"{_pptName}/{_previousSlideID}_{DateTime.Now:HH-mm-ss}");
                    else
                        SaveScreenShot(true);
                }

                BtnClear_Click(null, null);
            }
        }

        #endregion

        #region 主要的工具按鈕事件

        /// <summary>
        ///     浮動工具欄的“套索選”按鈕事件，重定向到舊UI的<c>BtnSelect_Click</c>方法
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">MouseButtonEventArgs</param>
        private void SymbolIconSelect_MouseUp(object sender, MouseButtonEventArgs e)
        {

            if (lastBorderMouseDownObject != null && lastBorderMouseDownObject is Panel)
                ((Panel)lastBorderMouseDownObject).Background = new SolidColorBrush(Colors.Transparent);
            if (sender == SymbolIconSelect && lastBorderMouseDownObject != SymbolIconSelect) return;

            FloatingbarSelectionBG.Visibility = Visibility.Visible;
            System.Windows.Controls.Canvas.SetLeft(FloatingbarSelectionBG, 140);

            //BtnSelect_Click
            forceEraser = true;
            inkCanvas.IsManipulationEnabled = false;
            if (inkCanvas.EditingMode == InkCanvasEditingMode.Select)
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
            else
            {
                inkCanvas.EditingMode = InkCanvasEditingMode.Select;
            }

            HideSubPanels("select");
        }

        #endregion

        private void FloatingBarToolBtnMouseDownFeedback_Panel(object sender, MouseButtonEventArgs e)
        {
            var s = (Panel)sender;
            lastBorderMouseDownObject = sender;
            if (s == SymbolIconDelete) s.Background = new SolidColorBrush(Color.FromArgb(28, 127, 29, 29));
            else s.Background = new SolidColorBrush(Color.FromArgb(28, 24, 24, 27));
        }

        private void FloatingBarToolBtnMouseLeaveFeedback_Panel(object sender, MouseEventArgs e)
        {
            var s = (Panel)sender;
            lastBorderMouseDownObject = null;
            s.Background = new SolidColorBrush(Colors.Transparent);
        }

        private void SymbolIconSettings_Click(object sender, RoutedEventArgs e)
        {
            HideSubPanels();
            _viewModel.IsSettingsPanelVisible = true;
            SettingsPanelScrollViewer.ScrollToTop();
        }

        private async void SymbolIconScreenshot_MouseUp(object sender, MouseButtonEventArgs e)
        {
            HideSubPanelsImmediately();
            await Task.Delay(50);
            SaveScreenShotToDesktop();
        }



        private void ImageCountdownTimer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);

            new CountdownTimerWindow().Show();
        }



        private void SymbolIconRand_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);

            App.GetService<RandWindow>().Show();
        }

        public void CheckEraserTypeTab()
        {
            if (Settings.EraserShapeType == 0)
            {
                CircleEraserTabButton.Background = new SolidColorBrush(Color.FromArgb(85, 59, 130, 246));
                CircleEraserTabButton.Opacity = 1;
                CircleEraserTabButtonText.FontWeight = FontWeights.Bold;
                CircleEraserTabButtonText.Margin = new Thickness(2, 0.5, 0, 0);
                CircleEraserTabButtonText.FontSize = 9.5;
                CircleEraserTabButtonIndicator.Visibility = Visibility.Visible;
                RectangleEraserTabButton.Background = new SolidColorBrush(Colors.Transparent);
                RectangleEraserTabButton.Opacity = 0.75;
                RectangleEraserTabButtonText.FontWeight = FontWeights.Normal;
                RectangleEraserTabButtonText.FontSize = 9;
                RectangleEraserTabButtonText.Margin = new Thickness(2, 1, 0, 0);
                RectangleEraserTabButtonIndicator.Visibility = Visibility.Collapsed;

                BoardCircleEraserTabButton.Background = new SolidColorBrush(Color.FromArgb(85, 59, 130, 246));
                BoardCircleEraserTabButton.Opacity = 1;
                BoardCircleEraserTabButtonText.FontWeight = FontWeights.Bold;
                BoardCircleEraserTabButtonText.Margin = new Thickness(2, 0.5, 0, 0);
                BoardCircleEraserTabButtonText.FontSize = 9.5;
                BoardCircleEraserTabButtonIndicator.Visibility = Visibility.Visible;
                BoardRectangleEraserTabButton.Background = new SolidColorBrush(Colors.Transparent);
                BoardRectangleEraserTabButton.Opacity = 0.75;
                BoardRectangleEraserTabButtonText.FontWeight = FontWeights.Normal;
                BoardRectangleEraserTabButtonText.FontSize = 9;
                BoardRectangleEraserTabButtonText.Margin = new Thickness(2, 1, 0, 0);
                BoardRectangleEraserTabButtonIndicator.Visibility = Visibility.Collapsed;
            }
            else
            {
                RectangleEraserTabButton.Background = new SolidColorBrush(Color.FromArgb(85, 59, 130, 246));
                RectangleEraserTabButton.Opacity = 1;
                RectangleEraserTabButtonText.FontWeight = FontWeights.Bold;
                RectangleEraserTabButtonText.Margin = new Thickness(2, 0.5, 0, 0);
                RectangleEraserTabButtonText.FontSize = 9.5;
                RectangleEraserTabButtonIndicator.Visibility = Visibility.Visible;
                CircleEraserTabButton.Background = new SolidColorBrush(Colors.Transparent);
                CircleEraserTabButton.Opacity = 0.75;
                CircleEraserTabButtonText.FontWeight = FontWeights.Normal;
                CircleEraserTabButtonText.FontSize = 9;
                CircleEraserTabButtonText.Margin = new Thickness(2, 1, 0, 0);
                CircleEraserTabButtonIndicator.Visibility = Visibility.Collapsed;

                BoardRectangleEraserTabButton.Background = new SolidColorBrush(Color.FromArgb(85, 59, 130, 246));
                BoardRectangleEraserTabButton.Opacity = 1;
                BoardRectangleEraserTabButtonText.FontWeight = FontWeights.Bold;
                BoardRectangleEraserTabButtonText.Margin = new Thickness(2, 0.5, 0, 0);
                BoardRectangleEraserTabButtonText.FontSize = 9.5;
                BoardRectangleEraserTabButtonIndicator.Visibility = Visibility.Visible;
                BoardCircleEraserTabButton.Background = new SolidColorBrush(Colors.Transparent);
                BoardCircleEraserTabButton.Opacity = 0.75;
                BoardCircleEraserTabButtonText.FontWeight = FontWeights.Normal;
                BoardCircleEraserTabButtonText.FontSize = 9;
                BoardCircleEraserTabButtonText.Margin = new Thickness(2, 1, 0, 0);
                BoardCircleEraserTabButtonIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private void SymbolIconRandOne_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);

            var randWindow = App.GetService<RandWindow>();
            randWindow.IsAutoClose = true;
            randWindow.ShowDialog();
        }

        private void GridInkReplayButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);

            InkCanvasForInkReplay.Visibility = Visibility.Visible;
            InkCanvasGridForInkReplay.Visibility = Visibility.Hidden;
            InkCanvasGridForInkReplay.IsHitTestVisible = false;
            FloatingbarUIForInkReplay.Visibility = Visibility.Hidden;
            FloatingbarUIForInkReplay.IsHitTestVisible = false;
            BlackboardUIGridForInkReplay.Visibility = Visibility.Hidden;
            BlackboardUIGridForInkReplay.IsHitTestVisible = false;

            AnimationsHelper.ShowWithFadeIn(BorderInkReplayToolBox);
            InkReplayPanelStatusText.Text = "正在重播墨迹...";
            InkReplayPlayPauseBorder.Background = new SolidColorBrush(Colors.Transparent);
            InkReplayPlayButtonImage.Visibility = Visibility.Collapsed;
            InkReplayPauseButtonImage.Visibility = Visibility.Visible;

            isStopInkReplay = false;
            isPauseInkReplay = false;
            isRestartInkReplay = false;
            inkReplaySpeed = 1;
            InkCanvasForInkReplay.Strokes.Clear();
            var strokes = inkCanvas.Strokes.Clone();
            if (inkCanvas.GetSelectedStrokes().Count != 0) strokes = inkCanvas.GetSelectedStrokes().Clone();
            int k = 1, i = 0;
            new Thread(() =>
            {
                isRestartInkReplay = true;
                while (isRestartInkReplay)
                {
                    isRestartInkReplay = false;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        InkCanvasForInkReplay.Strokes.Clear();
                    });
                    foreach (var stroke in strokes)
                    {

                        if (isRestartInkReplay) break;

                        var stylusPoints = new StylusPointCollection();
                        if (stroke.StylusPoints.Count == 629) //圆或椭圆
                        {
                            Stroke? s = null;
                            foreach (var stylusPoint in stroke.StylusPoints)
                            {

                                if (isRestartInkReplay) break;

                                while (isPauseInkReplay)
                                {
                                    Thread.Sleep(10);
                                }

                                if (i++ >= 50)
                                {
                                    i = 0;
                                    Thread.Sleep((int)(10 / inkReplaySpeed));
                                    if (isStopInkReplay) return;
                                }

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    try
                                    {
                                        InkCanvasForInkReplay.Strokes.Remove(s);
                                    }
                                    catch { }

                                    stylusPoints.Add(stylusPoint);
                                    s = new Stroke(stylusPoints.Clone());
                                    s.DrawingAttributes = stroke.DrawingAttributes;
                                    InkCanvasForInkReplay.Strokes.Add(s);
                                });
                            }
                        }
                        else
                        {
                            Stroke? s = null;
                            foreach (var stylusPoint in stroke.StylusPoints)
                            {

                                if (isRestartInkReplay) break;

                                while (isPauseInkReplay)
                                {
                                    Thread.Sleep(10);
                                }

                                if (i++ >= k)
                                {
                                    i = 0;
                                    Thread.Sleep((int)(10 / inkReplaySpeed));
                                    if (isStopInkReplay) return;
                                }

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    try
                                    {
                                        InkCanvasForInkReplay.Strokes.Remove(s);
                                    }
                                    catch { }

                                    stylusPoints.Add(stylusPoint);
                                    s = new Stroke(stylusPoints.Clone());
                                    s.DrawingAttributes = stroke.DrawingAttributes;
                                    InkCanvasForInkReplay.Strokes.Add(s);
                                });
                            }
                        }
                    }
                }

                Thread.Sleep(100);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    InkCanvasForInkReplay.Visibility = Visibility.Collapsed;
                    InkCanvasGridForInkReplay.Visibility = Visibility.Visible;
                    InkCanvasGridForInkReplay.IsHitTestVisible = true;
                    AnimationsHelper.HideWithFadeOut(BorderInkReplayToolBox);
                    FloatingbarUIForInkReplay.Visibility = Visibility.Visible;
                    FloatingbarUIForInkReplay.IsHitTestVisible = true;
                    BlackboardUIGridForInkReplay.Visibility = Visibility.Visible;
                    BlackboardUIGridForInkReplay.IsHitTestVisible = true;
                });
            }).Start();
        }

        private bool isStopInkReplay = false;
        private bool isPauseInkReplay = false;
        private bool isRestartInkReplay = false;
        private double inkReplaySpeed = 1;

        private void InkCanvasForInkReplay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                InkCanvasForInkReplay.Visibility = Visibility.Collapsed;
                InkCanvasGridForInkReplay.Visibility = Visibility.Visible;
                InkCanvasGridForInkReplay.IsHitTestVisible = true;
                FloatingbarUIForInkReplay.Visibility = Visibility.Visible;
                FloatingbarUIForInkReplay.IsHitTestVisible = true;
                BlackboardUIGridForInkReplay.Visibility = Visibility.Visible;
                BlackboardUIGridForInkReplay.IsHitTestVisible = true;
                AnimationsHelper.HideWithFadeOut(BorderInkReplayToolBox);
                isStopInkReplay = true;
            }
        }

        private void InkReplayPlayPauseBorder_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            InkReplayPlayPauseBorder.Background = new SolidColorBrush(Color.FromArgb(34, 9, 9, 11));
        }

        private void InkReplayPlayPauseBorder_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            InkReplayPlayPauseBorder.Background = new SolidColorBrush(Colors.Transparent);
            isPauseInkReplay = !isPauseInkReplay;
            InkReplayPanelStatusText.Text = isPauseInkReplay ? "已暂停！" : "正在重播墨迹...";
            InkReplayPlayButtonImage.Visibility = isPauseInkReplay ? Visibility.Visible : Visibility.Collapsed;
            InkReplayPauseButtonImage.Visibility = !isPauseInkReplay ? Visibility.Visible : Visibility.Collapsed;
        }

        private void InkReplayStopButtonBorder_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            InkReplayStopButtonBorder.Background = new SolidColorBrush(Color.FromArgb(34, 9, 9, 11));
        }

        private void InkReplayStopButtonBorder_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            InkReplayStopButtonBorder.Background = new SolidColorBrush(Colors.Transparent);
            InkCanvasForInkReplay.Visibility = Visibility.Collapsed;
            InkCanvasGridForInkReplay.Visibility = Visibility.Visible;
            InkCanvasGridForInkReplay.IsHitTestVisible = true;
            FloatingbarUIForInkReplay.Visibility = Visibility.Visible;
            FloatingbarUIForInkReplay.IsHitTestVisible = true;
            BlackboardUIGridForInkReplay.Visibility = Visibility.Visible;
            BlackboardUIGridForInkReplay.IsHitTestVisible = true;
            AnimationsHelper.HideWithFadeOut(BorderInkReplayToolBox);
            isStopInkReplay = true;
        }

        private void InkReplayReplayButtonBorder_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            InkReplayReplayButtonBorder.Background = new SolidColorBrush(Color.FromArgb(34, 9, 9, 11));
        }

        private void InkReplayReplayButtonBorder_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            InkReplayReplayButtonBorder.Background = new SolidColorBrush(Colors.Transparent);
            isRestartInkReplay = true;
            isPauseInkReplay = false;
            InkReplayPanelStatusText.Text = "正在重播墨迹...";
            InkReplayPlayButtonImage.Visibility = Visibility.Collapsed;
            InkReplayPauseButtonImage.Visibility = Visibility.Visible;
        }

        private void InkReplaySpeedButtonBorder_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            InkReplaySpeedButtonBorder.Background = new SolidColorBrush(Color.FromArgb(34, 9, 9, 11));
        }

        private void InkReplaySpeedButtonBorder_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            InkReplaySpeedButtonBorder.Background = new SolidColorBrush(Colors.Transparent);
            inkReplaySpeed = inkReplaySpeed == 0.5 ? 1 :
                inkReplaySpeed == 1 ? 2 :
                inkReplaySpeed == 2 ? 4 :
                inkReplaySpeed == 4 ? 8 : 0.5;
            InkReplaySpeedTextBlock.Text = inkReplaySpeed + "x";
        }

        private void SymbolIconTools_MouseUp(object sender, MouseButtonEventArgs e)
        {

            if (lastBorderMouseDownObject != null && lastBorderMouseDownObject is Panel)
                ((Panel)lastBorderMouseDownObject).Background = new SolidColorBrush(Colors.Transparent);
            if (sender == ToolsFloatingBarBtn && lastBorderMouseDownObject != ToolsFloatingBarBtn) return;

            if (BorderTools.Visibility == Visibility.Visible)
            {
                AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
                AnimationsHelper.HideWithSlideAndFade(BorderTools);
                AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                AnimationsHelper.HideWithSlideAndFade(PenPalette);
                AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
                AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
                AnimationsHelper.HideWithSlideAndFade(BorderTools);
                AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
                AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
            }
            else
            {
                AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
                AnimationsHelper.HideWithSlideAndFade(BorderTools);
                AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                AnimationsHelper.HideWithSlideAndFade(PenPalette);
                AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
                AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
                AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
                AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
                AnimationsHelper.ShowWithSlideFromBottomAndFade(BorderTools);
                AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardBorderTools);
            }
        }

        private bool isViewboxFloatingBarMarginAnimationRunning = false;

        public async void ViewboxFloatingBarMarginAnimation(int MarginFromEdge,
            bool PosXCaculatedWithTaskbarHeight = false)
        {
            if (MarginFromEdge == 60) MarginFromEdge = 55;
            await Dispatcher.InvokeAsync(() =>
            {
                if (Topmost == false)
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
                if (Topmost == false) ViewboxFloatingBar.Visibility = Visibility.Hidden;
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

        private void CursorIcon_Click(object? sender, RoutedEventArgs? e)
        {
            if (lastBorderMouseDownObject != null && lastBorderMouseDownObject is Panel)
                ((Panel)lastBorderMouseDownObject).Background = new SolidColorBrush(Colors.Transparent);
            if (sender == Cursor_Icon && lastBorderMouseDownObject != Cursor_Icon) return;
            // 隱藏高亮
            FloatingbarSelectionBG.Visibility = Visibility.Hidden;
            System.Windows.Controls.Canvas.SetLeft(FloatingbarSelectionBG, 0);

            // 切换前自动截图保存墨迹
            if (inkCanvas.Strokes.Count > 0 &&
                inkCanvas.Strokes.Count > Settings.MinimumAutomationStrokeNumber)
            {
                if (_powerPointService.IsInSlideShow)
                    SaveScreenShot(true, $"{_pptName}/{_previousSlideID}_{DateTime.Now:HH-mm-ss}");
                else SaveScreenShot(true);
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

            //GridBackgroundCoverHolder.Visibility = Visibility.Collapsed;
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
                HideSubPanels("cursor", true);

                if (_powerPointService.IsInSlideShow)
                    ViewboxFloatingBarMarginAnimation(60);
                else
                    ViewboxFloatingBarMarginAnimation(100, true);
            }
        }

        private void PenIcon_Click(object? sender, RoutedEventArgs? e)
        {

            if (lastBorderMouseDownObject != null && lastBorderMouseDownObject is Panel)
                ((Panel)lastBorderMouseDownObject).Background = new SolidColorBrush(Colors.Transparent);
            if (sender == Pen_Icon && lastBorderMouseDownObject != Pen_Icon) return;

            FloatingbarSelectionBG.Visibility = Visibility.Visible;
            System.Windows.Controls.Canvas.SetLeft(FloatingbarSelectionBG, 28);

            if (Pen_Icon.Background == null || StackPanelCanvasControls.Visibility == Visibility.Collapsed)
            {
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;

                GridTransparencyFakeBackground.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));

                inkCanvas.IsHitTestVisible = true;
                inkCanvas.Visibility = Visibility.Visible;

                GridBackgroundCoverHolder.Visibility = Visibility.Visible;
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

                /*if (forceEraser && _viewModel.AppMode == AppMode.Normal)
                    BtnColorRed_Click(sender, null);*/

                StackPanelCanvasControls.Visibility = Visibility.Visible;
                //AnimationsHelper.ShowWithSlideFromLeftAndFade(StackPanelCanvasControls);
                CheckEnableTwoFingerGestureBtnVisibility(true);
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                ColorSwitchCheck();
                HideSubPanels("pen", true);
            }
            else
            {
                if (inkCanvas.EditingMode == InkCanvasEditingMode.Ink)
                {
                    if (PenPalette.Visibility == Visibility.Visible)
                    {
                        AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
                        AnimationsHelper.HideWithSlideAndFade(BorderTools);
                        AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                        AnimationsHelper.HideWithSlideAndFade(PenPalette);
                        AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
                        AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
                        AnimationsHelper.HideWithSlideAndFade(BorderTools);
                        AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                        AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
                        AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
                    }
                    else
                    {
                        AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
                        AnimationsHelper.HideWithSlideAndFade(BorderTools);
                        AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                        AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
                        AnimationsHelper.HideWithSlideAndFade(BorderTools);
                        AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                        AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
                        AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
                        AnimationsHelper.ShowWithSlideFromBottomAndFade(PenPalette);
                        AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardPenPalette);
                    }
                }
                else
                {
                    inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    ColorSwitchCheck();
                    HideSubPanels("pen", true);
                }
            }
        }

        private void ColorThemeSwitch_MouseUp(object sender, RoutedEventArgs e)
        {
            isUselightThemeColor = !isUselightThemeColor;
            if (_viewModel.AppMode == AppMode.Normal) isDesktopUselightThemeColor = isUselightThemeColor;
            CheckColorTheme();
        }

        private void EraserIcon_Click(object sender, RoutedEventArgs e)
        {

            if (lastBorderMouseDownObject != null && lastBorderMouseDownObject is Panel)
                ((Panel)lastBorderMouseDownObject).Background = new SolidColorBrush(Colors.Transparent);
            if (sender == Eraser_Icon && lastBorderMouseDownObject != Eraser_Icon) return;

            FloatingbarSelectionBG.Visibility = Visibility.Visible;
            System.Windows.Controls.Canvas.SetLeft(FloatingbarSelectionBG, 84);

            forceEraser = true;
            forcePointEraser = true;
            if (Settings.EraserShapeType == 0)
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

                inkCanvas.EraserShape = new EllipseStylusShape(k * 90, k * 90);
            }
            else if (Settings.EraserShapeType == 1)
            {
                double k = 1;
                switch (Settings.EraserSize)
                {
                    case 0:
                        k = 0.7;
                        break;
                    case 1:
                        k = 0.9;
                        break;
                    case 3:
                        k = 1.2;
                        break;
                    case 4:
                        k = 1.6;
                        break;
                }

                inkCanvas.EraserShape = new RectangleStylusShape(k * 90 * 0.6, k * 90);
            }

            if (inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint)
            {
                if (EraserSizePanel.Visibility == Visibility.Collapsed)
                {
                    AnimationsHelper.HideWithSlideAndFade(BorderTools);
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                    AnimationsHelper.HideWithSlideAndFade(PenPalette);
                    AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
                    AnimationsHelper.HideWithSlideAndFade(BorderTools);
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                    AnimationsHelper.ShowWithSlideFromBottomAndFade(EraserSizePanel);
                    AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardEraserSizePanel);
                }
                else
                {
                    AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
                    AnimationsHelper.HideWithSlideAndFade(BorderTools);
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                    AnimationsHelper.HideWithSlideAndFade(PenPalette);
                    AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
                    AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
                    AnimationsHelper.HideWithSlideAndFade(BorderTools);
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                    AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
                    AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
                }
            }
            else
            {
                HideSubPanels("eraser");
            }

            inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;

            inkCanvas_EditingModeChanged(inkCanvas, null);
            CancelSingleFingerDragMode();
        }

        private void EraserIconByStrokes_Click(object sender, RoutedEventArgs e)
        {

            if (lastBorderMouseDownObject != null && lastBorderMouseDownObject is Panel)
                ((Panel)lastBorderMouseDownObject).Background = new SolidColorBrush(Colors.Transparent);
            if (sender == EraserByStrokes_Icon && lastBorderMouseDownObject != EraserByStrokes_Icon) return;

            FloatingbarSelectionBG.Visibility = Visibility.Visible;
            System.Windows.Controls.Canvas.SetLeft(FloatingbarSelectionBG, 112);

            forceEraser = true;
            forcePointEraser = false;

            inkCanvas.EraserShape = new EllipseStylusShape(5, 5);
            inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;

            inkCanvas_EditingModeChanged(inkCanvas, null);
            CancelSingleFingerDragMode();

            HideSubPanels("eraserByStrokes");
        }

        private void CursorWithDelIcon_Click(object? sender, RoutedEventArgs? e)
        {

            if (lastBorderMouseDownObject != null && lastBorderMouseDownObject is Panel)
                ((Panel)lastBorderMouseDownObject).Background = new SolidColorBrush(Colors.Transparent);
            if (sender == CursorWithDelFloatingBarBtn && lastBorderMouseDownObject != CursorWithDelFloatingBarBtn) return;

            SymbolIconDelete_MouseUp(sender, null);
            CursorIcon_Click(null, null);
        }

        private void CloseBordertools_MouseUp(object sender, MouseButtonEventArgs e)
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

        private bool forceEraser = false;

        private void BtnClear_Click(object? sender, RoutedEventArgs? e)
        {
            forceEraser = false;
            //BorderClearInDelete.Visibility = Visibility.Collapsed;

            if (_viewModel.AppMode == AppMode.Normal)
            {
                // 先回到画笔再清屏，避免 TimeMachine 的相关 bug 影响
                if (Pen_Icon.Background == null && StackPanelCanvasControls.Visibility == Visibility.Visible)
                    PenIcon_Click(null, null);
            }
            else
            {
                if (Pen_Icon.Background == null) PenIcon_Click(null, null);
            }

            if (inkCanvas.Strokes.Count != 0)
            {
                var whiteboardIndex = _viewModel.WhiteboardCurrentPage;
                if (_viewModel.AppMode == AppMode.Normal) whiteboardIndex = 0;
                strokeCollections[whiteboardIndex] = inkCanvas.Strokes.Clone();
            }

            ClearStrokes(false);
            inkCanvas.Children.Clear();

            CancelSingleFingerDragMode();

            if (Settings.ClearCanvasAndClearTimeMachine) timeMachine.ClearStrokeHistory();
        }

        private void CancelSingleFingerDragMode()
        {
            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

            //isSingleFingerDragMode = false;
        }

        private int BoundsWidth = 5;

        private void BtnHideInkCanvas_Click(object? sender, RoutedEventArgs? e)
        {
            if (GridTransparencyFakeBackground.Background == null)
            {
                GridTransparencyFakeBackground.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
                inkCanvas.IsHitTestVisible = true;
                inkCanvas.Visibility = Visibility.Visible;

                GridBackgroundCoverHolder.Visibility = Visibility.Visible;

                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            }
            else
            {
                inkCanvas.IsHitTestVisible = true;
                inkCanvas.Visibility = Visibility.Visible;

                GridTransparencyFakeBackground.Background = null;

                GridBackgroundCoverHolder.Visibility = Visibility.Collapsed;

                if (_viewModel.AppMode == AppMode.WhiteBoard)
                {
                    SaveStrokes();
                    RestoreStrokes(true);
                }
            }

            if (GridTransparencyFakeBackground.Background == null)
            {
                StackPanelCanvasControls.Visibility = Visibility.Collapsed;
                CheckEnableTwoFingerGestureBtnVisibility(false);
                HideSubPanels("cursor");
            }
            else
            {
                AnimationsHelper.ShowWithSlideFromLeftAndFade(StackPanelCanvasControls);
                CheckEnableTwoFingerGestureBtnVisibility(true);
            }
        }
        #endregion
        #endregion

        #region Hotkeys
        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!_powerPointService.IsInSlideShow || _viewModel.AppMode == AppMode.WhiteBoard)
                return;
            if (e.Delta >= 120)
                PPTNavigationPanel_PreviousClick(null, null);
            else if (e.Delta <= -120)
                PPTNavigationPanel_NextClick(null, null);
        }

        private void Main_Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_powerPointService.IsInSlideShow || _viewModel.AppMode == AppMode.WhiteBoard)
                return;
            if (e.Key == Key.Down || e.Key == Key.PageDown || e.Key == Key.Right || e.Key == Key.N || e.Key == Key.Space)
                PPTNavigationPanel_NextClick(null, null);
            if (e.Key == Key.Up || e.Key == Key.PageUp || e.Key == Key.Left || e.Key == Key.P)
                PPTNavigationPanel_PreviousClick(null, null);
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
                SymbolIconUndo_MouseUp(lastBorderMouseDownObject, null);
            }
            catch { }
        }

        private void HotKey_Redo(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                SymbolIconRedo_MouseUp(lastBorderMouseDownObject, null);
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
        private int lastNotificationShowTime = 0;
        private int notificationShowTime = 2500;

        public static void ShowNewMessage(string notice, bool isShowImmediately = true)
        {
            (Application.Current?.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow)
                ?.ShowNotification(notice, isShowImmediately);
        }

        public void ShowNotification(string notice, bool isShowImmediately = true)
        {
            try
            {
                lastNotificationShowTime = Environment.TickCount;

                TextBlockNotice.Text = notice;
                AnimationsHelper.ShowWithSlideFromBottomAndFade(GridNotifications);

                new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(notificationShowTime + 300);
                    if (Environment.TickCount - lastNotificationShowTime >= notificationShowTime)
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            AnimationsHelper.HideWithSlideAndFade(GridNotifications);
                        });
                })).Start();
            }
            catch { }
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


        private void BlackBoardLeftSidePageListView_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            AnimationsHelper.HideWithSlideAndFade(BoardBorderLeftPageListView);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderRightPageListView);
            var item = BlackBoardLeftSidePageListView.SelectedItem;
            var index = BlackBoardLeftSidePageListView.SelectedIndex;
            if (item != null)
            {
                SaveStrokes();
                ClearStrokes(true);
                _viewModel.WhiteboardCurrentPage = index + 1;
                RestoreStrokes();
                BlackBoardLeftSidePageListView.SelectedIndex = index;
            }
        }

        private void BlackBoardRightSidePageListView_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            AnimationsHelper.HideWithSlideAndFade(BoardBorderLeftPageListView);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderRightPageListView);
            var item = BlackBoardRightSidePageListView.SelectedItem;
            var index = BlackBoardRightSidePageListView.SelectedIndex;
            if (item != null)
            {
                SaveStrokes();
                ClearStrokes(true);
                _viewModel.WhiteboardCurrentPage = index + 1;
                RestoreStrokes();
                BlackBoardRightSidePageListView.SelectedIndex = index;
            }
        }
        #endregion

        #region PPT
        private string? _pptName = null;
        private bool isEnteredSlideShowEndEvent = false;
        private int _previousSlideID = 1;
        private Dictionary<int, MemoryStream> _memoryStreams = new();

        private void TimerCheckPPT_Tick(object? sender, EventArgs e)
        {
            if (_powerPointService.IsConnected) return; // 如果已经连接，就什么都不做

            if (_powerPointService.TryConnectToPowerPoint())
            {
                // 连接成功！
                timerCheckPPT.Stop(); // 停止定时器
            }
        }

        private void PptApplication_PresentationClose(Presentation Pres)
        {
            timerCheckPPT.Start();
        }


        private async void PptApplication_SlideShowBegin(SlideShowWindow Wn)
        {
            if (Settings.IsAutoFoldInPPTSlideShow && _viewModel.IsFloatingBarVisible)
                await HideFloatingBar(new object());
            else if (!_viewModel.IsFloatingBarVisible)
                await ShowFloatingBar(new object());

            isStopInkReplay = true;

            Logger.LogInformation("幻灯片放映开始");

            // 清理之前的数据
            foreach (var stream in _memoryStreams.Values)
            {
                stream?.Dispose();
            }
            _memoryStreams.Clear();

            int slidescount = _powerPointService.CurrentPresentationSlideCount;
            string? pptName = _powerPointService.CurrentPresentationName;
            string strokePath = Settings.AutoSaveStrokesPath + @"\Auto Saved - Presentations\" +
                            pptName + "_" + slidescount;

            //任何情况下都清除现有墨迹
            ClearStrokes(true);

            //检查是否有已有墨迹，并加载
            if (Settings.IsAutoSaveStrokesInPowerPoint && Directory.Exists(strokePath))
            {
                Logger.LogInformation("检测到已有保存的墨迹，正在加载...");
                var files = new DirectoryInfo(strokePath).GetFiles();
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
                        Logger.LogWarning(ex, $"加载第 {i} 页墨迹失败");
                    }
                }
                // 加载当前页墨迹到 InkCanvas
                if (_memoryStreams.ContainsKey(_powerPointService.CurrentSlidePosition) && _memoryStreams[_powerPointService.CurrentSlidePosition] != null)
                {
                    try
                    {
                        _memoryStreams[_powerPointService.CurrentSlidePosition].Position = 0;
                        await Application.Current.Dispatcher.InvokeAsync(() => inkCanvas.Strokes.Add(new StrokeCollection(_memoryStreams[_powerPointService.CurrentSlidePosition])));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, $"加载墨迹到 InkCanvas 失败");
                    }
                }
                Logger.LogInformation($"加载完成，共 {count} 页");
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (_viewModel.AppMode == AppMode.WhiteBoard)
                    ImageBlackboard_MouseUp(null, null);

                if (Settings.IsShowCanvasAtNewSlideShow &&
                    !Settings.IsAutoFoldInPPTSlideShow &&
                    GridTransparencyFakeBackground.Background == null)
                {
                    PenIcon_Click(null, null);
                }
                //if (Settings.IsShowCanvasAtNewSlideShow &&
                //    !Settings.IsAutoFoldInPPTSlideShow)
                //    BtnColorRed_Click(null, null);

                isEnteredSlideShowEndEvent = false;
                if (_viewModel.IsFloatingBarVisible)
                {
                    ViewboxFloatingBarMarginAnimation(60);
                }
            });
        }

        private async void PptApplication_SlideShowEnd(Presentation Pres)
        {
            if (!_viewModel.IsFloatingBarVisible)
                await ShowFloatingBar(new object());
            Logger.LogInformation("幻灯片放映结束");

            if (isEnteredSlideShowEndEvent)
            {
                Logger.LogInformation("检测到之前已经进入过退出事件，返回");
                return;
            }

            isEnteredSlideShowEndEvent = true;

            if (Settings.IsAutoSaveStrokesInPowerPoint)
            {
                var folderPath = Settings.AutoSaveStrokesPath
                                 + @"\Auto Saved - Presentations\"
                                 + _powerPointService.CurrentPresentationName
                                 + "_"
                                 + _powerPointService.CurrentPresentationSlideCount;
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
                            byte[] allBytes = value.ToArray();
                            if (value.Length > 8)
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
                            Logger.LogWarning(ex, $"为第 {i} 页保存墨迹失败");
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
                CursorIcon_Click(null, null);

                ClearStrokes(true);

                ViewboxFloatingBarMarginAnimation(100, true);
            });
        }

        private void PptApplication_SlideShowNextSlide(SlideShowWindow Wn)
        {
            var currentPage = _powerPointService.CurrentSlidePosition;
            Logger.LogTrace($"幻灯片跳转到第 {currentPage} 页");

            if (currentPage == _previousSlideID)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var ms = new MemoryStream();
                inkCanvas.Strokes.Save(ms);
                ms.Position = 0;
                _memoryStreams[_previousSlideID] = ms;

                if (inkCanvas.Strokes.Count > Settings.MinimumAutomationStrokeNumber &&
                    Settings.IsAutoSaveScreenShotInPowerPoint && !_isPptClickingBtnTurned)
                    SaveScreenShot(true, _powerPointService.CurrentPresentationName + "/" + currentPage);
                _isPptClickingBtnTurned = false;

                ClearStrokes(true);
                timeMachine.ClearStrokeHistory();

                try
                {
                    if (_memoryStreams.ContainsKey(currentPage) && _memoryStreams[currentPage] != null)
                    {
                        _memoryStreams[currentPage].Position = 0;
                        inkCanvas.Strokes.Add(new StrokeCollection(_memoryStreams[currentPage]));
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, $"加载第 {currentPage} 页墨迹失败");
                }
            });
            _previousSlideID = currentPage;
        }

        private bool _isPptClickingBtnTurned = false;

        private void ImagePPTControlEnd_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _powerPointService.EndSlideShow();
        }

        #region New PPT Navigation Panel Event Handlers
        private void PPTNavigationPanel_PreviousClick(object? sender, RoutedEventArgs? e)
        {
            _isPptClickingBtnTurned = true;
            if (inkCanvas.Strokes.Count > Settings.MinimumAutomationStrokeNumber &&
                Settings.IsAutoSaveScreenShotInPowerPoint)
                SaveScreenShot(true,
                    $"{_powerPointService.CurrentPresentationName}/{_powerPointService.CurrentSlidePosition}");
            _powerPointService.GoToPreviousSlide();
        }

        private void PPTNavigationPanel_NextClick(object? sender, RoutedEventArgs? e)
        {
            _isPptClickingBtnTurned = true;
            if (inkCanvas.Strokes.Count > Settings.MinimumAutomationStrokeNumber &&
                Settings.IsAutoSaveScreenShotInPowerPoint)
                SaveScreenShot(true,
                    $"{_powerPointService.CurrentPresentationName}/{_powerPointService.CurrentSlidePosition}");

            _powerPointService.GoToNextSlide();
        }

        private void PPTNavigationPanel_PageClick(object sender, RoutedEventArgs e)
        {
            if (!Settings.EnablePPTButtonPageClickable)
            {
                return;
            }
            CursorIcon_Click(null, null);
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
        private void SymbolIconSaveStrokes_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender || inkCanvas.Visibility != Visibility.Visible) return;

            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);

            GridNotifications.Visibility = Visibility.Collapsed;

            SaveInkCanvasStrokes(true, true);
        }

        private void SaveInkCanvasStrokes(bool newNotice = true, bool saveByUser = false)
        {
            try
            {
                var savePath = Settings.AutoSaveStrokesPath
                               + (saveByUser ? @"\User Saved - " : @"\Auto Saved - ")
                               + (_viewModel.AppMode == AppMode.Normal ? "Annotation Strokes" : "BlackBoard Strokes");
                if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
                string savePathWithName;
                if (_viewModel.AppMode == AppMode.WhiteBoard) // 黑板模式下
                    savePathWithName = savePath + @"\" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff") + " Page-" +
                                       _viewModel.WhiteboardCurrentPage + " StrokesCount-" + inkCanvas.Strokes.Count + ".icstk";
                else
                    //savePathWithName = savePath + @"\" + DateTime.Now.ToString("u").Replace(':', '-') + ".icstk";
                    savePathWithName = savePath + @"\" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff") + ".icstk";
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

        private void SymbolIconOpenStrokes_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);

            var openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Settings.AutoSaveStrokesPath;
            openFileDialog.Title = "打开墨迹文件";
            openFileDialog.Filter = "Ink Canvas Strokes File (*.icstk)|*.icstk";
            if (openFileDialog.ShowDialog() != true) return;
            Logger.LogInformation($"用户选择打开墨迹文件 {openFileDialog.FileName}");
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
                        Logger.LogInformation($"墨迹文件打开成功，墨迹数 {strokes.Count}");
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
                        Logger.LogInformation($"墨迹文件打开成功，墨迹数 {strokes.Count}");
                    }
            }
            catch
            {
                ShowNotification("墨迹打开失败");
            }
        }
        #endregion

        #region Screenshot
        private void SaveScreenShot(bool isHideNotification, string? fileName = null)
        {
            var rc = System.Windows.Forms.SystemInformation.VirtualScreen;
            var bitmap = new System.Drawing.Bitmap(rc.Width, rc.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var memoryGrahics = System.Drawing.Graphics.FromImage(bitmap))
            {
                memoryGrahics.CopyFromScreen(rc.X, rc.Y, 0, 0, rc.Size, System.Drawing.CopyPixelOperation.SourceCopy);
            }

            if (Settings.IsSaveScreenshotsInDateFolders)
            {
                if (string.IsNullOrWhiteSpace(fileName)) fileName = DateTime.Now.ToString("HH-mm-ss");
                var savePath = Settings.AutoSaveStrokesPath +
                               @"\Auto Saved - Screenshots\{DateTime.Now.Date:yyyyMMdd}\{fileName}.png";
                if (!Directory.Exists(Path.GetDirectoryName(savePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                bitmap.Save(savePath, ImageFormat.Png);
                if (!isHideNotification) ShowNotification("截图成功保存至 " + savePath);
            }
            else
            {
                var savePath = Settings.AutoSaveStrokesPath + @"\Auto Saved - Screenshots";
                if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
                bitmap.Save(savePath + @"\" + DateTime.Now.ToString("u").Replace(':', '-') + ".png", ImageFormat.Png);
                if (!isHideNotification)
                    ShowNotification("截图成功保存至 " + savePath + @"\" + DateTime.Now.ToString("u").Replace(':', '-') +
                                     ".png");
            }

            if (Settings.IsAutoSaveStrokesAtScreenshot) SaveInkCanvasStrokes(false, false);
        }

        private void SaveScreenShotToDesktop()
        {
            var rc = System.Windows.Forms.SystemInformation.VirtualScreen;
            var bitmap = new System.Drawing.Bitmap(rc.Width, rc.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var memoryGrahics = System.Drawing.Graphics.FromImage(bitmap))
            {
                memoryGrahics.CopyFromScreen(rc.X, rc.Y, 0, 0, rc.Size, System.Drawing.CopyPixelOperation.SourceCopy);
            }

            var savePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            bitmap.Save(savePath + @"\" + DateTime.Now.ToString("u").Replace(':', '-') + ".png", ImageFormat.Png);
            ShowNotification("截图成功保存至【桌面" + @"\" + DateTime.Now.ToString("u").Replace(':', '-') + ".png】");
            if (Settings.IsAutoSaveStrokesAtScreenshot) SaveInkCanvasStrokes(false, false);
        }
        #endregion

        #region SelectionGestures
        #region Floating Control

        private object? lastBorderMouseDownObject;

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lastBorderMouseDownObject = sender;
        }

        private bool isStrokeSelectionCloneOn = false;

        private void BorderStrokeSelectionClone_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            if (isStrokeSelectionCloneOn)
            {
                BorderStrokeSelectionClone.Background = Brushes.Transparent;

                isStrokeSelectionCloneOn = false;
            }
            else
            {
                BorderStrokeSelectionClone.Background = new SolidColorBrush(StringToColor("#FF1ED760"));

                isStrokeSelectionCloneOn = true;
            }
        }

        private void BorderStrokeSelectionCloneToNewBoard_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            var strokes = inkCanvas.GetSelectedStrokes();
            inkCanvas.Select(new StrokeCollection());
            strokes = strokes.Clone();
            WhiteBoardAddPage();
            inkCanvas.Strokes.Add(strokes);
        }

        private void BorderStrokeSelectionDelete_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;
            SymbolIconDelete_MouseUp(sender, e);
        }

        private void GridPenWidthDecrease_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;
            ChangeStrokeThickness(0.8);
        }

        private void GridPenWidthIncrease_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;
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

        private void GridPenWidthRestore_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            foreach (var stroke in inkCanvas.GetSelectedStrokes())
            {
                stroke.DrawingAttributes.Width = inkCanvas.DefaultDrawingAttributes.Width;
                stroke.DrawingAttributes.Height = inkCanvas.DefaultDrawingAttributes.Height;
            }
        }

        private void ImageFlipHorizontal_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            var m = new Matrix();

            // Find center of element and then transform to get current location of center
            var fe = e.Source as FrameworkElement;
            var center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
            center = new Point(inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Width / 2,
                inkCanvas.GetSelectionBounds().Top + inkCanvas.GetSelectionBounds().Height / 2);
            center = m.Transform(center); // 转换为矩阵缩放和旋转的中心点

            // Update matrix to reflect translation/rotation
            m.ScaleAt(-1, 1, center.X, center.Y); // 缩放

            var targetStrokes = inkCanvas.GetSelectedStrokes();
            foreach (var stroke in targetStrokes) stroke.Transform(m, false);

            if (DrawingAttributesHistory.Count > 0)
            {
                //var collecion = new StrokeCollection();
                //foreach (var item in DrawingAttributesHistory)
                //{
                //    collecion.Add(item.Key);
                //}
                timeMachine.CommitStrokeDrawingAttributesHistory(DrawingAttributesHistory);
                DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
                foreach (var item in DrawingAttributesHistoryFlag)
                {
                    item.Value.Clear();
                }
            }

            //updateBorderStrokeSelectionControlLocation();
        }

        private void ImageFlipVertical_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            var m = new Matrix();

            // Find center of element and then transform to get current location of center
            var fe = e.Source as FrameworkElement;
            var center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
            center = new Point(inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Width / 2,
                inkCanvas.GetSelectionBounds().Top + inkCanvas.GetSelectionBounds().Height / 2);
            center = m.Transform(center); // 转换为矩阵缩放和旋转的中心点

            // Update matrix to reflect translation/rotation
            m.ScaleAt(1, -1, center.X, center.Y); // 缩放

            var targetStrokes = inkCanvas.GetSelectedStrokes();
            foreach (var stroke in targetStrokes) stroke.Transform(m, false);

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

        private void ImageRotate45_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            var m = new Matrix();

            // Find center of element and then transform to get current location of center
            var fe = e.Source as FrameworkElement;
            var center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
            center = new Point(inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Width / 2,
                inkCanvas.GetSelectionBounds().Top + inkCanvas.GetSelectionBounds().Height / 2);
            center = m.Transform(center); // 转换为矩阵缩放和旋转的中心点

            // Update matrix to reflect translation/rotation
            m.RotateAt(45, center.X, center.Y); // 旋转

            var targetStrokes = inkCanvas.GetSelectedStrokes();
            foreach (var stroke in targetStrokes) stroke.Transform(m, false);

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

        private void ImageRotate90_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            var m = new Matrix();

            // Find center of element and then transform to get current location of center
            var fe = e.Source as FrameworkElement;
            var center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
            center = new Point(inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Width / 2,
                inkCanvas.GetSelectionBounds().Top + inkCanvas.GetSelectionBounds().Height / 2);
            center = m.Transform(center); // 转换为矩阵缩放和旋转的中心点

            // Update matrix to reflect translation/rotation
            m.RotateAt(90, center.X, center.Y); // 旋转

            var targetStrokes = inkCanvas.GetSelectedStrokes();
            foreach (var stroke in targetStrokes) stroke.Transform(m, false);

            if (DrawingAttributesHistory.Count > 0)
            {
                var collecion = new StrokeCollection();
                foreach (var item in DrawingAttributesHistory)
                {
                    collecion.Add(item.Key);
                }
                timeMachine.CommitStrokeDrawingAttributesHistory(DrawingAttributesHistory);
                DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
                foreach (var item in DrawingAttributesHistoryFlag)
                {
                    item.Value.Clear();
                }
            }
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
                BorderStrokeSelectionClone.Background = Brushes.Transparent;
                isStrokeSelectionCloneOn = false;
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

                if (isStrokeSelectionCloneOn)
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
        #region Behavior

        private void ToggleSwitchSupportPowerPoint_Toggled(object sender, RoutedEventArgs e)
        {
            if (Settings.PowerPointSupport)
                timerCheckPPT.Start();
            else
                timerCheckPPT.Stop();
        }

        #endregion

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
            UpdatePPTBtnPreview();
        }

        private void PPTBtnLSMinusBtn_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.PPTLSButtonPosition--;
            UpdatePPTBtnPreview();
        }

        private void PPTBtnLSSyncBtn_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.PPTRSButtonPosition = Settings.PPTLSButtonPosition;
            UpdatePPTBtnPreview();
        }

        private void PPTBtnLSResetBtn_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.PPTLSButtonPosition = 0;
            UpdatePPTBtnPreview();
        }

        private void PPTBtnRSPlusBtn_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.PPTRSButtonPosition++;
            UpdatePPTBtnPreview();
        }

        private void PPTBtnRSMinusBtn_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.PPTRSButtonPosition--;
            UpdatePPTBtnPreview();
        }

        private void PPTBtnRSSyncBtn_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.PPTLSButtonPosition = Settings.PPTRSButtonPosition;
            UpdatePPTBtnPreview();
        }

        private void PPTBtnRSResetBtn_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.PPTRSButtonPosition = 0;
            UpdatePPTBtnPreview();
        }

        private void UpdatePPTBtnPreview()
        {
            if (Settings.IsPPTButtonTranslucent)
            {
                PPTBtnPreviewLS.Opacity = 0.5;
                PPTBtnPreviewRS.Opacity = 0.5;
            }
            else
            {
                PPTBtnPreviewLS.Opacity = 1;
                PPTBtnPreviewRS.Opacity = 1;
            }

            if (Settings.IsPPTButtonBlackBackground)
            {
                PPTBtnPreviewLS.Source =
                    new BitmapImage(
                        new Uri("pack://application:,,,/Resources/PresentationExample/sidebar-dark.png"));
                PPTBtnPreviewRS.Source = new BitmapImage(
                    new Uri("pack://application:,,,/Resources/PresentationExample/sidebar-dark.png"));
            }
            else
            {
                PPTBtnPreviewLS.Source =
                    new BitmapImage(
                        new Uri("pack://application:,,,/Resources/PresentationExample/sidebar-white.png"));
                PPTBtnPreviewRS.Source = new BitmapImage(
                    new Uri("pack://application:,,,/Resources/PresentationExample/sidebar-white.png"));
            }

            if (Settings.ShowPPTButton)
            {
                // Only show side buttons
                PPTBtnPreviewLS.Visibility = Settings.IsLeftSidePPTButtonVisible ? Visibility.Visible : Visibility.Collapsed;
                PPTBtnPreviewRS.Visibility = Settings.IsRightSidePPTButtonVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                PPTBtnPreviewLS.Visibility = Visibility.Collapsed;
                PPTBtnPreviewRS.Visibility = Visibility.Collapsed;
            }

            PPTBtnPreviewRSTransform.Y = (Settings.PPTRSButtonPosition * 0.2);
            PPTBtnPreviewLSTransform.Y = (Settings.PPTLSButtonPosition * 0.2);
        }

        private void ToggleSwitchShowCursor_Toggled(object sender, RoutedEventArgs e)
        {
            inkCanvas_EditingModeChanged(inkCanvas, null);
        }

        #endregion

        #region Canvas

        private void ComboBoxPenStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLoaded) return;
            if (sender == ComboBoxPenStyle)
            {
                Settings.InkStyle = ComboBoxPenStyle.SelectedIndex;
                BoardComboBoxPenStyle.SelectedIndex = ComboBoxPenStyle.SelectedIndex;
            }
            else
            {
                Settings.InkStyle = BoardComboBoxPenStyle.SelectedIndex;
                ComboBoxPenStyle.SelectedIndex = BoardComboBoxPenStyle.SelectedIndex;
            }

            _settingsService.SaveSettings();
        }

        private void SwitchToCircleEraser(object sender, MouseButtonEventArgs e)
        {
            Settings.EraserShapeType = 0;
            CheckEraserTypeTab();
            UpdateEraserShape();
        }

        private void SwitchToRectangleEraser(object sender, MouseButtonEventArgs e)
        {
            Settings.EraserShapeType = 1;
            CheckEraserTypeTab();
            UpdateEraserShape();
        }


        private void InkWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isLoaded) return;
            if (sender == BoardInkWidthSlider) InkWidthSlider.Value = ((Slider)sender).Value;
            if (sender == InkWidthSlider) BoardInkWidthSlider.Value = ((Slider)sender).Value;
            drawingAttributes.Height = ((Slider)sender).Value / 2;
            drawingAttributes.Width = ((Slider)sender).Value / 2;
            Settings.InkWidth = ((Slider)sender).Value / 2;
            _settingsService.SaveSettings();
        }

        private void HighlighterWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isLoaded) return;
            // if (sender == BoardInkWidthSlider) InkWidthSlider.Value = ((Slider)sender).Value;
            // if (sender == InkWidthSlider) BoardInkWidthSlider.Value = ((Slider)sender).Value;
            drawingAttributes.Height = ((Slider)sender).Value;
            drawingAttributes.Width = ((Slider)sender).Value / 2;
            Settings.HighlighterWidth = ((Slider)sender).Value;
            _settingsService.SaveSettings();
        }

        private void InkAlphaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isLoaded) return;
            // if (sender == BoardInkWidthSlider) InkWidthSlider.Value = ((Slider)sender).Value;
            // if (sender == InkWidthSlider) BoardInkWidthSlider.Value = ((Slider)sender).Value;
            var NowR = drawingAttributes.Color.R;
            var NowG = drawingAttributes.Color.G;
            var NowB = drawingAttributes.Color.B;
            // Trace.WriteLine(BitConverter.GetBytes(((Slider)sender).Value));
            drawingAttributes.Color = Color.FromArgb((byte)((Slider)sender).Value, NowR, NowG, NowB);
            // drawingAttributes.Width = ((Slider)sender).Value / 2;
            // Settings.InkAlpha = ((Slider)sender).Value;
            // _settingsService.SaveSettings();
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

        private void ToggleSwitchAutoFoldInEasiNote_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.IsAutoFoldInEasiNote = ToggleSwitchAutoFoldInEasiNote.IsOn;
            _settingsService.SaveSettings();
            StartOrStoptimerCheckAutoFold();
        }

        private void ToggleSwitchAutoFoldInEasiCamera_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.IsAutoFoldInEasiCamera = ToggleSwitchAutoFoldInEasiCamera.IsOn;
            _settingsService.SaveSettings();
            StartOrStoptimerCheckAutoFold();
        }

        private void ToggleSwitchAutoFoldInEasiNote3_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.IsAutoFoldInEasiNote3 = ToggleSwitchAutoFoldInEasiNote3.IsOn;
            _settingsService.SaveSettings();
            StartOrStoptimerCheckAutoFold();
        }

        private void ToggleSwitchAutoFoldInEasiNote3C_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.IsAutoFoldInEasiNote3C = ToggleSwitchAutoFoldInEasiNote3C.IsOn;
            _settingsService.SaveSettings();
            StartOrStoptimerCheckAutoFold();
        }

        private void ToggleSwitchAutoFoldInEasiNote5C_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.IsAutoFoldInEasiNote5C = ToggleSwitchAutoFoldInEasiNote5C.IsOn;
            _settingsService.SaveSettings();
            StartOrStoptimerCheckAutoFold();
        }

        private void ToggleSwitchAutoFoldInPPTSlideShow_Toggled(object sender, RoutedEventArgs e)
        {
            if (Settings.IsAutoFoldInPPTSlideShow)
            {
                SettingsPPTInkingAndAutoFoldExplictBorder.Visibility = Visibility.Visible;
                SettingsShowCanvasAtNewSlideShowStackPanel.Opacity = 0.5;
                SettingsShowCanvasAtNewSlideShowStackPanel.IsHitTestVisible = false;
            }
            else
            {
                SettingsPPTInkingAndAutoFoldExplictBorder.Visibility = Visibility.Collapsed;
                SettingsShowCanvasAtNewSlideShowStackPanel.Opacity = 1;
                SettingsShowCanvasAtNewSlideShowStackPanel.IsHitTestVisible = true;
            }
            StartOrStoptimerCheckAutoFold();
        }

        private void ToggleSwitchAutoKillPptService_Toggled(object sender, RoutedEventArgs e)
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
            }
        }

        private void SetAutoSavedStrokesLocationToDiskDButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.AutoSaveStrokesPath = @"D:\ICC-Re";
        }

        private void SetAutoSavedStrokesLocationToDocumentFolderButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.AutoSaveStrokesPath =
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\ICC-Re";
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
                    inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    inkCanvas.Children.Clear();
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
                    inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    inkCanvas.Children.Clear();
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

        private void ToggleSwitchIsEnableFullScreenHelper_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.IsEnableFullScreenHelper = ToggleSwitchIsEnableFullScreenHelper.IsOn;
            _settingsService.SaveSettings();
        }

        private void ToggleSwitchIsEnableEdgeGestureUtil_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.IsEnableEdgeGestureUtil = ToggleSwitchIsEnableEdgeGestureUtil.IsOn;
            if (OSVersion.GetOperatingSystem() >= OSVersionExtension.OperatingSystem.Windows10) EdgeGestureUtil.DisableEdgeGestures(new WindowInteropHelper(this).Handle, ToggleSwitchIsEnableEdgeGestureUtil.IsOn);
            _settingsService.SaveSettings();
        }

        private void ToggleSwitchIsEnableForceFullScreen_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.IsEnableForceFullScreen = ToggleSwitchIsEnableForceFullScreen.IsOn;
            _settingsService.SaveSettings();
        }

        private void ToggleSwitchIsEnableDPIChangeDetection_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.IsEnableDPIChangeDetection = ToggleSwitchIsEnableDPIChangeDetection.IsOn;
            _settingsService.SaveSettings();
        }

        private void ToggleSwitchIsEnableResolutionChangeDetection_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.IsEnableResolutionChangeDetection = ToggleSwitchIsEnableResolutionChangeDetection.IsOn;
            _settingsService.SaveSettings();
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

            // PowerPointSettings

            if (Settings.PowerPointSupport)
            {
                timerCheckPPT.Start();
            }
            else
            {
                timerCheckPPT.Stop();
            }

            // -- new --

            UpdatePPTBtnPreview();

            // Gesture

            ToggleSwitchEnableMultiTouchMode.IsOn = Settings.IsEnableMultiTouchMode;

            ToggleSwitchEnableTwoFingerZoom.IsOn = Settings.IsEnableTwoFingerZoom;
            BoardToggleSwitchEnableTwoFingerZoom.IsOn = Settings.IsEnableTwoFingerZoom;

            ToggleSwitchEnableTwoFingerTranslate.IsOn = Settings.IsEnableTwoFingerTranslate;
            BoardToggleSwitchEnableTwoFingerTranslate.IsOn = Settings.IsEnableTwoFingerTranslate;

            if (Settings.AutoSwitchTwoFingerGesture)
            {
                if (Topmost)
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

            drawingAttributes.Height = Settings.InkWidth;
            drawingAttributes.Width = Settings.InkWidth;

            InkWidthSlider.Value = Settings.InkWidth * 2;
            HighlighterWidthSlider.Value = Settings.HighlighterWidth;

            if (Settings.IsShowCursor)
            {
                inkCanvas.ForceCursor = true;
            }
            else
            {
                inkCanvas.ForceCursor = false;
            }

            ComboBoxPenStyle.SelectedIndex = Settings.InkStyle;
            BoardComboBoxPenStyle.SelectedIndex = Settings.InkStyle;

            switch (Settings.EraserShapeType)
            {
                case 0:
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

                        inkCanvas.EraserShape = new EllipseStylusShape(k * 90, k * 90);
                        inkCanvas.EditingMode = InkCanvasEditingMode.None;
                        break;
                    }
                case 1:
                    {
                        double k = 1;
                        switch (Settings.EraserSize)
                        {
                            case 0:
                                k = 0.7;
                                break;
                            case 1:
                                k = 0.9;
                                break;
                            case 3:
                                k = 1.2;
                                break;
                            case 4:
                                k = 1.6;
                                break;
                        }

                        inkCanvas.EraserShape = new RectangleStylusShape(k * 90 * 0.6, k * 90);
                        inkCanvas.EditingMode = InkCanvasEditingMode.None;
                        break;
                    }
            }

            CheckEraserTypeTab();

            if (Settings.FitToCurve)
            {
                drawingAttributes.FitToCurve = true;
            }
            else
            {
                drawingAttributes.FitToCurve = false;
            }

            // Advanced

            ToggleSwitchIsEnableFullScreenHelper.IsOn = Settings.IsEnableFullScreenHelper;
            if (Settings.IsEnableFullScreenHelper)
            {
                FullScreenHelper.MarkFullscreenWindowTaskbarList(new WindowInteropHelper(this).Handle, true);
            }

            ToggleSwitchIsEnableEdgeGestureUtil.IsOn = Settings.IsEnableEdgeGestureUtil;
            if (Settings.IsEnableEdgeGestureUtil)
            {
                if (OSVersion.GetOperatingSystem() >= OSVersionExtension.OperatingSystem.Windows10)
                    EdgeGestureUtil.DisableEdgeGestures(new WindowInteropHelper(this).Handle, true);
            }

            ToggleSwitchIsEnableForceFullScreen.IsOn = Settings.IsEnableForceFullScreen;

            ToggleSwitchIsEnableDPIChangeDetection.IsOn = Settings.IsEnableDPIChangeDetection;

            ToggleSwitchIsEnableResolutionChangeDetection.IsOn =
                Settings.IsEnableResolutionChangeDetection;

            // Automation
            StartOrStoptimerCheckAutoFold();
            ToggleSwitchAutoFoldInEasiNote.IsOn = Settings.IsAutoFoldInEasiNote;

            ToggleSwitchAutoFoldInEasiCamera.IsOn = Settings.IsAutoFoldInEasiCamera;

            ToggleSwitchAutoFoldInEasiNote3C.IsOn = Settings.IsAutoFoldInEasiNote3C;

            ToggleSwitchAutoFoldInEasiNote3.IsOn = Settings.IsAutoFoldInEasiNote3;

            ToggleSwitchAutoFoldInEasiNote5C.IsOn = Settings.IsAutoFoldInEasiNote5C;

            SettingsPPTInkingAndAutoFoldExplictBorder.Visibility = Visibility.Collapsed;
            if (Settings.IsAutoFoldInPPTSlideShow)
            {
                SettingsPPTInkingAndAutoFoldExplictBorder.Visibility = Visibility.Visible;
                SettingsShowCanvasAtNewSlideShowStackPanel.Opacity = 0.5;
                SettingsShowCanvasAtNewSlideShowStackPanel.IsHitTestVisible = false;
            }

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
            BlackboardUIGridForInkReplay.IsHitTestVisible = true;

            inkCanvas_MouseUp(sender, null);
        }

        private void inkCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            inkCanvas.CaptureMouse();
            ViewboxFloatingBar.IsHitTestVisible = false;
            BlackboardUIGridForInkReplay.IsHitTestVisible = false;
        }

        private void inkCanvas_MouseUp(object? sender, MouseButtonEventArgs? e)
        {
            inkCanvas.ReleaseMouseCapture();
            ViewboxFloatingBar.IsHitTestVisible = true;
            BlackboardUIGridForInkReplay.IsHitTestVisible = true;
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
                drawingAttributes.FitToCurve = true;
        }
        #endregion

        #region SimulatePressure&InkToShape
        private void inkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            if (Settings.FitToCurve == true) drawingAttributes.FitToCurve = false;

            try
            {
                inkCanvas.Opacity = 1;

                foreach (var stylusPoint in e.Stroke.StylusPoints)
                    //LogHelper.WriteLogToFile(stylusPoint.PressureFactor.ToString(), LogHelper.LogType.Info);
                    // 检查是否是压感笔书写
                    //if (stylusPoint.PressureFactor != 0.5 && stylusPoint.PressureFactor != 0)
                    if (stylusPoint.PressureFactor is (> (float)0.501 or < (float)0.5) and not 0)
                        return;
                try
                {
                    if (e.Stroke.StylusPoints.Count > 3)
                    {
                        var random = new Random();
                        var _speed = GetPointSpeed(
                            e.Stroke.StylusPoints[random.Next(0, e.Stroke.StylusPoints.Count - 1)].ToPoint(),
                            e.Stroke.StylusPoints[random.Next(0, e.Stroke.StylusPoints.Count - 1)].ToPoint(),
                            e.Stroke.StylusPoints[random.Next(0, e.Stroke.StylusPoints.Count - 1)].ToPoint());
                    }
                }
                catch { }

                switch (Settings.InkStyle)
                {
                    case 1:
                        if (penType == 0)
                            try
                            {
                                var stylusPoints = new StylusPointCollection();
                                var n = e.Stroke.StylusPoints.Count - 1;
                                var s = "";

                                for (var i = 0; i <= n; i++)
                                {
                                    var speed = GetPointSpeed(e.Stroke.StylusPoints[Math.Max(i - 1, 0)].ToPoint(),
                                        e.Stroke.StylusPoints[i].ToPoint(),
                                        e.Stroke.StylusPoints[Math.Min(i + 1, n)].ToPoint());
                                    s += speed.ToString() + "\t";
                                    var point = new StylusPoint();
                                    if (speed >= 0.25)
                                        point.PressureFactor = (float)(0.5 - 0.3 * (Math.Min(speed, 1.5) - 0.3) / 1.2);
                                    else if (speed >= 0.05)
                                        point.PressureFactor = (float)0.5;
                                    else
                                        point.PressureFactor = (float)(0.5 + 0.4 * (0.05 - speed) / 0.05);

                                    point.X = e.Stroke.StylusPoints[i].X;
                                    point.Y = e.Stroke.StylusPoints[i].Y;
                                    stylusPoints.Add(point);
                                }

                                e.Stroke.StylusPoints = stylusPoints;
                            }
                            catch { }

                        break;
                    case 0:
                        if (penType == 0)
                            try
                            {
                                var stylusPoints = new StylusPointCollection();
                                var n = e.Stroke.StylusPoints.Count - 1;
                                var pressure = 0.1;
                                var x = 10;
                                if (n == 1) return;
                                if (n >= x)
                                {
                                    for (var i = 0; i < n - x; i++)
                                    {
                                        var point = new StylusPoint();

                                        point.PressureFactor = (float)0.5;
                                        point.X = e.Stroke.StylusPoints[i].X;
                                        point.Y = e.Stroke.StylusPoints[i].Y;
                                        stylusPoints.Add(point);
                                    }

                                    for (var i = n - x; i <= n; i++)
                                    {
                                        var point = new StylusPoint();

                                        point.PressureFactor = (float)((0.5 - pressure) * (n - i) / x + pressure);
                                        point.X = e.Stroke.StylusPoints[i].X;
                                        point.Y = e.Stroke.StylusPoints[i].Y;
                                        stylusPoints.Add(point);
                                    }
                                }
                                else
                                {
                                    for (var i = 0; i <= n; i++)
                                    {
                                        var point = new StylusPoint();

                                        point.PressureFactor = (float)(0.4 * (n - i) / n + pressure);
                                        point.X = e.Stroke.StylusPoints[i].X;
                                        point.Y = e.Stroke.StylusPoints[i].Y;
                                        stylusPoints.Add(point);
                                    }
                                }

                                e.Stroke.StylusPoints = stylusPoints;
                            }
                            catch { }

                        break;
                }
            }
            catch { }

            if (Settings.FitToCurve == true) drawingAttributes.FitToCurve = true;
        }

        public double GetPointSpeed(Point point1, Point point2, Point point3)
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
        private DispatcherTimer timerCheckPPT = new DispatcherTimer();
        private DispatcherTimer timerKillProcess = new DispatcherTimer();
        private DispatcherTimer timerCheckAutoFold = new DispatcherTimer();
        private bool isHidingSubPanelsWhenInking = false; // 避免书写时触发二次关闭二级菜单导致动画不连续

        private DispatcherTimer timerDisplayTime = new DispatcherTimer();
        private DispatcherTimer timerDisplayDate = new DispatcherTimer();

        private void InitTimers()
        {
            timerCheckPPT.Tick += TimerCheckPPT_Tick;
            timerCheckPPT.Interval = TimeSpan.FromMilliseconds(500);
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
                        _ = HideFloatingBar(null);
                }
                else
                {
                    // 不在特殊应用中，展开工具栏并重置用户标志
                    if (!_viewModel.IsFloatingBarVisible && !foldFloatingBarByUser)
                        _ = ShowFloatingBar(new object());
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
                _ => throw new NotImplementedException()
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
            if (inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint
                || inkCanvas.EditingMode == InkCanvasEditingMode.EraseByStroke
                || inkCanvas.EditingMode == InkCanvasEditingMode.Select) return;

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
                if (forceEraser)
                    return;
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

            inkCanvas.CaptureStylus();
            ViewboxFloatingBar.IsHitTestVisible = false;
            BlackboardUIGridForInkReplay.IsHitTestVisible = false;

            if (inkCanvas.EditingMode is InkCanvasEditingMode.EraseByPoint
                or InkCanvasEditingMode.EraseByStroke
                or InkCanvasEditingMode.Select)
                return;

            TouchDownPointsList[e.StylusDevice.Id] = InkCanvasEditingMode.None;
        }

        private async void MainWindow_StylusUp(object sender, StylusEventArgs e)
        {
            try
            {
                inkCanvas.Strokes.Add(GetStrokeVisual(e.StylusDevice.Id).Stroke);
                await Task.Delay(5); // 避免渲染墨迹完成前预览墨迹被删除导致墨迹闪烁
                inkCanvas.Children.Remove(GetVisualCanvas(e.StylusDevice.Id));

                inkCanvas_StrokeCollected(inkCanvas,
                    new InkCanvasStrokeCollectedEventArgs(GetStrokeVisual(e.StylusDevice.Id).Stroke));
            }
            catch (Exception ex)
            {
                Label.Content = ex.ToString();
            }

            try
            {
                StrokeVisualList.Remove(e.StylusDevice.Id);
                VisualCanvasList.Remove(e.StylusDevice.Id);
                TouchDownPointsList.Remove(e.StylusDevice.Id);
                if (StrokeVisualList.Count == 0 || VisualCanvasList.Count == 0 || TouchDownPointsList.Count == 0)
                {
                    inkCanvas.Children.Clear();
                    StrokeVisualList.Clear();
                    VisualCanvasList.Clear();
                    TouchDownPointsList.Clear();
                }
            }
            catch { }

            inkCanvas.ReleaseStylusCapture();
            ViewboxFloatingBar.IsHitTestVisible = true;
            BlackboardUIGridForInkReplay.IsHitTestVisible = true;
        }

        private void MainWindow_StylusMove(object sender, StylusEventArgs e)
        {
            try
            {
                if (GetTouchDownPointsList(e.StylusDevice.Id) != InkCanvasEditingMode.None) return;
                try
                {
                    if (e.StylusDevice.StylusButtons[1].StylusButtonState == StylusButtonState.Down) return;
                }
                catch { }

                var strokeVisual = GetStrokeVisual(e.StylusDevice.Id);
                var stylusPointCollection = e.GetStylusPoints(this);
                foreach (var stylusPoint in stylusPointCollection)
                    strokeVisual.Add(new StylusPoint(stylusPoint.X, stylusPoint.Y, stylusPoint.PressureFactor));
                strokeVisual.Redraw();
            }
            catch { }
        }

        private StrokeVisual GetStrokeVisual(int id)
        {
            if (StrokeVisualList.TryGetValue(id, out var visual)) return visual;

            var strokeVisual = new StrokeVisual(inkCanvas.DefaultDrawingAttributes.Clone());
            StrokeVisualList[id] = strokeVisual;
            StrokeVisualList[id] = strokeVisual;
            var visualCanvas = new VisualCanvas(strokeVisual);
            VisualCanvasList[id] = visualCanvas;
            inkCanvas.Children.Add(visualCanvas);

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
        private bool forcePointEraser = true;

        private void Main_Grid_TouchDown(object? sender, TouchEventArgs? e)
        {
            inkCanvas.CaptureTouch(e.TouchDevice);
            ViewboxFloatingBar.IsHitTestVisible = false;
            BlackboardUIGridForInkReplay.IsHitTestVisible = false;

            if (!isHidingSubPanelsWhenInking)
            {
                isHidingSubPanelsWhenInking = true;
                HideSubPanels(); // 书写时自动隐藏二级菜单
            }

            inkCanvas.Opacity = 1;
            double boundsWidth = GetTouchBoundWidth(e), eraserMultiplier = 1.0;
            if (!Settings.EraserBindTouchMultiplier && Settings.IsSpecialScreen)
                eraserMultiplier = 1 / Settings.TouchMultiplier;
            if (boundsWidth > BoundsWidth)
            {
                if (forceEraser)
                    return;
                if (boundsWidth > BoundsWidth * 2.5)
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

                    inkCanvas.EraserShape = new EllipseStylusShape(boundsWidth * k * eraserMultiplier,
                        boundsWidth * k * eraserMultiplier);
                    inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                }
                else
                {
                    if (_powerPointService.IsInSlideShow && inkCanvas.Strokes.Count == 0 &&
                        Settings.IsEnableFingerGestureSlideShowControl)
                    {
                        inkCanvas.EditingMode = InkCanvasEditingMode.GestureOnly;
                        inkCanvas.Opacity = 0.1;
                    }
                    else
                    {
                        inkCanvas.EraserShape = new EllipseStylusShape(5, 5);
                        inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                    }
                }
            }
            else
            {
                inkCanvas.EraserShape =
                    forcePointEraser ? new EllipseStylusShape(50, 50) : new EllipseStylusShape(5, 5);
                if (forceEraser) return;
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            }
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

            inkCanvas.CaptureTouch(e.TouchDevice);
            ViewboxFloatingBar.IsHitTestVisible = false;
            BlackboardUIGridForInkReplay.IsHitTestVisible = false;

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

            inkCanvas.ReleaseAllTouchCaptures();
            ViewboxFloatingBar.IsHitTestVisible = true;
            BlackboardUIGridForInkReplay.IsHitTestVisible = true;

            //手势完成后切回之前的状态
            if (dec.Count > 1)
                if (inkCanvas.EditingMode == InkCanvasEditingMode.None)
                    inkCanvas.EditingMode = lastInkCanvasEditingMode;
            dec.Remove(e.TouchDevice.Id);
            inkCanvas.Opacity = 1;

        }

        private void inkCanvas_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.Mode = ManipulationModes.All;
        }

        private void inkCanvas_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e) { }

        private void Main_Grid_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (e.Manipulators.Count() != 0) return;
            if (forceEraser) return;
            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
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
    }
}