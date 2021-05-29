using ChiaMonitor.Dto;
using ChiaMonitor.Rules;
using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;

namespace ChiaMonitor.Utils
{
    public static class ChiaLogExtension
    {
        public static LogLevel GetLogLevel(this string message)
        {
            if (new LogWarningRule().IsSatisfied(message))
            {
                return LogLevel.Warning;
            }
            if (new LogErrorRule().IsSatisfied(message))
            {
                return LogLevel.Error;
            }

            return LogLevel.Information;
        }

        public static EligiblePlotsInfo GetEligiblePlots(this string message)
        {
            EligiblePlotsInfo info = new EligiblePlotsInfo();

            string pattern = @"(\d+)\s+plots were eligible for farming.*Found (\d+) proofs.*Time:\s+(\S+)\s+(\S+)\s+Total\s+(\d+)\s+plots";
            Match m = new Regex(pattern, RegexOptions.IgnoreCase).Match(message);

            if (m.Success)
            {
                info.EligiblePlots = Convert.ToInt32(m.Groups[1].Value);
                info.Proofs = Convert.ToInt32(m.Groups[2].Value);
                info.ResponseTime = Math.Round((Double)Convert.ToDouble(m.Groups[3].Value), 1);
                info.UnitOfTime = m.Groups[4].Value;
                info.TotalPlots = Convert.ToInt32(m.Groups[5].Value);
            }

            return info;
        }

    }
}
