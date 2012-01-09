using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongooseSoftware.Robotics.RobotLib.I2C;

namespace MongooseSoftware.Robotics.RobotLib.Components
{
    public class PowerSupplyComponent : SingleAddressI2CSlaveComponent
    {
        #region Constants

        public class I2CRegisters 
        {
            public const byte MagicNumber = 0;
            public const byte Version = 1;
            public const byte ChargerAState = 2;
            public const byte ChargerBState = 3;
            public const byte ChargerADisabled= 4;
            public const byte ChargerBDisabled = 5;
        }

        private class ChargerStateResponse 
        {
            public const byte PowerGood = 1;
            public const byte Stat1 = 2;
            public const byte Stat2 = 4;
            public const byte Enabled = 8;
            public const byte DisabledByTimer = 16;
            public const byte DisabledByI2C = 32;
        }

        public enum ChargerState
        {
            Unknown,
            NoExternalPower,
            Charging,
            FinishedCharging,
            DisabledByTimer,
            DisabledByI2C,
            DisabledOther,
            BatteryMissing
        }

        #endregion


        #region Constructors

        public PowerSupplyComponent()
        {
        }

        #endregion


        #region Methods
        
        public override bool OnConnecting()
        {
            try
            {
                AddPoll(1000, I2CRegisters.ChargerAState);
                AddPoll(1000, I2CRegisters.ChargerBState);
                return true;
            }
            catch (I2CException e) { return false; }
        }
        
        protected override void OnPollDataReceived(int pollID, byte i2cAddress, byte register, int[] values, object data)
        {
            if (values == null) return;
            int value = values[0];
            switch (register)
            {
                case I2CRegisters.ChargerAState:
                    ChargerState x = GetChargerStatus((byte)value);
                    if (chargerAState == x) return;
                    chargerAState = x;
                    OnNotifyPropertyChanged("ChargerAState");
                    break;

                case I2CRegisters.ChargerBState:
                    x = GetChargerStatus((byte)value);
                    if (chargerBState == x) return;
                    chargerBState = x;
                    OnNotifyPropertyChanged("ChargerBState");
                    break;
            }
        }

        static ChargerState GetChargerStatus(byte value)
        {
            if ((value & (byte)ChargerStateResponse.PowerGood) == 0)
                return ChargerState.NoExternalPower;
            if ((value & ChargerStateResponse.Stat1) != 0 && (value & ChargerStateResponse.Stat2) == 0)
                return ChargerState.Charging;
            if ((value & ChargerStateResponse.Stat2) != 0 && (value & ChargerStateResponse.Stat2) != 0)
                return ChargerState.FinishedCharging;
            if ((value & ChargerStateResponse.DisabledByI2C) != 0)
                return ChargerState.DisabledByI2C;
            if ((value & ChargerStateResponse.DisabledByTimer) != 0)
                return ChargerState.DisabledByTimer;
            if ((value & ChargerStateResponse.Enabled) == 0)
                return ChargerState.DisabledOther;
            return ChargerState.BatteryMissing;
        }

        #endregion


        #region Properties

        public override string Name
        {
            get { return "Power Supply"; }
        }

        public override byte I2CAddress
        {
            get { return 0x30; }
        }

        public ChargerState ChargerAState
        {
            get { return chargerAState; }
            private set
            {
                if (value == chargerAState) return;
                chargerAState = value;
                OnNotifyPropertyChanged("ChargerAState");
            }
        }

        public bool IsChargerAEnabled
        {
            get;
            set;
        }

        public ChargerState ChargerBState
        {
            get { return chargerBState; }
            private set
            {
                if (value == chargerBState) return;
                chargerBState = value;
                OnNotifyPropertyChanged("ChargerBState");
            }
        }

        public bool IsChargerBEnabled
        {
            get;
            set;
        }
        
        #endregion


        #region Fields

        private ChargerState chargerAState;
        private ChargerState chargerBState;

        #endregion
    }
}
