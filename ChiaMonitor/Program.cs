using ChiaMonitor.Dto;
using ChiaMonitor.Notifications;
using CommandLine;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace ChiaHelper
{
    class Program
    {
        const int ERROR_COMMAND_ARGS = 1;

        static INotifier notifier = new StdConsole();
        static Options options = new Options();

        public class Options
        {
            [Option('t', "token", HelpText = "Line Notify Token (Get here : https://notify-bot.line.me/my/)")]
            public string Token { get; set; }
            [Option('l', "log", Default = "debug.log", HelpText = "Chia log file")]
            public string Logfile { get; set; }
            [Option('i', "info", Default = false, HelpText = "Show info")]
            public bool ShowInfo { get; set; }
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            Console.Write("Please enter to exit..");
            Console.ReadLine();
            Environment.Exit(ERROR_COMMAND_ARGS);
        }

        private static void RunOptionsAndReturnExitCode(Options opts)
        {
            options = opts;
        }

        private static FileStream InitFileStream(string file, EventWaitHandle ewh)
        {
            var fsw = new FileSystemWatcher(".")
            {
                Filter = file,
                EnableRaisingEvents = true
            };
            fsw.Changed += (s, e) => ewh.Set();
            return new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
               .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts))
               .WithNotParsed<Options>((errs) => HandleParseError(errs));

            var eventWaitHandle = new AutoResetEvent(false);
            var fileStream = InitFileStream(options.Logfile, eventWaitHandle);

            if (!String.IsNullOrEmpty(options.Token))
            {
                notifier = new LineNotify(options.Token);
            }

            Console.WriteLine("Notification : " + notifier.GetType().Name);
            Console.WriteLine("Including Harvester Info : " + (options.ShowInfo ? "YES" : "NO"));

            using (var sr = new StreamReader(fileStream))
            {
                var line = "";
                sr.ReadToEnd();
                Console.WriteLine("Running..");

                while (true)
                {
                    line = sr.ReadLine();
                    if (line != null)
                    {
                        if (line.Contains("harvester"))
                        {
                            if (line.Contains(": WARNING") || line.Contains(": ERROR"))
                            {
                                notifier.Notify(LogLevel.Error, line);
                            }

                            string pattern = @"(\d+)\s+plots were eligible for farming.*Found (\d+) proofs.*Time:\s+(\S+)\s+(\S+)\s+Total\s+(\d+)\s+plots";
                            Match m = new Regex(pattern, RegexOptions.IgnoreCase).Match(line);

                            if (m.Success)
                            {
                                HarvesterInfo hInfo = new HarvesterInfo();
                                hInfo.EligiblePlots = Convert.ToInt32(m.Groups[1].Value);
                                hInfo.Proofs = Convert.ToInt32(m.Groups[2].Value);
                                hInfo.ResponseTime = Math.Round((Double)Convert.ToDouble(m.Groups[3].Value), 1);
                                hInfo.UnitOfTime = m.Groups[4].Value;
                                hInfo.TotalPlots = Convert.ToInt32(m.Groups[5].Value);

                                if (hInfo.EligiblePlots > 0)
                                {
                                    if (options.ShowInfo)
                                    {
                                        notifier.Notify("Eligible : " + hInfo.EligiblePlots + "/" + hInfo.TotalPlots + " | RT : " + hInfo.ResponseTime + " " + hInfo.UnitOfTime);
                                    }
                                }

                                if (hInfo.Proofs > 0)
                                {
                                    notifier.Notify("Found " + hInfo.Proofs + " proofs." + " Congrats !!");
                                }
                            }

                        }
                    }
                    else
                    {
                        eventWaitHandle.WaitOne(1000);
                    }
                }
            }

            eventWaitHandle.Close();
        }
    }
}
