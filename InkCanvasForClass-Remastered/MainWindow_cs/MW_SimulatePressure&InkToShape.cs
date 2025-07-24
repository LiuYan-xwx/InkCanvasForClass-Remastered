using InkCanvasForClass_Remastered.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace InkCanvasForClass_Remastered {
    public partial class MainWindow : Window {
        private StrokeCollection newStrokes = new StrokeCollection();
        private List<Circle> circles = new List<Circle>();

        private void inkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e) {
            if (Settings.Canvas.FitToCurve == true) drawingAttributes.FitToCurve = false;

            try {
                inkCanvas.Opacity = 1;

                foreach (var stylusPoint in e.Stroke.StylusPoints)
                    //LogHelper.WriteLogToFile(stylusPoint.PressureFactor.ToString(), LogHelper.LogType.Info);
                    // 检查是否是压感笔书写
                    //if (stylusPoint.PressureFactor != 0.5 && stylusPoint.PressureFactor != 0)
                    if ((stylusPoint.PressureFactor > 0.501 || stylusPoint.PressureFactor < 0.5) &&
                        stylusPoint.PressureFactor != 0)
                        return;

                try {
                    if (e.Stroke.StylusPoints.Count > 3) {
                        var random = new Random();
                        var _speed = GetPointSpeed(
                            e.Stroke.StylusPoints[random.Next(0, e.Stroke.StylusPoints.Count - 1)].ToPoint(),
                            e.Stroke.StylusPoints[random.Next(0, e.Stroke.StylusPoints.Count - 1)].ToPoint(),
                            e.Stroke.StylusPoints[random.Next(0, e.Stroke.StylusPoints.Count - 1)].ToPoint());

                        RandWindow.randSeed = (int)(_speed * 100000 * 1000);
                    }
                }
                catch { }

                switch (Settings.Canvas.InkStyle) {
                    case 1:
                        if (penType == 0)
                            try {
                                var stylusPoints = new StylusPointCollection();
                                var n = e.Stroke.StylusPoints.Count - 1;
                                var s = "";

                                for (var i = 0; i <= n; i++) {
                                    var speed = GetPointSpeed(e.Stroke.StylusPoints[Math.Max(i - 1, 0)].ToPoint(),
                                        e.Stroke.StylusPoints[i].ToPoint(),
                                        e.Stroke.StylusPoints[Math.Min(i + 1, n)].ToPoint());
                                    s += speed.ToString() + "\t";
                                    var point = new StylusPoint();
                                    if (speed >= 0.25)
                                        point.PressureFactor = (float)(0.5 - 0.3 * (Math.Min(speed, 1.5) - 0.3) / 1.2);
                                    else if (speed >= 0.05)
                                        point.PressureFactor = (float)0.5;
                                    else
                                        point.PressureFactor = (float)(0.5 + 0.4 * (0.05 - speed) / 0.05);

                                    point.X = e.Stroke.StylusPoints[i].X;
                                    point.Y = e.Stroke.StylusPoints[i].Y;
                                    stylusPoints.Add(point);
                                }

                                e.Stroke.StylusPoints = stylusPoints;
                            }
                            catch { }

                        break;
                    case 0:
                        if (penType == 0)
                            try {
                                var stylusPoints = new StylusPointCollection();
                                var n = e.Stroke.StylusPoints.Count - 1;
                                var pressure = 0.1;
                                var x = 10;
                                if (n == 1) return;
                                if (n >= x) {
                                    for (var i = 0; i < n - x; i++) {
                                        var point = new StylusPoint();

                                        point.PressureFactor = (float)0.5;
                                        point.X = e.Stroke.StylusPoints[i].X;
                                        point.Y = e.Stroke.StylusPoints[i].Y;
                                        stylusPoints.Add(point);
                                    }

                                    for (var i = n - x; i <= n; i++) {
                                        var point = new StylusPoint();

                                        point.PressureFactor = (float)((0.5 - pressure) * (n - i) / x + pressure);
                                        point.X = e.Stroke.StylusPoints[i].X;
                                        point.Y = e.Stroke.StylusPoints[i].Y;
                                        stylusPoints.Add(point);
                                    }
                                }
                                else {
                                    for (var i = 0; i <= n; i++) {
                                        var point = new StylusPoint();

                                        point.PressureFactor = (float)(0.4 * (n - i) / n + pressure);
                                        point.X = e.Stroke.StylusPoints[i].X;
                                        point.Y = e.Stroke.StylusPoints[i].Y;
                                        stylusPoints.Add(point);
                                    }
                                }

                                e.Stroke.StylusPoints = stylusPoints;
                            }
                            catch { }

                        break;
                }
            }
            catch { }

            if (Settings.Canvas.FitToCurve == true) drawingAttributes.FitToCurve = true;
        }

        private void SetNewBackupOfStroke() {
            lastTouchDownStrokeCollection = inkCanvas.Strokes.Clone();
            var whiteboardIndex = CurrentWhiteboardIndex;
            if (currentMode == 0) whiteboardIndex = 0;

            strokeCollections[whiteboardIndex] = lastTouchDownStrokeCollection;
        }

        public double GetDistance(Point point1, Point point2) {
            return Math.Sqrt((point1.X - point2.X) * (point1.X - point2.X) +
                             (point1.Y - point2.Y) * (point1.Y - point2.Y));
        }

        public double GetPointSpeed(Point point1, Point point2, Point point3) {
            return (Math.Sqrt((point1.X - point2.X) * (point1.X - point2.X) +
                              (point1.Y - point2.Y) * (point1.Y - point2.Y))
                    + Math.Sqrt((point3.X - point2.X) * (point3.X - point2.X) +
                                (point3.Y - point2.Y) * (point3.Y - point2.Y)))
                   / 20;
        }

        public Point[] FixPointsDirection(Point p1, Point p2) {
            if (Math.Abs(p1.X - p2.X) / Math.Abs(p1.Y - p2.Y) > 8) {
                //水平
                var x = Math.Abs(p1.Y - p2.Y) / 2;
                if (p1.Y > p2.Y) {
                    p1.Y -= x;
                    p2.Y += x;
                }
                else {
                    p1.Y += x;
                    p2.Y -= x;
                }
            }
            else if (Math.Abs(p1.Y - p2.Y) / Math.Abs(p1.X - p2.X) > 8) {
                //垂直
                var x = Math.Abs(p1.X - p2.X) / 2;
                if (p1.X > p2.X) {
                    p1.X -= x;
                    p2.X += x;
                }
                else {
                    p1.X += x;
                    p2.X -= x;
                }
            }

            return new Point[2] { p1, p2 };
        }

        public Point GetCenterPoint(Point point1, Point point2) {
            return new Point((point1.X + point2.X) / 2, (point1.Y + point2.Y) / 2);
        }

        public StylusPoint GetCenterPoint(StylusPoint point1, StylusPoint point2) {
            return new StylusPoint((point1.X + point2.X) / 2, (point1.Y + point2.Y) / 2);
        }
    }
}