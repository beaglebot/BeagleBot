using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongooseSoftware.Robotics.RobotLib.I2C;

namespace MongooseSoftware.Robotics.RobotLib.Components
{

    public class ServoController : SingleAddressI2CClient
    {
        #region Constants

        private const int MinServoPulseLength = 600;
        private const int MaxServoPulseLength = 2400;

        public class I2CRegisters 
        {
            public const byte MagicNumber = 0;
            public const byte Version = 1;
            public const byte PanServoEnabled = 2;
            public const byte PanServoHighByte = 3;
            public const byte PanServoLowByte = 4;
            public const byte TiltServoEnabled = 5;
            public const byte TiltServoHighByte = 6;
            public const byte TiltServoLowByte = 7;
        }

        #endregion


        #region Constructors

        public ServoController()
        {
            isPanServoEnabled = false;
            panAngle = 0;

            isTiltServoEnabled = false;
            tiltAngle = 0;
        }

        #endregion


        #region Methods

        public override bool OnConnecting()
        {
            try
            {
                AddPoll(1000, I2CRegisters.PanServoEnabled);
                AddPoll(1000, I2CRegisters.PanServoHighByte, 2);
                AddPoll(1000, I2CRegisters.TiltServoEnabled);
                AddPoll(1000, I2CRegisters.TiltServoHighByte, 2);
                return true;
            }
            catch (I2CException) { return false; }
        }

        protected override void OnPollDataReceived(int pollID, byte i2cAddress, byte register, int[] values, object data)
        {
            if (values == null) return;

            switch (register)
            {
                case I2CRegisters.PanServoEnabled:
                    if (isPanServoEnabled == (values[0] == 1)) return;
                    isPanServoEnabled = values[0] == 1;
                    OnNotifyPropertyChanged("IsPanServoEnabled");
                    break;

                case I2CRegisters.TiltServoEnabled:
                    if (isTiltServoEnabled == (values[0] == 1)) return;
                    isTiltServoEnabled = values[0] == 1;
                    OnNotifyPropertyChanged("IsTiltServoEnabled");
                    break;

                case I2CRegisters.PanServoHighByte:
                    double x = ConvertPulseWidthToAngle(ToUnsigned16Bit(values[1], values[0]));
                    if (panAngle == x) return;
                    panAngle = x;
                    OnNotifyPropertyChanged("PanAngle");
                    break;

                case I2CRegisters.TiltServoHighByte:
                    x = ConvertPulseWidthToAngle(ToUnsigned16Bit(values[1], values[0]));
                    if (tiltAngle == x) return;
                    tiltAngle = x;
                    OnNotifyPropertyChanged("TiltAngle");
                    break;
            }
        }

        #endregion


        #region Properties

        public override string Name
        {
            get { return "Servo Controller"; }
        }

        public override byte I2CAddress
        {
            get { return 0x20; }
        }

        public bool IsPanServoEnabled
        {
            get { return isPanServoEnabled; }
            set
            {
                if (isPanServoEnabled == value) return;
                SetI2CRegister(I2CRegisters.PanServoEnabled, (byte)(value ? 1 : 0));
                isPanServoEnabled = value;
            }
        }

        /// <summary>
        /// Valid values are -90 to +90, inclusive.
        /// </summary>
        public double PanAngle
        {
            get { return panAngle; }
            set
            {
                if (value < -90) value = -90;
                if (value > 90) value = 90;
                if (value == panAngle) return;

                UInt16 pulseLength = ConvertAngleToPulseWidth(value);
                SetI2CRegister(I2CRegisters.PanServoHighByte, GetHighByte(pulseLength));
                SetI2CRegister(I2CRegisters.PanServoLowByte, GetLowByte(pulseLength));
                panAngle = value;

                if (!IsPanServoEnabled) IsPanServoEnabled = true;
            }
        }

        public bool IsTiltServoEnabled
        {
            get { return isTiltServoEnabled; }
            set
            {
                if (isTiltServoEnabled == value) return;
                SetI2CRegister(I2CRegisters.TiltServoEnabled, (byte)(value ? 1 : 0));
                isTiltServoEnabled = value;
            }
        }

        /// <summary>
        /// Valid values are -90 to +90, inclusive.
        /// </summary>
        public double TiltAngle
        {
            get { return tiltAngle; }
            set
            {
                if (value < -90) value = -90;
                if (value > 90) value = 90;
                if (value == tiltAngle) return;

                UInt16 pulseLength = ConvertAngleToPulseWidth(value);
                SetI2CRegister(I2CRegisters.TiltServoHighByte, GetHighByte(pulseLength));
                SetI2CRegister(I2CRegisters.TiltServoLowByte, GetLowByte(pulseLength));
                tiltAngle = value;

                if (!IsTiltServoEnabled) IsTiltServoEnabled = true;
            }
        }

        #endregion


        #region Implementation

        private static double ConvertPulseWidthToAngle(UInt16 pulseWidth)
        {
            return ((double)pulseWidth - MinServoPulseLength) / (MaxServoPulseLength - MinServoPulseLength) * 180 - 90;
        }

        private static UInt16 ConvertAngleToPulseWidth(double angle)
        {
            return (UInt16)Math.Round((angle + 90.0) / 180.0 * (MaxServoPulseLength - MinServoPulseLength) + MinServoPulseLength);
        }

        private bool isPanServoEnabled; 
        private double panAngle;

        private bool isTiltServoEnabled;
        private double tiltAngle;

        #endregion
    }
}
