using ChiaMonitor.Dto;
using ChiaMonitor.Notifications;
using ChiaMonitor.Rules;
using ChiaMonitor.Stats;
using ChiaMonitor.Utils;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Timers;

namespace ChiaMonitor
{
    class Program
    {
        const int ERROR_COMMAND_ARGS = 1;
        const int ERROR_STREAMREADER_READLINE = 2;
        const int ERROR_MAINLOOP = 3;

        const int EVENT_WAIT_HANDLE = 1000; // ms

        static bool LogIsAppendingFlag = false;
        static bool FarmIsRunningFlag = false;

        static INotifier notifier = new StdConsole();
        static Options options = new Options();
        private static System.Timers.Timer aTimer;

        public class Options
        {
            [Option('t', "token", HelpText = "Line Notify Token (Get here : https://notify-bot.line.me/my/)")]
            public string Token { get; set; }
            [Option('l', "log", Default = "debug.log", HelpText = "Chia log file")]
            public string Logfile { get; set; }
            [Option('i', "info", Default = false, HelpText = "Show info")]
            public bool ShowInfo { get; set; }
            [Option('n', "num", Default = 1000, HelpText = "Number of values to calculate statistics")]
            public int StatsLength { get; set; }
            [Option('r', "interval", Default = 30, HelpText = "Interval time to notify (minutes)")]
            public int IntervalNotifyMinutes { get; set; }
            [Option('d', "digits", Default = 2, HelpText = "Digits of precision")]
            public int DigitsOfPrecision { get; set; }
            [Option('w', "watchdog", Default = 1, HelpText = "Watchdog timer to check your farm (minutes)")]
            public int WatchgodTimerMinutes { get; set; }
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

        private static void InitWatchdogTimer(int minutes)
        {
            // Create a watchdog timer.
            aTimer = new System.Timers.Timer(minutes * 60000);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            bool logFlag = LogIsAppendingFlag;
            bool farmFlag = FarmIsRunningFlag;

            LogIsAppendingFlag = false;
            FarmIsRunningFlag = false;

            if (!logFlag)
            {
                Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}", e.SignalTime);
                notifier.Notify("Chia debug log is not running, Please check that log_level: INFO has been already configured and restart Chia.");
                return;
            }
            if (!farmFlag)
            {
                Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}", e.SignalTime);
                notifier.Notify("Your farm is not running well, Please check that log_level: INFO has been already configured and restart Chia.");
            }
        }

        static void Main(string[] args)
        {
            var stopwatch = Stopwatch.StartNew();

            CommandLine.Parser.Default.ParseArguments<Options>(args)
               .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts))
               .WithNotParsed<Options>((errs) => HandleParseError(errs));

            InitWatchdogTimer(options.WatchgodTimerMinutes);
            EligiblePlotsStat rtStat = new EligiblePlotsStat(options.StatsLength);
            ChiaLogExtension.DigitsOfPrecision = options.DigitsOfPrecision;

            var eventWaitHandle = new AutoResetEvent(false);
            var fileStream = InitFileStream(options.Logfile, eventWaitHandle);

            if (!String.IsNullOrEmpty(options.Token))
            {
                notifier = new LineNotify(options.Token);
            }

            notifier.Notify("Welcome to Chia Monitor v" + AppInfo.GetVersion());

            Console.WriteLine("Notification : " + notifier.GetType().Name);
            Console.WriteLine("Including Harvester Info : " + (options.ShowInfo ? "YES" : "NO"));

            using (var sr = new StreamReader(fileStream))
            {
                var line = "";
                sr.ReadToEnd();
                Console.WriteLine("Running..");

                while (true)
                {
                    try
                    {
                        line = sr.ReadLine();
                    }
                    catch (Exception ex)
                    {
                        ExitError(ex.ToString(), ERROR_STREAMREADER_READLINE);
                    }

                    try
                    {
                        if (line != null)
                        {
                            LogIsAppendingFlag = true;

                            if (stopwatch.Elapsed.TotalMinutes > options.IntervalNotifyMinutes)
                            {
                                int total = rtStat.TotalEligiblePlots + rtStat.TotalDelayPlots;
                                double farmPerformance = 0;

                                if (total > 0)
                                {
                                    farmPerformance = Math.Round(((double)rtStat.TotalEligiblePlots / total * 100), 0);
                                }

                                notifier.Notify("During the past " + options.IntervalNotifyMinutes + " mins.\nTotal Plots : " + rtStat.TotalPlots +
                                    "\nEligible/Delay plots : " + rtStat.TotalEligiblePlots + "/" + rtStat.TotalDelayPlots + "\nFarm Performance : " + farmPerformance + "%" +
                                    "\n\nResponse Time in " + options.StatsLength + " latest data\nFastest/Avg/Worst : " + rtStat.FastestRT().RoundToString() + "/" + rtStat.AverageRT().RoundToString() + "/" + rtStat.WorstRT().RoundToString() + "s.");
                                rtStat.ResetTotalPlotsStats();
                                stopwatch.Restart();
                            }

                            if (NotifyValidator.IsFarmingMessage(line))
                            {
                                FarmIsRunningFlag = true;
                            }

                            if (NotifyValidator.IsWarningMessage(line))
                            {
                                notifier.Notify(line.GetLogLevel(), line);
                            }

                            if (NotifyValidator.IsInfoMessage(line))
                            {
                                EligiblePlotsInfo eligibleInfo = line.GetEligiblePlots();
                                if (eligibleInfo.EligiblePlots > 0)
                                {
                                    rtStat.Enqueue(eligibleInfo);

                                    if (options.ShowInfo)
                                    {
                                        notifier.Notify("Eligible : " + eligibleInfo.EligiblePlots + "/" + eligibleInfo.TotalPlots + " | RT : " + eligibleInfo.ResponseTime.RoundToString() + " " + eligibleInfo.UnitOfTime + " " + eligibleInfo.PlotKey);
                                    }
                                }

                                if (eligibleInfo.Proofs > 0)
                                {
                                    notifier.Notify("##### Found " + eligibleInfo.Proofs + " proofs." + " Congrats ! #####");
                                }
                            }
                        }
                        else
                        {
                            eventWaitHandle.WaitOne(EVENT_WAIT_HANDLE);
                        }
                    }
                    catch (Exception ex)
                    {
                        ExitError(ex.ToString(), ERROR_MAINLOOP);
                    }
                }
            }
#pragma warning disable CS0162 // Unreachable code detected
            eventWaitHandle.Close();
#pragma warning restore CS0162 // Unreachable code detected
        }

        private static void ExitError(string errorMsg, int errorCode)
        {
            Console.WriteLine("Error Code : " + errorCode);
            Console.WriteLine("Error Msg  : " + errorMsg);

            Console.WriteLine("\nPlease send this error to the developer to fix");
            Console.Write("Please enter to exit..");
            Console.ReadLine();
            Environment.Exit(errorCode);
        }
    }
}
