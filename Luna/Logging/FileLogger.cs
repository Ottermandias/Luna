using Microsoft.Extensions.Logging;
using Serilog;

namespace Luna;

/// <inheritdoc cref="FileLogger"/>
/// <typeparam name="T"> Unused type parameter for dependency injection. </typeparam>
public class FileLogger<T> : FileLogger, ILogger<T>
{
    /// <inheritdoc cref="FileLogger"/>
    /// <typeparam name="T"> Unused type parameter for dependency injection. </typeparam>
    public FileLogger(string filePath, LogLevel level)
        : base(filePath, level)
    { }

    /// <summary> Create a new logger sharing an existing logger. </summary>
    internal FileLogger(string filePath, LogLevel level, Serilog.ILogger logger)
        : base(filePath, level, logger)
    { }
}

/// <summary> A custom file logger that does not use the regular plugin log and stays on a specific log level. </summary>
public class FileLogger : LunaLogger
{
    /// <summary> Get a typed version of this logger. </summary>
    /// <typeparam name="T"> The unused type parameter. </typeparam>
    public virtual FileLogger<T> Typed<T>()
        => new(FilePath, Level, _logger);

    /// <summary> The constant minimum log level for this logger. </summary>
    public readonly LogLevel Level;

    /// <summary> The path to the logger's file. </summary>
    public readonly string FilePath;

    /// <inheritdoc/>
    public sealed override Serilog.ILogger Logger
        => _logger;

    private readonly Serilog.ILogger _logger;

    /// <summary> Create a new logger based on the given file path and level. </summary>
    /// <param name="filePath"> The path to the file to write to. </param>
    /// <param name="level"> The log level the logger accepts. </param>
    public FileLogger(string filePath, LogLevel level)
    {
        Level    = level;
        FilePath = filePath;
        _logger = new LoggerConfiguration()
            .MinimumLevel.Is(level.Serilog)
            .WriteTo.File(filePath, level.Serilog, shared: true)
            .CreateLogger();
    }

    /// <summary> Create a new logger sharing an existing logger. </summary>
    protected FileLogger(string filePath, LogLevel level, Serilog.ILogger logger)
    {
        Level    = level;
        FilePath = filePath;
        _logger  = logger;
    }
}
