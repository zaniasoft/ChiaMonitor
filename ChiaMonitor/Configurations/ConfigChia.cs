namespace ChiaMonitor.Configurations
{
    public class ConfigChia
    {
        public string LogFile { get; set; }
        public string PlotterLogDirectory { get; set; }

        public ConfigChia getInstance()
        {
            return this;
        }
    }
}
