using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InkCanvasForClass_Remastered.Services
{
    namespace InkCanvasForClass_Remastered.Services
    {
        public interface ISettingsService
        {
            /// <summary>
            /// 获取当前加载的设置对象。
            /// </summary>
            Settings Current { get; }

            /// <summary>
            /// 从文件加载设置。
            /// </summary>
            void LoadSettings();

            /// <summary>
            /// 将当前设置保存到文件。
            /// </summary>
            void SaveSettings();
            void ReplaceSettings(Settings newSettings);
        }
    }
}
