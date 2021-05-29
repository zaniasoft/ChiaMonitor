using System.Linq;

namespace ChiaMonitor.Rules
{
    public class NotifyRulesCollection : INotifyRule
    {
        private readonly INotifyRule[] rules;

        public NotifyRulesCollection(params INotifyRule[] rules)
        {
            this.rules = rules;
        }

        public bool IsSatisfied(string Message)
        {
            return this.rules.All(r => r.IsSatisfied(Message));
        }
    }
}
