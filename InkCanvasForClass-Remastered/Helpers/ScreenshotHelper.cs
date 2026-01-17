using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace InkCanvasForClass_Remastered.Helpers;

/// <summary>
/// 提供屏幕截图功能的帮助类
/// </summary>
public static class ScreenshotHelper
{
    /// <summary>
    /// 捕获整个虚拟屏幕的截图
    /// </summary>
    /// <returns>包含屏幕截图的 Bitmap 对象</returns>
    public static Bitmap CaptureScreen()
    {
        var rc = SystemInformation.VirtualScreen;
        var bitmap = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);

        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(rc.X, rc.Y, 0, 0, rc.Size, CopyPixelOperation.SourceCopy);

        return bitmap;
    }

    /// <summary>
    /// 将屏幕截图保存到指定文件夹
    /// </summary>
    /// <param name="folderPath">保存截图的文件夹路径</param>
    /// <param name="fileNameWithoutExtension">不含扩展名的文件名，如果为 null 则使用时间戳</param>
    /// <returns>保存的文件完整路径</returns>
    public static string SaveScreenshot(string folderPath, string? fileNameWithoutExtension = null)
    {
        fileNameWithoutExtension ??= DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff");
        var filePath = Path.Combine(folderPath, fileNameWithoutExtension + ".png");

        using var bitmap = CaptureScreen();
        bitmap.Save(filePath, ImageFormat.Png);

        return filePath;
    }

    /// <summary>
    /// 将屏幕截图保存到桌面
    /// </summary>
    /// <param name="fileNameWithoutExtension">不含扩展名的文件名，如果为 null 则使用时间戳</param>
    /// <returns>保存的文件完整路径</returns>
    public static string SaveScreenshotToDesktop(string? fileNameWithoutExtension = null)
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        return SaveScreenshot(desktopPath, fileNameWithoutExtension);
    }
}