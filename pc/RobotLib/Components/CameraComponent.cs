using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using MongooseSoftware.Robotics.RobotLib.Video;

namespace MongooseSoftware.Robotics.RobotLib.Components
{
    public class CameraComponent : RobotComponent
    {

        #region Constructors

        public CameraComponent()
        {
            Camera = new Camera();
            Camera.PropertyChanged += Camera_PropertyChanged;
            Camera.ImageArrived += Camera_ImageArrived;
        }

        #endregion


        #region Methods

        public override bool OnConnecting()
        {
            return Camera.Connect();
        }

        public override bool CheckIfPossibleToConnect()
        {
            return true;
        }

        public override bool CheckIfStillConnected()
        {
            return Camera.State == CameraState.Connected;
        }

        public override void Disconnect()
        {
            base.Disconnect();
            Camera.Disconnect();
            State = ComponentState.Disconnected;
        }

        public override void Dispose()
        {
            base.Dispose();
            Camera.Dispose();
        }

        private void Camera_PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName != "State") return;
            switch (Camera.State)
            {
                case CameraState.Disconnected:
                    State = ComponentState.Disconnected;
                    break;

                case CameraState.Connected:
                    State = ComponentState.Connected;
                    break;

                case CameraState.Failed:
                    State = ComponentState.Failed;
                    break;
            }
        }

        private void Camera_ImageArrived(object sender, ImageArrivedEventArgs args)
        {
            if (ImageArrived != null) ImageArrived(this, args);
        }

        #endregion


        #region Properties

        public override string Name
        {
            get { return "Camera"; }
        }

        public Camera Camera
        {
            get;
            private set;
        }

        public string Host
        {
            get { return Camera.Host; }
            set { Camera.Host = value; }
        }

        public int Port
        {
            get { return Camera.Port; }
            set { Camera.Port = value; }
        }

        public int NumImagesSuccesfullyReceived
        {
            get { return Camera.NumImagesSuccesfullyReceived; }
        }

        public int NumImagesAttemptedToReceive
        {
            get { return Camera.NumImagesAttemptedToReceive; }
        }

        #endregion


        #region Events

        public event EventHandler<ImageArrivedEventArgs> ImageArrived;

        #endregion

    }
}
