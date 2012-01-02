using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MongooseSoftware.Robotics.RobotLib.I2C;

namespace MongooseSoftware.Robotics.RobotLib.Components
{
    public class IMU : I2CClient
    {

        #region Constants
        
        public const byte CompassI2CAddress = 0x1E;

        protected double CompassResolution = 0.00092; // 0.92 mGaus / LSb

        protected class CompassRegisters
        {
            public const byte ConfigurationA = 0;
            public const byte ConfigurationB = 1;
            public const byte Mode = 2;
            public const byte DataXMsb = 3;
            public const byte DataXLsb = 4;
            public const byte DataZMsb = 5;
            public const byte DataZLsb = 6;
            public const byte DataYMsb = 7;
            public const byte DataYLsb = 8;
            public const byte Status = 9;
        }
        
        #endregion
        

        #region Constructors

        public IMU()
        {
        }

        #endregion


        #region Methods

        public override bool OnConnecting()
        {
            try
            {
                // Put HMC5883L into continuous measurement mode.
                I2CChannel.Set(CompassI2CAddress, CompassRegisters.Mode, 0);
                AddPoll(1000, CompassI2CAddress, CompassRegisters.DataXMsb, 6);
            }
            catch (I2CException e) { return false; }

            return true;
        }

        public override bool CheckIfPossibleToConnect()
        {
            try { I2CChannel.Get(CompassI2CAddress, 0); }
            catch (I2CException) { return false; }
            return true;
        }

        public override bool CheckIfStillConnected()
        {
            return CheckIfPossibleToConnect();
        }
        
        protected override void OnPollDataReceived(int pollID, byte i2cAddress, byte register, int[] values, object data)
        {
            if (values == null) return;
            switch (register)
            {
                case CompassRegisters.DataXMsb:
                    CompassX = ToSigned16Bit(values[1], values[0]) * CompassResolution;
                    CompassZ = ToSigned16Bit(values[3], values[2]) * CompassResolution;
                    CompassY = ToSigned16Bit(values[5], values[4]) * CompassResolution;
                    OnNotifyPropertyChanged("CompassX");
                    OnNotifyPropertyChanged("CompassY");
                    OnNotifyPropertyChanged("CompassZ");
                    OnNotifyPropertyChanged("CompassHeading");
                    break;
            }
        }

        #endregion


        #region Properties

        public override string Name
        {
            get { return "IMU"; }
        }

        public double CompassHeading
        {
            get { return Math.Atan2(CompassZ,CompassY) * 180 / Math.PI; }
        }

        public double CompassX
        {
            get;
            private set;
        }

        public double CompassY
        {
            get;
            private set;
        }

        public double CompassZ
        {
            get;
            private set;
        }
        #endregion

    }
}
