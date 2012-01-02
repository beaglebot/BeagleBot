using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongooseSoftware.Robotics.RobotLib.I2C
{
    public class I2CException : Exception
    {
        public I2CException()
        {
        }

        public I2CException(string message)
            : base(message)
        {
        }

        public I2CException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
