using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace InkCanvasForClass_Remastered.Helpers
{
    public class VisualCanvas : FrameworkElement
    {
        protected override Visual GetVisualChild(int index) => Visual;

        protected override int VisualChildrenCount => 1;

        public VisualCanvas(DrawingVisual visual)
        {
            Visual = visual;
            AddVisualChild(visual);
            
            // GPU优化：启用硬件加速渲染
            SetGPUOptimizations();
        }

        public DrawingVisual Visual { get; }
        
        /// <summary>
        ///     设置GPU优化渲染选项
        /// </summary>
        private void SetGPUOptimizations()
        {
            try
            {
                // GPU关键优化：启用缓存和硬件加速
                RenderOptions.SetCachingHint(this, CachingHint.Cache);
                RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
                RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
                
                // 布局优化减少GPU计算
                SnapsToDevicePixels = true;
                UseLayoutRounding = true;
                
                // 缓存呈现的视觉效果
                CacheMode = new BitmapCache();
            }
            catch
            {
                // 如果GPU优化失败，静默忽略
                // 在某些系统上某些设置可能不支持
            }
        }
    }

    /// <summary>
    ///     用于显示笔迹的类
    /// </summary>
    public class StrokeVisual : DrawingVisual
    {
        /// <summary>
        ///     创建显示笔迹的类
        /// </summary>
        /// <param name="drawingAttributes"></param>
        public StrokeVisual(DrawingAttributes drawingAttributes)
        {
            _drawingAttributes = drawingAttributes ?? throw new ArgumentNullException(nameof(drawingAttributes));
        }

        /// <summary>
        ///     设置或获取显示的笔迹
        /// </summary>
        public Stroke Stroke { get; private set; }

        /// <summary>
        ///     在笔迹中添加点
        /// </summary>
        /// <param name="point"></param>
        public void Add(StylusPoint point)
        {
            if (Stroke == null)
            {
                StylusPointCollection collection = [point];
                Stroke = new Stroke(collection, _drawingAttributes);
            }
            else
            {
                Stroke.StylusPoints.Add(point);
            }
        }

        /// <summary>
        /// 在笔迹中批量添加点
        /// </summary>
        /// <param name="points"></param>
        public void AddRange(StylusPointCollection points)
        {
            if (points == null || points.Count == 0)
                return;
            if (Stroke == null)
            {
                Stroke = new Stroke(points.Clone(), _drawingAttributes);
            }
            else
            {
                Stroke.StylusPoints.Add(points);
            }
        }

        /// <summary>
        ///     重新画出笔迹
        /// </summary>
        public void Redraw()
        {
            if (Stroke == null)
                return;
            using DrawingContext dc = RenderOpen();
            Stroke.Draw(dc);
        }

        private readonly DrawingAttributes _drawingAttributes;

        public static implicit operator Stroke?(StrokeVisual v)
        {
            return v?.Stroke;
        }
    }

    /// <summary>
    ///     增量绘制笔迹的类
    ///     使用StreamGeometry实现高性能增量绘制
    /// </summary>
    public class IncrementalStrokeVisual : DrawingVisual
    {
        private StreamGeometry _geometry;
        private readonly List<Point> _points = new();
        private bool _isFigureOpen = false;
        private readonly DrawingAttributes _drawingAttributes;
        private Pen _pen;
        private Brush _brush;
        private DateTime _lastRedrawTime = DateTime.MinValue;
        private const int MaxRedrawIntervalMs = 16; // ~60fps
        private const int RedrawPointsThreshold = 3; // 每N个点重绘一次
        
        /// <summary>
        ///     创建显示笔迹的类（GPU优化版本）
        /// </summary>
        /// <param name="drawingAttributes"></param>
        public IncrementalStrokeVisual(DrawingAttributes drawingAttributes)
        {
            _drawingAttributes = drawingAttributes ?? throw new ArgumentNullException(nameof(drawingAttributes));
            
            // GPU优化：创建并冻结笔刷和画笔，使其可被GPU硬件加速
            CreateGpuOptimizedDrawingResources(drawingAttributes);
            
            // 初始化几何体
            _geometry = new StreamGeometry();
        }
        
        /// <summary>
        ///     创建GPU优化的绘制资源
        /// </summary>
        private void CreateGpuOptimizedDrawingResources(DrawingAttributes drawingAttributes)
        {
            try
            {
                // 创建笔刷并启用GPU加速
                _brush = new SolidColorBrush(drawingAttributes.Color);
                
                // GPU优化：设置笔刷的渲染选项
                RenderOptions.SetCachingHint(_brush, CachingHint.Cache);
                RenderOptions.SetBitmapScalingMode(_brush, BitmapScalingMode.LowQuality); // 对于笔迹，低质量缩放足够
                
                // GPU关键优化：冻结笔刷，使其变为只读且可被GPU缓存
                _brush.Freeze();
                
                // 创建画笔并启用GPU加速
                _pen = new Pen(_brush, drawingAttributes.Width)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round
                };
                
                // 对于非常细的线条，使用更适合GPU的笔尖形状
                if (drawingAttributes.Width < 3)
                {
                    _pen.DashCap = PenLineCap.Round;
                }
                
                // GPU优化：冻结画笔
                _pen.Freeze();
            }
            catch (Exception ex)
            {
                // 如果GPU优化失败，使用传统创建方法
                System.Diagnostics.Debug.WriteLine($"GPU优化绘图资源创建失败: {ex.Message}");
                CreateFallbackDrawingResources(drawingAttributes);
            }
        }
        
        /// <summary>
        ///     创建传统绘制资源（GPU加速回退）
        /// </summary>
        private void CreateFallbackDrawingResources(DrawingAttributes drawingAttributes)
        {
            _brush = new SolidColorBrush(drawingAttributes.Color);
            _pen = new Pen(_brush, drawingAttributes.Width)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };
        }

        /// <summary>
        ///     在笔迹中批量添加点
        /// </summary>
        /// <param name="points"></param>
        public void AddRangeIncremental(StylusPointCollection points)
        {
            if (points == null || points.Count == 0)
                return;
            
            try
            {
                // 添加点到缓存
                foreach (var point in points)
                {
                    _points.Add(new Point(point.X, point.Y));
                }
                
                // 节流重绘：基于时间和点数的双重控制
                var now = DateTime.Now;
                bool shouldRedraw = false;
                
                if (_points.Count == 1)
                {
                    // 第一个点，立即绘制
                    shouldRedraw = true;
                }
                else if (!_isFigureOpen)
                {
                    // 有足够点数但未开始绘制
                    shouldRedraw = _points.Count >= 2;
                }
                else
                {
                    // 已有图形，节流控制
                    var timeSinceLastRedraw = (now - _lastRedrawTime).TotalMilliseconds;
                    var newPointsCount = _points.Count - (RedrawPointsThreshold * (int)(_points.Count / RedrawPointsThreshold));
                    
                    shouldRedraw = timeSinceLastRedraw >= MaxRedrawIntervalMs || 
                                   newPointsCount >= RedrawPointsThreshold;
                }
                
                if (shouldRedraw)
                {
                    RedrawIncremental();
                    _lastRedrawTime = now;
                }
            }
            catch (Exception ex)
            {
                // 如果增量绘制失败，使用传统Stroke作为回退
                System.Diagnostics.Debug.WriteLine($"增量绘制失败: {ex.Message}");
                FallbackRedraw(points);
            }
        }
        
        /// <summary>
        ///     增量重绘 - 使用StreamGeometry构建路径（GPU优化版本）
        /// </summary>
        private void RedrawIncremental()
        {
            if (_points.Count == 0)
                return;
                
            try
            {
                // 重新构建StreamGeometry
                _geometry = new StreamGeometry();
                
                // GPU优化：将冻结几何体，使其可被GPU缓存
                _geometry.FillRule = FillRule.Nonzero;
                
                using (var context = _geometry.Open())
                {
                    // 开始第一个图形
                    context.BeginFigure(_points[0], false, false);
                    
                    // 批量添加所有点
                    // 优化：批量写入点集，减少上下文切换
                    for (int i = 1; i < _points.Count; i++)
                    {
                        context.LineTo(_points[i], true, false);
                    }
                }
                
                // GPU优化：冻结几何体，使其可被GPU硬件加速
                _geometry.Freeze();
                
                // 绘制几何体
                using DrawingContext dc = RenderOpen();
                
                // GPU优化：使用缓存的笔刷和画笔
                dc.DrawGeometry(_brush, _pen, _geometry);
                
                _isFigureOpen = true;
            }
            catch (Exception ex)
            {
                // 如果GPU加速失败，使用传统方法并记录
                System.Diagnostics.Debug.WriteLine($"GPU加速重绘失败: {ex.Message}");
                RedrawIncrementalFallback();
            }
        }
        
        /// <summary>
        ///     回退重绘方法（不使用GPU加速）
        /// </summary>
        private void RedrawIncrementalFallback()
        {
            if (_points.Count == 0)
                return;
                
            // 重新构建StreamGeometry但不冻结
            _geometry = new StreamGeometry();
            
            using (var context = _geometry.Open())
            {
                context.BeginFigure(_points[0], false, false);
                
                for (int i = 1; i < _points.Count; i++)
                {
                    context.LineTo(_points[i], true, false);
                }
            }
            
            // 不使用Freeze
            using DrawingContext dc = RenderOpen();
            dc.DrawGeometry(_brush, _pen, _geometry);
            
            _isFigureOpen = true;
        }
        
        /// <summary>
        ///     回退绘制 - 使用传统Stroke对象
        /// </summary>
        private void FallbackRedraw(StylusPointCollection points)
        {
            try
            {
                var stroke = new Stroke(points.Clone(), _drawingAttributes);
                using DrawingContext dc = RenderOpen();
                stroke.Draw(dc);
                _isFigureOpen = true;
            }
            catch
            {
                // 如果回退也失败，至少清空显示
                using DrawingContext dc = RenderOpen();
            }
        }
        
        /// <summary>
        ///     获取最终的Stroke对象（用于抬起后的处理）
        /// </summary>
        public Stroke? GetStroke()
        {
            if (_points.Count < 2)
                return null;
                
            try
            {
                // 从点集创建Stroke
                var pointCollection = new StylusPointCollection();
                foreach (var point in _points)
                {
                    // 使用默认压力因子0.5f，与实际使用场景一致
                    pointCollection.Add(new StylusPoint(point.X, point.Y, 0.5f));
                }
                
                var stroke = new Stroke(pointCollection, _drawingAttributes);
                
                // 如果设置中启用了曲线拟合，应用之
                // 注意：原始StrokeVisual不控制这个，由后续的inkCanvas_StrokeCollected处理
                return stroke;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        ///     清空所有数据
        /// </summary>
        public void Clear()
        {
            _points.Clear();
            _geometry = new StreamGeometry();
            _isFigureOpen = false;
            
            // 清空显示
            using DrawingContext dc = RenderOpen();
        }
        
        /// <summary>
        ///     获取当前点数量
        /// </summary>
        public int PointCount => _points.Count;
    }
}
