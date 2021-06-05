namespace ChiaMonitor.Configurations
{
    public class ConfigNotification
    {
        public string LineToken { get; set; }
        public bool ShowEligiblePlot { get; set; }
        public bool ShowPlottingStatus { get; set; }
        public int NotifyInterval { get; set; }
        public int DigitsOfPrecision { get; set; }
        public int WatchdogTimer { get; set; }
        public int StatsLength { get; set; }
    }
}
