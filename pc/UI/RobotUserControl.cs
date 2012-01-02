using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using MongooseSoftware.Robotics.RobotLib;

namespace MongooseSoftware.Robotics.UI
{
    public partial class RobotUserControl : UserControl
    {
        public RobotUserControl()
        {
        }

        public virtual void Bind(Beagle robot, Joystick joystick)
        {
            if (robot == null) throw new ArgumentNullException("robot");
            if (joystick == null) throw new ArgumentNullException("joystick");
            _robot = robot;
            _joystick = joystick;
        }


        public Beagle Robot
        {
            get { return _robot; }
        }

        public Joystick Joystick
        {
            get { return _joystick; }
        }

        private Beagle _robot;
        private Joystick _joystick;
    }
}
