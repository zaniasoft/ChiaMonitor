using ChiaMonitor.Configurations;
using ChiaMonitor.Dto;
using ChiaMonitor.Notifications;
using ChiaMonitor.Rules;
using ChiaMonitor.Stats;
using ChiaMonitor.Utils;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;

namespace ChiaMonitor
{
    class Program
    {
        const int ERROR_COMMAND_ARGS = 1;
        const int ERROR_STREAMREADER_READLINE = 2;
        const int ERROR_MAINLOOP = 3;
        const int ERROR_LOG_NOTFOUND = 4;

        const int EVENT_WAIT_HANDLE = 1000; // ms

        static bool LogIsAppendingFlag = false;
        static bool FarmIsRunningFlag = false;

        static readonly List<INotifier> notifiers = new List<INotifier>();
        static NotifyManager notifyManager;

        private static System.Timers.Timer aTimer;

        static ConfigChia configChia = null;
        static ConfigNotification configNotification = null;
        static ConfigConsole configConsole = null;

        static string debugLogPath;
        static string plotterLogPath;

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
                notifyManager.Notify("Chia debug log is not running, Please check that log_level: INFO has been already configured and restart Chia.");
                return;
            }
            if (!farmFlag)
            {
                notifyManager.Notify("Your farm is not running well, Please check that log_level: INFO has been already configured and restart Chia.");
            }
        }

        static private void AutoFindLog()
        {
            // Auto find debug.log under .chia folder
            string chia_path = "";

            if (String.IsNullOrEmpty(configChia.LogFile))
            {
                Log.Information("Auto find debug.log");
                try
                {
                    string path = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName;
                    if (Environment.OSVersion.Version.Major >= 6)
                    {
                        path = Directory.GetParent(path).ToString();
                    }
                    chia_path = Directory.GetDirectories(path).Where(s => s.EndsWith(".chia")).FirstOrDefault();
                }
                catch (Exception ex)
                {

                }
            }

            if (String.IsNullOrEmpty(chia_path))
            {
                Log.Information("Finding debug log from configuration");
                debugLogPath = configChia.LogFile;
                plotterLogPath = configChia.PlotterLogDirectory;
            }
            else
            {
                debugLogPath = Path.GetFullPath(Path.Combine(chia_path, @"mainnet\log\debug.log"));
                plotterLogPath = Path.GetFullPath(Path.Combine(chia_path, @"mainnet\plotter\"));

                // If can not find debug.log then use value from config.json
                if (!File.Exists(debugLogPath))
                {
                    Log.Information("Auto find debug.log failed, Finding debug log from configuration");
                    debugLogPath = configChia.LogFile;
                }
                /* if (!Directory.Exists(plotterLogPath))
                {
                    Log.Information("Auto find plotter directory failed, Finding debug log from configuration");
                    plotterLogPath = configChia.PlotterLogDirectory;
                } */
            }

            // Final check debug log file and plotter directory
            if (!File.Exists(debugLogPath))
            {
                ExitError("Can not find debug.log, Please check your config.json", ERROR_LOG_NOTFOUND);
            }

            /* if (!Directory.Exists(plotterLogPath))
             {
                 ExitError("Can not find plotter directory, Please check your config.json", ERROR_LOG_NOTFOUND);
             } */

            //Log.Information("Found debug log and plotter folder");
            Log.Information("Found debug log");
        }

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                // .WriteTo.File("logs/chiamonitor.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("Welcome to Chia Monitor v{0}", AppInfo.GetVersion());

            var stopwatch = Stopwatch.StartNew();

            Log.Information("Loading config.json");
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false);

            try
            {
                IConfiguration config = builder.Build();
                configChia = config.GetSection("Chia").Get<ConfigChia>();
                configNotification = config.GetSection("Notification").Get<ConfigNotification>();
                configConsole = config.GetSection("Console").Get<ConfigConsole>();
            }
            catch (Exception ex)
            {
                ExitError(ex.Message, ERROR_LOG_NOTFOUND);
            }

            // Auto find debug.log under .chia folder
            AutoFindLog();

            Log.Information("Initialize Watchdog");
            InitWatchdogTimer(configNotification.WatchdogTimer);

            EligiblePlotsStat rtStat = new EligiblePlotsStat(configNotification.StatsLength);

            ChiaLogExtension.DigitsOfPrecision = configNotification.DigitsOfPrecision;
            Log.Information("Digits Of Precision is {0}", ChiaLogExtension.DigitsOfPrecision);

            var eventWaitHandle = new AutoResetEvent(false);
            var fileStream = InitFileStream(debugLogPath, eventWaitHandle);

            // Add default notifier (StdConsole)
            notifiers.Add(new StdConsole());

            // Add Line Notify if LineToken is not empty, 
            if (!String.IsNullOrEmpty(configNotification.LineToken))
            {
                notifiers.Add(new LineNotify(configNotification.LineToken));
            }
            else
            {
                Log.Warning("Line Token is not set in config.json");
            }

            // Add Discord Webhook if DiscordWebhook is not empty, 
            if (!String.IsNullOrEmpty(configNotification.DiscordWebhook))
            {
                notifiers.Add(new DiscordWebhook(configNotification.DiscordWebhook));
            }
            else
            {
                Log.Warning("Discord Webhook is not set in config.json");
            }

            notifyManager = new NotifyManager(notifiers);

            if (!String.IsNullOrEmpty(configNotification.Title))
            {
                Log.Information("Name : {0}", configNotification.Title);
                notifyManager.Title = configNotification.Title;
            }

            notifyManager.Notify("Welcome to Chia Monitor v" + AppInfo.GetVersion() + " " + Char.ConvertFromUtf32(0x10003D));

            Log.Information("Debug log : {0}", debugLogPath);
            Log.Information("Notification : {0}", notifyManager.ListNotifiers());
            Log.Information("Show Passed filter plots : {0}", (configNotification.ShowEligiblePlot ? "YES" : "NO"));
            Log.Information("Sending notification welcome message");

            using (var sr = new StreamReader(fileStream))
            {
                var line = "";
                sr.ReadToEnd();
                Log.Information("Running..");

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

                            if (configConsole.ShowAllDebugLog)
                            {
                                Log.Debug(line);
                            }

                            if (stopwatch.Elapsed.TotalMinutes > configNotification.NotifyInterval)
                            {
                                int total = rtStat.TotalEligiblePlots + rtStat.TotalDelayPlots;
                                double farmPerformance = 0;
                                string farmPerformanceEmoji = "";

                                if (total > 0)
                                {
                                    farmPerformance = Math.Round(((double)rtStat.TotalEligiblePlots / total * 100), 2);
                                    if (farmPerformance == 100)
                                    {
                                        farmPerformanceEmoji = Char.ConvertFromUtf32(0x100079);
                                    }
                                    else if (farmPerformance >= 95)
                                    {
                                        farmPerformanceEmoji = Char.ConvertFromUtf32(0x100090);
                                    }
                                    else if (farmPerformance >= 90)
                                    {
                                        farmPerformanceEmoji = Char.ConvertFromUtf32(0x100092);
                                    }
                                    else if (farmPerformance >= 85)
                                    {
                                        farmPerformanceEmoji = Char.ConvertFromUtf32(0x10007B);
                                    }
                                    else if (farmPerformance >= 80)
                                    {
                                        farmPerformanceEmoji = Char.ConvertFromUtf32(0x10007C);
                                    }
                                    else
                                    {
                                        farmPerformanceEmoji = Char.ConvertFromUtf32(0x10007E);
                                    }
                                }

                                notifyManager.Notify("During the past " + configNotification.NotifyInterval + " mins.\nTotal Plots : " + rtStat.TotalPlots +
                                    "\nEligible/Delay plots : " + rtStat.TotalEligiblePlots + "/" + rtStat.TotalDelayPlots + "\nFarm Performance : " + farmPerformance + "% " + farmPerformanceEmoji +
                                    "\n\nResponse Time in " + configNotification.StatsLength + " latest data\nFastest/Avg/Worst : " + rtStat.FastestRT().RoundToString() + "/" + rtStat.AverageRT().RoundToString() + "/" + rtStat.WorstRT().RoundToString() + "s.");
                                rtStat.ResetTotalPlotsStats();
                                stopwatch.Restart();
                            }

                            if (NotifyValidator.IsFarmingMessage(line))
                            {
                                FarmIsRunningFlag = true;
                            }

                            if (NotifyValidator.IsWarningMessage(line))
                            {
                                notifyManager.Notify(line.GetLogLevel(), line + Char.ConvertFromUtf32(0x10007C));
                            }

                            if (NotifyValidator.IsInfoMessage(line))
                            {
                                EligiblePlotsInfo eligibleInfo = line.GetEligiblePlots();
                                if (eligibleInfo.EligiblePlots > 0)
                                {
                                    string eligiblePlotMsg = "Passed : " + eligibleInfo.EligiblePlots + "/" + eligibleInfo.TotalPlots + " | RT : " + eligibleInfo.ResponseTime.RoundToString() + " " + eligibleInfo.UnitOfTime + " " + eligibleInfo.PlotKey;
                                    rtStat.Enqueue(eligibleInfo);
                                    if (configNotification.ShowEligiblePlot)
                                    {
                                        string emoji = "";
                                        if (eligibleInfo.ResponseTime <= 1)
                                        {
                                            emoji = Char.ConvertFromUtf32(0x100079);
                                        }
                                        else if (eligibleInfo.ResponseTime <= 2)
                                        {
                                            emoji = Char.ConvertFromUtf32(0x100090);
                                        }
                                        else if (eligibleInfo.ResponseTime <= 3)
                                        {
                                            emoji = Char.ConvertFromUtf32(0x100092);
                                        }
                                        else if (eligibleInfo.ResponseTime <= 4)
                                        {
                                            emoji = Char.ConvertFromUtf32(0x10007B);
                                        }
                                        else if (eligibleInfo.ResponseTime <= 5)
                                        {
                                            emoji = Char.ConvertFromUtf32(0x10007C);
                                        }
                                        else
                                        {
                                            emoji = Char.ConvertFromUtf32(0x10007E);
                                        }
                                        notifyManager.Notify(eligiblePlotMsg + emoji);
                                    }
                                    else
                                    {
                                        Log.Information(eligiblePlotMsg);
                                    }
                                }

                                if (eligibleInfo.Proofs > 0)
                                {
                                    notifyManager.Notify("\n" + Char.ConvertFromUtf32(0x1F389) + Char.ConvertFromUtf32(0x1F389) + Char.ConvertFromUtf32(0x1F389) + Char.ConvertFromUtf32(0x1F389) + Char.ConvertFromUtf32(0x1F389) + Char.ConvertFromUtf32(0x1F389) +
"\n\nCongratulations.\nFound " + eligibleInfo.Proofs + " proofs." +
"\n\n" + Char.ConvertFromUtf32(0x1F38A) + Char.ConvertFromUtf32(0x1F38A) + Char.ConvertFromUtf32(0x1F38A) + Char.ConvertFromUtf32(0x1F38A) + Char.ConvertFromUtf32(0x1F38A) + Char.ConvertFromUtf32(0x1F38A));
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
            Log.Error("Error Code : {0}", errorCode);
            Log.Error("Error Msg  : {0}", errorMsg);
            Log.Error("Please send this error to the developer to fix");
            Console.Write("\nPlease enter to exit..");
            Console.ReadLine();
            Log.CloseAndFlush();

            Environment.Exit(errorCode);
        }
    }
}
