using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace MongooseSoftware.Robotics.RobotLib.Video
{
    public enum CameraState
    {
        Connected,
        Disconnected,
        Failed
    }

    public class ImageArrivedEventArgs : EventArgs
    {
        public int ImageNumber { get; set; }
        public long Timestamp { get; set; }
        public Bitmap Bitmap { get; set; }
    }

    public class Camera : INotifyPropertyChanged
    {

        #region Constants 
        
        const int ImageHeaderMagicNumber = 0x34343434;
        const int MaxQueueSize = 2;

        #endregion


        #region Constructors

        public Camera()
        {
            unsafe { imageHeaderSize = sizeof(ImageHeader); }
            headerBuffer = new byte[imageHeaderSize];

            unprocessedImageLock = new object();
            unprocessedImages = new Queue<UnprocessedImage>();
            unprocessedImagesNoLongerEmptyEvent = new AutoResetEvent(false);

            imageProcessingThread = new Thread(ImageProcessorMain);
            imageProcessingThread.Name = "Image Processing";
            imageProcessingThread.Start();

            State = CameraState.Disconnected;
        }

        #endregion


        #region Methods

        public bool Connect()
        {
            if (Port == 0) throw new InvalidOperationException("Port hasn't been assigned.");
            
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try { socket.Connect(Host, Port); }
            catch (SocketException e)
            {
                Debug.WriteLine(String.Format("Error connecting to {0}:{1}. The error was: {2}", Host, Port, e.Message));
                State = CameraState.Failed;
                return false;
            }

            var args = new SocketAsyncEventArgs();
            args.Completed += receive_Completed;
            PrepareToReceiveImageHeader(args);
            State = CameraState.Connected;

            return true;
        }

        public void Disconnect()
        {
            State = CameraState.Disconnected;
            try { socket.Close(); }
            catch (SocketException e) { }
        }

        public void Dispose()
        {
            stopImageProcessing = true;
            Disconnect();
            imageProcessingThread.Interrupt();
            imageProcessingThread.Join();
        }

        #endregion


        #region Properties

        public string Host
        {
            get;
            set;
        }

        public int Port
        {
            get;
            set;
        }

        public int NumImagesSuccesfullyReceived
        {
            get { return numImagesSuccesfullyReceived; }
        }        
        
        public int NumImagesAttemptedToReceive
        {
            get { return numImagesAttemptedToReceive; }
        }

        public CameraState State
        {
            get { return state; }
            set
            {
                if (state == value) return;
                state = value;
                OnPropertyChanged("State");
            }
        }

        #endregion


        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<ImageArrivedEventArgs> ImageArrived;

        #endregion


        #region Implementation

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ImageProcessorMain()
        {
            while (!stopImageProcessing)
            {
                // Get an image to process.
                UnprocessedImage current = null;
                while (current == null)
                {
                    lock (unprocessedImageLock)
                    {
                        if (unprocessedImages.Count > 0) current = unprocessedImages.Dequeue();
                    }

                    if (current == null)
                    {
                        try { unprocessedImagesNoLongerEmptyEvent.WaitOne(); }
                        catch (ThreadInterruptedException) { }
                    }
                    if (stopImageProcessing) return;
                }

                // Process it.
                Bitmap bitmap = null;
                switch (current.Format)
                {
                    case "YUYV":
                        bitmap = ConvertYUYVToColorBitmap(current);
                        break;
                    case "MJPG":
                        bitmap = ConvertMJPGToColorBitmap(current);
                        break;
                    default:
                        Debug.WriteLine(String.Format("ERROR: unknown image type {0}.", current.Format));
                        break;
                }
                if (bitmap != null && ImageArrived != null) ImageArrived(this, new ImageArrivedEventArgs() { Bitmap = bitmap, ImageNumber = current.ImageNumber, Timestamp = current.TimeStamp });
            }
        }

        void receive_Completed(object sender, SocketAsyncEventArgs args)
        {
            ProcessReceive(args);
        }

        void PrepareToReceiveImageHeader(SocketAsyncEventArgs args)
        {
            args.SetBuffer(headerBuffer, 0, headerBuffer.Length);
            numHeaderBytesReceived = 0;
            receiveState = ReceiveState.WaitingForImageHeader;
            ReceiveNextPacketAsync(args);
        }

        void ProcessReceive(SocketAsyncEventArgs args)
        {
            if (State != CameraState.Connected)
                return;

            if (args.BytesTransferred == 0 || args.SocketError != SocketError.Success)
            {
                Debug.WriteLine("Camera connection closed.");
                Disconnect();
                State = CameraState.Failed;
                return;
            }
                
            switch (receiveState)
            {
                case ReceiveState.WaitingForImageHeader:
                    ProcessImageHeader(args);
                    break;

                case ReceiveState.WaitingForDataBlock:
                    ProcessDataBlock(args);
                    break;
            }
        }

        void ReceiveNextPacketAsync(SocketAsyncEventArgs args)
        {
            if (!socket.ReceiveAsync(args))
                ProcessReceive(args);
        }

        void ProcessImageHeader(SocketAsyncEventArgs args)
        {
            // Have we received an entire image header yet?
            numHeaderBytesReceived += args.BytesTransferred;
            if (numHeaderBytesReceived < imageHeaderSize)
            {
                // No, so wait till we get some more data.
                args.SetBuffer(numHeaderBytesReceived, imageHeaderSize - numHeaderBytesReceived);
                ReceiveNextPacketAsync(args);
                return;
            }

            // Marshal it into a struct.
            GCHandle pinnedBuffer = GCHandle.Alloc(args.Buffer, GCHandleType.Pinned);
            imageHeader = (ImageHeader)Marshal.PtrToStructure(pinnedBuffer.AddrOfPinnedObject(), typeof(ImageHeader));
            pinnedBuffer.Free();

            // Check it looks sensible.
            if (imageHeader.MagicNumber != ImageHeaderMagicNumber ||
                imageHeader.Height <= 0 && imageHeader.Height > 10000 ||
                imageHeader.Width <= 0 || imageHeader.Width > 10000 ||
                imageHeader.Size <= 0 || imageHeader.Size > 10000000)
            {
                Debug.WriteLine("ERROR: bad image header");
                ReceiveNextPacketAsync(args);
                return;
            }

            // Looks like we've got a new image to download. Setup to receive the image data.
            numImagesAttemptedToReceive++;
            imageBuffer = new byte[imageHeader.Size];
            numImagesBytesReceived = 0;
            receiveState = ReceiveState.WaitingForDataBlock;
            args.SetBuffer(imageBuffer, 0, imageBuffer.Length);
            ReceiveNextPacketAsync(args);
        }

        void ProcessDataBlock(SocketAsyncEventArgs args)
        {
            numImagesBytesReceived += args.BytesTransferred;
            if (numImagesBytesReceived < imageBuffer.Length)
            {
                // No, so wait till we get some more data.
                args.SetBuffer(numImagesBytesReceived, imageBuffer.Length - numImagesBytesReceived);
                ReceiveNextPacketAsync(args);
                return;
            }

            numImagesSuccesfullyReceived++;

            var image = new UnprocessedImage();
            image.Width = imageHeader.Width;
            image.Height = imageHeader.Height;
            image.Format = "" + (char)imageHeader.Format1 + (char)imageHeader.Format2 + (char)imageHeader.Format3 +(char)imageHeader.Format4;
            image.TimeStamp = imageHeader.TimeStamp;
            image.Data = imageBuffer;

            lock (unprocessedImageLock)
            {
                unprocessedImages.Enqueue(image);
                if (unprocessedImages.Count == 1) unprocessedImagesNoLongerEmptyEvent.Set();
                while (unprocessedImages.Count > MaxQueueSize) unprocessedImages.Dequeue();
            }
            imageBuffer = null;

            PrepareToReceiveImageHeader(args);
        }

        private static Bitmap ConvertYUYVToColorBitmap(UnprocessedImage unprocessedImage)
        {
            var bitmap = new Bitmap(unprocessedImage.Width, unprocessedImage.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            int x = 0, y = 0;
            for (int i = 0; i < unprocessedImage.Data.Length; i+=4)
            {
                byte y1 = unprocessedImage.Data[i];
                byte u = unprocessedImage.Data[i + 1];
                byte y2 = unprocessedImage.Data[i + 2];
                byte v = unprocessedImage.Data[i + 3];

                bitmap.SetPixel(x++, y, ConvertYuv2ToRgb(y1, u, v));
                bitmap.SetPixel(x++, y, ConvertYuv2ToRgb(y2, u, v));

                if (x ==  unprocessedImage.Width)
                {
                    x = 0;
                    y++;
                }
            }

            return bitmap;
        }
        
        private static Bitmap ConvertMJPGToColorBitmap(UnprocessedImage unprocessedImage)
        {
            byte[] jpegData = MJpegUtilities.ConvertMJpegBufferToJpeg(unprocessedImage.Data);
            return (Bitmap)Image.FromStream(new MemoryStream(jpegData));
        }

        static Color ConvertYuv2ToRgb(byte y, byte u, byte v)
        {
            var blue =  Clamp(1.164 * (y - 16.0)                      + 2.018 * (u - 128.0));
            var green = Clamp(1.164 * (y - 16.0) - 0.813 * (v - 128.0) - 0.391 * (u - 128.0));
            var red =   Clamp(1.164 * (y - 16.0) + 1.596 * (v - 128.0));
            return Color.FromArgb(red, green, blue);
        }

        static byte Clamp(double f)
        {
            f = Math.Round(f);
            if (f < 0) return 0;
            if (f > 255) return 255;
            return (byte)f;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ImageHeader
        {
            public int MagicNumber;
            public int Width;
            public int Height;
            public int Size;
            public byte Format1;
            public byte Format2;
            public byte Format3;
            public byte Format4;
            public int ImageNumber;
            public long TimeStamp;
        }

        enum ReceiveState
        {
            WaitingForImageHeader,
            WaitingForDataBlock
        }

        private CameraState state;

        private readonly int imageHeaderSize;

        private Socket socket;
        private ReceiveState receiveState;
        private int numImagesAttemptedToReceive, numImagesSuccesfullyReceived;

        private readonly byte[] headerBuffer;
        private int numHeaderBytesReceived;
        private ImageHeader imageHeader;

        private byte[] imageBuffer;
        private int numImagesBytesReceived;

        class UnprocessedImage
        {
            public int Width;
            public int Height;
            public string Format;
            public long TimeStamp;
            public int ImageNumber;
            public byte[] Data;
        }

        Thread imageProcessingThread;
        private Queue<UnprocessedImage> unprocessedImages;
        private object unprocessedImageLock;
        private AutoResetEvent unprocessedImagesNoLongerEmptyEvent;
        bool stopImageProcessing;

        #endregion


    }
}
