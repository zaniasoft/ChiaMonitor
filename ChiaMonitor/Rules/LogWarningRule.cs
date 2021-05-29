namespace ChiaMonitor.Rules
{
    class LogWarningRule : INotifyRule
    {
        public bool IsSatisfied(string message)
        {
            if (message.Contains(": WARNING"))
            {
                return true;
            }
            return false;
        }
    }
}
