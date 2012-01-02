using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using MongooseSoftware.Robotics.RobotLib.I2C;

namespace MongooseSoftware.Robotics.RobotLib.Components
{
    public abstract class SingleAddressI2CClient : I2CClient
    {
        #region Constructors

        protected SingleAddressI2CClient() : base()
        {
        }

        #endregion


        #region Methods

        public override bool CheckIfPossibleToConnect()
        {
            try { GetI2CRegister(0); }
            catch (I2CException) { return false; }
            return true;
        }

        public override bool CheckIfStillConnected()
        {
            return CheckIfPossibleToConnect();
        }

        protected void AddPoll(int delayInMilliseconds, byte register, int numRegisters = 1, object data = null)
        {
            AddPoll(delayInMilliseconds, I2CAddress, register, numRegisters, data);
        }
        
        #endregion


        #region Properties

        public abstract byte I2CAddress
        {
            get;
        }

        #endregion


        #region Implementation

        /// <summary>
        /// Writes a value to an I2C register on the target system.
        /// </summary>
        /// <param name="register"></param>
        /// <param name="value"></param>
        /// <exception cref="I2CException">Thrown if the write fails on the target system, or the network connection fails.</exception>
        /// <returns></returns>
        protected void SetI2CRegister(byte register, byte value)
        {
            I2CChannel.Set(I2CAddress, register, value);
        }

        /// <summary>
        /// Returns the value in the I2C register, or throws an I2CException.
        /// </summary>
        /// <param name="register"></param>
        /// <returns></returns>
        protected byte GetI2CRegister(byte register)
        {
            return I2CChannel.Get(I2CAddress, register);
        }

        /// <summary>
        /// Returns the values of multiple sequential I2C registers, or throws an I2CException.
        /// </summary>
        /// <param name="register"></param>
        /// <param name="numRegisters"></param>
        /// <returns></returns>
        protected byte[] GetI2CRegisters(byte register, int numRegisters)
        {
            return I2CChannel.Get(I2CAddress, register, numRegisters);
        }

        #endregion

    }
}
