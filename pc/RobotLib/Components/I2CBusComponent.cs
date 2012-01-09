using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using MongooseSoftware.Robotics.RobotLib.I2C;

namespace MongooseSoftware.Robotics.RobotLib.Components
{
    public class I2CBusComponent : RobotComponent
    {
        #region Constructors

        public I2CBusComponent()
        {
            Bus = new I2CBus();
        }

        #endregion


        #region Methods

        public override bool OnConnecting()
        {
            return Bus.Connect();
        }

        public override void Disconnect()
        {
            Bus.Disconnect();
        }

        public override bool CheckIfPossibleToConnect()
        {
            return true;
        }

        public override bool CheckIfStillConnected()
        {
            return Bus.Ping();
        }

        public override void Dispose()
        {
            base.Dispose();
            Bus.Dispose();
        }

        protected void BusPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "State" && Bus != null)
            {
                if (Bus.State == BusState.Disconnecting || Bus.State == BusState.Disconnected)
                    Disconnect();
            }
        }

        #endregion


        #region Properties

        public override string Name
        {
            get { return "I2C"; }
        }

        public string Host
        {
            get { return Bus.Host; }
            set { Bus.Host = value; }
        }

        public int CommandPort
        {
            get { return Bus.CommandPort; }
            set { Bus.CommandPort = value; }
        }

        public int PollPort
        {
            get { return Bus.PollPort; }
            set { Bus.PollPort = value; }
        }

        public I2CBus Bus
        {
            get { return bus; }
            private set
            {
                if (bus != null) bus.PropertyChanged -= BusPropertyChanged;
                bus = value;
                if (bus != null) bus.PropertyChanged += BusPropertyChanged;
            }
        }

        #endregion


        #region Fields

        private I2CBus bus;

        #endregion
    }
}
