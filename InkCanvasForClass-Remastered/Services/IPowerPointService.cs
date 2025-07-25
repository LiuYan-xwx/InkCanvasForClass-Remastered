// InkCanvasForClass-Remastered/Services/IPowerPointService.cs

using Microsoft.Office.Interop.PowerPoint;
using System;

namespace InkCanvasForClass_Remastered.Services
{
    public interface IPowerPointService
    {
        /// <summary>
        /// 当PPT演示开始时触发。
        /// </summary>
        event Action<SlideShowWindow> SlideShowBegin;

        /// <summary>
        /// 当PPT演示结束时触发。
        /// </summary>
        event Action<Presentation> SlideShowEnd;

        /// <summary>
        /// 当切换到下一张幻灯片时触发。
        /// </summary>
        event Action<SlideShowWindow> SlideShowNextSlide;

        /// <summary>
        /// 当打开一个新的PPT文档时触发。
        /// </summary>
        event Action<Presentation> PresentationOpen;

        /// <summary>
        /// 当关闭一个PPT文档时触发。
        /// </summary>
        event Action<Presentation> PresentationClose;

        /// <summary>
        /// 获取当前是否连接到PowerPoint应用。
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 获取当前是否正在放映幻灯片。
        /// </summary>
        bool IsInSlideShow { get; }

        /// <summary>
        /// 获取当前活动的演示文稿对象。如果不存在则为 null。
        /// </summary>
        Presentation ActivePresentation { get; }

        /// <summary>
        /// 获取当前活动的放映窗口对象。如果不存在则为 null。
        /// </summary>
        SlideShowWindow ActiveSlideShowWindow { get; }

        /// <summary>
        /// 尝试连接到正在运行的PowerPoint实例并开始监视事件。
        /// </summary>
        /// <returns>如果成功连接则返回 true，否则返回 false。</returns>
        bool TryConnectAndMonitor();

        /// <summary>
        /// 断开与PowerPoint的连接并停止监视事件。
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 切换到上一张幻灯片。
        /// </summary>
        void GoToPreviousSlide();

        /// <summary>
        /// 切换到下一张幻灯片。
        /// </summary>
        void GoToNextSlide();

        /// <summary>
        /// 开始幻灯片放映。
        /// </summary>
        void StartSlideShow();

        /// <summary>
        /// 结束幻灯片放映。
        /// </summary>
        void EndSlideShow();
    }
}