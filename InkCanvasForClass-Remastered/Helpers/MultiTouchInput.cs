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
}
