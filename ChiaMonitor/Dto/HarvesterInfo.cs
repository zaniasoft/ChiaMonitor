using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
