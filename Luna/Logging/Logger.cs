using Microsoft.Extensions.Logging;
using Serilog.Events;

// ReSharper disable MethodOverloadWithOptionalParameter

namespace Luna;

/// <summary> Get the Serilog log, convert it to a Microsoft ILogger and set a plugin specific prefix. </summary>
/// <typeparam name="T"> Unused type for dependency injection. </typeparam>
public class Logger<T> : ILogger<T>, Serilog.ILogger
{
    /// <summary> Get the loggers prefix. </summary>
    public string GlobalPrefix
        => Logger.GlobalPrefix;

    /// <summary> Get the name of the plugin. </summary>
    public string GlobalPluginName
        => Logger.GlobalPluginName;

    /// <summary> Get the Serilog logger provided by Dalamud. </summary>
    public Serilog.ILogger MainLogger
        => Logger.GlobalPluginLogger;

    /// <summary> Create a new instance by getting the assembly name as a plugin name and fetching Dalamuds context. </summary>
    public Logger(string? pluginName = null)
    {
        // The statics are null!-initialized.
        Logger.GlobalPluginName   ??= pluginName ?? Assembly.GetCallingAssembly().GetName().Name ?? "Unknown";
        Logger.GlobalPluginLogger ??= Serilog.Log.ForContext("Dalamud.PluginName", Logger.GlobalPluginName);
        Logger.GlobalPrefix       ??= $"[{Logger.GlobalPluginName}] ";
    }

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

    /// <summary> Write a string message if the logger fulfills the log level. </summary>
    /// <param name="level"> The minimum level the logger must have enabled. </param>
    /// <param name="text"> The message. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Message(LogLevel level, string text)
        => Message(level, $"{text}");

    /// <summary> Write an optimized string message if the logger fulfills the log level but do not interpolate the string if it does not. </summary>
    /// <param name="level"> The minimum level the logger must have enabled. </param>
    /// <param name="text"> The message. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Message(LogLevel level, [InterpolatedStringHandlerArgument("", "level")] ref LogLevelInterpolatedStringHandler text)
    {
        if (Serilog.Log.IsEnabled((LogEventLevel)level))
            Serilog.Log.Write((LogEventLevel)level, text.GetFormattedText());
    }


    /// <summary> Write a string message if the logger fulfills <see cref="LogLevel.Fatal"/>. </summary>
    /// <param name="text"> The message. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fatal(string text)
        => Fatal($"{text}");

    /// <summary> Write an optimized string message if the logger fulfills <see cref="LogLevel.Fatal"/> but do not interpolate the string if it does not. </summary>
    /// <param name="text"> The message. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fatal([InterpolatedStringHandlerArgument("")] FatalInterpolatedStringHandler text)
    {
        if (MainLogger.IsEnabled(LogEventLevel.Fatal))
            MainLogger.Fatal(text.GetFormattedText());
    }

    /// <summary> Write a string message if the logger fulfills <see cref="LogLevel.Error"/>. </summary>
    /// <param name="text"> The message. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Error(string text)
        => Error($"{text}");

    /// <summary> Write an optimized string message if the logger fulfills <see cref="LogLevel.Error"/> but do not interpolate the string if it does not. </summary>
    /// <param name="text"> The message. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Error([InterpolatedStringHandlerArgument("")] ErrorInterpolatedStringHandler text)
    {
        if (MainLogger.IsEnabled(LogEventLevel.Error))
            MainLogger.Error(text.GetFormattedText());
    }

    /// <summary> Write a string message if the logger fulfills <see cref="LogLevel.Warning"/>. </summary>
    /// <param name="text"> The message. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Warning(string text)
        => Warning($"{text}");

    /// <summary> Write an optimized string message if the logger fulfills <see cref="LogLevel.Warning"/> but do not interpolate the string if it does not. </summary>
    /// <param name="text"> The message. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Warning([InterpolatedStringHandlerArgument("")] WarningInterpolatedStringHandler text)
    {
        if (MainLogger.IsEnabled(LogEventLevel.Warning))
            MainLogger.Warning(text.GetFormattedText());
    }

