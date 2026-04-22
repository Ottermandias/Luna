using Serilog.Events;

namespace Luna;

/// <summary> The supported log levels. </summary>
public enum LogLevel
{
    /// <summary> Excessive is only logged when <c>EXCESSIVE_LOGGING</c> is defined. Use this to keep logging statements that are not generally useful and too much for even verbose logging. </summary>
    Excessive = LogEventLevel.Verbose,

    /// <summary> Verbose logging should be used for very detailed information but should be refrained from for permanent per-frame log statements. </summary>
    Verbose = LogEventLevel.Verbose,

    /// <summary> Debug logging should be used for user interactions and irregularly occuring events. </summary>
    Debug = LogEventLevel.Debug,

    /// <summary> Information logging should be used for rarely occuring events like plugin load and unload info. </summary>
    Information = LogEventLevel.Information,

    /// <summary> Warning logging should be used whenever unexpected and potentially harmful events occur. </summary>
    Warning = LogEventLevel.Warning,

    /// <summary> Warning logging should be used whenever recoverable errors occur. </summary>
    Error = LogEventLevel.Error,

    /// <summary> Warning logging should be used whenever unrecoverable errors occur. </summary>
    Fatal = LogEventLevel.Fatal,
}

/// <summary> Conversion extensions between the different log level types. </summary>
public static class LogLevelExtensions
{
    extension(Microsoft.Extensions.Logging.LogLevel logLevel)
    {
        /// <summary> Microsoft to Serilog. </summary>
        public LogEventLevel Serilog
            => logLevel switch
            {
                Microsoft.Extensions.Logging.LogLevel.None        => LevelAlias.Off,
                Microsoft.Extensions.Logging.LogLevel.Critical    => LogEventLevel.Fatal,
                Microsoft.Extensions.Logging.LogLevel.Error       => LogEventLevel.Error,
                Microsoft.Extensions.Logging.LogLevel.Warning     => LogEventLevel.Warning,
                Microsoft.Extensions.Logging.LogLevel.Information => LogEventLevel.Information,
                Microsoft.Extensions.Logging.LogLevel.Debug       => LogEventLevel.Debug,
                _                                                 => LogEventLevel.Verbose,
            };

        /// <summary> Microsoft to Luna. </summary>
        public LogLevel Luna
            => logLevel switch
            {
                Microsoft.Extensions.Logging.LogLevel.None        => LogLevel.Excessive,
                Microsoft.Extensions.Logging.LogLevel.Critical    => LogLevel.Fatal,
                Microsoft.Extensions.Logging.LogLevel.Error       => LogLevel.Error,
                Microsoft.Extensions.Logging.LogLevel.Warning     => LogLevel.Warning,
                Microsoft.Extensions.Logging.LogLevel.Information => LogLevel.Information,
                Microsoft.Extensions.Logging.LogLevel.Debug       => LogLevel.Debug,
                _                                                 => LogLevel.Verbose,
            };
    }

    extension(LogLevel logLevel)
    {
        /// <summary> Luna to Serilog. </summary>
        public LogEventLevel Serilog
            => (LogEventLevel)logLevel;

        /// <summary> Luna to Microsoft. </summary>
        public Microsoft.Extensions.Logging.LogLevel Microsoft
            => logLevel switch
            {
                LogLevel.Verbose     => Microsoft.Extensions.Logging.LogLevel.Trace,
                LogLevel.Debug       => Microsoft.Extensions.Logging.LogLevel.Debug,
                LogLevel.Information => Microsoft.Extensions.Logging.LogLevel.Information,
                LogLevel.Warning     => Microsoft.Extensions.Logging.LogLevel.Warning,
                LogLevel.Error       => Microsoft.Extensions.Logging.LogLevel.Error,
                LogLevel.Fatal       => Microsoft.Extensions.Logging.LogLevel.Critical,
                _                    => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null),
            };
    }
}
