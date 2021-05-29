namespace ChiaMonitor.Dto
{
    public class EligiblePlotsInfo
    {
        public int TotalPlots { get; set; }
        public int EligiblePlots { get; set; }
        public int Proofs { get; set; }
        public double ResponseTime { get; set; } // seconds
        public string UnitOfTime { get; set; }

    }
}
