using Dalamud.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable InconsistentNaming

namespace Luna;

/// <summary> Wrapper for handling the window system. </summary>
public sealed partial class WindowSystem : Dalamud.Interface.Windowing.WindowSystem, IUiService, IDisposable
{
    /// <summary> Rate limit the warnings. </summary>
    private DateTime _limitStart = DateTime.UnixEpoch;

    private DateTime _limitColor = DateTime.UnixEpoch;
    private DateTime _limitStyle = DateTime.UnixEpoch;
    private int      _colorStackMax;
    private int      _styleStackMax;

    /// <summary> The size of the color stack at the start of drawing this window system. </summary>
    public int ColorStackStart { get; private set; }

    /// <summary> The size of the style stack at the start of drawing this window system. </summary>
    public int StyleStackStart { get; private set; }

    /// <summary> The UI Builder this window system is registered with. </summary>
    public readonly IUiBuilder UiBuilder;

    /// <inheritdoc/>
    private WindowSystem(IUiBuilder uiBuilder, string name)
        : base(name)
    {
        UiBuilder      =  uiBuilder;
        UiBuilder.Draw += Draw;
    }

    /// <summary> Create a factory for a window system with the given name. </summary>
    /// <param name="name"> The name. </param>
    /// <returns> A factory function. </returns>
    public static Func<IServiceProvider, WindowSystem> Factory(string name)
        => p => new WindowSystem(p.GetRequiredService<IUiBuilder>(), name);

    /// <returns> A factory function. </returns>
    public static WindowSystem Create(IUiBuilder builder, string name)
        => new(builder, name);

    /// <inheritdoc/>
    public void Dispose()
        => UiBuilder.Draw -= Draw;

    /// <summary> Draw only if our context has been initialized. </summary>
    private new void Draw()
    {
        if (!ImSharpConfiguration.IsInitialized)
            return;

        ReadStacks();
        base.Draw();
        CheckStacks();
    }

    private void ReadStacks()
    {
        ColorStackStart = Im.Context.ColorStackSize;
        StyleStackStart = Im.Context.StyleStackSize;

        if (DateTime.UtcNow <= _limitStart)
            return;

        if (ColorStackStart <= _colorStackMax && StyleStackStart <= _styleStackMax)
            return;

        _colorStackMax = Math.Max(_colorStackMax, ColorStackStart);
        _styleStackMax = Math.Max(_styleStackMax, StyleStackStart);
        _limitStart   = DateTime.UtcNow.AddMinutes(1);
        LogStartStacks(ImSharpConfiguration.Logger, GetType().Name, ColorStackStart, StyleStackStart);
    }

    private void CheckStacks()
    {
        if (DateTime.UtcNow > _limitColor && ColorStackStart != Im.Context.ColorStackSize)
        {
            _limitColor = DateTime.UtcNow.AddSeconds(5);
            if (ColorStackStart > Im.Context.ColorStackSize)
                LogPoppedColors(ImSharpConfiguration.Logger, GetType().Name, ColorStackStart - Im.Context.ColorStackSize);
            else
                LogPushedColors(ImSharpConfiguration.Logger, GetType().Name, Im.Context.ColorStackSize - ColorStackStart);
        }

        if (DateTime.UtcNow > _limitStyle && StyleStackStart != Im.Context.StyleStackSize)
        {
            _limitStyle = DateTime.UtcNow.AddSeconds(5);
            if (StyleStackStart > Im.Context.StyleStackSize)
                LogPoppedStyles(ImSharpConfiguration.Logger, GetType().Name, StyleStackStart - Im.Context.StyleStackSize);
            else
                LogPushedStyles(ImSharpConfiguration.Logger, GetType().Name, Im.Context.StyleStackSize - StyleStackStart);
        }
    }

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Warning,
        "Starting {Type:l}.Draw with color stack size {ColorStackSize} and style stack size {StyleStackSize}.")]
    static partial void LogStartStacks(ILogger logger, string Type, int ColorStackSize, int StyleStackSize);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Error,
        "{Type:l}.Draw popped {Difference} more colors from the stack than it pushed to it.")]
    static partial void LogPoppedColors(ILogger logger, string Type, int Difference);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Error,
        "{Type:l}.Draw pushed {Difference} more colors to the stack than it popped from it.")]
    static partial void LogPushedColors(ILogger logger, string Type, int Difference);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Error,
        "{Type:l}.Draw popped {Difference} more styles from the stack than it pushed to it.")]
    static partial void LogPoppedStyles(ILogger logger, string Type, int Difference);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Error,
        "{Type:l}.Draw pushed {Difference} more styles to the stack than it popped from it.")]
    static partial void LogPushedStyles(ILogger logger, string Type, int Difference);
}
