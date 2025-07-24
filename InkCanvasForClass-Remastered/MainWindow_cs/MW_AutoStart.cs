using System;
using System.IO;
using System.Windows;
using WindowsShortcutFactory;

namespace InkCanvasForClass_Remastered {
    public partial class MainWindow : Window {
        
        /// <summary>
        /// 检查是否已启用自启动
        /// </summary>
        /// <param name="exeName">快捷方式名称</param>
        /// <returns>是否启用自启动</returns>
        public static bool IsStartAutomaticallyEnabled(string exeName) {
            try {
                var shortcutPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup), 
                    exeName + ".lnk");
                return File.Exists(shortcutPath);
            }
            catch (Exception) {
                return false;
            }
        }

        /// <summary>
        /// 创建自启动快捷方式
        /// </summary>
        /// <param name="exeName">快捷方式名称</param>
        /// <returns>是否创建成功</returns>
        public static bool StartAutomaticallyCreate(string exeName) {
            try {
                var shortcutPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup), 
                    exeName + ".lnk");
                
                using var shortcut = new WindowsShortcut {
                    Path = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName,
                    WorkingDirectory = Environment.CurrentDirectory,
                    Description = exeName + "_Ink",
                };
                
                shortcut.Save(shortcutPath);
                return true;
            }
            catch (Exception) {
                return false;
            }
        }

        /// <summary>
        /// 删除自启动快捷方式
        /// </summary>
        /// <param name="exeName">快捷方式名称</param>
        /// <returns>是否删除成功</returns>
        public static bool StartAutomaticallyDel(string exeName) {
            try {
                var shortcutPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup), 
                    exeName + ".lnk");
                
                if (File.Exists(shortcutPath)) {
                    File.Delete(shortcutPath);
                    return true;
                }
                return false;
            }
            catch (Exception) {
                return false;
            }
        }
    }
}