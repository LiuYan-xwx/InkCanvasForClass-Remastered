using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Office.Interop.PowerPoint;

namespace InkCanvasForClass_Remastered.Interfaces
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
        Presentation? ActivePresentation { get; }

        /// <summary>
        /// 获取当前活动的放映窗口对象。如果不存在则为 null。
        /// </summary>
        SlideShowWindow? ActiveSlideShowWindow { get; }

        /// <summary>
        /// 尝试连接到正在运行的PowerPoint实例并开始监听事件。
        /// </summary>
        /// <returns>如果成功连接则返回 true，否则返回 false。</returns>
        bool TryConnectToPowerPoint();

        /// <summary>
        /// 断开与PowerPoint的连接并停止监听事件。
        /// </summary>
        void DisconnectFromPowerPoint();

        /// <summary>
        /// 切换到上一张幻灯片。
        /// </summary>
        void GoToPreviousSlide();

        /// <summary>
        /// 切换到下一张幻灯片。
        /// </summary>
        void GoToNextSlide();

        /// <summary>
        /// 当前幻灯片的页码，没有幻灯片时为 -1。
        /// </summary>
        int CurrentSlidePosition { get; }

        /// <summary>
        /// 总幻灯片数，没有幻灯片时为 -1。
        /// </summary>
        int CurrentPresentationSlideCount { get; }

        /// <summary>
        /// 当前演示文稿的名称，没有演示文稿时为 null。
        /// </summary>
        string? CurrentPresentationName { get; }

        /// <summary>
        /// 结束幻灯片放映。
        /// </summary>
        void EndSlideShow();
    }
}