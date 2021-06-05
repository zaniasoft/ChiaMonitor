using ChiaMonitor.Utils;
using Microsoft.Extensions.Logging;

namespace ChiaMonitor.Notifications
{
    public class StdConsole : INotifier
    {
        public void Notify(LogLevel level, string message)
        {
            LoggerUtil.WriteLog(level, message);
        }
    }
}
