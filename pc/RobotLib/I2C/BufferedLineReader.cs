using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace MongooseSoftware.Robotics.RobotLib.I2C
{
    public delegate int ReadBytesDelegate(byte[] buffer, int start, int count);

    public class BufferedLineReader
    {

        public BufferedLineReader(ReadBytesDelegate readBytesDelegate, int bufferSize)
        {
            this.readBytesDelegate = readBytesDelegate;
            buffer = new byte[bufferSize];
            nextFreeByte = 0;
            firstUsedByte = -1;
            nextPlaceToStartScan = -1;
        }

        private enum State
        {
            /// <summary>
            /// No data in the buffer.
            /// </summary>
            Empty,

            /// <summary>
            /// There's a single continuous segment of data in the buffer, eg ...FDDDDD... or FDDDDDDDD (where D indicates data, and F indicates the first used byte of data)
            /// </summary>
            SingleSegment,

            /// <summary>
            /// The data has wrapped around the top of the buffer, so there are two seperate segments, eg DDD......FDDD or DDDDFDDDDDD
            /// </summary>
            DoubleSegment
        }

        /// <summary>
        /// Returns null if the connection is closed.
        /// </summary>
        /// <returns></returns>
        public string ReadLine()
        {
            lock (this)
            {
                int bufferSize = buffer.Length;
                while (true)
                {
                    // What state is the buffer in?
                    State state;
                    if (firstUsedByte == -1 && nextFreeByte == 0)
                        state = State.Empty;
                    else if (firstUsedByte > nextFreeByte && nextFreeByte > 0 || firstUsedByte == nextFreeByte && firstUsedByte > 0)
                        state = State.DoubleSegment;
                    else
                        state = State.SingleSegment;

                    // Is there a line in the buffer already?
                    if (state == State.SingleSegment)
                    {
                        int count = nextFreeByte - nextPlaceToStartScan;
                        if (count <= 0) count = bufferSize - nextPlaceToStartScan;
                        int index = Array.IndexOf<byte>(buffer, 0x0A, nextPlaceToStartScan, count);
                        if (index != -1)
                        {
                            nextPlaceToStartScan = index + 1;
                            if (nextPlaceToStartScan == bufferSize) nextPlaceToStartScan = 0;

                            string result = Encoding.ASCII.GetString(buffer, firstUsedByte, index - firstUsedByte + 1);
                            firstUsedByte = index + 1;
                            if (firstUsedByte == bufferSize) firstUsedByte = 0;

                            // Have we emptied the buffer?
                            if (firstUsedByte == nextFreeByte) { nextPlaceToStartScan = firstUsedByte = -1; nextFreeByte = 0; }

                            return result;
                        }
                        nextPlaceToStartScan = nextFreeByte;
                    }
                    else if (state == State.DoubleSegment)
                    {
                        // Check the last segment (we must've already checked the first segment).
                        int index = Array.IndexOf<byte>(buffer, 0x0A, nextPlaceToStartScan, nextFreeByte - nextPlaceToStartScan);
                        if (index != -1)
                        {
                            nextPlaceToStartScan = index + 1;
                            if (nextPlaceToStartScan == bufferSize) nextPlaceToStartScan = 0;

                            string result =
                                Encoding.ASCII.GetString(buffer, firstUsedByte, bufferSize - firstUsedByte) +
                                Encoding.ASCII.GetString(buffer, 0, index + 1);
                            firstUsedByte = index + 1;
                            if (firstUsedByte == bufferSize) firstUsedByte = 0;

                            // Have we emptied the buffer?
                            if (firstUsedByte == nextFreeByte) { nextPlaceToStartScan = firstUsedByte = -1; nextFreeByte = 0; }

                            return result;
                        }
                        nextPlaceToStartScan = nextFreeByte;
                    }

                    // Is the buffer full?
                    if (IsFull) throw new I2CException(String.Format("Buffer overflow. Received more than {0} bytes without a new line character.", bufferSize));

                    // Work out the amount of data we can recieve.
                    int maxBytesCanReceive;
                    if (firstUsedByte < nextFreeByte)
                        maxBytesCanReceive = bufferSize - nextFreeByte;
                    else
                        maxBytesCanReceive = firstUsedByte - nextFreeByte;

                    // Receive it.
                    int bytesReceived = readBytesDelegate(buffer, nextFreeByte, maxBytesCanReceive);

                    // Was the connection closed?
                    if (bytesReceived <= 0) return null;

                    // Update the buffer indicies.
                    if (firstUsedByte == -1) nextPlaceToStartScan = firstUsedByte = nextFreeByte;
                    nextFreeByte += bytesReceived;
                    if (nextFreeByte == bufferSize) nextFreeByte = 0;
                }
            }
        }

        public bool IsEmpty
        {
            get { return firstUsedByte == -1; }
        }

        public bool IsFull
        {
            get { return firstUsedByte == nextFreeByte; }
        }

        /// <summary>
        /// Method used to fetch data.
        /// </summary>
        ReadBytesDelegate readBytesDelegate;

        /// <summary>
        /// Circular buffer.
        /// </summary>
        byte[] buffer;

        /// <summary>
        /// The index into the buffer where the next byte read from the socket can be written. If the buffer is empty, this will be 0. 
        /// If the buffer is full, then nextFreeByte == firstUsedByte.
        /// </summary>
        int nextFreeByte;

        /// <summary>
        /// The first byte that should be returned to the user when a single byte is read from the buffer. If the buffer is empty, this will be -1.
        /// </summary>
        int firstUsedByte;

        /// <summary>
        /// The next byte we should start searching for \n.
        /// </summary>
        int nextPlaceToStartScan;

    }
}