using InkCanvasForClass_Remastered.Models;

namespace InkCanvasForClass_Remastered.Services
{
    public interface ISettingsService
    {
        /// <summary>
        /// 获取当前加载的设置对象。
        /// </summary>
        Settings Settings { get; }

        /// <summary>
        /// 从文件加载设置。
        /// </summary>
        void LoadSettings();

        /// <summary>
        /// 将当前设置保存到文件。
        /// </summary>
        void SaveSettings();

        /// <summary>
        /// 将当前的设置实例重置为默认值，同时保持实例不变以维持数据绑定。
        /// </summary>
        void ResetToDefaults();
    }
}
