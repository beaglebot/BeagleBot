using System;
using System.ComponentModel;
using System.Diagnostics;
using MongooseSoftware.Robotics.RobotLib.I2C;

namespace MongooseSoftware.Robotics.RobotLib.Components
{
    public enum ComponentState
    {
        None,
        Failed,
        Disconnected,
        Connected,
        Disposed
    }

    public abstract class RobotComponent : INotifyPropertyChanged, IDisposable
    {

        #region Constructors

        protected RobotComponent()
        {
        }

        #endregion


        #region Methods

        public virtual void Init(Robot robot)
        {
            if (State != ComponentState.None) throw new InvalidOperationException("State should be None.");
            Robot = robot;
            State = ComponentState.Disconnected;
        }

        public bool Connect()
        {
            if (State == ComponentState.Connected || State == ComponentState.Disposed) throw new InvalidOperationException("Invalid state");

            if (!CheckIfPossibleToConnect())
            {
                State = ComponentState.Failed;
                return false;
            }

            bool success = false;
            try { success = OnConnecting(); }
            catch (Exception e) 
            {
                Debug.WriteLine(String.Format("Exception occured while attempting to connect to component {0}. The exception was: {1}", Name, e));
            }

            if (!success)
            {
                State = ComponentState.Failed;
                return false;
            }

            State = ComponentState.Connected;
            return true;
        }

        /// <summary>
        /// Implementations of this method should return false if something in the connection process fails. 
        /// </summary>
        /// <returns></returns>
        public virtual bool OnConnecting()
        {
            return true;
        }

        public abstract bool CheckIfPossibleToConnect();

        public abstract bool CheckIfStillConnected();

        public virtual void Disconnect()
        {
        }

        public virtual void Dispose()
        {
        }

        protected void OnNotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        #endregion


        #region Properties

        public abstract string Name { get; }

        public ComponentState State
        {
            get { return state; }
            internal set
            {
                if (state == value) return;
                state = value;
                OnNotifyPropertyChanged("State");
            }
        }

        public Robot Robot
        {
            get;
            private set;
        }

        #endregion


        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion


        #region Fields

        private ComponentState state;

        #endregion

    }

}
