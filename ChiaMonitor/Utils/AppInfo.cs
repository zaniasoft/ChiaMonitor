using System.Reflection;

namespace ChiaMonitor.Utils
{
    public static class AppInfo
    {
        public static string GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString()[0..^2];
        }
    }
}
