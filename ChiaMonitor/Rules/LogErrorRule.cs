namespace ChiaMonitor.Rules
{
    class LogErrorRule : INotifyRule
    {
        public bool IsSatisfied(string message)
        {
            if (message.Contains(": ERROR"))
            {
                return true;
            }
            return false;
        }
    }
}
