using InkCanvasForClass_Remastered.Enums;
using InkCanvasForClass_Remastered.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using Screen = System.Windows.Forms.Screen;

namespace InkCanvasForClass_Remastered.Helpers
{
    internal sealed class ForegroundWindowInfo
    {
        private nint _lastExternalWindow;

        public AutoFoldTarget? GetAutoFoldTarget(Settings settings, bool isInSlideShow)
        {
            if (!settings.IsEnableAutoFold) return AutoFoldTarget.None;
            var slideShowTarget = settings.IsAutoFoldInPPTSlideShow ? AutoFoldTarget.SlideShow : AutoFoldTarget.None;
            if (isInSlideShow) return slideShowTarget;

            try
            {
                var window = GetExternalWindow(GetForegroundWindow());
                var target = GetSeewoTarget(window, settings);
                // 保留按窗口标题识别放映的入口，兼容没有 COM 事件的放映窗口。
                if ((settings.IsAutoFoldInPPTSlideShow || target != AutoFoldTarget.None) &&
                    WinTabWindowsChecker.IsWindowExisted("幻灯片放映", false))
                    return slideShowTarget;
                return target;
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException
                or UnauthorizedAccessException or IOException or ElementNotAvailableException or COMException)
            {
                // 窗口可能在读取信息时关闭或拒绝访问，等下一次检测。
                return null;
            }
        }

        private nint GetExternalWindow(nint foreground)
        {
            GetWindowThreadProcessId(foreground, out var processId);
            if (processId == 0 || processId == Environment.ProcessId || foreground == _lastExternalWindow)
                return _lastExternalWindow;

            // 打开任务栏通知区域和 ICC 托盘菜单时，继续沿用原来的授课窗口。
            var className = new StringBuilder(64);
            GetClassName(foreground, className, className.Capacity);
            if (className.ToString() is not ("Shell_TrayWnd" or "Shell_SecondaryTrayWnd"
                or "NotifyIconOverflowWindow" or "TopLevelWindowForOverflowXamlIsland"))
                _lastExternalWindow = foreground;
            return _lastExternalWindow;
        }

        private static AutoFoldTarget GetSeewoTarget(nint window, Settings settings)
        {
            GetWindowThreadProcessId(window, out var processId);
            if (processId == 0) return AutoFoldTarget.None;

            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName switch
            {
                "EasiCamera" when settings.IsAutoFoldInEasiCamera && IsFullScreen(window) => AutoFoldTarget.EasiCamera,
                "EasiNote5C" when settings.IsAutoFoldInEasiNote5C && IsFullScreen(window) => AutoFoldTarget.EasiNote5C,
                "EasiNote" when ShouldFoldEasiNote(window, process.MainModule?.FileVersionInfo, settings) => AutoFoldTarget.EasiNote,
                _ => AutoFoldTarget.None
            };
        }

        private static bool ShouldFoldEasiNote(nint window, FileVersionInfo? info, Settings settings)
        {
            if (info?.FileVersion?.StartsWith("5.") == true && settings.IsAutoFoldInEasiNote)
            {
                if (!settings.IsAutoFoldInEasiNoteIgnoreDesktopAnno) return true;
                var title = new StringBuilder(256);
                GetWindowText(window, title, title.Capacity);
                GetWindowRect(window, out var rect);
                return title.Length > 0 || rect.Bottom - rect.Top >= 500;
            }
            if (info?.FileVersion?.StartsWith("3.") == true && settings.IsAutoFoldInEasiNote3)
                return true;
            return info?.ProductName?.Contains("3C") == true && settings.IsAutoFoldInEasiNote3C && IsFullScreen(window);
        }

        private static bool IsFullScreen(nint window)
        {
            if (!GetWindowRect(window, out var rect)) return false;
            var workArea = Screen.FromHandle(window).WorkingArea;
            const int tolerance = 16;
            return rect.Right - rect.Left >= workArea.Width - tolerance &&
                   rect.Bottom - rect.Top >= workArea.Height - tolerance;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern nint GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(nint hWnd, StringBuilder title, int capacity);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(nint hWnd, StringBuilder className, int capacity);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(nint hWnd, out RECT rect);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);
    }
}
