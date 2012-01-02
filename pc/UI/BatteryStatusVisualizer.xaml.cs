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
using System.Windows.Threading;
using MongooseSoftware.Robotics.RobotLib.Components;

namespace MongooseSoftware.Robotics.UI
{
    /// <summary>
    /// Interaction logic for BatteryStatusVisualizer.xaml
    /// </summary>
    public partial class BatteryStatusVisualizer : RobotComponentUserControl
    {
        #region Constructors

        public BatteryStatusVisualizer() 
        {
            InitializeComponent();
        }
        
        #endregion


        #region Methods

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            graphControl.MaxYValue = 1000;
            graphControl.Data.Add(new Series() { Color = Colors.Red, SampleDepth = 100 });
            graphControl.Data.Add(new Series() { Color = Colors.Green, SampleDepth = 100 });

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs args)
        {
            if (graphControl != null && graphControl.Data.Count == 2)
            {
                graphControl.Data[0].Add(Math.Abs(Robot.BatteryMonitorA.Current));
                graphControl.Data[1].Add(Math.Abs(Robot.BatteryMonitorB.Current));
                graphControl.InvalidateVisual();
            }
        }

        public override void Bind(Beagle robot, Joystick joystick)
        {
 	        base.Bind(robot, joystick);
            DataContext = robot;
        }

        #endregion


        #region Properties

        public override RobotComponent Component
        {
            get { return Robot.BatteryMonitorA; }
        }

        public override Shape StateShape
        {
            get { return stateEllipse; }
        }

        #endregion


        #region Fields

        private DispatcherTimer timer;

        #endregion
    }
}
