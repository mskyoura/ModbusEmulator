using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace ModbusEmulator;

public class CustomConsoleFormatter : ConsoleFormatter
{
    public CustomConsoleFormatter(IOptionsMonitor<ConsoleFormatterOptions> options) : base("CustomFormatter")
    {
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logLevel = GetLogLevelString(logEntry.LogLevel);
        var category = logEntry.Category;
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        
        textWriter.WriteLine($"[{timestamp}] [{logLevel}] [{category}] {message}");
        
        if (logEntry.Exception != null)
        {
            textWriter.WriteLine($"[{timestamp}] [{logLevel}] [{category}] Exception: {logEntry.Exception}");
        }
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO ",
            LogLevel.Warning => "WARN ",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT ",
            _ => "UNKN "
        };
    }
}
