using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongooseSoftware.Robotics.RobotLib.I2C;

namespace MongooseSoftware.Robotics.RobotLib.Components
{
    public class I2CBus : RobotComponent
    {
        #region Constructors

        public I2CBus()
        {
            Channel = new I2CChannel();
        }

        #endregion


        #region Methods

        public override bool OnConnecting()
        {
            return Channel.Connect();
        }

        public override void Disconnect()
        {
            Channel.Disconnect();
        }

        public override bool CheckIfPossibleToConnect()
        {
            return true;
        }

        public override bool CheckIfStillConnected()
        {
            return Channel.Ping();
        }

        public override void Dispose()
        {
            base.Dispose();
            Channel.Dispose();
        }

        #endregion


        #region Properties

        public override string Name
        {
            get { return "I2C"; }
        }

        public string Host
        {
            get { return Channel.Host; }
            set { Channel.Host = value; }
        }

        public int CommandPort
        {
            get { return Channel.CommandPort; }
            set { Channel.CommandPort = value; }
        }

        public int PollPort
        {
            get { return Channel.PollPort; }
            set { Channel.PollPort = value; }
        }

        public I2CChannel Channel
        {
            get;
            private set;
        }

        #endregion

    }
}
