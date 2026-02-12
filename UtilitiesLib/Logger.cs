using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace UtilitiesLib
{
    public static class Logger
    {
        public static Action<string> WriteMessage;

        // This method is used to log a message. It will call the WriteMessage action if it has been subscribed to.
        public static void LogMessage(string msg)
        {
            WriteMessage?.Invoke(msg);            
        }
    }
}
