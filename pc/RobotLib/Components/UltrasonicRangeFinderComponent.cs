using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using MongooseSoftware.Robotics.RobotLib.I2C;

namespace MongooseSoftware.Robotics.RobotLib.Components
{

    public class UltrasonicRangeFinderComponent : SingleAddressI2CSlaveComponent
    {
        #region Constants

        protected class I2CRegisters
        {
            public const byte Command = 0;
            public const byte LightSensor = 1;
            public const byte AnalogueGain = 1;
            public const byte RangeRegister = 2;
            public const byte FirstEchoHigh = 2;
            public const byte SecondEchoLow = 3;
        }

        #endregion


        #region Constructors

        public UltrasonicRangeFinderComponent()
        {
            isWaitingForPingToFinishLock = new object();
            pingFinishedTimer = new Timer(PingFinishedCallback, null, Timeout.Infinite, Timeout.Infinite);
            pingContinuouslyTimer = new Timer(PingContinuouslyCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        #endregion


        #region Methods

        public override bool OnConnecting()
        {
            var ok = base.OnConnecting();
            if (!ok) return false;

            // Need to decrease the gain to get reliable results.
            SetI2CRegister(I2CRegisters.AnalogueGain, 16);

            return true;
        }

        public void TriggerPing()
        {
            lock (isWaitingForPingToFinishLock)
            {
                if (isWaitingForPingToFinish) return;
                isWaitingForPingToFinish = true;
            }

            try
            {
                SetI2CRegister(I2CRegisters.Command, 81);
                pingFinishedTimer.Change(65, 0);
            }
            catch (I2CException e) 
            {
                PingContinously = false;
            }
        }

        private void PingFinishedCallback(object state)
        {
            lock (isWaitingForPingToFinishLock)
            {
                isWaitingForPingToFinish = false;
                pingFinishedTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }

            try
            {
                byte[] result = GetI2CRegisters(I2CRegisters.FirstEchoHigh, 2);
                LastDistanceInMeters = ToUnsigned16Bit(result[1], result[0]);
                OnNotifyPropertyChanged("LastDistanceInMeters");
            }
            catch (I2CException e)
            {
                Debug.WriteLine("Error reading ultrasonic ranger range: " + e.Message);
            }
        }

        private void PingContinuouslyCallback(object state)
        {
            TriggerPing();
        }

        #endregion


        #region Properties

        public override string Name
        {
            get { return "Ultrasonic Range Finder";  }
        }

        public override byte I2CAddress
        {
            get { return 0x70; }
        }

        public bool PingContinously
        {
            get { return isPingingContinously; }
            set 
            {
                if (value == isPingingContinously) return;
                isPingingContinously = value;

                if (isPingingContinously)
                    pingContinuouslyTimer.Change(1000, 1000);
                else
                    pingContinuouslyTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public double LastDistanceInMeters
        {
            get;
            private set;
        }

        #endregion


        #region Fields

        private Timer pingFinishedTimer;
        private object isWaitingForPingToFinishLock;
        private bool isWaitingForPingToFinish;

        private Timer pingContinuouslyTimer;
        private bool isPingingContinously;

        #endregion
    }
}
