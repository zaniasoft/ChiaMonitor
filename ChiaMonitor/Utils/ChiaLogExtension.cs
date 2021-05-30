using ChiaMonitor.Dto;
using ChiaMonitor.Rules;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ChiaMonitor.Utils
{
    public static class ChiaLogExtension
    {
        public static int DigitsOfPrecision { get; set; } = 2;

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

            // Ex. 2 plots were eligible for farming 1771d71848... Found 0 proofs. Time: 0.63919 s. Total 484 plots
            //     2 plots were eligible for farming xxxxxxxxxx... Found 1 proofs. Time: 0.89684 s. Total 276 plots
            string pattern = @"(\d+)\s+plots were eligible for farming\s+(\S+)\s+Found (\d+) proofs.*Time:\s+(\S+)\s+(\S+)\s+Total\s+(\d+)\s+plots";
            Match m = new Regex(pattern, RegexOptions.IgnoreCase).Match(message);

            if (m.Success)
            {
                if (Int32.TryParse(m.Groups[1].Value, out int eligiblePlots))
                {
                    info.EligiblePlots = eligiblePlots;
                }
                info.PlotKey = m.Groups[2].Value;
                if (Int32.TryParse(m.Groups[3].Value, out int proofs))
                {
                    info.Proofs = proofs;
                }
                if (Double.TryParse(m.Groups[4].Value, out double responseTime))
                {
                    info.ResponseTime = responseTime;
                }
                info.UnitOfTime = m.Groups[5].Value;
                if (Int32.TryParse(m.Groups[6].Value, out int totalPlots))
                {
                    info.TotalPlots = totalPlots;
                }
            }
            return info;
        }

        public static string RoundToString(this double value)
        {
            return Math.Round(value, DigitsOfPrecision).ToString("F" + DigitsOfPrecision, CultureInfo.InvariantCulture);
        }
    }
}
