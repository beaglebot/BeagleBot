using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MongooseSoftware.Robotics.RobotLib;
using System.Threading;
using MongooseSoftware.Robotics.RobotLib.Components;

namespace MongooseSoftware.Robotics.UI
{
    /// <summary>
    /// Interaction logic for ServoController.xaml
    /// </summary>
    public partial class ServoController : RobotComponentUserControl
    {
        #region Constructor

        public ServoController()
        {
            InitializeComponent();
        }

        #endregion


        #region Methods

        public override void Bind(Beagle robot, Joystick joystick)
        {
            base.Bind(robot, joystick);
            if (Joystick != null) joystick.PointOfViewPressed += joystick_PointOfViewChanged;
            DataContext = robot.ServoController;
        }

        #endregion


        #region Event Handlers

        void joystick_PointOfViewChanged(object sender, EventArgs e)
        {
            const double Scale = 2;

            int? angleInDegrees = Joystick.PointOfView;
            if (angleInDegrees == null) return;
            double angleInRadians = angleInDegrees.Value / 180.0 * Math.PI;
            double horizontalComponent = Math.Sin(angleInRadians) * Scale;
            double verticalComponent = Math.Cos(angleInRadians) * Scale;
            horizontalComponent = Math.Round(horizontalComponent * 1) / 1;
            verticalComponent = Math.Round(verticalComponent * 1) / 1;

            Robot.ServoController.TiltAngle += verticalComponent;
            Robot.ServoController.PanAngle += horizontalComponent;
        }

        #endregion


        #region Properties

        public override RobotComponent Component
        {
            get { return Robot.ServoController; }
        }

        public override Shape StateShape
        {
            get { return stateEllipse; }
        }

        #endregion
    }
}
