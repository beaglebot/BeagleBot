using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MongooseSoftware.Robotics.UI
{
    public class MyTraceListener : TraceListener
    {
        public MyTraceListener()
        {
        }

        public event EventHandler<StringEventArgs> NewMessage;

        public override void Write(string message)
        {
            if (NewMessage != null) NewMessage(this, new StringEventArgs(message));
        }

        public override void WriteLine(string message)
        {
            Write(message + Environment.NewLine);
        }
    }


    public class StringEventArgs : EventArgs
    {
        public StringEventArgs(string message)
        {
            Message = message;
        }

        public string Message
        {
            get;
            private set;
        }
    }
}
