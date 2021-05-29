namespace ChiaMonitor.Rules
{
    class HarvesterLogRule : INotifyRule
    {
        public bool IsSatisfied(string message)
        {
            if (message.Contains("harvester"))
            {
                return true;
            }
            return false;
        }
    }
}
