using ChiaMonitor.Dto;
using System.Collections.Concurrent;
using System.Linq;

namespace ChiaMonitor.Stats
{
    public class EligiblePlotsStat
    {
        readonly ConcurrentQueue<EligiblePlotsInfo> EligiblePlotsInfoQueue = new ConcurrentQueue<EligiblePlotsInfo>();
        private readonly object lockObject = new object();
        public int Limit { get; set; }

        public EligiblePlotsStat(int limit)
        {
            Limit = limit;
        }

        public double AverageRT()
        {
            return EligiblePlotsInfoQueue.Select(x => x.ResponseTime).Average();
        }

        public double WorstRT()
        {
            return EligiblePlotsInfoQueue.Select(x => x.ResponseTime).Max();
        }

        public void Enqueue(EligiblePlotsInfo value)
        {
            EligiblePlotsInfoQueue.Enqueue(value);
            lock (lockObject)
            {
                while (EligiblePlotsInfoQueue.Count > Limit && EligiblePlotsInfoQueue.TryDequeue(out _)) ;
            }
        }
    }
}
