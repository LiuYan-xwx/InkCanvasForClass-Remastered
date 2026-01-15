using InkCanvasForClass_Remastered.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;

namespace InkCanvasForClass_Remastered.Services
{
    public class FileFolderService(SettingsService settingsService, ILogger<FileFolderService> logger) : IHostedService
    {
        private readonly SettingsService _settingsService = settingsService;
        private readonly ILogger<FileFolderService> _logger = logger;

        private static readonly List<string> Folders = [
            CommonDirectories.AppLogFolderPath,
            ];
        private static List<string> SaveFolders => [
            CommonDirectories.AppSavesRootFolderPath,
            CommonDirectories.AutoSaveAnnotationStrokesFolderPath,
            CommonDirectories.AutoSavePresentationStrokesFolderPath,
            CommonDirectories.AutoSaveScreenshotsFolderPath,
            CommonDirectories.AutoSaveWhiteboardStrokesFolderPath,
            CommonDirectories.UserSaveAnnotationStrokesFolderPath,
            CommonDirectories.UserSaveWhiteboardStrokesFolderPath,
            ];
        public static void CreateFolders()
        {
            foreach (var i in Folders.Where(i => !Directory.Exists(i)))
            {
                Directory.CreateDirectory(i);
            }
        }

        public static void CreateSaveFolders()
        {
            foreach (var i in SaveFolders.Where(i => !Directory.Exists(i)))
            {
                Directory.CreateDirectory(i);
            }
        }
        public async Task ProcessOldFilesAsync()
        {
            try
            {
                var directoryPath = CommonDirectories.AppSavesRootFolderPath;
                var daysThreshold = _settingsService.Settings.AutoDelSavedFilesDays;

                _logger.LogInformation("开始清理旧文件，路径: {DirectoryPath}, 天数阈值: {DaysThreshold}", directoryPath, daysThreshold);

                await Task.Run(() => DeleteFilesOlder(directoryPath, daysThreshold));
                await Task.Run(() => DeleteEmptyFolders(directoryPath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理旧文件时发生错误");
            }
        }

        private void DeleteFilesOlder(string directoryPath, int daysThreshold)
        {
            DirectoryInfo di = new(directoryPath);

            try
            {
                foreach (var fi in di.EnumerateFiles("*", SearchOption.AllDirectories)
                                     .Where(fi => fi.CreationTime < DateTime.Now.AddDays(-daysThreshold)))
                {
                    fi.Delete();
                    _logger.LogInformation("已删除文件: {FilePath}", fi.FullName);
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理目录时出错: {DirectoryPath}", directoryPath);
            }
        }

        private void DeleteEmptyFolders(string directoryPath)
        {
            try
            {
                DirectoryInfo di = new(directoryPath);

                // 获取受保护的目录列表（SaveFolders中除了AppSavesRootFolderPath之外的目录）
                var protectedFolders = SaveFolders
                    .Where(f => f != CommonDirectories.AppSavesRootFolderPath)
                    .Select(f => Path.GetFullPath(f))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // 递归处理所有子目录
                foreach (var subDir in di.EnumerateDirectories("*", SearchOption.AllDirectories)
                                         .OrderByDescending(d => d.FullName.Length))
                {
                    try
                    {
                        // 检查当前目录是否为受保护目录
                        var fullPath = Path.GetFullPath(subDir.FullName);
                        if (protectedFolders.Contains(fullPath))
                        {
                            //_logger.LogInformation("跳过删除受保护的文件夹: {FolderPath}", subDir.FullName);
                            continue;
                        }

                        // 检查目录是否为空（没有文件和子目录）
                        if (!subDir.EnumerateFileSystemInfos().Any())
                        {
                            subDir.Delete();
                            _logger.LogInformation("已删除空文件夹: {FolderPath}", subDir.FullName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "删除空文件夹时出错: {FolderPath}", subDir.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查找空文件夹时出错: {DirectoryPath}", directoryPath);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }
    }
}
