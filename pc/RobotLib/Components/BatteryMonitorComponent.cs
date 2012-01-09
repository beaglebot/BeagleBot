using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using MongooseSoftware.Robotics.RobotLib.I2C;

namespace MongooseSoftware.Robotics.RobotLib.Components
{
    public class BatteryMonitorComponent : SingleAddressI2CSlaveComponent
    {
        #region Constants

        public class I2CRegisters 
        {
            public const byte Status = 0x01;
            public const byte RemainingCapacityHigh = 0x02;
            public const byte RemainingCapacityLow = 0x03;
            public const byte Capacity = 0x06;
            public const byte TemperatureHigh = 0x0A;
            public const byte TemperatureLow = 0x0B;
            public const byte AccumulatedCurrentHigh = 0x10;
            public const byte AccumulatedCurrentLow = 0x11;
            public const byte VoltageHigh = 0x0C;
            public const byte VoltageLow = 0x0D;
            public const byte CurrentHigh = 0x0E;
            public const byte CurrentLow = 0x0F;
        }

        private const double SenseResistor = 0.02;

        #endregion


        #region Constructor

        public BatteryMonitorComponent(string batteryName, byte i2cAddress)
        {
            BatteryName = batteryName;
            this.i2cAddress = i2cAddress;
        }

        #endregion


        #region Methods

        public override bool OnConnecting()
        {
            try
            {
                AddPoll(1000, I2CRegisters.Capacity);
                AddPoll(1000, I2CRegisters.CurrentHigh, 2);
                AddPoll(1000, I2CRegisters.VoltageHigh, 2);
                AddPoll(1000, I2CRegisters.TemperatureHigh, 2);
                AddPoll(1000, I2CRegisters.AccumulatedCurrentHigh, 2);
                AddPoll(1000, I2CRegisters.RemainingCapacityHigh, 2);
                return true;
            }
            catch (I2CException) { return false; }
        }

        protected override void OnPollDataReceived(int pollID, byte i2cAddress, byte register, int[] values, object data)
        {
            if (values == null) return;

            int value1 = values[0];
            switch (register)
            {
                case I2CRegisters.Capacity:
                    ChargeRemainingPercentage = value1;
                    break;

                case I2CRegisters.CurrentHigh:
                    var value2 = values[1];
                    current = ToSigned16Bit(value2, value1) * 1.5625e-3 / SenseResistor;
                    OnNotifyPropertyChanged("Current");
                    break;

                case I2CRegisters.VoltageHigh:
                    value2 = values[1];
                    voltage = ToUnsigned16Bit(value2, value1) * 0.00488 / 32 * 2;
                    OnNotifyPropertyChanged("Voltage");
                    break;

                case I2CRegisters.TemperatureHigh:
                    value2 = values[1];
                    temperature = ToSigned16Bit(value2, value1) * 0.125 / 32;
                    OnNotifyPropertyChanged("Temperature");
                    break;

                case I2CRegisters.AccumulatedCurrentHigh:
                    value2 = values[1];
                    accumulatedCurrent = ToSigned16Bit(value2, value1) * 6.25e-3 / SenseResistor;
                    OnNotifyPropertyChanged("AccumulatedCurrent");
                    break;

                case I2CRegisters.RemainingCapacityHigh:
                    value2 = values[1];
                    remainingCapacity = ToSigned16Bit(value2, value1) * 1.6;
                    OnNotifyPropertyChanged("RemainingCapacity");
                    break;
            }
        }


        #endregion


        #region Properties

        public override string Name
        {
            get
            {
                if (!String.IsNullOrEmpty(BatteryName))
                    return String.Concat("Battery Monitor - ", BatteryName);
                else
                    return "Battery Monitor";
            }
        }

        public string BatteryName
        {
            get;
            private set;
        }

        public override byte I2CAddress
        {
            get { return i2cAddress; }
        }

        public double Voltage
        {
            get { return voltage; }
            private set
            {
                if (value == voltage) return;
                voltage = value;
                OnNotifyPropertyChanged("Voltage");
            }
        }

        public double Current
        {
            get { return current; }
            private set
            {
                if (value == current) return;
                current = value;
                OnNotifyPropertyChanged("Current");
            }
        }

        public double Temperature
        {
            get { return temperature; }
            private set
            {
                if (value == temperature) return;
                temperature = value;
                OnNotifyPropertyChanged("Temperature");
            }
        }

        public double AccumulatedCurrent
        {
            get { return accumulatedCurrent; }
            private set
            {
                if (value == accumulatedCurrent) return;
                accumulatedCurrent = value;
                OnNotifyPropertyChanged("AccumulatedCurrent");
            }
        }

        public double RemainingCapacity
        {
            get { return remainingCapacity; }
            private set
            {
                if (value == remainingCapacity) return;
                remainingCapacity = value;
                OnNotifyPropertyChanged("RemainingCapacity");
            }
        }

        public int ChargeRemainingPercentage
        {
            get { return chargeRemainingPercentage; }
            private set
            {
                if (value == chargeRemainingPercentage) return;
                chargeRemainingPercentage = value;
                OnNotifyPropertyChanged("ChargeRemainingPercentage");
            }
        }

        #endregion


        #region Private

        private byte i2cAddress;

        private double voltage;
        private double current;
        private double temperature;
        private double accumulatedCurrent;
        private double remainingCapacity;
        private int chargeRemainingPercentage;

        #endregion
    }
}
