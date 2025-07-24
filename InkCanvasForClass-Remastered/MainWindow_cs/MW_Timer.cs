using InkCanvasForClass_Remastered.Helpers;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace InkCanvasForClass_Remastered {
    public class TimeViewModel : INotifyPropertyChanged {
        private string _nowTime;
        private string _nowDate;

        public string nowTime {
            get => _nowTime;
            set {
                if (_nowTime != value) {
                    _nowTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public string nowDate {
            get => _nowDate;
            set {
                if (_nowDate != value) {
                    _nowDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class MainWindow : Window {
        private DispatcherTimer timerCheckPPT = new DispatcherTimer();
        private DispatcherTimer timerKillProcess = new DispatcherTimer();
        private DispatcherTimer timerCheckAutoFold = new DispatcherTimer();
        private string AvailableLatestVersion = null;
        private DispatcherTimer timerCheckAutoUpdateWithSilence = new DispatcherTimer();
        private bool isHidingSubPanelsWhenInking = false; // 避免书写时触发二次关闭二级菜单导致动画不连续

        private DispatcherTimer timerDisplayTime = new DispatcherTimer();
        private DispatcherTimer timerDisplayDate = new DispatcherTimer();

        private TimeViewModel nowTimeVM = new TimeViewModel();

        private void InitTimers() {
            timerCheckPPT.Tick += TimerCheckPPT_Tick;
            timerCheckPPT.Interval = TimeSpan.FromMilliseconds(500);
            timerKillProcess.Tick += TimerKillProcess_Tick;
            timerKillProcess.Interval = TimeSpan.FromMilliseconds(2000);
            timerCheckAutoFold.Tick += timerCheckAutoFold_Tick;
            timerCheckAutoFold.Interval = TimeSpan.FromMilliseconds(500);
            
            WaterMarkTime.DataContext = nowTimeVM;
            WaterMarkDate.DataContext = nowTimeVM;
            timerDisplayTime.Tick += TimerDisplayTime_Tick;
            timerDisplayTime.Interval = TimeSpan.FromMilliseconds(1000);
            timerDisplayTime.Start();
            timerDisplayDate.Tick += TimerDisplayDate_Tick;
            timerDisplayDate.Interval = TimeSpan.FromMilliseconds(1000 * 60 * 60 * 1);
            timerDisplayDate.Start();
            timerKillProcess.Start();
            nowTimeVM.nowDate = DateTime.Now.ToShortDateString().ToString();
            nowTimeVM.nowTime = DateTime.Now.ToShortTimeString().ToString();
        }

        private void TimerDisplayTime_Tick(object sender, EventArgs e) {
            nowTimeVM.nowTime = DateTime.Now.ToShortTimeString().ToString();
        }

        private void TimerDisplayDate_Tick(object sender, EventArgs e) {
            nowTimeVM.nowDate = DateTime.Now.ToShortDateString().ToString();
        }

        private void TimerKillProcess_Tick(object sender, EventArgs e) {
            try {
                // 希沃相关： easinote swenserver RemoteProcess EasiNote.MediaHttpService smartnote.cloud EasiUpdate smartnote EasiUpdate3 EasiUpdate3Protect SeewoP2P CefSharp.BrowserSubprocess SeewoUploadService
                var arg = "/F";
                if (Settings.Automation.IsAutoKillPptService) {
                    var processes = Process.GetProcessesByName("PPTService");
                    if (processes.Length > 0) arg += " /IM PPTService.exe";
                    processes = Process.GetProcessesByName("SeewoIwbAssistant");
                    if (processes.Length > 0) arg += " /IM SeewoIwbAssistant.exe" + " /IM Sia.Guard.exe";
                }

                if (Settings.Automation.IsAutoKillEasiNote) {
                    var processes = Process.GetProcessesByName("EasiNote");
                    if (processes.Length > 0) arg += " /IM EasiNote.exe";
                }

                if (Settings.Automation.IsAutoKillHiteAnnotation) {
                    var processes = Process.GetProcessesByName("HiteAnnotation");
                    if (processes.Length > 0) arg += " /IM HiteAnnotation.exe";
                }

                if (Settings.Automation.IsAutoKillVComYouJiao)
                {
                    var processes = Process.GetProcessesByName("VcomTeach");
                    if (processes.Length > 0) arg += " /IM VcomTeach.exe" + " /IM VcomDaemon.exe" + " /IM VcomRender.exe";
                }

                if (Settings.Automation.IsAutoKillICA) {
                    var processesAnnotation = Process.GetProcessesByName("Ink Canvas Annotation");
                    var processesArtistry = Process.GetProcessesByName("Ink Canvas Artistry");
                    if (processesAnnotation.Length > 0) arg += " /IM \"Ink Canvas Annotation.exe\"";
                    if (processesArtistry.Length > 0) arg += " /IM \"Ink Canvas Artistry.exe\"";
                }

                if (Settings.Automation.IsAutoKillInkCanvas) {
                    var processes = Process.GetProcessesByName("Ink Canvas");
                    if (processes.Length > 0) arg += " /IM \"Ink Canvas.exe\"";
                }

                if (Settings.Automation.IsAutoKillIDT) {
                    var processes = Process.GetProcessesByName("智绘教");
                    if (processes.Length > 0) arg += " /IM \"智绘教.exe\"";
                }

                if (Settings.Automation.IsAutoKillSeewoLauncher2DesktopAnnotation) {
                    //由于希沃桌面2.0提供的桌面批注是64位应用程序，32位程序无法访问，目前暂不做精准匹配，只匹配进程名称，后面会考虑封装一套基于P/Invoke和WMI的综合进程识别方案。
                    var processes = Process.GetProcessesByName("DesktopAnnotation");
                    if (processes.Length > 0) arg += " /IM DesktopAnnotation.exe";
                }

                if (arg != "/F") {
                    var p = new Process();
                    p.StartInfo = new ProcessStartInfo("taskkill", arg);
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.Start();

                    if (arg.Contains("EasiNote")) {
                        ShowNotification("“希沃白板 5”已自动关闭");
                    }

                    if (arg.Contains("HiteAnnotation")) {
                        ShowNotification("“鸿合屏幕书写”已自动关闭");
                    }

                    if (arg.Contains("Ink Canvas Annotation") || arg.Contains("Ink Canvas Artistry")) {
                        ShowNewMessage("“ICA”已自动关闭");
                    }

                    if (arg.Contains("\"Ink Canvas.exe\"")) {
                        ShowNotification("“Ink Canvas”已自动关闭");
                    }

                    if (arg.Contains("智绘教")) {
                        ShowNotification("“智绘教”已自动关闭");
                    }

                    if (arg.Contains("VcomTeach"))
                    {
                        ShowNotification("“优教授课端”已自动关闭");
                    }

                    if (arg.Contains("DesktopAnnotation"))
                    {
                        ShowNotification("“DesktopAnnotation”已自动关闭");
                    }
                }
            }
            catch {}
        }


        private bool foldFloatingBarByUser = false, // 保持收纳操作不受自动收纳的控制
            unfoldFloatingBarByUser = false; // 允许用户在希沃软件内进行展开操作

        private void timerCheckAutoFold_Tick(object sender, EventArgs e) {
            if (isFloatingBarChangingHideMode) return;
            try {
                var windowProcessName = ForegroundWindowInfo.ProcessName();
                var windowTitle = ForegroundWindowInfo.WindowTitle();
                //LogHelper.WriteLogToFile("windowTitle | " + windowTitle + " | windowProcessName | " + windowProcessName);

                if (windowProcessName == "EasiNote") {
                    // 检测到有可能是EasiNote5或者EasiNote3/3C
                    if (ForegroundWindowInfo.ProcessPath() != "Unknown") {
                        var versionInfo = FileVersionInfo.GetVersionInfo(ForegroundWindowInfo.ProcessPath());
                        string version = versionInfo.FileVersion;
                        string prodName = versionInfo.ProductName;
                        Trace.WriteLine(ForegroundWindowInfo.ProcessPath());
                        Trace.WriteLine(version);
                        Trace.WriteLine(prodName);
                        if (version.StartsWith("5.") && Settings.Automation.IsAutoFoldInEasiNote && (!(windowTitle.Length == 0 && ForegroundWindowInfo.WindowRect().Height < 500) ||
                                                         !Settings.Automation.IsAutoFoldInEasiNoteIgnoreDesktopAnno)) { // EasiNote5
                            if (!unfoldFloatingBarByUser && !isFloatingBarFolded) FoldFloatingBar_MouseUp(null, null);
                        } else if (version.StartsWith("3.") && Settings.Automation.IsAutoFoldInEasiNote3) { // EasiNote3
                            if (!unfoldFloatingBarByUser && !isFloatingBarFolded) FoldFloatingBar_MouseUp(null, null);
                        } else if (prodName.Contains("3C") && Settings.Automation.IsAutoFoldInEasiNote3C &&
                                   ForegroundWindowInfo.WindowRect().Height >= SystemParameters.WorkArea.Height - 16 &&
                                   ForegroundWindowInfo.WindowRect().Width >= SystemParameters.WorkArea.Width - 16) { // EasiNote3C
                            if (!unfoldFloatingBarByUser && !isFloatingBarFolded) FoldFloatingBar_MouseUp(null, null);
                        }
                    }
                    // EasiCamera
                } else if (Settings.Automation.IsAutoFoldInEasiCamera && windowProcessName == "EasiCamera" &&
                           ForegroundWindowInfo.WindowRect().Height >= SystemParameters.WorkArea.Height - 16 &&
                           ForegroundWindowInfo.WindowRect().Width >= SystemParameters.WorkArea.Width - 16) {
                    if (!unfoldFloatingBarByUser && !isFloatingBarFolded) FoldFloatingBar_MouseUp(null, null);
                    // EasiNote5C
                } else if (Settings.Automation.IsAutoFoldInEasiNote5C && windowProcessName == "EasiNote5C" &&
                           ForegroundWindowInfo.WindowRect().Height >= SystemParameters.WorkArea.Height - 16 &&
                           ForegroundWindowInfo.WindowRect().Width >= SystemParameters.WorkArea.Width - 16) {
                    if (!unfoldFloatingBarByUser && !isFloatingBarFolded) FoldFloatingBar_MouseUp(null, null);
                    // SeewoPinco
                } else if (Settings.Automation.IsAutoFoldInSeewoPincoTeacher && (windowProcessName == "BoardService" || windowProcessName == "seewoPincoTeacher")) {
                    if (!unfoldFloatingBarByUser && !isFloatingBarFolded) FoldFloatingBar_MouseUp(null, null);
                    // HiteCamera
                } else if (Settings.Automation.IsAutoFoldInHiteCamera && windowProcessName == "HiteCamera" &&
                           ForegroundWindowInfo.WindowRect().Height >= SystemParameters.WorkArea.Height - 16 &&
                           ForegroundWindowInfo.WindowRect().Width >= SystemParameters.WorkArea.Width - 16) {
                    if (!unfoldFloatingBarByUser && !isFloatingBarFolded) FoldFloatingBar_MouseUp(null, null);
                    // HiteTouchPro
                } else if (Settings.Automation.IsAutoFoldInHiteTouchPro && windowProcessName == "HiteTouchPro" &&
                           ForegroundWindowInfo.WindowRect().Height >= SystemParameters.WorkArea.Height - 16 &&
                           ForegroundWindowInfo.WindowRect().Width >= SystemParameters.WorkArea.Width - 16) {
                    if (!unfoldFloatingBarByUser && !isFloatingBarFolded) FoldFloatingBar_MouseUp(null, null);
                    // WxBoardMain
                } else if (Settings.Automation.IsAutoFoldInWxBoardMain && windowProcessName == "WxBoardMain" &&
                           ForegroundWindowInfo.WindowRect().Height >= SystemParameters.WorkArea.Height - 16 &&
                           ForegroundWindowInfo.WindowRect().Width >= SystemParameters.WorkArea.Width - 16) {
                    if (!unfoldFloatingBarByUser && !isFloatingBarFolded) FoldFloatingBar_MouseUp(null, null);
                    // MSWhiteboard
                } else if (Settings.Automation.IsAutoFoldInMSWhiteboard && (windowProcessName == "MicrosoftWhiteboard" || 
                                                                            windowProcessName == "msedgewebview2")) {
                    if (!unfoldFloatingBarByUser && !isFloatingBarFolded) FoldFloatingBar_MouseUp(null, null);
                    // OldZyBoard
                } else if (Settings.Automation.IsAutoFoldInOldZyBoard && // 中原旧白板
                        (WinTabWindowsChecker.IsWindowExisted("WhiteBoard - DrawingWindow")
                         || WinTabWindowsChecker.IsWindowExisted("InstantAnnotationWindow"))) {
                    if (!unfoldFloatingBarByUser && !isFloatingBarFolded) FoldFloatingBar_MouseUp(null, null);
                    // HiteLightBoard
                } else if (Settings.Automation.IsAutoFoldInHiteLightBoard && windowProcessName == "HiteLightBoard" &&
                           ForegroundWindowInfo.WindowRect().Height >= SystemParameters.WorkArea.Height - 16 &&
                           ForegroundWindowInfo.WindowRect().Width >= SystemParameters.WorkArea.Width - 16) {
                    if (!unfoldFloatingBarByUser && !isFloatingBarFolded) FoldFloatingBar_MouseUp(null, null);
                    // AdmoxWhiteboard
                } else if (Settings.Automation.IsAutoFoldInAdmoxWhiteboard && windowProcessName == "Amdox.WhiteBoard" &&
                           ForegroundWindowInfo.WindowRect().Height >= SystemParameters.WorkArea.Height - 16 &&
                           ForegroundWindowInfo.WindowRect().Width >= SystemParameters.WorkArea.Width - 16) {
                    if (!unfoldFloatingBarByUser && !isFloatingBarFolded) FoldFloatingBar_MouseUp(null, null);
                    // AdmoxBooth
                } else if (Settings.Automation.IsAutoFoldInAdmoxBooth && windowProcessName == "Amdox.Booth" &&
                           ForegroundWindowInfo.WindowRect().Height >= SystemParameters.WorkArea.Height - 16 &&
                           ForegroundWindowInfo.WindowRect().Width >= SystemParameters.WorkArea.Width - 16) {
                    if (!unfoldFloatingBarByUser && !isFloatingBarFolded) FoldFloatingBar_MouseUp(null, null);
                    // QPoint
                } else if (Settings.Automation.IsAutoFoldInQPoint && windowProcessName == "QPoint" &&
                           ForegroundWindowInfo.WindowRect().Height >= SystemParameters.WorkArea.Height - 16 &&
                           ForegroundWindowInfo.WindowRect().Width >= SystemParameters.WorkArea.Width - 16) {
                    if (!unfoldFloatingBarByUser && !isFloatingBarFolded) FoldFloatingBar_MouseUp(null, null);
                    // YiYunVisualPresenter
                } else if (Settings.Automation.IsAutoFoldInYiYunVisualPresenter && windowProcessName == "YiYunVisualPresenter" &&
                           ForegroundWindowInfo.WindowRect().Height >= SystemParameters.WorkArea.Height - 16 &&
                           ForegroundWindowInfo.WindowRect().Width >= SystemParameters.WorkArea.Width - 16) {
                    if (!unfoldFloatingBarByUser && !isFloatingBarFolded) FoldFloatingBar_MouseUp(null, null);
                    // MaxHubWhiteboard
                } else if (Settings.Automation.IsAutoFoldInMaxHubWhiteboard && windowProcessName == "WhiteBoard" &&
                           WinTabWindowsChecker.IsWindowExisted("白板书写") &&
                           ForegroundWindowInfo.WindowRect().Height >= SystemParameters.WorkArea.Height - 16 &&
                           ForegroundWindowInfo.WindowRect().Width >= SystemParameters.WorkArea.Width - 16) {
                    if (ForegroundWindowInfo.ProcessPath() != "Unknown") {
                        var versionInfo = FileVersionInfo.GetVersionInfo(ForegroundWindowInfo.ProcessPath());
                        var version = versionInfo.FileVersion; var prodName = versionInfo.ProductName;
                        if (version.StartsWith("6.") && prodName=="WhiteBoard") if (!unfoldFloatingBarByUser && !isFloatingBarFolded) FoldFloatingBar_MouseUp(null, null);
                    }
                } else if (WinTabWindowsChecker.IsWindowExisted("幻灯片放映", false)) {
                    // 处于幻灯片放映状态
                    if (!Settings.Automation.IsAutoFoldInPPTSlideShow && isFloatingBarFolded && !foldFloatingBarByUser)
                        UnFoldFloatingBar_MouseUp(new object(), null);
                } else {
                    if (isFloatingBarFolded && !foldFloatingBarByUser) UnFoldFloatingBar_MouseUp(new object(), null);
                    unfoldFloatingBarByUser = false;
                }
            }
            catch { }
        }

        
    }
}