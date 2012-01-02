using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace MongooseSoftware.Robotics.RobotLib.I2C
{

    public delegate void PollCallback(int pollID, byte i2cAddress, byte register, int[] values, object data);

    public struct PollEntry
    {
        public int PollID;
        public int Delay;
        public byte I2CAddress;
        public byte Register;
        public int NumRegisters;
        public PollCallback Callback;
        public object Data;
    }

    public enum ChannelState
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting
    }

    public enum DisconnectionReason
    {
        UserRequested,
        ConnectionError
    }

    public class I2CChannel : II2CChannel
    {
        #region Constants

        public const int CommandSocketBufferSize = 4000;
        public const int PollSocketBufferSize = 4000;

        #endregion


        #region Constructors

        public I2CChannel()
        {
            pollEntries = new Dictionary<int, PollEntry>();
            stateLock = new object();

            state = ChannelState.Disconnected;
            channelConnectEvent = new AutoResetEvent(false);

            pollThread = new Thread(StartPollThread) {Name = "I2c Channel Poll Thread"};
            pollThread.Start();
        }

        #endregion


        #region Public Methods
        
        /// <summary>
        /// Attempts to establish a connection with the i2cproxy running on the robot.
        /// </summary>
        /// <exception cref="InvalidOperationException">If Host, CommandPort or PollPort aren't setup correctly, or is not in the Disconnected state.</exception>
        /// <returns>True if successful, false otherwise.</returns>
        public bool Connect()
        {
            if (String.IsNullOrEmpty(Host)) throw new InvalidOperationException("Host hasn't been set.");
            if (CommandPort == 0) throw new InvalidOperationException("CommandPort hasn't been set.");
            if (PollPort == 0) throw new InvalidOperationException("PollPort hasn't been set.");

            lock (stateLock)
            {
                if (State != ChannelState.Disconnected) throw new InvalidOperationException("Can't connect unless in the disconnected state.");
                State = ChannelState.Connecting;
            }

            Debug.WriteLine(String.Format("Creating command socket: host={0}, port={1}", Host, CommandPort));
            commandSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            commandSocket.ReceiveTimeout = 5000;
            commandSocket.SendTimeout = 5000;
            try { commandSocket.Connect(Host, CommandPort); }
            catch (SocketException e)
            {
                Debug.WriteLine(String.Format("Couldn't connect to {0}:{1}. The error was: {2}", Host, CommandPort, e.Message));
                lock (stateLock) State = ChannelState.Disconnected;
                commandSocket.Close();
                return false;
            }
            commandLineReader = new BufferedLineReader((buffer, index, count) => commandSocket.Receive(buffer, index, count, SocketFlags.None), CommandSocketBufferSize);

            Debug.WriteLine(String.Format("Creating poll socket: host={0}, port={1}", Host, PollPort));
            pollSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            pollSocket.SendTimeout = 5000;
            pollSocket.ReceiveTimeout = 5000;
            try { pollSocket.Connect(Host, PollPort); }
            catch (SocketException e)
            {
                Debug.WriteLine(String.Format("Couldn't connect to {0}:{1}. The error was: {2}", Host, PollPort, e.Message));
                lock (stateLock) State = ChannelState.Disconnected;
                pollSocket.Close();
                commandSocket.Close();
                return false;
            }
            pollLineReader = new BufferedLineReader((buffer, index, count) => ReadFromSocket(pollSocket, buffer, index, count), PollSocketBufferSize);

            lock (stateLock) State = ChannelState.Connected;
            channelConnectEvent.Set();

            Debug.WriteLine("Connected");
            return true;
        }

        private int ReadFromSocket(Socket socket, byte[] buffer, int index, int count)
        {
            try { return socket.Receive(buffer, index, count, SocketFlags.None); }
            catch (SocketException e)
            {
                Debug.WriteLine("I2CChannel Socket Error: " + e.Message);
                Disconnect();
                throw new I2CException("Connection closed.", e);
            }
        }

        public void Disconnect()
        {
            Debug.WriteLine("Disconnecting");

            channelConnectEvent.Reset();
            lock (stateLock)
            {
                if (State == ChannelState.Disconnecting || State == ChannelState.Disconnected) return;
                if (State == ChannelState.Connecting) throw new InvalidOperationException();
                State = ChannelState.Disconnecting;
            }

            commandSocket.Close();
            pollSocket.Close();

            lock (stateLock) State = ChannelState.Disconnected;

            Debug.WriteLine("Disconnected");
        }
        
        public bool Ping()
        {
            lock (this)
            {
                SendLine("ping");
                try
                {
                    return ReadLine() == "OK";
                }
                catch (I2CException) { return false; }
            }
        }

        public void Set(byte slaveAddress, byte register, byte value)
        {
            lock (this)
            {
                var request = String.Format("set {0} {1} {2}", slaveAddress, register, value);
                SendLine(request);

                var response = ReadLine();
                if (response != "OK")
                    throw new I2CException(String.Format("Unexpected response '{0}'.", response));
            }
        }

        public byte Get(byte slaveAddress, byte register)
        {
            lock (this)
            {
                var request = String.Format("get {0} {1}", slaveAddress, register);
                SendLine(request);

                var response = ReadLine();
                byte value;
                if (!byte.TryParse(response, out value)) throw new I2CException("Unexpected response:" + value);

                return value;
            }
        }

        public byte[] Get(byte slaveAddress, byte register, int numRegisters)
        {
            lock (this)
            {
                if (numRegisters < 1 || numRegisters > 255) throw new ArgumentException("numRegisters is invalid");

                var request = String.Format("get {0} {1} {2}", slaveAddress, register, numRegisters);
                SendLine(request);

                var response = ReadLine();

                var result = new byte[numRegisters];
                var splitResponse = response.Split(' ');
                if (splitResponse.Length != numRegisters) throw new I2CException("Incorrect number of values: " + response);

                for (int i = 0; i < splitResponse.Length; i++)
                    if (!byte.TryParse(splitResponse[i], out result[i])) throw new I2CException("Unexpected response:" + response);

                return result;
            }
        }

        public int AddPoll(int delayInMilliseconds, byte slaveAddress, byte register, int numRegisters, PollCallback pollCallback, object data)
        {
            lock (this)
            {
                string request = String.Format("addpoll {0} {1} {2} {3}", delayInMilliseconds, slaveAddress, register, numRegisters);
                SendLine(request);
                string response = ReadLine();

                var match = Regex.Match(response, "OK (\\d+)");
                if (!match.Success) throw new I2CException(String.Format("Unexpected response '{0}'", response));

                int pollID = int.Parse(match.Groups[1].Value);

                pollEntries.Add(pollID, new PollEntry() { PollID = pollID, Delay = delayInMilliseconds, I2CAddress = slaveAddress, Register = register, NumRegisters = numRegisters, Callback = pollCallback, Data = data });

                return pollID;
            }
        }

        public void RemovePoll(int pollID)
        {
            lock (this)
            {
                string request = String.Format("rmpoll {0}", pollID);
                SendLine(request);

                string response = ReadLine();
                if (response != "OK") throw new I2CException(String.Format("Unexpected response '{0}'", response));

                pollEntries.Remove(pollID);
            }
        }

        public void Dispose()
        {
            shouldFinish = true;
            pollThread.Interrupt();
            pollThread.Join();
            if (commandSocket != null) commandSocket.Close();
            if (pollSocket != null) pollSocket.Close();
        }

        #endregion


        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        
        #endregion


        #region Properties

        public ChannelState State
        {
            get { return state; }
            private set
            {
                if (state == value) return;
                state = value;
                OnPropertyChanged("State");
            }
        }

        public string Host
        {
            get;
            set;
        }

        public int CommandPort
        {
            get;
            set;
        }

        public int PollPort
        {
            get;
            set;
        }

        #endregion


        #region Private Methods

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SendLine(string s)
        {
            lock (this)
            {
            Debug.WriteLine("I2CChannel.SendLine: " + s);
            byte[] buffer = Encoding.ASCII.GetBytes(String.Concat(s, "\n"));

            int totalSent = 0;
            while (totalSent < buffer.Length) 
            {
                var num = commandSocket.Send(buffer, totalSent, buffer.Length - totalSent, SocketFlags.None);
                if (num <= 0)
                {
                    Debug.WriteLine("Connection appears to have closed.");
                    Disconnect();
                    throw new I2CException("Connection closed.");
                }
                totalSent += num;
            }
                }
        }

        private string ReadLine()
        {
            var response = commandLineReader.ReadLine();
            response = response.TrimEnd('\n', '\r');
            Debug.WriteLine("I2CChannel.ReadLine: " + response);
            return response;
        }

        private void StartPollThread()
        {
            while (!shouldFinish)
            {
                // Are we connected?
                if (State != ChannelState.Connected)
                {
                    // No, so wait until a new connection is made.
                    try { channelConnectEvent.WaitOne(); }
                    catch (ThreadInterruptedException) { }
                }
                if (shouldFinish) break;

                // Read a line.
                string response;
                try { response = pollLineReader.ReadLine(); }
                catch (ThreadInterruptedException) { break; }
                catch (I2CException e) { continue; }
                if (shouldFinish) break;

                // Get the poll ID.
                int indexOfColon = response.IndexOf(':');
                if (indexOfColon == -1)
                {
                    Debug.WriteLine("Unexpected poll response: " + response);
                    throw new I2CException();
                }
                int pollID;
                var unparsedPollID = response.Substring(0, indexOfColon);
                if (!int.TryParse(unparsedPollID, out pollID))
                {
                    Debug.WriteLine("Unexpected poll response: " + response);
                    throw new I2CException();
                }

                // Get the PollEntry
                PollEntry pollEntry;
                if (!pollEntries.TryGetValue(pollID, out pollEntry))
                {
                    Debug.WriteLine(String.Format("Unknown poll ID {0}. Skipping.", pollID));
                    continue;
                }

                // Parse the response.
                bool wasError = false;
                var pieces = response.Split(new [] { ' ' });
                if (pieces.Length != pollEntry.NumRegisters + 1 || pieces[0] != String.Format("{0}:", pollID))
                {
                    Debug.WriteLine("Unexpected poll response: " + response);
                    wasError = true;
                }
                
                var values = new int[pollEntry.NumRegisters];
                for (var i = 1; i <= pollEntry.NumRegisters && !wasError; i++)
                {
                    int valueAsInt;
                    if (!int.TryParse(pieces[i], out valueAsInt))
                    {
                        Debug.WriteLine("Error parsing poll response: " + response);
                        wasError = true;
                        break;
                    }
                    values[i - 1] = valueAsInt;
                }

                // Call the callback.
                if (wasError) values = null;
                pollEntry.Callback(pollEntry.PollID, pollEntry.I2CAddress, pollEntry.Register, values, pollEntry.Data);
            }
        }

        #endregion


        #region Fields

        ChannelState state;
        Thread pollThread;
        Socket commandSocket;
        Socket pollSocket;
        BufferedLineReader commandLineReader;
        BufferedLineReader pollLineReader;
        private Dictionary<int, PollEntry> pollEntries;
        private object stateLock;
        private AutoResetEvent channelConnectEvent;
        bool shouldFinish;

        #endregion

    }

}