    /// <summary> Write a string message if the logger fulfills <see cref="LogLevel.Information"/>. </summary>
    /// <param name="text"> The message. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Information(string text)
        => Information($"{text}");

    /// <summary> Write an optimized string message if the logger fulfills <see cref="LogLevel.Information"/> but do not interpolate the string if it does not. </summary>
    /// <param name="text"> The message. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Information([InterpolatedStringHandlerArgument("")] InformationInterpolatedStringHandler text)
    {
        if (MainLogger.IsEnabled(LogEventLevel.Information))
            MainLogger.Information(text.GetFormattedText());
    }

    /// <summary> Write a string message if the logger fulfills <see cref="LogLevel.Debug"/>. </summary>
    /// <param name="text"> The message. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Debug(string text)
        => Debug($"{text}");

    /// <summary> Write an optimized string message if the logger fulfills <see cref="LogLevel.Debug"/> but do not interpolate the string if it does not. </summary>
    /// <param name="text"> The message. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Debug([InterpolatedStringHandlerArgument("")] DebugInterpolatedStringHandler text)
    {
        if (MainLogger.IsEnabled(LogEventLevel.Debug))
            MainLogger.Debug(text.GetFormattedText());
    }

    /// <summary> Write a string message if the logger fulfills <see cref="LogLevel.Verbose"/>. </summary>
    /// <param name="text"> The message. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Verbose(string text)
        => Verbose($"{text}");

    /// <summary> Write a structured string message if the logger fulfills <see cref="LogLevel.Verbose"/>. </summary>
    /// <param name="format"> The structured message format. </param>
    /// <param name="args"> The additional parameters for the format. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Verbose(string format, params object?[]? args)
        => MainLogger.Verbose(GlobalPrefix + format, args);

    /// <summary> Write an optimized string message if the logger fulfills <see cref="LogLevel.Verbose"/> but do not interpolate the string if it does not. </summary>
    /// <param name="text"> The message. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Verbose([InterpolatedStringHandlerArgument("")] VerboseInterpolatedStringHandler text)
    {
        if (MainLogger.IsEnabled(LogEventLevel.Verbose))
            MainLogger.Verbose(text.GetFormattedText());
    }

