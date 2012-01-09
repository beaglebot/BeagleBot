using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongooseSoftware.Robotics.RobotLib.I2C;

namespace MongooseSoftware.Robotics.RobotLib.Components
{
    public class MotorControllerComponent : SingleAddressI2CSlaveComponent
    {
        #region Constants
                
        public class I2CRegisters 
        {
            public const byte MagicNumber = 0;
            public const byte Version = 1;
            public const byte LeftMotorState = 2;
            public const byte LeftMotorSpeed = 3;
            public const byte RightMotorState = 4;
            public const byte RightMotorSpeed = 5;
            public const byte LeftQuadrature = 6;
            public const byte RightQuadrature = 7;
        }

        public enum MotorState
        {
            Disabled,
            Forwards,
            Reverse,
            Brake
        }

        #endregion


        #region Constructors

        public MotorControllerComponent() : base()
        {
            leftMotorState = MotorState.Disabled;
            leftMotorSpeed = 0;

            rightMotorState = MotorState.Disabled;
            rightMotorSpeed = 0;
        }

        #endregion


        #region Methods

        public override bool OnConnecting()
        {
            try
            {
                AddPoll(1000, I2CRegisters.LeftMotorState);
                AddPoll(1000, I2CRegisters.LeftMotorSpeed);
                AddPoll(1000, I2CRegisters.RightMotorState);
                AddPoll(1000, I2CRegisters.RightMotorSpeed);
                return true;
            }
            catch (I2CException) { return false; }
        }
       
        protected override void OnPollDataReceived(int pollID, byte i2cAddress, byte register, int[] values, object data)
        {
            if (values == null) return;

            int value = values[0];
            switch (register)
            {
                case I2CRegisters.LeftMotorState:
                    if (leftMotorState == (MotorState)value) return;
                    if (!Enum.IsDefined(typeof(MotorState), value))
                        throw new RobotException("Invalid MotorState");
                    leftMotorState = (MotorState)value;
                    OnNotifyPropertyChanged("LeftMotorState");
                    break;
                
                case I2CRegisters.LeftMotorSpeed:
                    double x = ConvertRegistersToMotorSpeed((byte)value, leftMotorState);
                    if (leftMotorSpeed == x) return;
                    leftMotorSpeed = x;
                    OnNotifyPropertyChanged("LeftMotorState");
                    break;

                case I2CRegisters.RightMotorState:
                    if (rightMotorState == (MotorState)value) return;
                    if (!Enum.IsDefined(typeof(MotorState), value))
                        throw new RobotException("Invalid MotorState");
                    rightMotorState = (MotorState)value;
                    OnNotifyPropertyChanged("RightMotorState");
                    break;

                case I2CRegisters.RightMotorSpeed:
                    x = ConvertRegistersToMotorSpeed((byte)value, rightMotorState);
                    if (rightMotorSpeed == x) return;
                    rightMotorSpeed = x;
                    OnNotifyPropertyChanged("RightMotorSpeed");
                    break;
            }
        }

        private static double ConvertRegistersToMotorSpeed(byte speedRegister, MotorState state)
        {
            switch (state)
            {
                case MotorState.Brake:
                case MotorState.Disabled:
                    return 0;
                case MotorState.Forwards:
                    return speedRegister / 255.0;
                case MotorState.Reverse:
                    return -speedRegister / 255.0;
                default:
                    throw new InvalidOperationException();
            }
        }

        private static void ConvertMotorSpeedToRegisters(double speed, out byte speedRegister, out MotorState state)
        {
            if (speed < -1) speed = -1;
            if (speed > +1) speed = +1;

            state = MotorState.Disabled;
            if (speed < 0) state = MotorState.Reverse;
            else if (speed > 0) state = MotorState.Forwards;

            speedRegister = (byte)Math.Round(Math.Abs(speed) * 255);
        }

        #endregion


        #region Properties

        public override string Name
        {
            get { return "Motor Controller"; }
        }

        public override byte I2CAddress
        {
            get { return 0x10; }
        }

        public int LeftQuadrature
        {
            get { return leftQuadrature; }
        }

        public int RightQuadrature
        {
            get { return rightQuadrature; }
        }

        public MotorState LeftMotorState
        {
            get { return leftMotorState; }
        }

        /// <summary>
        /// Valid values are -1 to +1, inclusive.
        /// </summary>
        public double LeftMotorSpeed
        {
            get { return leftMotorSpeed; }
            set
            {
                if (value == leftMotorSpeed) return;

                byte desiredSpeed;
                MotorState desiredState;
                ConvertMotorSpeedToRegisters(value, out desiredSpeed, out desiredState);
                if (desiredState != leftMotorState)
                    SetI2CRegister(I2CRegisters.LeftMotorState, (byte)desiredState);
                SetI2CRegister(I2CRegisters.LeftMotorSpeed, (byte)desiredSpeed);
                leftMotorSpeed = value;
                leftMotorState = desiredState;
            }
        }

        public MotorState RightMotorState
        {
            get { return rightMotorState; }
        }

        /// <summary>
        /// Valid values are -1 to +1, inclusive.
        /// </summary>
        public double RightMotorSpeed
        {
            get { return rightMotorSpeed; }
            set
            {
                if (value == rightMotorSpeed) return;

                byte desiredSpeed;
                MotorState desiredState;
                ConvertMotorSpeedToRegisters(value, out desiredSpeed, out desiredState);
                if (desiredState != rightMotorState)
                    SetI2CRegister(I2CRegisters.RightMotorState, (byte)desiredState);
                SetI2CRegister(I2CRegisters.RightMotorSpeed, (byte)desiredSpeed);
                rightMotorSpeed = value;
                rightMotorState = desiredState;
            }
        }

        #endregion


        #region Fields

        private MotorState leftMotorState;
        private double leftMotorSpeed;
        private int leftQuadrature;

        private MotorState rightMotorState;
        private double rightMotorSpeed;
        private int rightQuadrature;

        #endregion

    }
}
