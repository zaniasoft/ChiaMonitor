namespace ChiaMonitor.Rules
{
    class DelayResponseRule : IResponseTimeRule
    {
        public bool IsDelay(double seconds)
        {
            if (seconds > 5)
            {
                return true;
            }
            return false;
        }
    }
}
