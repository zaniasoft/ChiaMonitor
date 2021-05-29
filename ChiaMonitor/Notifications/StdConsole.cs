using Microsoft.Extensions.Logging;
using System;

namespace ChiaMonitor.Notifications
{
    public class StdConsole : INotifier
    {
        public void Notify(LogLevel level, string message)
        {
            Console.WriteLine(level.ToString() + " : " + message);
        }
    }
}
