using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace InkCanvasForClass_Remastered.Helpers
{
    internal class ForegroundWindowInfo
    {
        public unsafe static string WindowTitle()
        {
            const int nChars = 256;
            IntPtr buffer = Marshal.AllocHGlobal(nChars * sizeof(char));
            try
            {
                PWSTR pWindowTitle = new((char*)buffer);
                int length = PInvoke.GetWindowText(PInvoke.GetForegroundWindow(), pWindowTitle, nChars);

                if (length > 0)
                {
                    return Marshal.PtrToStringUni(buffer, length);
                }
                return string.Empty;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public unsafe static string WindowClassName()
        {
            const int nChars = 256;
            IntPtr buffer = Marshal.AllocHGlobal(nChars * sizeof(char));
            try
            {
                PWSTR pWindowTitle = new((char*)buffer);
                int length = PInvoke.GetClassName(PInvoke.GetForegroundWindow(), pWindowTitle, nChars);

                if (length > 0)
                {
                    return Marshal.PtrToStringUni(buffer, length);
                }
                return string.Empty;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public static RECT WindowRect()
        {
            IntPtr foregroundWindowHandle = PInvoke.GetForegroundWindow();

            _ = PInvoke.GetWindowRect(new HWND(foregroundWindowHandle), out RECT windowRect);

            return windowRect;
        }

        public static string ProcessName()
        {
            IntPtr foregroundWindowHandle = PInvoke.GetForegroundWindow();
            _ = PInvoke.GetWindowThreadProcessId(new HWND(foregroundWindowHandle), out uint processId);

            try
            {
                Process process = Process.GetProcessById((int)processId);
                return process.ProcessName;
            }
            catch (ArgumentException)
            {
                // Process with the given ID not found
                return "Unknown";
            }
        }

        public static string ProcessPath()
        {
            IntPtr foregroundWindowHandle = PInvoke.GetForegroundWindow();
            _ = PInvoke.GetWindowThreadProcessId(new HWND(foregroundWindowHandle), out uint processId);

            try
            {
                Process process = Process.GetProcessById((int)processId);
                return process.MainModule.FileName;
            }
            catch
            {
                // Process with the given ID not found
                return "Unknown";
            }
        }
    }
}