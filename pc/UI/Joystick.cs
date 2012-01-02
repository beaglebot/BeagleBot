using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.DirectX.DirectInput;
using System.Diagnostics;
using System.Threading;

namespace MongooseSoftware.Robotics.RobotLib
{
	public class Joystick
    {
        #region Constructors

        public Joystick()
		{
            IsConnected = false;
        }

        #endregion


        #region Methods

        public void Initialize()
		{
			// Find all the GameControl devices that are attached.
			DeviceList gameControllerList = Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly);

			// check that we have at least one device.
			if (gameControllerList.Count == 0) return;
		
			// Move to the first device
			gameControllerList.MoveNext();
			DeviceInstance deviceInstance = (DeviceInstance)gameControllerList.Current;

			// create a device from this controller.
			joystickDevice = new Device(deviceInstance.InstanceGuid);
			joystickDevice.SetCooperativeLevel(null,CooperativeLevelFlags.Background | CooperativeLevelFlags.NonExclusive);
		
			// Tell DirectX that this is a Joystick.
			joystickDevice.SetDataFormat(DeviceDataFormat.Joystick);

			// Finally, acquire the device.
			joystickDevice.Acquire();

            IsConnected = true;
		}

        public void StartPolling()
        {
            if (pollingThreadState != PollingThreadState.Stopped) throw new ApplicationException("Can't start polling as it is still running.");
            pollingThreadState = PollingThreadState.Running;
            pollingThread = new Thread(PollingThreadMain);
            pollingThread.IsBackground = true;
            pollingThread.Start();
        }

        public void StopPolling()
        {
            if (pollingThreadState == PollingThreadState.Stopped) throw new ApplicationException("Can't stop polling as it isn't running.");
            pollingThreadState = PollingThreadState.Stopping;
        }

        public void GetCapabilities()
        {
            DeviceCaps cps = joystickDevice.Caps;
            Debug.WriteLine("Joystick Axis: " + cps.NumberAxes);
            Debug.WriteLine("Joystick Buttons: " + cps.NumberButtons);
            Debug.WriteLine("Joystick PoV hats: " + cps.NumberPointOfViews);
        }

        #endregion


        #region Properties

        public bool IsConnected
        {
            get;
            private set;
        }

		public int X
		{
			get { return state.X; }
		}

		public int Y
		{
			get { return state.Y; }
		}

        public bool Trigger
        {
            get { return state.GetButtons()[0] >= 128; }
        }

        public int? PointOfView
        {
            get 
            { 
                int x = state.GetPointOfView()[0];
                if (x == -1) return null;
                return x / 100;
            }
        }

        #endregion
        

        #region Events

        public event EventHandler TriggerPressed;

        public event EventHandler PointOfViewPressed;

        public event EventHandler MainAxisChanged;

        #endregion


        #region Private

        private void PollingThreadMain()
        {
            while (pollingThreadState == PollingThreadState.Running)
            {
                Poll();
                Thread.Sleep(50);
            }
        }

        private void Poll()
        {
			// Get the new state.
            JoystickState oldState = state;
            joystickDevice.Poll();
            state = joystickDevice.CurrentJoystickState;

			// Has the joystick been moved?
            if ((state.X != oldState.X || state.Y != oldState.Y) && MainAxisChanged != null) MainAxisChanged(this, EventArgs.Empty);

			// Is the trigger pressed?
            if (state.GetButtons()[0] >= 128 && TriggerPressed != null) TriggerPressed(this, EventArgs.Empty);

			// Is the point-of-view hat pressed?
            int[] statePointOfView = state.GetPointOfView();
			if (statePointOfView != null && statePointOfView[0] != -1 && PointOfViewPressed != null) PointOfViewPressed(this, EventArgs.Empty);
        }


        private enum PollingThreadState
        {
            Stopped,
            Stopping,
            Running
        }

        private PollingThreadState pollingThreadState;
        private Thread pollingThread;
        private Device joystickDevice;
        private JoystickState state;

        #endregion
    }
}
