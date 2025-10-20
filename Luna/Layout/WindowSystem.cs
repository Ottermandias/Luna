using Dalamud.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace Luna;

/// <summary> Wrapper for handling the window system. </summary>
public sealed class WindowSystem : Dalamud.Interface.Windowing.WindowSystem, IUiService, IDisposable
{
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

    /// <inheritdoc/>
    public void Dispose()
        => UiBuilder.Draw -= Draw;
}
