using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChiaMonitor.Notifications
{
    public class NotifyManager : INotifier
    {
        private readonly INotifier[] notifiers;

        private string title;

        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                Array.ForEach(this.notifiers, notifier => { notifier.Title = value; });
            }
        }

        public NotifyManager(params INotifier[] notifiers)
        {
            this.notifiers = notifiers;
        }

        public NotifyManager(IList<INotifier> notifiers)
        {
            this.notifiers = notifiers.ToArray();
        }

        public void Notify(LogLevel level, string message)
        {
            try
            {
                Array.ForEach(this.notifiers, notifier => notifier.Notify(level, message));
            }
            catch (Exception e)
            {
                Log.Error("NotifyManager Error : " + e.Message);
            }
        }

        public void Notify(string message)
        {
            Notify(LogLevel.Information, message);
        }

        public string ListNotifiers()
        {
            string result = "";
            Array.ForEach(this.notifiers, notifier => result += notifier.GetType().Name + " ");
            return result;
        }
    }
}
