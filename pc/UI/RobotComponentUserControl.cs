using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Media;
using System.Windows.Shapes;
using MongooseSoftware.Robotics.RobotLib.Components;

namespace MongooseSoftware.Robotics.UI
{
    public class RobotComponentUserControl : RobotUserControl
    {
        public override void Bind(RobotLib.Beagle robot, RobotLib.Joystick joystick)
        {
            base.Bind(robot, joystick);
            Component.PropertyChanged += Component_PropertyChanged;
            Component_PropertyChanged(this, new PropertyChangedEventArgs("State"));
        }

        protected virtual void Component_PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "State" && Component != null && StateShape != null)
            {
                var color = Component.State == ComponentState.Connected ? Colors.Green : Colors.Red;
                Dispatcher.BeginInvoke((ThreadStart)delegate { StateShape.Fill = new SolidColorBrush(color); });
            }
        }

        public virtual RobotComponent Component
        {
            get { return null; }
        }

        public virtual Shape StateShape
        {
            get { return null; }
        }
    }
}
