using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using MongooseSoftware.Robotics.RobotLib;
using System.ComponentModel;
using System.Threading;
using MongooseSoftware.Robotics.RobotLib.Components;

namespace MongooseSoftware.Robotics.UI
{
    /// <summary>
    /// Interaction logic for CompassVisualizer.xaml
    /// </summary>
    public partial class CompassVisualizer : RobotComponentUserControl
    {
        #region Constructors

        public CompassVisualizer() 
        {
            InitializeComponent();
        }

        #endregion


        #region Methods

        protected override void Component_PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            base.Component_PropertyChanged(sender, args);
            
            switch (args.PropertyName)
            {
                case "CompassHeading":
                    Dispatcher.BeginInvoke((ThreadStart)InvalidateVisual);
                    break;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // Draw compass circle.
            Point center = new Point(ActualWidth / 2, (ActualHeight - 25) / 2);
            double radius = Math.Min(ActualWidth / 2, (ActualHeight - 25) / 2) - 10;
            drawingContext.DrawEllipse(null, new Pen(new SolidColorBrush(Colors.Black), 1), center, radius, radius);

            // Draw label.
            FormattedText labelText = new FormattedText("Compass", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Tahoma"), 11.5, new SolidColorBrush(Colors.Black));
            drawingContext.DrawText(labelText, new Point(center.X - labelText.Width / 2, ActualHeight - 25));

            // Draw compass points.
            FormattedText northText = new FormattedText("N", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Tahoma"), 11.5, new SolidColorBrush(Colors.Black));
            drawingContext.DrawText(northText, center + new Vector(-northText.Width / 2, -radius + 5));

            FormattedText eastText = new FormattedText("E", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Tahoma"), 11.5, new SolidColorBrush(Colors.Black));
            drawingContext.DrawText(eastText, center + new Vector(radius - eastText.Width - 5, -eastText.Height / 2));

            FormattedText southText = new FormattedText("S", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Tahoma"), 11.5, new SolidColorBrush(Colors.Black));
            drawingContext.DrawText(southText, center + new Vector(-southText.Width / 2, radius - southText.Height - 5));

            FormattedText westText = new FormattedText("W", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Tahoma"), 11.5, new SolidColorBrush(Colors.Black));
            drawingContext.DrawText(westText, center + new Vector(-radius + 5, -eastText.Height / 2));

            // Draw current compass direction.
            if (Robot != null)
            {
                const double IndicatorWidth = 7.0;

                double angle = Robot.IMU.CompassHeading / 180 * Math.PI;
                Vector direction = new Vector(Math.Sin(angle), Math.Cos(angle));
                Point leftPoint = new Point(center.X - direction.Y * IndicatorWidth, center.Y - direction.X * IndicatorWidth);
                Point rightPoint = new Point(center.X + direction.Y * IndicatorWidth, center.Y + direction.X * IndicatorWidth);
                Point frontPoint = new Point(center.X + direction.X * radius * 0.9, center.Y - direction.Y * radius * 0.9);
                Point backPoint = new Point(center.X - direction.X * radius * 0.9, center.Y + direction.Y * radius * 0.9);

                PathFigure northIndicatorFigure1 = new PathFigure();
                northIndicatorFigure1.Segments.Add(new LineSegment(leftPoint, false));
                northIndicatorFigure1.Segments.Add(new LineSegment(frontPoint, true));
                northIndicatorFigure1.Segments.Add(new LineSegment(center, true));
                northIndicatorFigure1.Segments.Add(new LineSegment(leftPoint, true));

                PathGeometry northIndicatorGeometry1 = new PathGeometry();
                northIndicatorGeometry1.Figures.Add(northIndicatorFigure1);

                drawingContext.DrawGeometry(new SolidColorBrush(Color.FromRgb(255,64,0)), null, northIndicatorGeometry1);

                PathFigure northIndicatorFigure2 = new PathFigure();
                northIndicatorFigure2.Segments.Add(new LineSegment(center, false));
                northIndicatorFigure2.Segments.Add(new LineSegment(frontPoint, true));
                northIndicatorFigure2.Segments.Add(new LineSegment(rightPoint, true));
                northIndicatorFigure2.Segments.Add(new LineSegment(center, true));

                PathGeometry northIndicatorGeometry2 = new PathGeometry();
                northIndicatorGeometry2.Figures.Add(northIndicatorFigure2);

                drawingContext.DrawGeometry(new SolidColorBrush(Color.FromRgb(128, 0, 0)), null, northIndicatorGeometry2);

                PathFigure northIndicatorFigure3 = new PathFigure();
                northIndicatorFigure3.Segments.Add(new LineSegment(center, false));
                northIndicatorFigure3.Segments.Add(new LineSegment(backPoint, true));
                northIndicatorFigure3.Segments.Add(new LineSegment(rightPoint, true));
                northIndicatorFigure3.Segments.Add(new LineSegment(center, true));

                PathGeometry northIndicatorGeometry3 = new PathGeometry();
                northIndicatorGeometry3.Figures.Add(northIndicatorFigure3);

                drawingContext.DrawGeometry(new SolidColorBrush(Color.FromRgb(128, 128, 128)), null, northIndicatorGeometry3);

                PathFigure northIndicatorFigure4 = new PathFigure();
                northIndicatorFigure4.Segments.Add(new LineSegment(center, false));
                northIndicatorFigure4.Segments.Add(new LineSegment(backPoint, true));
                northIndicatorFigure4.Segments.Add(new LineSegment(leftPoint, true));
                northIndicatorFigure4.Segments.Add(new LineSegment(center, true));

                PathGeometry northIndicatorGeometry4 = new PathGeometry();
                northIndicatorGeometry4.Figures.Add(northIndicatorFigure4);

                drawingContext.DrawGeometry(new SolidColorBrush(Color.FromRgb(64, 64, 64)), null, northIndicatorGeometry4);

                FormattedText angleText = new FormattedText(String.Format("{0:0}°",angle * 180 / Math.PI), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Tahoma"), 11.5, new SolidColorBrush(Colors.Black));
                drawingContext.DrawText(angleText, new Point(ActualWidth - angleText.Width - 10, 10));

            }
        }

        #endregion


        #region Properties

        public override RobotComponent Component
        {
            get { return Robot.IMU; }
        }

        public override Shape StateShape
        {
            get { return stateEllipse; }
        }

        #endregion
    }
}
