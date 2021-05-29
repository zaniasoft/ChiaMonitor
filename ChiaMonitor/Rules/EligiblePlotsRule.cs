namespace ChiaMonitor.Rules
{
    public class EligiblePlotsRule : INotifyRule
    {
        public bool IsSatisfied(string message)
        {
            if (message.Contains("plots were eligible"))
            {
                return true;
            }
            return false;
        }
    }
}
