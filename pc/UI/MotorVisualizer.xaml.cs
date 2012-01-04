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
using System.ComponentModel;
using System.Threading;
using System.Globalization;
using MongooseSoftware.Robotics.RobotLib;
using MongooseSoftware.Robotics.RobotLib.Components;

namespace MongooseSoftware.Robotics.UI
{
    /// <summary>
    /// Interaction logic for MotorVisualizer.xaml
    /// </summary>
    public partial class MotorVisualizer : RobotComponentUserControl
    {
        #region Constants

        private const int DescriptionHeight = 20;

        #endregion


        #region Constructors

        public MotorVisualizer()
        {
            InitializeComponent();
        }

        #endregion


        #region Methods

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            double barWidth = (ActualWidth - 40) / 5;
            double height = ActualHeight - DescriptionHeight;

            Brush barBackgroundBrush = new SolidColorBrush(Colors.Black);
            barBackgroundBrush.Opacity = 0.2;

            Point topLeftPoint = new Point(20 + barWidth, 10);
            Point bottomLeftPoint = new Point(topLeftPoint.X + barWidth, height - 10);
            drawingContext.DrawRectangle(barBackgroundBrush, null, new Rect(topLeftPoint, bottomLeftPoint));

            Point topRightPoint = new Point(bottomLeftPoint.X + barWidth, 10);
            Point bottomRightPoint = new Point(topRightPoint.X + barWidth, height - 10);
            drawingContext.DrawRectangle(barBackgroundBrush, null, new Rect(topRightPoint, bottomRightPoint));

            if (Robot != null)
            {

                Brush barBrush = new SolidColorBrush(Colors.DarkBlue);

                double leftHeight = (height - 20) / 2 * Robot.MotorController.LeftMotorSpeed;
                if (leftHeight < 0)
                {
                    topLeftPoint.Y = height / 2;
                    bottomLeftPoint.Y = height / 2 - leftHeight;
                }
                else
                {
                    topLeftPoint.Y = height / 2 - leftHeight;
                    bottomLeftPoint.Y = height / 2;
                }
                drawingContext.DrawRectangle(barBrush, null, new Rect(topLeftPoint, bottomLeftPoint));

                double rightHeight = (height - 20) / 2 * Robot.MotorController.RightMotorSpeed;
                if (rightHeight < 0)
                {
                    topRightPoint.Y = height / 2;
                    bottomRightPoint.Y = height / 2 - rightHeight;
                }
                else
                {
                    topRightPoint.Y = height / 2 - rightHeight;
                    bottomRightPoint.Y = height / 2;
                }
                drawingContext.DrawRectangle(barBrush, null, new Rect(topRightPoint, bottomRightPoint));
            }

            drawingContext.DrawLine(new Pen(new SolidColorBrush(Colors.Black), 2), new Point(topLeftPoint.X - barWidth / 2, height / 2), new Point(bottomRightPoint.X + barWidth / 2, height / 2));

            FormattedText descriptionText = new FormattedText("Motor Visualizer", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Tahoma"), 11.5, new SolidColorBrush(Colors.Black));
            Point descriptionPoint = new Point((ActualWidth - descriptionText.Width) / 2, ActualHeight - DescriptionHeight);
            drawingContext.DrawText(descriptionText, descriptionPoint);

        }

        protected override void  Component_PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            base.Component_PropertyChanged(sender, args);
            switch (args.PropertyName)
            {
                case "LeftMotorSpeed":
                case "RightMotorSpeed":
                    Dispatcher.BeginInvoke((ThreadStart)InvalidateVisual);
                    break;
            }
        }

        #endregion


        #region Properties

        public override RobotComponent Component
        {
            get { return Robot.MotorController; }
        }

        public override Shape StateShape
        {
            get { return stateEllipse; }
        }

        #endregion
    }
}
