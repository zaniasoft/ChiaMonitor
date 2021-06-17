using Microsoft.Extensions.Logging;
using System;

namespace ChiaMonitor.Notifications
{
    interface INotifier
    {
        string Title { get; set; }
        void Notify(LogLevel level, string message);
        void Notify(string message) => Notify(LogLevel.Information, message);
        void Notify(Exception ex) => Notify(LogLevel.Error, ex.ToString());
    }
}
