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

        public VisualCanvas(Visual visual)
        {
            Visual = visual;
            AddVisualChild(visual);
        }

        public Visual Visual { get; }
    }

    /// <summary>
    ///     用于显示笔迹的类。
    ///     继承 ContainerVisual，每次 StylusMove 仅创建新 DrawingVisual 片段追加到 Children，
    ///     已渲染的旧片段不再重绘，从 O(N²) 降为 O(N)。
    /// </summary>
    public class StrokeVisual : ContainerVisual
    {
        /// <summary>
        ///     创建显示笔迹的类
        /// </summary>
        /// <param name="drawingAttributes"></param>
        public StrokeVisual(DrawingAttributes drawingAttributes)
        {
            _drawingAttributes = drawingAttributes ?? throw new ArgumentNullException(nameof(drawingAttributes));

            var brush = new SolidColorBrush(drawingAttributes.Color);
            if (brush.CanFreeze) brush.Freeze();
            var pen = new Pen(brush, drawingAttributes.Width)
            {
                StartLineCap = drawingAttributes.StylusTip == StylusTip.Rectangle ? PenLineCap.Flat : PenLineCap.Round,
                EndLineCap = drawingAttributes.StylusTip == StylusTip.Rectangle ? PenLineCap.Flat : PenLineCap.Round,
            };
            
            if (pen.CanFreeze) pen.Freeze();
            _pen = pen;
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
        ///     增量渲染：仅将新增线段写入新的 DrawingVisual 并追加到 Children，
        ///     旧片段保留不动，无需重绘。
        /// </summary>
        public void Redraw()
        {
            if (Stroke == null)
                return;

            var allPoints = Stroke.StylusPoints;
            var count = allPoints.Count;
            if (count == _renderedPointCount)
                return;

            var startIdx = Math.Max(0, _renderedPointCount - 1);

            var segVisual = new DrawingVisual();
            using (var dc = segVisual.RenderOpen())
            {
                for (var i = startIdx; i < count - 1; i++)
                {
                    dc.DrawLine(_pen, (Point)allPoints[i], (Point)allPoints[i + 1]);
                }
            }

            Children.Add(segVisual);
            _renderedPointCount = count;
        }

        private readonly DrawingAttributes _drawingAttributes;
        private readonly Pen _pen;
        private int _renderedPointCount;

        public static implicit operator Stroke?(StrokeVisual v)
        {
            return v?.Stroke;
        }
    }
}
