using ChiaMonitor.Dto;
using ChiaMonitor.Notifications;
using ChiaMonitor.Rules;
using ChiaMonitor.Utils;
using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ChiaHelper
{
    class Program
    {
        const int ERROR_COMMAND_ARGS = 1;

        const int EVENT_WAIT_HANDLE = 1000;

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
            [Option('n', "num", Default = false, HelpText = "Number of values to calculate statistics")]
            public bool StatsLength { get; set; }
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
                        if (NotifyValidator.IsWarningMessage(line))
                        {
                            notifier.Notify(line.GetLogLevel(), line);
                        }

                        if (NotifyValidator.IsInfoMessage(line))
                        {
                            EligiblePlotsInfo eligibleInfo = line.GetEligiblePlots();
                            if (eligibleInfo.EligiblePlots > 0)
                            {
                                if (options.ShowInfo)
                                {
                                    notifier.Notify("Eligible : " + eligibleInfo.EligiblePlots + "/" + eligibleInfo.TotalPlots + " | RT : " + eligibleInfo.ResponseTime + " " + eligibleInfo.UnitOfTime);
                                }
                            }

                            if (eligibleInfo.Proofs > 0)
                            {
                                notifier.Notify("Found " + eligibleInfo.Proofs + " proofs." + " Congrats !!");
                            }
                        }
                    }
                    else
                    {
                        eventWaitHandle.WaitOne(EVENT_WAIT_HANDLE);
                    }
                }
            }
#pragma warning disable CS0162 // Unreachable code detected
            eventWaitHandle.Close();
#pragma warning restore CS0162 // Unreachable code detected
        }
    }
}
