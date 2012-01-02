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
using System.Windows.Threading;

namespace MongooseSoftware.Robotics.UI
{
    /// <summary>
    /// Interaction logic for GraphControl.xaml
    /// </summary>
    public partial class GraphControl : UserControl
    {

        #region Constructors

        public GraphControl()
        {
            InitializeComponent();

            Data = new List<Series>();
            PixelsPerSample = 10;
            MaxYValue = 10;
        }

        #endregion


        #region Methods

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // Draw axes.
            var axesPen = new Pen(new SolidColorBrush(Colors.Black), 1);
            drawingContext.DrawLine(axesPen, Origin, new Point(Origin.X, Origin.Y - YAxisLength));
            drawingContext.DrawLine(axesPen, Origin, new Point(Origin.X + XAxisLength, Origin.Y));
            drawingContext.DrawLine(axesPen, new Point(Origin.X - 5, Origin.Y - YAxisLength), new Point(Origin.X + 5, Origin.Y - YAxisLength));
            var maxValueText = new FormattedText(MaxYValue.ToString("0.0"), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Tahoma"), 11.5, new SolidColorBrush(Colors.Black));
            drawingContext.DrawText(maxValueText, new Point(Origin.X - 10 - maxValueText.Width, Origin.Y - YAxisLength - 4));
            
            // Draw graphs.
            foreach (var series in Data)
                DrawGraph(series, drawingContext);
        }

        private Point ConvertToPixels(double t, double value)
        {
            return new Point()
            {
                X = Origin.X + PixelsPerSample * (MaxSamples - t), Y = Origin.Y - value / MaxYValue * YAxisLength
            };
        }

        private void DrawGraph(Series series, DrawingContext drawingContext)
        {
            var pen = new Pen(new SolidColorBrush(series.Color), 1);

            double deltaX = XAxisLength / MaxSamples;
            var origin = Origin;

            LinkedListNode<double> node = series.Samples.Last;
            if (node == null) return;
            var lastPoint = ConvertToPixels(0, node.Value);

            double t = 0;
            node = node.Previous;
            while (node != null)
            {
                var currentPoint = ConvertToPixels(t, node.Value);
                if (currentPoint.X < Origin.X) break;
                drawingContext.DrawLine(pen, lastPoint, currentPoint);
                lastPoint = currentPoint;
                node = node.Previous;
                t++;
            }
        }

        #endregion


        #region Properties

        public IList<Series> Data
        {
            get;
            private set;
        }

        public Point Origin
        {
            get { return new Point(20, ActualHeight - 10); }
        }

        public double YAxisLength
        {
            get { return ActualHeight - 25; }
        }

        public double XAxisLength
        {
            get { return ActualWidth - 40; }
        }

        public double MaxYValue
        {
            get;
            set;
        }

        public int PixelsPerSample
        {
            get;
            set;
        }

        public int MaxSamples
        {
            get { return (int)Math.Floor(XAxisLength / PixelsPerSample); }
        }

        #endregion
    }

    public class Series
    {
        public Series()
        {
            Samples = new LinkedList<double>();
        }

        public void Add(double sample)
        {
            Samples.AddLast(sample);
            if (Samples.Count > SampleDepth) Samples.RemoveFirst();
        }

        public LinkedList<double> Samples { get; private set; }

        public Color Color { get; set; }

        public int SampleDepth { get; set; }
    }
}
