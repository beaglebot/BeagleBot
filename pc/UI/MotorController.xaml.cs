using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.IO.Ports;
using System.Threading;
using System.Globalization;
using MongooseSoftware.Robotics.RobotLib;
using MongooseSoftware.Robotics.RobotLib.Components;

namespace MongooseSoftware.Robotics.UI
{
	/// <summary>
	/// Interaction logic for MotorController.xaml
	/// </summary>
	public partial class MotorController : RobotComponentUserControl
    {
        #region Constants

        private const int CrossLength = 10;
        private const int DescriptionHeight = 20;

        #endregion


        #region Constructors

        public MotorController()
		{
			InitializeComponent();
        }

        #endregion


        #region Methods

        public override void Bind(Beagle robot, Joystick joystick)
        {
            base.Bind(robot, joystick);
            joystick.MainAxisChanged += Joystick_MainAxisChanged;
        }

        protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);

			// Draw the MaxRadius circle.
			Pen pen = new Pen(new SolidColorBrush(Colors.Black), 2);
			Brush brush = new SolidColorBrush(CapturedMouse ? Colors.Red : Colors.White);
			drawingContext.DrawEllipse(brush, pen, Center, MaxRadius, MaxRadius);

			// Draw the DeadRadius circle.
			Pen thinPen = new Pen(new SolidColorBrush(Colors.Black), 1);
			Brush whiteBrush = new SolidColorBrush(Colors.White);
			drawingContext.DrawEllipse(whiteBrush, thinPen, Center, DeadRadius, DeadRadius);

            // Draw the description.
            FormattedText descriptionText = new FormattedText("Motor Controller", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Tahoma"), 11.5, new SolidColorBrush(Colors.Black));
            Point descriptionPoint = new Point(Center.X - descriptionText.Width / 2, ActualHeight - DescriptionHeight);
            drawingContext.DrawText(descriptionText, descriptionPoint);

            if (Robot == null) return;

			// Draw the cross.
			Pen crossPen = new Pen(new SolidColorBrush(Colors.Blue), 1);
			drawingContext.DrawLine(crossPen, Point.Add(CurrentCrossPosition, new Vector(-CrossLength / 2, 0)), Point.Add(CurrentCrossPosition, new Vector(CrossLength / 2, 0)));
			drawingContext.DrawLine(crossPen, Point.Add(CurrentCrossPosition, new Vector(0, -CrossLength / 2)), Point.Add(CurrentCrossPosition, new Vector(0, CrossLength / 2)));

        }

        private void CalculateEngineParameters()
        {
            if (CurrentRadius < DeadRadius)
            {
                Robot.MotorController.LeftMotorSpeed = 0;
                Robot.MotorController.RightMotorSpeed = 0;
                return;
            }

            double left, right;
            double octant = CurrentAngle / Math.PI * 4;
            if (octant < 2)
            {
                left = 1;
                right = 1 - octant;
            }
            else if (octant < 4)
            {
                left = 3 - octant;
                right = -1;
            }
            else if (octant < 6)
            {
                left = -1;
                right = octant - 5;
            }
            else
            {
                right = 1;
                left = CurrentAngle / (Math.PI / 4) - 7;
            }

            double leftEngineSpeed = left * CurrentRadius / MaxRadius;
            double rightEngineSpeed = right * CurrentRadius / MaxRadius;
            
            if (leftEngineSpeed != Robot.MotorController.LeftMotorSpeed)
                Robot.MotorController.LeftMotorSpeed = leftEngineSpeed;
            if (rightEngineSpeed != Robot.MotorController.RightMotorSpeed)
                Robot.MotorController.RightMotorSpeed = rightEngineSpeed;
        }

        #endregion


        #region Event Handlers

        private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            CapturedMouse = !CapturedMouse;
            InvalidateVisual();
            if (CapturedMouse)
            {
                Mouse.Capture(this, CaptureMode.Element);
            }
            else
            {
                Mouse.Capture(this, CaptureMode.None);
            }
        }

        DateTime lastUpdate;
        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (CapturedMouse && (DateTime.Now - lastUpdate).TotalMilliseconds > 100)
            {
                Point mousePosition = e.GetPosition(this);
                Vector vector = Point.Subtract(mousePosition, Center);
                CurrentRadius = vector.Length;
                if (CurrentRadius > MaxRadius) CurrentRadius = MaxRadius;
                CurrentAngle = Math.Atan2(vector.X, -vector.Y);
                if (CurrentAngle < 0) CurrentAngle += 2 * Math.PI;
                CalculateEngineParameters();
                lastUpdate = DateTime.Now;
                InvalidateVisual();
            }
        }

        private void Joystick_MainAxisChanged(object sender, EventArgs e)
        {
            if (!CapturedMouse)
            {
                Vector vector = new Vector(Joystick.X / 32767.0 - 1, Joystick.Y / 32767.0 - 1);

                double newRadius = vector.Length * MaxRadius;
                if (newRadius > MaxRadius) newRadius = MaxRadius;
                CurrentRadius = newRadius;

                double newAngle = Math.Atan2(vector.X, -vector.Y);
                if (newAngle < 0) newAngle += 2 * Math.PI;
                CurrentAngle = newAngle;

                CalculateEngineParameters();
                Dispatcher.BeginInvoke((ThreadStart)delegate{ InvalidateVisual(); });
            }
        }

        #endregion


        #region Properties

        public Point Center
		{
            get { return new Point(ActualWidth / 2, (ActualHeight - DescriptionHeight) / 2); }
		}

		public double MaxRadius
		{
			get { return Math.Min(ActualWidth, ActualHeight - DescriptionHeight) / 2 - 10; }
		}

		public double DeadRadius
		{
			get { return 10; }
		}

		public Point CurrentCrossPosition
		{
			get { return new Vector(Math.Sin(CurrentAngle), -Math.Cos(CurrentAngle)) * CurrentRadius + Center; }
		}

		public bool CapturedMouse
		{
			get;
			set;
		}

		public double CurrentAngle
		{
			get;
			private set;
		}

		public double CurrentRadius
		{
            get;
            private set;
        }

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