    [Conditional("EXCESSIVE_LOGGING")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Excessive(string text)
        => Verbose($"{text}");

    [Conditional("EXCESSIVE_LOGGING")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Excessive(string format, params object?[] args)
        => MainLogger.Verbose(GlobalPrefix + format, args);


    [Conditional("EXCESSIVE_LOGGING")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Excessive([InterpolatedStringHandlerArgument("")] VerboseInterpolatedStringHandler builder)
    {
        if (MainLogger.IsEnabled(LogEventLevel.Verbose))
            MainLogger.Verbose(builder.GetFormattedText());
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(LogEvent logEvent)
        => MainLogger.Write(logEvent);

    private static readonly ConcurrentDictionary<string, string> DestructureDictionary = [];
    private static readonly ConcurrentDictionary<string, string> StringifyDictionary   = [];
    private static readonly CachingMessageTemplateParser         MessageTemplateParser = new();

    private readonly EventIdPropertyCache _eventIdPropertyCache = new();


    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (logLevel is Microsoft.Extensions.Logging.LogLevel.None)
            return;

        var level = ToSerilogLevel(logLevel);
        if (!MainLogger.IsEnabled(level))
            return;

        LogEvent? @event = null;
        try
        {
            @event = PrepareWrite(level, eventId, state, exception, formatter);
        }
        catch (Exception ex)
        {
            MainLogger.Error(ex, "Failed to write event: {Exception}", ex);
        }

        if (@event is not null)
            MainLogger.Write(@event);
    }

    private LogEvent PrepareWrite<TState>(LogEventLevel level, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        string? messageTemplate = null;

        var properties = new Dictionary<string, LogEventPropertyValue>();

        if (state is IEnumerable<KeyValuePair<string, object?>> structure)
        {
            foreach (var property in structure)
            {
                if (property is { Key: "{OriginalFormat}", Value: string value })
                {
                    messageTemplate = value;
                }
                else if (property.Key.StartsWith('@'))
                {
                    if (MainLogger.BindProperty(GetKeyWithoutFirstSymbol(DestructureDictionary, property.Key), property.Value, true,
                            out var destructured))
                        properties[destructured.Name] = destructured.Value;
                }
                else if (property.Key.StartsWith('$'))
                {
                    if (MainLogger.BindProperty(GetKeyWithoutFirstSymbol(StringifyDictionary, property.Key), property.Value?.ToString(),
                            true, out var stringified))
                        properties[stringified.Name] = stringified.Value;
                }
                else
                {
                    // Simple micro-optimization for the most common and reliably scalar values; could go further here.
                    if (property.Value is null or string or int or long && LogEventProperty.IsValidName(property.Key))
                        properties[property.Key] = new ScalarValue(property.Value);
                    else if (MainLogger.BindProperty(property.Key, property.Value, false, out var bound))
                        properties[bound.Name] = bound.Value;
                }
            }

            var stateType     = state.GetType();
            var stateTypeInfo = stateType.GetTypeInfo();
            // Imperfect, but at least eliminates `1 names
            if (messageTemplate is null && !stateTypeInfo.IsGenericType)
            {
                messageTemplate = $"{{{stateType.Name}:l}}";
                if (MainLogger.BindProperty(stateType.Name, AsLoggableValue(state, formatter), false, out var stateTypeProperty))
                    properties[stateTypeProperty.Name] = stateTypeProperty.Value;
            }
        }

        if (messageTemplate is null)
        {
            string? propertyName = null;
            if (state != null)
            {
                propertyName    = "State";
                messageTemplate = "{State:l}";
            }
            // `formatter` was originally accepted as nullable, so despite the new annotation, this check should still
            // be made.
            else if (formatter != null!)
            {
                propertyName    = "Message";
                messageTemplate = "{Message:l}";
            }

            if (propertyName is not null)
                if (MainLogger.BindProperty(propertyName, AsLoggableValue(state, formatter!), false, out var property))
                    properties[property.Name] = property.Value;
        }

        // The overridden `!=` operator on this type ignores `Name`.
        if (eventId.Id is not 0 || eventId.Name is not null)
            properties["EventId"] = _eventIdPropertyCache.GetOrCreatePropertyValue(in eventId);

        var (traceId, spanId) = Activity.Current is { } activity
            ? (activity.TraceId, activity.SpanId)
            : (default(ActivityTraceId), default(ActivitySpanId));

        if (messageTemplate is not null)
            messageTemplate = GlobalPrefix + messageTemplate;

        var parsedTemplate = messageTemplate != null ? MessageTemplateParser.Parse(messageTemplate) : MessageTemplate.Empty;
        return LogEvent.UnstableAssembleFromParts(DateTimeOffset.Now, level, exception, parsedTemplate, properties, traceId, spanId);
    }

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        => MainLogger.IsEnabled(ToSerilogLevel(logLevel));

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => null;

    public static LogEventLevel ToSerilogLevel(Microsoft.Extensions.Logging.LogLevel logLevel)
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

    private static object? AsLoggableValue<TState>(TState state, Func<TState, Exception?, string>? formatter)
    {
        object? stateObj = null;
        if (formatter != null)
            stateObj = formatter(state, null);
        return stateObj ?? state;
    }

    private static string GetKeyWithoutFirstSymbol(ConcurrentDictionary<string, string> source, string key)
    {
        if (source.TryGetValue(key, out var value))
            return value;

        return source.Count < 1000 ? source.GetOrAdd(key, k => k[1..]) : key[1..];
    }


    [InterpolatedStringHandler]
    public ref struct LogLevelInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _builder;

        public LogLevelInterpolatedStringHandler(int literalLength, int formattedCount, Logger<T> logger, LogLevel level, out bool isEnabled)
        {
            isEnabled = logger.MainLogger.IsEnabled((LogEventLevel)level);
            if (isEnabled)
            {
                _builder = new DefaultInterpolatedStringHandler(literalLength + logger.GlobalPluginName.Length + 3, formattedCount);
                _builder.AppendLiteral(logger.GlobalPrefix);
            }
            else
            {
                _builder = default;
            }
        }

        public void AppendLiteral(string s)
            => _builder.AppendLiteral(s);

        public void AppendFormatted(scoped ReadOnlySpan<byte> utf8Value)
            => _builder.AppendLiteral(Encoding.UTF8.GetString(utf8Value));

        public void AppendFormatted(scoped ReadOnlySpan<byte> utf8Value, int alignment = 0, string? format = null)
            => _builder.AppendFormatted(Encoding.UTF8.GetString(utf8Value), alignment, format);

        public void AppendFormatted<TValue>(TValue t)
            => _builder.AppendFormatted(t);

        public void AppendFormatted<TValue>(TValue t, string format) where TValue : IFormattable
            => _builder.AppendFormatted(t, format);

        public void AppendFormatted<TValue>(TValue t, int alignment) where TValue : IFormattable
            => _builder.AppendFormatted(t, alignment);

        public void AppendFormatted<TValue>(TValue t, int alignment, string format) where TValue : IFormattable
            => _builder.AppendFormatted(t, alignment, format);

        internal string GetFormattedText()
            => _builder.ToStringAndClear();
    }

    [InterpolatedStringHandler]
    public ref struct FatalInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _builder;

        public FatalInterpolatedStringHandler(int literalLength, int formattedCount, Logger<T> logger, out bool isEnabled)
        {
            isEnabled = logger.MainLogger.IsEnabled(LogEventLevel.Fatal);
            if (isEnabled)
            {
                _builder = new DefaultInterpolatedStringHandler(literalLength + logger.GlobalPluginName.Length + 3, formattedCount);
                _builder.AppendLiteral(logger.GlobalPrefix);
            }
            else
            {
                _builder = default;
            }
        }

        public void AppendLiteral(string s)
            => _builder.AppendLiteral(s);

        public void AppendLiteral(ReadOnlySpan<byte> s)
            => _builder.AppendLiteral(Encoding.UTF8.GetString(s));

        public void AppendFormatted<TValue>(TValue t)
            => _builder.AppendFormatted(t);

        public void AppendFormatted<TValue>(TValue t, string format) where TValue : IFormattable
            => _builder.AppendFormatted(t, format);

        public void AppendFormatted<TValue>(TValue t, int alignment) where TValue : IFormattable
            => _builder.AppendFormatted(t, alignment);

        public void AppendFormatted<TValue>(TValue t, int alignment, string format) where TValue : IFormattable
            => _builder.AppendFormatted(t, alignment, format);

        internal string GetFormattedText()
            => _builder.ToStringAndClear();
    }

    [InterpolatedStringHandler]
    public ref struct ErrorInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _builder;

        public ErrorInterpolatedStringHandler(int literalLength, int formattedCount, Logger<T> logger, out bool isEnabled)
        {
            isEnabled = logger.MainLogger.IsEnabled(LogEventLevel.Error);
            if (isEnabled)
            {
                _builder = new DefaultInterpolatedStringHandler(literalLength + logger.GlobalPluginName.Length + 3, formattedCount);
                _builder.AppendLiteral(logger.GlobalPrefix);
            }
            else
            {
                _builder = default;
            }
        }

        public void AppendLiteral(string s)
            => _builder.AppendLiteral(s);

        public void AppendFormatted(scoped ReadOnlySpan<byte> utf8Value)
            => _builder.AppendLiteral(Encoding.UTF8.GetString(utf8Value));

        public void AppendFormatted(scoped ReadOnlySpan<byte> utf8Value, int alignment = 0, string? format = null)
            => _builder.AppendFormatted(Encoding.UTF8.GetString(utf8Value), alignment, format);

        public void AppendFormatted<TValue>(TValue t)
            => _builder.AppendFormatted(t);

        public void AppendFormatted<TValue>(TValue t, string format) where TValue : IFormattable
            => _builder.AppendFormatted(t, format);

        public void AppendFormatted<TValue>(TValue t, int alignment) where TValue : IFormattable
            => _builder.AppendFormatted(t, alignment);

        public void AppendFormatted<TValue>(TValue t, int alignment, string format) where TValue : IFormattable
            => _builder.AppendFormatted(t, alignment, format);

        internal string GetFormattedText()
            => _builder.ToStringAndClear();
    }

