// This file has been refactored. TrayIcon functionality moved to TrayIconService.
// The old implementation was part of the App partial class but has been moved to improve
// separation of concerns and maintainability.

using Hardcodet.Wpf.TaskbarNotification;
using iNKORE.UI.WPF.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace InkCanvasForClass_Remastered
{
    // This partial class declaration kept for backwards compatibility if needed,
    // but TrayIcon functionality has been moved to TrayIconService
    public partial class App : Application
    {
        // TrayIcon functionality has been refactored to TrayIconService
        // See Services/TrayIconService.cs for the new implementation
    }
}
