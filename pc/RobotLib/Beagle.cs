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

    public class Beagle : Robot
    {
        #region Constructors

        public Beagle()
        {
            I2CBus = new I2CBus();
            ServoController = new ServoController();
            MotorController = new MotorController();
            PowerSupplyController = new PowerSupplyController();
            BatteryMonitorA = new BatteryMonitor("A", 0x35);
            BatteryMonitorB = new BatteryMonitor("B", 0x34);
            IMU = new IMU();
            Camera = new Camera();
            UltrasonicRangeFinder = new UltrasonicRangeFinder();
        }


        #endregion


        #region Methods

        public override void Init()
        {
            base.Init();
            ServoController.I2CChannel = I2CBus.Channel;
            MotorController.I2CChannel = I2CBus.Channel;
            PowerSupplyController.I2CChannel = I2CBus.Channel;
            BatteryMonitorA.I2CChannel = I2CBus.Channel;
            BatteryMonitorB.I2CChannel = I2CBus.Channel;
            IMU.I2CChannel = I2CBus.Channel;
            UltrasonicRangeFinder.I2CChannel = I2CBus.Channel;
        }

        #endregion


        #region Properties

        public override string Name
        {
            get { return "Beagle"; }
        }

        public string Host
        {
            get { return I2CBus.Host; }
            set
            {
                I2CBus.Host = value;
                Camera.Host = value;
            }
        }

        public int I2CCommandPort
        {
            get { return I2CBus.CommandPort; }
            set { I2CBus.CommandPort = value; }
        }

        public int I2CPollPort
        {
            get { return I2CBus.PollPort; }
            set { I2CBus.PollPort = value; }
        }

        public int CameraPort
        {
            get { return Camera.Port; }
            set { Camera.Port = value; }
        }

        public override IEnumerable<RobotComponent> Components
        {
            get
            {
                yield return I2CBus;
                yield return MotorController;
                yield return ServoController;
                yield return PowerSupplyController;
                yield return BatteryMonitorA;
                yield return BatteryMonitorB;
                yield return IMU;
                yield return Camera;
                yield return UltrasonicRangeFinder;
            }
        }

        public I2CBus I2CBus
        {
            get;
            private set;
        }

        public MotorController MotorController
        {
            get;
            private set;
        }

        public ServoController ServoController
        {
            get;
            private set;
        }

        public PowerSupplyController PowerSupplyController
        {
            get;
            private set;
        }

        public BatteryMonitor BatteryMonitorA
        {
            get;
            private set;
        }

        public BatteryMonitor BatteryMonitorB
        {
            get;
            private set;
        }

        public IMU IMU
        {
            get;
            private set;
        }

        public Camera Camera
        {
            get;
            private set;
        }

        public UltrasonicRangeFinder UltrasonicRangeFinder
        {
            get;
            private set;
        }

        #endregion

    }
}
