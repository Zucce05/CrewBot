﻿using Discord;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CrewBot.Classes
{
    public class Logging
    {
        private readonly object CriticalLogLock = new object();
        private readonly object ErrorLogLock = new object();
        private readonly object WarningLogLock = new object();
        private readonly object InfoLogLock = new object();
        private readonly object VerboseLogLock = new object();
        private readonly object DebugLogLock = new object();

        public Logging()
        {
            if (!File.Exists("CriticalLog.txt"))
            {
                using (FileStream fs = File.Create("CriticalLog.txt"))
                {
                    fs.Close();
                }
            }
            if (!File.Exists("ErrorLog.txt"))
            {
                using (FileStream fs = File.Create("ErrorLog.txt"))
                {
                    fs.Close();
                }
            }
            if (!File.Exists("WarningLog.txt"))
            {
                using (FileStream fs = File.Create("WarningLog.txt"))
                {
                    fs.Close();
                }
            }
            if (!File.Exists("InfoLog.txt"))
            {
                using (FileStream fs = File.Create("InfoLog.txt"))
                {
                    fs.Close();
                }
            }
            if (!File.Exists("VerboseLog.txt"))
            {
                using (FileStream fs = File.Create("VerboseLog.txt"))
                {
                    fs.Close();
                }
            }
            if (!File.Exists("DebugLog.txt"))
            {
                using (FileStream fs = File.Create("DebugLog.txt"))
                {
                    fs.Close();
                }
            }
        }

        public Task Log(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                    lock (CriticalLogLock)
                    {
                        try
                        {
                            StreamWriter CriticalLogWriter = File.AppendText("CriticalLog.txt");
                            CriticalLogWriter.WriteLine($":: {DateTime.Now.ToString()} ::");
                            CriticalLogWriter.WriteLine($"{msg}");
                            CriticalLogWriter.WriteLine($"------------------");
                            CriticalLogWriter.Close();
                        }
                        catch (System.InvalidOperationException exception)
                        {
                            _ = Log(new LogMessage(LogSeverity.Error, $"Logging", $"{exception.Message}"));
                        }
                        catch (System.IO.IOException exception)
                        {
                            _ = Log(new LogMessage(LogSeverity.Error, $"Logging", $"{exception.Message}"));
                        }
                    }
                    break;
                case LogSeverity.Error:
                    lock (ErrorLogLock)
                    {
                        try
                        {
                            StreamWriter ErrorLogWriter = File.AppendText("ErrorLog.txt");
                            ErrorLogWriter.WriteLine($":: {DateTime.Now.ToString()} ::");
                            ErrorLogWriter.WriteLine($"{msg}");
                            ErrorLogWriter.WriteLine($"------------------");
                            ErrorLogWriter.Close();
                        }
                        catch (System.InvalidOperationException exception)
                        {
                            _ = Log(new LogMessage(LogSeverity.Error, $"Logging", $"{exception.Message}"));
                        }
                        catch (System.IO.IOException exception)
                        {
                            _ = Log(new LogMessage(LogSeverity.Error, $"Logging", $"{exception.Message}"));
                        }
                    }
                    break;
                case LogSeverity.Warning:
                    lock (WarningLogLock)
                    {
                        try
                        {
                            StreamWriter WarningLogWriter = File.AppendText("WarningLog.txt");
                            WarningLogWriter.WriteLineAsync($":: {DateTime.Now.ToString()} ::");
                            WarningLogWriter.WriteLineAsync($"{msg}");
                            WarningLogWriter.WriteLineAsync($"------------------");
                            WarningLogWriter.Close();
                        }
                        catch (System.InvalidOperationException exception)
                        {
                            _ = Log(new LogMessage(LogSeverity.Error, $"Logging", $"{exception.Message}"));
                        }
                        catch (System.IO.IOException exception)
                        {
                            _ = Log(new LogMessage(LogSeverity.Error, $"Logging", $"{exception.Message}"));
                        }
                    }
                    break;
                case LogSeverity.Info:
                    lock (InfoLogLock)
                    {
                        try
                        {
                            StreamWriter InfoLogWriter = File.AppendText("InfoLog.txt");
                            InfoLogWriter.WriteLine($":: {DateTime.Now.ToString()} ::");
                            InfoLogWriter.WriteLine($"{msg}");
                            InfoLogWriter.WriteLine($"------------------");
                            InfoLogWriter.Close();
                        }
                        catch (System.InvalidOperationException exception)
                        {
                            _ = Log(new LogMessage(LogSeverity.Error, $"Logging", $"{exception.Message}"));
                        }
                        catch (System.IO.IOException exception)
                        {
                            _ = Log(new LogMessage(LogSeverity.Error, $"Logging", $"{exception.Message}"));
                        }
                    }
                    break;
                case LogSeverity.Verbose:
                    lock (VerboseLogLock)
                    {
                        try
                        {
                            StreamWriter VerboseLogWriter = File.AppendText("VerboseLog.txt");
                            VerboseLogWriter.WriteLineAsync($":: {DateTime.Now.ToString()} ::");
                            VerboseLogWriter.WriteLineAsync($"{msg}");
                            VerboseLogWriter.WriteLineAsync($"------------------");
                            VerboseLogWriter.Close();
                        }
                        catch (System.InvalidOperationException exception)
                        {
                            _ = Log(new LogMessage(LogSeverity.Error, $"Logging", $"{exception.Message}"));
                        }
                        catch (System.IO.IOException exception)
                        {
                            _ = Log(new LogMessage(LogSeverity.Error, $"Logging", $"{exception.Message}"));
                        }
                    }
                    break;
                case LogSeverity.Debug:
                    lock (DebugLogLock)
                    {
                        try
                        {
                            StreamWriter DebugLogWriter = File.AppendText("DebugLog.txt");
                            DebugLogWriter.WriteLineAsync($":: {DateTime.Now.ToString()} ::");
                            DebugLogWriter.WriteLineAsync($"{msg}");
                            DebugLogWriter.WriteLineAsync($"------------------");
                            DebugLogWriter.Close();
                        }
                        catch (System.InvalidOperationException exception)
                        {
                            _ = Log(new LogMessage(LogSeverity.Error, $"Logging", $"{exception.Message}"));
                        }
                        catch (System.IO.IOException exception)
                        {
                            _ = Log(new LogMessage(LogSeverity.Error, $"Logging", $"{exception.Message}"));
                        }
                    }
                    break;
                default:
                    // TODO: Add some sort of error message if this is hit.
                    break;
            }
            return Task.CompletedTask;
        }
    }
}
