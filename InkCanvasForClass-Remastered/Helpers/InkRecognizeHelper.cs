using System.Windows;
using System.Windows.Ink;

namespace InkCanvasForClass_Remastered.Helpers
{
    public class InkRecognizeHelper
    {

    }

    //用于自动控制其他形状相对于圆的位置

    public class Circle
    {
        public Circle(Point centroid, double r, Stroke stroke)
        {
            Centroid = centroid;
            R = r;
            Stroke = stroke;
        }

        public Point Centroid { get; set; }

        public double R { get; set; }

        public Stroke Stroke { get; set; }
    }
}
