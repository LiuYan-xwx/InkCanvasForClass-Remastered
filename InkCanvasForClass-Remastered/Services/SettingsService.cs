using InkCanvasForClass_Remastered.Services.InkCanvasForClass_Remastered.Services;
using Newtonsoft.Json;
using System.IO;

namespace InkCanvasForClass_Remastered.Services
{
    public class SettingsService : ISettingsService
    {
        private const string settingsFileName = "Settings.json";
        private Settings _settings = new Settings();

        public Settings Current => _settings;

        public void LoadSettings()
        {
            try
            {
                var settingsPath = Path.Combine(App.RootPath, settingsFileName);
                if (File.Exists(settingsPath))
                {
                    string text = File.ReadAllText(settingsPath);
                    _settings = JsonConvert.DeserializeObject<Settings>(text) ?? new Settings();
                }
                else
                {
                    // 如果文件不存在，则创建一个新的默认设置并保存它
                    _settings = new Settings();
                    SaveSettings();
                }
            }
            catch
            {
                // 如果加载失败，使用默认设置
                _settings = new Settings();
            }
        }

        public void SaveSettings()
        {
            try
            {
                var text = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                var settingsPath = Path.Combine(App.RootPath, settingsFileName);
                File.WriteAllText(settingsPath, text);
            }
            catch
            {
                // 可以选择在这里添加日志记录
            }
        }

        public void ReplaceSettings(Settings newSettings)
        {
            _settings = newSettings ?? new Settings();
        }
    }
}