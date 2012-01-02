using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MongooseSoftware.Robotics.RobotLib.I2C;

namespace MongooseSoftware.Robotics.RobotLib.Components
{
    abstract public class I2CClient : RobotComponent
    {

        #region Constructors

        protected I2CClient()
        {
            pollIDs = new List<int>();
        }

        #endregion


        #region Methods

        public override bool OnConnecting()
        {
            if (I2CChannel == null)
            {
                Debug.WriteLine("I2CChannel hasn't been assigned, so can't connect this component.");
            }

            if (I2CChannel.State != ChannelState.Connected)
            {
                Debug.WriteLine("I2CChannel isn't connected, so can't connect this component.");
                return false;
            }

            return true;
        }

        public override void Disconnect()
        {
            foreach (var pollID in pollIDs)
                I2CChannel.RemovePoll(pollID);
            pollIDs.Clear();
        }

        #endregion


        #region Methods


        protected void AddPoll(int delayInMilliseconds, byte i2cAddress, byte register, int numRegisters = 1, object data = null)
        {
            int pollID = I2CChannel.AddPoll(delayInMilliseconds, i2cAddress, register, numRegisters, OnPollDataReceived, data);
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

        #endregion


        #region Properties

        public I2CChannel I2CChannel { get; set; }

        #endregion


        #region Fields

        private List<int> pollIDs;

        #endregion

    }
}
