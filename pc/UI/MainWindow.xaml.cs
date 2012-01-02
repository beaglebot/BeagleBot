using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MongooseSoftware.Robotics.RobotLib;
using System.Diagnostics;

namespace MongooseSoftware.Robotics.UI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {
        #region Constructors

        public MainWindow()
		{
			InitializeComponent();
        }

        #endregion

        #region Event Handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
			if (_robot == null) throw new ApplicationException("The Robot property has not been set.");

            MyTraceListener traceListener = new MyTraceListener();
            traceListener.NewMessage += OnNewMessage;
            Debug.Listeners.Add(traceListener);

            _joystick = new Joystick();
            _joystick.Initialize();
            if (_joystick.IsConnected) _joystick.StartPolling();

            _motorController.Bind(_robot, _joystick);
            _motorVisualizer.Bind(_robot, _joystick);
            _batteryStatusVisualizer.Bind(_robot, _joystick);
            _servoController.Bind(_robot, _joystick);
            _ultrasonicRangeFinderVisualizer.Bind(_robot, _joystick);
            _compassVisualizer.Bind(_robot, _joystick);
            _camera.Bind(_robot, _joystick);
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            if (_robot != null) _robot.Dispose();

            Environment.Exit(0);
        }

        void OnNewMessage(object sender, StringEventArgs e)
        {
            //Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate { AppendToLog(e.Message); });
        }

        void AppendToLog(string text)
        {
            _logTextBox.AppendText(text);
            _logTextBox.ScrollToEnd();
        }

        #endregion

		#region Properties

		public Beagle Robot
		{
			get { return _robot; }
			set { _robot = value; }
		}

		#endregion

		#region Fields

		private Beagle _robot;
        private Joystick _joystick;

        #endregion

    }
}
