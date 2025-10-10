using InkCanvasForClass_Remastered.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;

namespace InkCanvasForClass_Remastered.Services
{
    public class SettingsService
    {
        private readonly ILogger<SettingsService> Logger;
        private const string settingsFileName = "Settings.json";
        private Settings _settings = new();

        public Settings Settings => _settings;

        public SettingsService(ILogger<SettingsService> logger)
        {
            _settings = new Settings();
            Logger = logger;
        }

        public void LoadSettings()
        {
            try
            {
                var settingsPath = Path.Combine(App.AppRootFolderPath, settingsFileName);
                if (File.Exists(settingsPath))
                {
                    string text = File.ReadAllText(settingsPath);
                    var loadedSettings = JsonConvert.DeserializeObject<Settings>(text);
                    if (loadedSettings != null)
                    {
                        _settings = loadedSettings;
                    }
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
                var settingsPath = Path.Combine(App.AppRootFolderPath, settingsFileName);
                File.WriteAllText(settingsPath, text);
                Logger.LogInformation("设置被保存");
            }
            catch
            {
                // 可以选择在这里添加日志记录
            }
        }

        /// <summary>
        /// 将当前设置重置为默认值，而无需替换实例。
        /// 这确保了所有现有的数据绑定保持活动状态。
        /// </summary>
        public void ResetToDefaults()
        {
            // 创建一个临时的默认设置实例
            var defaultSettings = new Settings();
            var type = typeof(Settings);

            // 使用反射获取所有可写的公共属性
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.CanWrite);

            // 遍历每个属性，将默认值复制到当前的设置实例中
            foreach (var prop in properties)
            {
                var defaultValue = prop.GetValue(defaultSettings);
                prop.SetValue(Settings, defaultValue);
            }

            // 保存重置后的设置
            SaveSettings();
        }
    }
}