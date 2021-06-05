using Microsoft.Extensions.Logging;
using Serilog;

namespace ChiaMonitor.Utils
{
    public static class LoggerUtil
    {
        public static void WriteLog(LogLevel level, string message)
        {
            if (level.ToString().Equals("Trace") || level.ToString().Equals("Debug"))
            {
                Log.Logger.Debug(message);
            }
            if (level.ToString().Equals("Information"))
            {
                Log.Logger.Information(message);
            }
            if (level.ToString().Equals("Warning"))
            {
                Log.Logger.Warning(message);
            }
            if (level.ToString().Equals("Error"))
            {
                Log.Logger.Error(message);
            }
            if (level.ToString().Equals("Critical"))
            {
                Log.Logger.Fatal(message);
            }
        }
    }
}
