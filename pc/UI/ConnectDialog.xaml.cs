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
using System.Windows.Shapes;
using MongooseSoftware.Robotics.UI.Properties;
using MongooseSoftware.Robotics.RobotLib;

namespace MongooseSoftware.Robotics.UI
{
	/// <summary>
	/// Interaction logic for ConnectDialog.xaml
	/// </summary>
	public partial class ConnectDialog : Window
	{
		public ConnectDialog()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
            hostTextBox.Text = Settings.Default.Host;
            i2cCommandPortTextBox.Text = Settings.Default.I2CProxyPort.ToString();
            cameraPortTextBox.Text = Settings.Default.CameraPort.ToString();
		}

		private void connectButton_Click(object sender, RoutedEventArgs e)
		{
            string host = hostTextBox.Text;
            int i2cCommandPort = int.Parse(i2cCommandPortTextBox.Text);
            int cameraPort = int.Parse(cameraPortTextBox.Text);

			Settings.Default.Host = host;
			Settings.Default.I2CProxyPort = i2cCommandPort;
            Settings.Default.CameraPort = cameraPort;
			Settings.Default.Save();

			var robot = new Beagle();
            robot.Host = host;
            robot.I2CCommandPort = i2cCommandPort;
            robot.I2CPollPort = i2cCommandPort + 1;
            robot.CameraPort = cameraPort;
            robot.Init();
            bool ok = robot.Connect();
			if (!ok)
			{
				MessageBox.Show("Unable to connect. Please check your settings.", "Connect", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			var main = new MainWindow();
			main.Robot = robot;
			main.Show();

			expectedToClose = true;
			Close();
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			if (!expectedToClose) Environment.Exit(0);
		}

		private bool expectedToClose;
	}
}
