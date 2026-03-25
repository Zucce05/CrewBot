using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CrewBot.Classes
{
    public class Logging
    {
        private static readonly IReadOnlyDictionary<LogSeverity, string> LogFileNames = new Dictionary<LogSeverity, string>
        {
            { LogSeverity.Critical, "CriticalLog.txt" },
            { LogSeverity.Error, "ErrorLog.txt" },
            { LogSeverity.Warning, "WarningLog.txt" },
            { LogSeverity.Info, "InfoLog.txt" },
            { LogSeverity.Verbose, "VerboseLog.txt" },
            { LogSeverity.Debug, "DebugLog.txt" }
        };

        private readonly Dictionary<LogSeverity, object> logLocks = new Dictionary<LogSeverity, object>
        {
            { LogSeverity.Critical, new object() },
            { LogSeverity.Error, new object() },
            { LogSeverity.Warning, new object() },
            { LogSeverity.Info, new object() },
            { LogSeverity.Verbose, new object() },
            { LogSeverity.Debug, new object() }
        };

        public Logging()
        {
            foreach (string fileName in LogFileNames.Values)
            {
                EnsureLogFileExists(Path.Combine(BotStartup.LogsDirectoryPath, fileName));
            }
        }

        public Task Log(LogMessage msg)
        {
            if (!logLocks.TryGetValue(msg.Severity, out object fileLock))
            {
                return Task.CompletedTask;
            }

            string path = GetLogPath(msg.Severity);
            if (string.IsNullOrWhiteSpace(path))
            {
                return Task.CompletedTask;
            }

            lock (fileLock)
            {
                try
                {
                    Directory.CreateDirectory(BotStartup.LogsDirectoryPath);
                    using (StreamWriter writer = File.AppendText(path))
                    {
                        writer.WriteLine($":: {DateTime.Now} ::");
                        writer.WriteLine($"{msg}");
                        writer.WriteLine("------------------");
                    }
                }
                catch (InvalidOperationException)
                {
                    Console.Error.WriteLine($"Logging failure writing {path}");
                }
                catch (IOException)
                {
                    Console.Error.WriteLine($"Logging IO failure writing {path}");
                }
                catch (UnauthorizedAccessException)
                {
                    Console.Error.WriteLine($"Logging access failure writing {path}");
                }
            }

            return Task.CompletedTask;
        }

        private static void EnsureLogFileExists(string path)
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (File.Exists(path))
                {
                    return;
                }

                using (FileStream fs = File.Create(path))
                {
                    fs.Close();
                }
            }
            catch (IOException)
            {
                Console.Error.WriteLine($"Logging IO failure creating {path}");
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Logging access failure creating {path}");
            }
        }

        private static string GetLogPath(LogSeverity severity)
        {
            if (!LogFileNames.TryGetValue(severity, out string fileName))
            {
                return string.Empty;
            }

            return Path.Combine(BotStartup.LogsDirectoryPath, fileName);
        }
    }
}
