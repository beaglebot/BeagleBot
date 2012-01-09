using System;
using System.ComponentModel;

namespace MongooseSoftware.Robotics.RobotLib.I2C
{
    public interface II2CBus : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Attempts to connect to the i2cproxy instance running on the target system. 
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        bool Connect();

        /// <summary>
        /// Returns true if successful, or false otherwise. Doesn't thrown an I2CException if it fails.
        /// </summary>
        /// <returns></returns>
        bool Ping();

        /// <summary>
        /// Close the socket and disconnection.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Reads a value from an I2C register on the target system.
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <param name="register"></param>
        /// <exception cref="I2CException">Thrown if the I2C operation fails, or the network connection fails.</exception>
        /// <returns></returns>
        byte Get(byte slaveAddress, byte register);

        /// <summary>
        /// Reads multiple sequential values from I2C registers on the target system.
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <param name="register"></param>
        /// <param name="numRegisters"></param>
        /// <returns></returns>
        byte[] Get(byte slaveAddress, byte register, int numRegisters);

        /// <summary>
        /// Writes a value to an I2C register on the target system.
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <param name="register"></param>
        /// <param name="value"></param>
        /// <exception cref="I2CException">Thrown if the I2C operation fails, or the network connection fails.</exception>
        /// <returns></returns>
        void Set(byte slaveAddress, byte register, byte value);
       
        /// <summary>
        /// Reads values from the given I2C register(s) every delayInMilliseconds, calling the pollCallback delegate with the result. If there
        /// is an error, the callback will be called with the values parameter set to null.
        /// </summary>
        /// <param name="delayInMilliseconds"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="register"></param>
        /// <param name="numRegisters"></param>
        /// <param name="pollCallback"></param>
        /// <param name="data"></param>
        /// <exception cref="I2CException">Thrown if the I2C operation fails, or the network connection fails.</exception>
        /// <returns></returns>
        int AddPoll(int delayInMilliseconds, byte slaveAddress, byte register, int numRegisters, PollCallback pollCallback, object data);

        /// <summary>
        /// Removes the give poll entry.
        /// </summary>
        /// <param name="pollID"></param>
        /// <exception cref="I2CException"></exception>
        void RemovePoll(int pollID);
        
        BusState State { get; }
        string Host { get; set; }
        int CommandPort { get; set; }
        int PollPort { get; set; }
    }
}
