using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using MongooseSoftware.Robotics.RobotLib;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using MongooseSoftware.Robotics.RobotLib.Components;
using MongooseSoftware.Robotics.RobotLib.I2C;
using MongooseSoftware.Robotics.RobotLib.Utilities;

namespace MongooseSoftware.Robotics.TestHarness
{
    public enum PacketType
    {
        ImageHeader = 0,
        DataBlock = 1
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ImageHeader
    {
        public PacketType PacketType;
        public int Width;
        public int Height;
        public int Size;
        public int PixelFormat;
        public int ImageNumber;
        public long TimeStamp;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BlockHeader
    {
        public PacketType PacketType;
        public int ImageNumber;
        public int BlockNumber;
    }

    class Program
    {

        static void Main(string[] args)
        {
            /*byte[] mjpeg = File.ReadAllBytes(@"c:\tmp\out - mjpeg.bin");
            var jpeg = MJpegUtilities.ConvertMJpegBufferToJpeg(mjpeg);
            File.WriteAllBytes(@"c:\tmp\out.jpg", jpeg);
            return;

            var i2c = new I2CChannel();
            i2c.CommandPort = 2000;
            i2c.PollPort = 2001;
            i2c.Host = "beagle";
            i2c.Connect();

            i2c.AddPoll(1000, 0x69, 29, 6, cb, null);*/

            Camera camera = new Camera();
            camera.Host = "192.168.0.70";
            camera.Port = 3000;
            camera.Init(null);
            camera.Connect();

            /*var imu = new IMUBoardProxy();
            imu.Init(null);
            imu.I2CChannel = i2c;
            imu.Connect();*/

            DateTime start = DateTime.Now;
            while (true)
            {
                Thread.Sleep(1000);
                //Debug.WriteLine(String.Format("Frame rate={0}, Success rate={1}",  camera.NumImagesAttemptedToReceive / (DateTime.Now - start).TotalMilliseconds * 1000, (double)camera.NumImagesSuccesfullyReceived / camera.NumImagesAttemptedToReceive * 100));
            }

            /*i2c = new I2CChannel();
            i2c.Connect("beagle", 3000);
            i2c.AddPoll2(1000, 48, 2, 4, cb2, null);
            while (true) Thread.Sleep(1000);*/

            /*var beagle = new Beagle();
            beagle.Connect("beagle", 2000);
            beagle.StartPolling();
            beagle.MotorController.LeftMotorSpeed = +0.5;
            while (true)
            {
                Debug.WriteLine("LeftMotorSpeed:" + beagle.MotorController.LeftMotorSpeed);
                Debug.WriteLine("LeftMotorState:" + beagle.MotorController.LeftMotorState);
                Debug.WriteLine("ChargerA->State: " + beagle.PowerSupplyController.ChargerAState);
                Debug.WriteLine("BatteryA->Current: " + beagle.BatteryMonitorA.Current);
                Debug.WriteLine("BatteryA->Voltage: " + beagle.BatteryMonitorA.Voltage);
                Debug.WriteLine("BatteryA->ChargeRemaining %: " + beagle.BatteryMonitorA.ChargeRemainingPercentage);
                Debug.WriteLine("BatteryA->Temperature: " + beagle.BatteryMonitorA.Temperature);
                Debug.WriteLine("BatteryA->Accumulated Current: " + beagle.BatteryMonitorA.AccumulatedCurrent);
                Debug.WriteLine("BatteryA->Remaining Capacity: " + beagle.BatteryMonitorA.RemainingCapacity);
                Debug.WriteLine("");
                Debug.WriteLine("ChargerB->State: " + beagle.PowerSupplyController.ChargerBState);
                Debug.WriteLine("BatteryB->Current: " + beagle.BatteryMonitorB.Current);
                Debug.WriteLine("BatteryB->Voltage: " + beagle.BatteryMonitorB.Voltage);
                Debug.WriteLine("BatteryB->ChargeRemaining %: " + beagle.BatteryMonitorB.ChargeRemainingPercentage);
                Debug.WriteLine("BatteryB->Temperature: " + beagle.BatteryMonitorB.Temperature);
                Debug.WriteLine("BatteryB->Accumulated Current: " + beagle.BatteryMonitorB.AccumulatedCurrent);
                Debug.WriteLine("BatteryB->Remaining Capacity: " + beagle.BatteryMonitorB.RemainingCapacity);
                Debug.WriteLine("");
                Debug.WriteLine("Servo->IsPanServoEnabled:" + beagle.ServoController.IsPanServoEnabled);
                Debug.WriteLine("Servo->PanAngle:" + beagle.ServoController.PanAngle);
                Debug.WriteLine("Servo->IsTiltServoEnabled:" + beagle.ServoController.IsTiltServoEnabled);
                Debug.WriteLine("Servo->TiltAngle:" + beagle.ServoController.TiltAngle);
                Debug.WriteLine("");
                Debug.WriteLine("Motor->LeftMotorState:" + beagle.MotorController.LeftMotorState);
                Debug.WriteLine("Motor->LeftMotorSpeed:" + beagle.MotorController.LeftMotorSpeed);
                Debug.WriteLine("");
                Debug.WriteLine("Motor->RightMotorState:" + beagle.MotorController.RightMotorState);
                Debug.WriteLine("Motor->RightMotorSpeed:" + beagle.MotorController.RightMotorSpeed);
                Debug.WriteLine("");
                Debug.WriteLine("");

                Thread.Sleep(1000);
            }   
            return;

           Thread thread = new Thread(PollThreadMain);
           thread.Start();*/

            int imageHeaderSize;
            unsafe { imageHeaderSize = sizeof(ImageHeader); }

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.Bind(new IPEndPoint(IPAddress.Any, 3456));
                byte[] buffer = new byte[1024];
                while (true)
                {
                    // Wait for an image header.
                    int count = socket.Receive(buffer);
                    if (count != imageHeaderSize)
                    {
                        Debug.WriteLine("Packet wrong size while waiting for an image header.");
                        continue;
                    }

                    // Marshal it into a struct.
                    GCHandle pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    ImageHeader imageHeader = (ImageHeader)Marshal.PtrToStructure(pinnedBuffer.AddrOfPinnedObject(), typeof(ImageHeader));
                    pinnedBuffer.Free();

                    // Check it looks sensible.
                    if (imageHeader.PacketType != PacketType.ImageHeader ||
                        imageHeader.Height <= 0 && imageHeader.Height > 10000 ||
                        imageHeader.Width <= 0 || imageHeader.Width > 10000 ||
                        imageHeader.Size <= 0 || imageHeader.Size > 10000000)
                    {
                        Debug.WriteLine("Unexpected packet while waiting for an image header.");
                        continue;
                    }

                    byte[] data = ReceiveDataBlocks(imageHeader, socket);

                    var bitmap = ConvertToColorBitmap(imageHeader, data);

                    bitmap.Save("out.bmp");
                }
            }
        }

        private static Bitmap ConvertToColorBitmap(ImageHeader imageHeader, byte[] imageData)
        {
            var bitmap = new Bitmap(imageHeader.Width, imageHeader.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            int x = 0, y = 0;
            for (int i = 0; i < imageHeader.Size; i+=4)
            {
                byte y1 = imageData[i];
                byte u = imageData[i + 1];
                byte y2 = imageData[i + 2];
                byte v = imageData[i + 3];

                bitmap.SetPixel(x++, y, yuv2rgb(y1, u, v));
                bitmap.SetPixel(x++, y, yuv2rgb(y2, u, v));

                if (x == imageHeader.Width)
                {
                    x = 0;
                    y++;
                }
            }

            return bitmap;
        }

        private static byte[] ReceiveDataBlocks(ImageHeader imageHeader, Socket socket)
        {
            int blockHeaderSize;
            unsafe { blockHeaderSize = sizeof(BlockHeader); }

            var receiveBuffer = new byte[1024];
            var resultBuffer = new byte[imageHeader.Size];
            int resultOffset = 0;
            int nextExpectedBlockNumber = 0;

            while (resultOffset < imageHeader.Size)
            {
                // Wait for a data block.
                int bytesRead = socket.Receive(receiveBuffer);
                if (bytesRead < blockHeaderSize)
                {
                    Debug.WriteLine("Packet too small while waiting for a data block.");
                    return null;
                }

                // Marshal it into a struct.
                var pinnedBuffer = GCHandle.Alloc(receiveBuffer, GCHandleType.Pinned);
                var blockHeader = (BlockHeader)Marshal.PtrToStructure(pinnedBuffer.AddrOfPinnedObject(), typeof(BlockHeader));
                pinnedBuffer.Free();

                // Check it looks sensible.
                if (blockHeader.PacketType != PacketType.DataBlock ||
                    blockHeader.ImageNumber != imageHeader.ImageNumber ||
                    blockHeader.BlockNumber != nextExpectedBlockNumber)
                {
                    Debug.WriteLine("Unexpected packet while waiting for a data block.");
                    continue;
                }

                // Copy the data to the result buffer.
                Array.Copy(receiveBuffer, blockHeaderSize, resultBuffer, resultOffset, bytesRead - blockHeaderSize);
                resultOffset += bytesRead - blockHeaderSize;
                nextExpectedBlockNumber++;
            }

            return resultBuffer;
        }
    
        static byte Clamp(double f)
        {
            f = Math.Round(f);
            if (f < 0) return 0;
            if (f > 255) return 255;
            return (byte)f;
        }
            
        static Color yuv2rgb(byte y, byte u, byte v)
        {
            var blue =  Clamp(1.164 * (y - 16.0)                      + 2.018 * (u - 128.0));
            var green = Clamp(1.164 * (y - 16.0) - 0.813 * (v - 128.0) - 0.391 * (u - 128.0));
            var red =   Clamp(1.164 * (y - 16.0) + 1.596 * (v - 128.0));
            return Color.FromArgb(red, green, blue);
        }
        
        static void cb(int pollID, byte i2cAddress, byte register, int[] value, object data)
        {
            Debug.WriteLine(String.Format("{0}: {1}", pollID, value));
        }
        
        static void ReadResponse(Socket socket)
        {
            byte[] buffer = new byte[1000];
            int count = socket.Receive(buffer);
            string response = ASCIIEncoding.ASCII.GetString(buffer, 0, count);
            Debug.WriteLine(response);
        }
        static void PollThreadMain()
        {
            while (true)
            {
                Console.WriteLine("Creating socket");
                using (var pollSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    pollSocket.Connect("beagle", 3001);
                    Console.WriteLine("Connected");

                    byte[] buffer = new byte[1000];

                    bool isRunning = true;
                    while (isRunning)
                    {
                        int count = pollSocket.Receive(buffer);
                        if (count == 0) break;
                        string response = ASCIIEncoding.ASCII.GetString(buffer, 0, count);
                        Console.Write(response);
                    }

                    pollSocket.Close();
                    Console.WriteLine("Closed socket");
                }
            }
        }
    }
}
