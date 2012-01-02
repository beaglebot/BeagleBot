using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MongooseSoftware.Robotics.RobotLib.Components;

namespace MongooseSoftware.Robotics.UI
{
    /// <summary>
    /// Interaction logic for CameraControl.xaml
    /// </summary>
    public partial class CameraControl : RobotComponentUserControl
    {
        #region Constructors

        public CameraControl()
        {
            InitializeComponent();
        }

        #endregion


        #region Methods

        public override void Bind(RobotLib.Beagle robot, RobotLib.Joystick joystick)
        {
            base.Bind(robot, joystick);
            robot.Camera.ImageArrived += Camera_ImageArrived;
            robot.Camera.PropertyChanged += Camera_PropertyChanged;
            Camera_PropertyChanged(this, new PropertyChangedEventArgs("State"));
        }
        
        private void Camera_ImageArrived(object source, ImageArrivedEventArgs args)
        {
            Dispatcher.BeginInvoke((ThreadStart) (() => Camera_ImageArrived_UIThread(source, args)));
        }

        private void Camera_PropertyChanged(object source, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "State")
            {
                try
                {
                    ignoreConnectedCheckBoxCheckedEvent = true;
                    if (Robot.Camera.State == ComponentState.Connected)
                        connectedCheckBox.IsChecked = true;
                    else if (Robot.Camera.State == ComponentState.Disconnected)
                        connectedCheckBox.IsChecked = false;
                }
                finally
                {
                    ignoreConnectedCheckBoxCheckedEvent = false;
                }
            }
        }

        private void connectedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreConnectedCheckBoxCheckedEvent) return;

            Robot.Camera.Connect();
        }

        private void connectedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ignoreConnectedCheckBoxCheckedEvent) return;
            Robot.Camera.Disconnect();
        }

        private void Camera_ImageArrived_UIThread(object source, ImageArrivedEventArgs args)
        {
            var bitmap = LoadBitmap(args.Bitmap);
            image.Width = bitmap.Width;
            image.Height = bitmap.Height;
            image.Source = bitmap;
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr handle);

        public static BitmapSource LoadBitmap(System.Drawing.Bitmap source)
        {
            var ip = source.GetHbitmap();
            var bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(ip);
            return bs;

        }

        #endregion


        #region Properties

        public int Port
        {
            get { return Robot.Camera.Port; }
            set { Robot.Camera.Port = value; }
        }

        public override RobotComponent Component
        {
            get { return Robot.Camera; }
        }

        public override Shape StateShape
        {
            get { return stateEllipse; }
        }

        #endregion


        #region Fields

        private bool ignoreConnectedCheckBoxCheckedEvent;

        #endregion

    }
}
