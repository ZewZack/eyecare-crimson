using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace eyecarebyzewzack.Helpers;

public class Arc : Shape
{
    public static readonly DependencyProperty StartAngleProperty =
        DependencyProperty.Register("StartAngle", typeof(double), typeof(Arc),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty EndAngleProperty =
        DependencyProperty.Register("EndAngle", typeof(double), typeof(Arc),
            new FrameworkPropertyMetadata(360.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double StartAngle
    {
        get => (double)GetValue(StartAngleProperty);
        set => SetValue(StartAngleProperty, value);
    }

    public double EndAngle
    {
        get => (double)GetValue(EndAngleProperty);
        set => SetValue(EndAngleProperty, value);
    }

    protected override Geometry DefiningGeometry
    {
        get
        {
            double stroke = StrokeThickness;
            double w = Math.Max(0, ActualWidth - stroke);
            double h = Math.Max(0, ActualHeight - stroke);
            double r = Math.Min(w, h) / 2.0;

            if (r <= 0) return Geometry.Empty;

            double cx = ActualWidth / 2.0;
            double cy = ActualHeight / 2.0;

            double sweep = EndAngle - StartAngle;
            if (sweep < 0) sweep = 0;
            if (sweep >= 360) sweep = 359.999;

            double startRad = (StartAngle - 90.0) * Math.PI / 180.0;
            double endRad = (StartAngle + sweep - 90.0) * Math.PI / 180.0;

            Point startPoint = new Point(cx + r * Math.Cos(startRad), cy + r * Math.Sin(startRad));
            Point endPoint = new Point(cx + r * Math.Cos(endRad), cy + r * Math.Sin(endRad));

            bool isLargeArc = sweep > 180.0;

            StreamGeometry geom = new StreamGeometry();
            using (StreamGeometryContext ctx = geom.Open())
            {
                ctx.BeginFigure(startPoint, false, false);
                ctx.ArcTo(endPoint, new Size(r, r), 0, isLargeArc, SweepDirection.Clockwise, true, false);
            }

            geom.Freeze();
            return geom;
        }
    }
}
