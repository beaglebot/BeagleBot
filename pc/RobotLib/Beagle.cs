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
            I2CBus = new I2CBusComponent();
            ServoController = new ServoComponent();
            MotorController = new MotorControllerComponent();
            PowerSupplyController = new PowerSupplyComponent();
            BatteryMonitorA = new BatteryMonitorComponent("A", 0x35);
            BatteryMonitorB = new BatteryMonitorComponent("B", 0x34);
            IMU = new ImuComponent();
            Camera = new CameraComponent();
            UltrasonicRangeFinder = new UltrasonicRangeFinderComponent();
        }


        #endregion


        #region Methods

        public override void Init()
        {
            base.Init();
            ServoController.I2CBus = I2CBus.Bus;
            MotorController.I2CBus = I2CBus.Bus;
            PowerSupplyController.I2CBus = I2CBus.Bus;
            BatteryMonitorA.I2CBus = I2CBus.Bus;
            BatteryMonitorB.I2CBus = I2CBus.Bus;
            IMU.I2CBus = I2CBus.Bus;
            UltrasonicRangeFinder.I2CBus = I2CBus.Bus;
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

        public I2CBusComponent I2CBus
        {
            get;
            private set;
        }

        public MotorControllerComponent MotorController
        {
            get;
            private set;
        }

        public ServoComponent ServoController
        {
            get;
            private set;
        }

        public PowerSupplyComponent PowerSupplyController
        {
            get;
            private set;
        }

        public BatteryMonitorComponent BatteryMonitorA
        {
            get;
            private set;
        }

        public BatteryMonitorComponent BatteryMonitorB
        {
            get;
            private set;
        }

        public ImuComponent IMU
        {
            get;
            private set;
        }

        public CameraComponent Camera
        {
            get;
            private set;
        }

        public UltrasonicRangeFinderComponent UltrasonicRangeFinder
        {
            get;
            private set;
        }

        #endregion

    }
}
