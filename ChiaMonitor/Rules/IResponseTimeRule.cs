namespace ChiaMonitor.Rules
{
    public interface IResponseTimeRule
    {
        bool IsDelay(double seconds);

    }
}
