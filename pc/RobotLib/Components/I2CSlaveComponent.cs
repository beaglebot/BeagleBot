using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MongooseSoftware.Robotics.RobotLib.I2C;

namespace MongooseSoftware.Robotics.RobotLib.Components
{
    abstract public class I2CSlaveComponent : RobotComponent
    {

        #region Constructors

        protected I2CSlaveComponent()
        {
            pollIDs = new List<int>();
        }

        #endregion


        #region Methods

        public override bool OnConnecting()
        {
            if (I2CBus == null)
            {
                Debug.WriteLine("I2CBus hasn't been assigned, so can't connect this component.");
                return false;
            }

            if (I2CBus.State != BusState.Connected)
            {
                Debug.WriteLine("I2CBus isn't connected, so can't connect this component.");
                return false;
            }

            return true;
        }

        public override void Disconnect()
        {
            foreach (var pollID in pollIDs)
            {
                try { I2CBus.RemovePoll(pollID); }
                catch (I2CException) { }
            }
            pollIDs.Clear();
        }

        #endregion


        #region Methods


        protected void AddPoll(int delayInMilliseconds, byte i2cAddress, byte register, int numRegisters = 1, object data = null)
        {
            int pollID = I2CBus.AddPoll(delayInMilliseconds, i2cAddress, register, numRegisters, OnPollDataReceived, data);
            pollIDs.Add(pollID);
        }

        protected virtual void OnPollDataReceived(int pollID, byte i2cAddress, byte register, int[] values, object data)
        {
            if (values == null) return;
        }

        protected byte GetHighByte(UInt16 i)
        {
            return (byte)(i >> 8);
        }

        protected byte GetLowByte(UInt16 i)
        {
            return (byte)(i & 0xff);
        }

        protected UInt16 ToUnsigned16Bit(int lowByte, int highByte)
        {
            return (ushort)((byte)lowByte | ((byte)highByte) << 8);
        }

        protected Int16 ToSigned16Bit(int lowByte, int highByte)
        {
            return (short)((byte)lowByte | ((byte)highByte) << 8);
        }

        protected void BusPropertyChanged(object source, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "State" && bus != null && (bus.State == BusState.Disconnecting || bus.State == BusState.Disconnected))
            {
                Disconnect();
            }
        }

        #endregion


        #region Properties

        public I2CBus I2CBus 
        {
            get { return bus; }
            set
            {
                if (bus != null) bus.PropertyChanged -= BusPropertyChanged;
                bus = value;
                if (bus != null) bus.PropertyChanged += BusPropertyChanged;
            }
        }

        #endregion


        #region Fields

        private I2CBus bus;
        private List<int> pollIDs;

        #endregion

    }
}
