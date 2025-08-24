using InkCanvasForClass_Remastered.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Office.Interop.PowerPoint;
using System.Runtime.InteropServices;
using Application = Microsoft.Office.Interop.PowerPoint.Application;

namespace InkCanvasForClass_Remastered.Services
{
    public class PowerPointService : IPowerPointService
    {
        private readonly ILogger<PowerPointService> Logger;

        public PowerPointService(ILogger<PowerPointService> logger)
        {
            Logger = logger;
        }

        private Application? _pptApplication;

        public event Action<SlideShowWindow> SlideShowBegin;
        public event Action<Presentation> SlideShowEnd;
        public event Action<SlideShowWindow> SlideShowNextSlide;
        public event Action<Presentation> PresentationOpen;
        public event Action<Presentation> PresentationClose;

        public bool IsConnected => _pptApplication != null;

        public bool IsInSlideShow => _pptApplication?.SlideShowWindows.Count > 0;

        public Presentation? ActivePresentation
        {
            get
            {
                try
                {
                    return _pptApplication?.ActivePresentation;
                }
                catch { return null; }
            }
        }

        public SlideShowWindow? ActiveSlideShowWindow
        {
            get
            {
                try
                {
                    if (_pptApplication?.SlideShowWindows.Count > 0)
                    {
                        return _pptApplication.SlideShowWindows[1];
                    }
                    return null;
                }
                catch { return null; }
            }
        }

        public bool TryConnectAndMonitor()
        {
            if (_pptApplication != null)
            {
                return true; // 已经连接
            }

            try
            {
                // 尝试获取正在运行的PowerPoint实例
                _pptApplication = Marshal2.GetActiveObject("PowerPoint.Application") as Application;

                if (_pptApplication != null)
                {
                    Logger.LogInformation("成功连接到 PowerPoint 应用");
                    // 挂载事件处理器
                    _pptApplication.PresentationOpen += OnPresentationOpen;
                    _pptApplication.PresentationClose += OnPresentationClose;
                    _pptApplication.SlideShowBegin += OnSlideShowBegin;
                    _pptApplication.SlideShowNextSlide += OnSlideShowNextSlide;
                    _pptApplication.SlideShowEnd += OnSlideShowEnd;

                    // 如果已经有打开的文档，手动触发一次Open事件
                    if (_pptApplication.Presentations.Count > 0)
                    {
                        // 延迟一小段时间再触发，确保MainWindow已经加载完毕
                        _ = new Timer(_ => OnPresentationOpen(_pptApplication.ActivePresentation), null, 500, Timeout.Infinite);
                    }

                    return true;
                }
            }
            catch (COMException)
            {
                // 没有找到运行中的PowerPoint实例
                _pptApplication = null;
                return false;
            }
            catch (Exception ex)
            {
                // 其他未知错误
                _pptApplication = null;
                Logger.LogWarning(ex, "连接 PowerPoint 时发生未知错误");
                return false;
            }
            return false;
        }

        public void Disconnect()
        {
            if (_pptApplication != null)
            {
                // 解除事件挂载
                _pptApplication.PresentationOpen -= OnPresentationOpen;
                _pptApplication.PresentationClose -= OnPresentationClose;
                _pptApplication.SlideShowBegin -= OnSlideShowBegin;
                _pptApplication.SlideShowNextSlide -= OnSlideShowNextSlide;
                _pptApplication.SlideShowEnd -= OnSlideShowEnd;

                // 释放COM对象
                Marshal.ReleaseComObject(_pptApplication);
                Logger.LogInformation("已断开与 PowerPoint 应用的连接");
                _pptApplication = null;
            }
        }

        public void GoToPreviousSlide()
        {
            if (!IsConnected || _pptApplication.SlideShowWindows.Count < 1)
                return;
            try
            {
                _pptApplication.SlideShowWindows[1].View.Previous();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "切换到上一张幻灯片失败");
            }
        }

        public void GoToNextSlide()
        {
            if (!IsConnected || _pptApplication.SlideShowWindows.Count < 1) return;
            try
            {
                _pptApplication.SlideShowWindows[1].View.Next();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "切换到下一张幻灯片失败");
            }
        }

        public void EndSlideShow()
        {
            if (!IsConnected || _pptApplication.SlideShowWindows.Count < 1) return;
            try
            {
                _pptApplication.SlideShowWindows[1].View.Exit();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "结束幻灯片放映失败");
            }
        }

        // 私有的事件转发器
        private void OnPresentationOpen(Presentation Pres) => PresentationOpen?.Invoke(Pres);
        private void OnPresentationClose(Presentation Pres) => PresentationClose?.Invoke(Pres);
        private void OnSlideShowBegin(SlideShowWindow Wn) => SlideShowBegin?.Invoke(Wn);
        private void OnSlideShowEnd(Presentation Pres) => SlideShowEnd?.Invoke(Pres);
        private void OnSlideShowNextSlide(SlideShowWindow Wn) => SlideShowNextSlide?.Invoke(Wn);
    }
}