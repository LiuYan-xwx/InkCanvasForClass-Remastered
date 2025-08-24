using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InkCanvasForClass_Remastered.Services
{
    public class FileFolderService(SettingsService settingsService, ILogger<FileFolderService> logger) : IHostedService
    {
        private readonly SettingsService _settingsService = settingsService;
        private readonly ILogger<FileFolderService> _logger = logger;

        private static readonly List<string> Folders = [
            App.AppLogFolderPath
            ];
        public static void CreateFolders()
        {
            foreach (var i in Folders.Where(i => !Directory.Exists(i)))
            {
                Directory.CreateDirectory(i);
            }
        }

        public async Task ProcessOldFilesAsync()
        {
            try
            {
                var directoryPath = _settingsService.Settings.AutoSaveStrokesPath;
                var daysThreshold = _settingsService.Settings.AutoDelSavedFilesDays;

                _logger.LogInformation("开始清理旧文件，路径: {DirectoryPath}, 天数阈值: {DaysThreshold}", directoryPath, daysThreshold);

                await Task.Run(() => DeleteFilesOlder(directoryPath, daysThreshold));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理旧文件时发生错误");
            }
        }

        private void DeleteFilesOlder(string directoryPath, int daysThreshold)
        {
            string[] extensionsToDel = { ".icstk", ".png" };
            if (Directory.Exists(directoryPath))
            {
                // 获取目录中的所有子目录
                string[] subDirectories = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories);
                foreach (string subDirectory in subDirectories)
                {
                    try
                    {
                        // 获取子目录下的所有文件
                        string[] files = Directory.GetFiles(subDirectory);
                        foreach (string filePath in files)
                        {
                            // 获取文件的创建日期
                            DateTime creationDate = File.GetCreationTime(filePath);
                            // 获取文件的扩展名
                            string fileExtension = Path.GetExtension(filePath);
                            // 如果文件的创建日期早于指定天数且是要删除的扩展名，则删除文件
                            if (creationDate < DateTime.Now.AddDays(-daysThreshold))
                            {
                                if (Array.Exists(extensionsToDel, ext => ext.Equals(fileExtension, StringComparison.OrdinalIgnoreCase))
                                    || Path.GetFileName(filePath).Equals("Position", StringComparison.OrdinalIgnoreCase))
                                {
                                    File.Delete(filePath);
                                    _logger.LogInformation("已删除文件: {FilePath}", filePath);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "处理文件时出错: {SubDirectory}", subDirectory);
                    }
                }
            }
            else
            {
                _logger.LogWarning("指定的目录不存在: {DirectoryPath}", directoryPath);
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
