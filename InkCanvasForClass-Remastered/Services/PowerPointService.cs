// InkCanvasForClass-Remastered/Services/PowerPointService.cs

using InkCanvasForClass_Remastered.Helpers;
using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Application = Microsoft.Office.Interop.PowerPoint.Application;

namespace InkCanvasForClass_Remastered.Services
{
    public class PowerPointService : IPowerPointService
    {
        private Application _pptApplication;

        public event Action<SlideShowWindow> SlideShowBegin;
        public event Action<Presentation> SlideShowEnd;
        public event Action<SlideShowWindow> SlideShowNextSlide;
        public event Action<Presentation> PresentationOpen;
        public event Action<Presentation> PresentationClose;

        public bool IsConnected => _pptApplication != null;

        public bool IsInSlideShow => _pptApplication?.SlideShowWindows.Count > 0;

        public Presentation ActivePresentation
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

        public SlideShowWindow ActiveSlideShowWindow
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
                _pptApplication = (Application)Marshal2.GetActiveObject("PowerPoint.Application");

                if (_pptApplication != null)
                {
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
                        new Timer(_ => OnPresentationOpen(_pptApplication.ActivePresentation), null, 500, Timeout.Infinite);
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
            catch (Exception)
            {
                // 其他未知错误
                _pptApplication = null;
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
                _pptApplication = null;
            }
        }

        public void GoToPreviousSlide()
        {
            if (!IsConnected || _pptApplication.SlideShowWindows.Count < 1) return;
            new Thread(() =>
            {
                try { _pptApplication.SlideShowWindows[1].View.Previous(); } catch { }
            }).Start();
        }

        public void GoToNextSlide()
        {
            if (!IsConnected || _pptApplication.SlideShowWindows.Count < 1) return;
            new Thread(() =>
            {
                try { _pptApplication.SlideShowWindows[1].View.Next(); } catch { }
            }).Start();
        }

        public void StartSlideShow()
        {
            if (!IsConnected || _pptApplication.Presentations.Count < 1) return;
            new Thread(() =>
            {
                try { _pptApplication.ActivePresentation.SlideShowSettings.Run(); } catch { }
            }).Start();
        }

        public void EndSlideShow()
        {
            if (!IsConnected || _pptApplication.SlideShowWindows.Count < 1) return;
            new Thread(() =>
            {
                try { _pptApplication.SlideShowWindows[1].View.Exit(); } catch { }
            }).Start();
        }

        // 私有的事件转发器
        private void OnPresentationOpen(Presentation Pres) => PresentationOpen?.Invoke(Pres);
        private void OnPresentationClose(Presentation Pres) => PresentationClose?.Invoke(Pres);
        private void OnSlideShowBegin(SlideShowWindow Wn) => SlideShowBegin?.Invoke(Wn);
        private void OnSlideShowEnd(Presentation Pres) => SlideShowEnd?.Invoke(Pres);
        private void OnSlideShowNextSlide(SlideShowWindow Wn) => SlideShowNextSlide?.Invoke(Wn);
    }
}