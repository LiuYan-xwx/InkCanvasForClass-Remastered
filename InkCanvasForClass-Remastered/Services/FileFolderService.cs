using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InkCanvasForClass_Remastered.Services
{
    public class FileFolderService : IHostedService
    {
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

        public async Task StartAsync(CancellationToken cancellationToken)
        {
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }
    }
}
