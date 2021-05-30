using ChiaMonitor.Dto;
using ChiaMonitor.Rules;
using System.Collections.Concurrent;
using System.Linq;

namespace ChiaMonitor.Stats
{
    public class EligiblePlotsStat
    {
        readonly ConcurrentQueue<EligiblePlotsInfo> EligiblePlotsInfoQueue = new ConcurrentQueue<EligiblePlotsInfo>();
        private readonly object lockObject = new object();
        public int Limit { get; set; }
        public int TotalEligiblePlots { get; set; }
        public int TotalDelayPlots { get; set; }
        public int TotalPlots { get; set; }


        public EligiblePlotsStat(int limit)
        {
            Limit = limit;
        }

        public void ResetTotalPlotsStats()
        {
            TotalEligiblePlots = 0;
            TotalDelayPlots = 0;
        }

        public double FastestRT()
        {
            if (!EligiblePlotsInfoQueue.IsEmpty)
            {
                return EligiblePlotsInfoQueue.Select(x => x.ResponseTime).Min();
            }
            return 0;
        }

        public double AverageRT()
        {
            if (!EligiblePlotsInfoQueue.IsEmpty)
            {
                return EligiblePlotsInfoQueue.Select(x => x.ResponseTime).Average();
            }
            return 0;
        }

        public double WorstRT()
        {
            if (!EligiblePlotsInfoQueue.IsEmpty)
            {
                return EligiblePlotsInfoQueue.Select(x => x.ResponseTime).Max();
            }
            return 0;
        }

        public void Enqueue(EligiblePlotsInfo value)
        {
            IResponseTimeRule rtRule = new DelayResponseRule();

            if (rtRule.IsDelay(value.ResponseTime))
            {
                TotalDelayPlots += value.EligiblePlots;
            }
            else
            {
                TotalEligiblePlots += value.EligiblePlots;
            }

            TotalPlots = value.TotalPlots;

            EligiblePlotsInfoQueue.Enqueue(value);
            lock (lockObject)
            {
                while (EligiblePlotsInfoQueue.Count > Limit && EligiblePlotsInfoQueue.TryDequeue(out _)) ;
            }
        }
    }
}