    [InterpolatedStringHandler]
    public ref struct WarningInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _builder;

        public WarningInterpolatedStringHandler(int literalLength, int formattedCount, Logger<T> logger, out bool isEnabled)
        {
            isEnabled = logger.MainLogger.IsEnabled(LogEventLevel.Warning);
            if (isEnabled)
            {
                _builder = new DefaultInterpolatedStringHandler(literalLength + logger.GlobalPluginName.Length + 3, formattedCount);
                _builder.AppendLiteral(logger.GlobalPrefix);
            }
            else
            {
                _builder = default;
            }
        }

        public void AppendLiteral(string s)
            => _builder.AppendLiteral(s);

        public void AppendFormatted(scoped ReadOnlySpan<byte> utf8Value)
            => _builder.AppendLiteral(Encoding.UTF8.GetString(utf8Value));

        public void AppendFormatted(scoped ReadOnlySpan<byte> utf8Value, int alignment = 0, string? format = null)
            => _builder.AppendFormatted(Encoding.UTF8.GetString(utf8Value), alignment, format);

        public void AppendFormatted<TValue>(TValue t)
            => _builder.AppendFormatted(t);

        public void AppendFormatted<TValue>(TValue t, string format) where TValue : IFormattable
            => _builder.AppendFormatted(t, format);

