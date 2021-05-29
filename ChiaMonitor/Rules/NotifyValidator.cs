namespace ChiaMonitor.Rules
{
    public static class NotifyValidator
    {
        public static bool IsInfoMessage(string message)
        {
            INotifyRule allRules = AddRules(new HarvesterLogRule());

            return allRules.IsSatisfied(message);
        }

        public static bool IsWarningMessage(string message)
        {
            INotifyRule allRules = AddRules(new LogWarningRule(), new LogErrorRule(), new HarvesterLogRule());

            return allRules.IsSatisfied(message);
        }

        public static INotifyRule AddRules(params INotifyRule[] rules)
        {
            return new NotifyRulesCollection(rules);
        }
    }
}
