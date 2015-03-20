using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfInterface
{
    /// <summary>
    /// Class used to draw over the canvas. Based on the drawing class found in Vitrivius 
    /// </summary>
    static class DrawingUtils
    {
        private static Color DEFAULT_COLOR = Colors.LightCyan;

        private static int DEFAULT_ELLIPSE_RADIUS = 10;

        private static int DEFAULT_LINE_THICKNESS = 4;

        public static void DrawPoint(Canvas canvas, Point point, Color color, double radius, string tag)
        {
            Ellipse ellipse = new Ellipse
            {
                Tag = tag,
                Width = radius,
                Height = radius,
                Fill = new SolidColorBrush(color)
            };

            Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

            canvas.Children.Add(ellipse);
        }

        public static void DrawPoint(Canvas canvas, Point joint, Color color, string tag)
        {
            DrawPoint(canvas, joint, color, DEFAULT_ELLIPSE_RADIUS, tag);
        }

        public static void DrawPoint(Canvas canvas, Point joint, string tag)
        {
            DrawPoint(canvas, joint, DEFAULT_COLOR, DEFAULT_ELLIPSE_RADIUS, tag);
        }

        public static void DrawLine(Canvas canvas, Point start, Point end, Color color, double thickness, string tag)
        {
            Line line = new Line
            {
                Tag = tag,
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                StrokeThickness = thickness,
                Stroke = new SolidColorBrush(color)
            };

            canvas.Children.Add(line);
        }

        public static void DrawLine(Canvas canvas, Point start, Point end, Color color, string tag)
        {
            DrawLine(canvas, start, end, color, DEFAULT_LINE_THICKNESS, tag);
        }

        public static void DrawLine(Canvas canvas, Point start, Point end, string tag)
        {
            DrawLine(canvas, start, end, DEFAULT_COLOR, DEFAULT_LINE_THICKNESS, tag);
        }

        public static void deleteElements(this Canvas canvas, string tag)
        { 
            List<UIElement> deleteList = new List<UIElement>();
            foreach (UIElement elem in canvas.Children)
            {
                if (elem is Shape)
                {
                    Shape shape = elem as Shape;
                    if (shape.Tag != null && shape.Tag.ToString() == tag)
                    {
                        deleteList.Add(elem);
                    }
                }
            }
            foreach (UIElement elem in deleteList)
            {
                canvas.Children.Remove(elem);
            }
        }
    }
}
