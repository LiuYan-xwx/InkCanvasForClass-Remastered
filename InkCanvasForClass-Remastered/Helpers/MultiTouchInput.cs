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
        }

        public DrawingVisual Visual { get; }
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
        ///     创建显示笔迹的类
        /// </summary>
        /// <param name="drawingAttributes"></param>
        public IncrementalStrokeVisual(DrawingAttributes drawingAttributes)
        {
            _drawingAttributes = drawingAttributes ?? throw new ArgumentNullException(nameof(drawingAttributes));
            
            // 初始化Pen和Brush
            _brush = new SolidColorBrush(drawingAttributes.Color);
            _brush.Freeze();
            
            _pen = new Pen(_brush, drawingAttributes.Width)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };
            _pen.Freeze();
            
            // 初始化几何体
            _geometry = new StreamGeometry();
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
        ///     增量重绘 - 使用StreamGeometry构建路径
        /// </summary>
        private void RedrawIncremental()
        {
            if (_points.Count == 0)
                return;
                
            // 重新构建StreamGeometry
            _geometry = new StreamGeometry();
            
            using (var context = _geometry.Open())
            {
                // 开始第一个图形
                context.BeginFigure(_points[0], false, false);
                
                // 批量添加所有点
                for (int i = 1; i < _points.Count; i++)
                {
                    context.LineTo(_points[i], true, false);
                }
            }
            
            // 绘制几何体
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
