using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO.Ports;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using MongooseSoftware.Robotics.RobotLib.Components;

namespace MongooseSoftware.Robotics.RobotLib
{
    public enum RobotState
    {
        None,
        Disconnected,
        Connected,
        Disposed
    }

    public abstract class Robot : INotifyPropertyChanged,IDisposable
    {
        #region Constructors

        protected Robot()
        {
            watchdogThread = new Thread(WatchDogThreadMain) {Name = "Robot Watchdog", IsBackground = true};
            watchdogThread.Start();
        }


        #endregion


        #region Methods

        public virtual void Init()
        {
            if (State != RobotState.None) throw new InvalidOperationException("The state should be none.");

            foreach (var component in Components)
                component.Init(this);

            State = RobotState.Disconnected;
        }

        public bool Connect()
        {
            if (State != RobotState.Disconnected) throw new InvalidOperationException("The state should be Disconnected.");

            foreach (var component in Components)
                component.Connect();

            State = RobotState.Connected;
			return true;
        }

        public void Disconnect()
        {
            if (State != RobotState.Connected) throw new InvalidOperationException("The robot is not connected.");

            foreach (var component in Components.ToList().Reverse<RobotComponent>())
                if (component.State == ComponentState.Connected)
                    component.Disconnect();

            State = RobotState.Disconnected;
        }

        public void Dispose()
        {
            // Stop the watchdog, so it doesn't try and wake everyone up again.
            if (watchdogThread.IsAlive)
            {
                stopWatchdog = true;
                watchdogThread.Interrupt();
                watchdogThread.Join();
            }

            // Disconnect the components.
            if (State == RobotState.Connected) Disconnect();

            // Dispose of the components.
            foreach (var component in Components.ToList().Reverse<RobotComponent>())
                component.Dispose();

            State = RobotState.Disposed;
        }

        #endregion


        #region Private Methods

        private void WatchDogThreadMain()
        {
            while (!stopWatchdog)
            {
                try { Thread.Sleep(5000); }
                catch (ThreadInterruptedException) { }
                if (stopWatchdog) continue;

                foreach (var component in Components)
                {
                    if (component.State == ComponentState.Connected)
                    {
                        if (!component.CheckIfStillConnected())
                        {
                            Debug.WriteLine(String.Format("Component {0} appears to be missing. Attempting to disconnect.", component.Name));
                            component.Disconnect();
                            component.State = ComponentState.Failed;
                        }
                    }
                    else if (component.State == ComponentState.Failed)
                    {
                        if (component.CheckIfPossibleToConnect())
                        {
                        Debug.WriteLine(String.Format("Component {0} was missing, but appears to be back. Attempting to connect.", component.Name));
                        component.Connect();
                        }
                    }
                }
            }
        }

        protected void OnNotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        
        #endregion


        #region Properties

        public abstract string Name
        {
            get;
        }

        public RobotState State
        {
            get;
            private set;
        }

        public abstract IEnumerable<RobotComponent> Components
        {
            get;
        }

        #endregion


        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        
        #endregion


        #region Fields

        private Thread watchdogThread;
        private bool stopWatchdog;

        #endregion

    }
}