        public void AppendFormatted<TValue>(TValue t, int alignment) where TValue : IFormattable
            => _builder.AppendFormatted(t, alignment);

        public void AppendFormatted<TValue>(TValue t, int alignment, string format) where TValue : IFormattable
            => _builder.AppendFormatted(t, alignment, format);

        internal string GetFormattedText()
            => _builder.ToStringAndClear();
    }

    [InterpolatedStringHandler]
    public ref struct InformationInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _builder;

        public InformationInterpolatedStringHandler(int literalLength, int formattedCount, Logger<T> logger, out bool isEnabled)
        {
            isEnabled = logger.MainLogger.IsEnabled(LogEventLevel.Information);
            if (isEnabled)
            {
                _builder = new DefaultInterpolatedStringHandler(literalLength + logger.GlobalPluginName.Length + 3, formattedCount);
                _builder.AppendLiteral(logger.GlobalPrefix);
            }
            else
            {
                _builder = default;
            }
        }

        public void AppendLiteral(string s)
            => _builder.AppendLiteral(s);

        public void AppendFormatted(scoped ReadOnlySpan<byte> utf8Value)
            => _builder.AppendLiteral(Encoding.UTF8.GetString(utf8Value));

        public void AppendFormatted(scoped ReadOnlySpan<byte> utf8Value, int alignment = 0, string? format = null)
            => _builder.AppendFormatted(Encoding.UTF8.GetString(utf8Value), alignment, format);

        public void AppendFormatted<TValue>(TValue t)
            => _builder.AppendFormatted(t);

        public void AppendFormatted<TValue>(TValue t, string format) where TValue : IFormattable
            => _builder.AppendFormatted(t, format);

        public void AppendFormatted<TValue>(TValue t, int alignment) where TValue : IFormattable
            => _builder.AppendFormatted(t, alignment);

        public void AppendFormatted<TValue>(TValue t, int alignment, string format) where TValue : IFormattable
            => _builder.AppendFormatted(t, alignment, format);

        internal string GetFormattedText()
            => _builder.ToStringAndClear();
    }

    [InterpolatedStringHandler]
    public ref struct DebugInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _builder;

        public DebugInterpolatedStringHandler(int literalLength, int formattedCount, Logger<T> logger, out bool isEnabled)
        {
            isEnabled = logger.MainLogger.IsEnabled(LogEventLevel.Debug);
            if (isEnabled)
            {
                _builder = new DefaultInterpolatedStringHandler(literalLength + logger.GlobalPluginName.Length + 3, formattedCount);
                _builder.AppendLiteral(logger.GlobalPrefix);
            }
            else
            {
                _builder = default;
            }
        }

        public void AppendLiteral(string s)
            => _builder.AppendLiteral(s);

        public void AppendFormatted(scoped ReadOnlySpan<byte> utf8Value)
            => _builder.AppendLiteral(Encoding.UTF8.GetString(utf8Value));

        public void AppendFormatted(scoped ReadOnlySpan<byte> utf8Value, int alignment = 0, string? format = null)
            => _builder.AppendFormatted(Encoding.UTF8.GetString(utf8Value), alignment, format);

        public void AppendFormatted<TValue>(TValue t)
            => _builder.AppendFormatted(t);

        public void AppendFormatted<TValue>(TValue t, string format) where TValue : IFormattable
            => _builder.AppendFormatted(t, format);

        public void AppendFormatted<TValue>(TValue t, int alignment) where TValue : IFormattable
            => _builder.AppendFormatted(t, alignment);

        public void AppendFormatted<TValue>(TValue t, int alignment, string format) where TValue : IFormattable
            => _builder.AppendFormatted(t, alignment, format);

        internal string GetFormattedText()
            => _builder.ToStringAndClear();
    }

    [InterpolatedStringHandler]
    public ref struct VerboseInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _builder;

        public VerboseInterpolatedStringHandler(int literalLength, int formattedCount, Logger<T> logger, out bool isEnabled)
        {
            isEnabled = logger.MainLogger.IsEnabled(LogEventLevel.Verbose);
            if (isEnabled)
            {
                _builder = new DefaultInterpolatedStringHandler(literalLength + logger.GlobalPluginName.Length + 3, formattedCount);
                _builder.AppendLiteral(logger.GlobalPrefix);
            }
            else
            {
                _builder = default;
            }
        }

        public void AppendLiteral(string s)
            => _builder.AppendLiteral(s);

        public void AppendFormatted(scoped ReadOnlySpan<byte> utf8Value)
            => _builder.AppendLiteral(Encoding.UTF8.GetString(utf8Value));

        public void AppendFormatted(scoped ReadOnlySpan<byte> utf8Value, int alignment = 0, string? format = null)
            => _builder.AppendFormatted(Encoding.UTF8.GetString(utf8Value), alignment, format);

        public void AppendFormatted<TValue>(TValue t)
            => _builder.AppendFormatted(t);

        public void AppendFormatted<TValue>(TValue t, string format) where TValue : IFormattable
            => _builder.AppendFormatted(t, format);

        public void AppendFormatted<TValue>(TValue t, int alignment) where TValue : IFormattable
            => _builder.AppendFormatted(t, alignment);

        public void AppendFormatted<TValue>(TValue t, int alignment, string format) where TValue : IFormattable
            => _builder.AppendFormatted(t, alignment, format);

        internal string GetFormattedText()
            => _builder.ToStringAndClear();
    }
}

/// <summary> An untyped version of <see cref="Logger{T}"/> for when no type is necessary. </summary>
public sealed class Logger(string? pluginName = null) : Logger<object>(pluginName)
{
    // Keep loggers footprint a bit smaller by keeping those fields static. </summary>
    public new static string          GlobalPluginName   = null!;
    public new static string          GlobalPrefix       = null!;
    public static     Serilog.ILogger GlobalPluginLogger = null!;
}
