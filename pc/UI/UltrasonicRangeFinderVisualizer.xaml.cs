using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MongooseSoftware.Robotics.RobotLib;
using MongooseSoftware.Robotics.RobotLib.Components;

namespace MongooseSoftware.Robotics.UI
{
    /// <summary>
    /// Interaction logic for UltrasonicRangeFinderVisualizer.xaml
    /// </summary>
    public partial class UltrasonicRangeFinderVisualizer : RobotComponentUserControl
    {

        #region Constructor

        public UltrasonicRangeFinderVisualizer()
        {
            InitializeComponent();
        }

        #endregion


        #region Methods

        public override void Bind(Beagle robot, Joystick joystick)
        {
            base.Bind(robot, joystick);
            joystick.TriggerPressed += joystick_TriggerClicked;
            DataContext = robot.UltrasonicRangeFinder;
        }

        #endregion


        #region Event Handlers

        void joystick_TriggerClicked(object sender, EventArgs e)
        {
            Robot.UltrasonicRangeFinder.TriggerPing();
        }

        private void _pingButton_Click(object sender, RoutedEventArgs e)
        {
            Robot.UltrasonicRangeFinder.TriggerPing();
        }

        #endregion


        #region Properties

        public override RobotComponent Component
        {
            get { return Robot.UltrasonicRangeFinder; }
        }

        public override Shape StateShape
        {
            get { return stateEllipse; }
        }

        #endregion


    }
}
