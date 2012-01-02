using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongooseSoftware.Robotics.RobotLib
{
    public class RobotException : Exception
    {
        public RobotException()
        {
        }

        public RobotException(string message)
            : base(message)
        {
        }

        public RobotException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
