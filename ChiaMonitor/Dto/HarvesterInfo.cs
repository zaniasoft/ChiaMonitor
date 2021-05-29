using System;

namespace ChiaMonitor.Dto
{
    public class HarvesterInfo
    {
        public int TotalPlots { get; set; }
        public int EligiblePlots { get; set; }
        public int Proofs { get; set; }
        public Double ResponseTime { get; set; } // seconds
        public string UnitOfTime { get; set; }


    }
}
