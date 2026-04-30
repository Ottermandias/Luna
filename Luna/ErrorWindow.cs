using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Luna;

/// <summary> A window that specifically is used to display failures to load and check for updates. </summary>
public class ErrorWindow : Window, IDisposable
{
    private readonly   ImSharpDalamudContext   _context;
    private readonly   string                  _name;
    private readonly   WindowSystem            _system;
    private readonly   Task<PluginUpdate?>     _updateCheck;
    protected readonly IDalamudPluginInterface PluginInterface;

    /// <summary> Create a new error window that will also create its own window system and subscribe to drawing. </summary>
    /// <param name="pi"> The plugin interface. </param>
    /// <param name="label"> The label for the window itself. </param>
    /// <param name="name"> The name of the plugin and window system. </param>
    public ErrorWindow(IDalamudPluginInterface pi, LunaLogger log, string label, string name)
        : base(label)
    {
        PluginInterface = pi;
        _context = new ImSharpDalamudContext(PluginInterface, PluginInterface.UiBuilder, PluginInterface.GetRequiredService<IFramework>(), log);
        _name = name;
        _system = WindowSystem.Create(pi.UiBuilder, name, false);
        _system.AddWindow(this);
        _updateCheck    = pi.CheckForUpdateAsync();
        IsOpen          = true;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(640, 480) };
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        using (ImGuiColor.Text.Push(LunaStyle.ErrorForeground))
        {
            Im.TextWrapped($"Error initializing {_name}. Further information can be found in /xllog.");
        }

        LunaStyle.DrawSeparator();

        if (!_updateCheck.IsCompleted)
            Im.Text("Checking for Updates..."u8);
        else if (_updateCheck.IsFaulted)
            Im.Text("Failed to check for updates."u8, LunaStyle.ErrorForeground);
        else if (_updateCheck.Result is not { } update)
            Im.Text("No new versions found."u8, LunaStyle.AttentionForeground);
        else
            Im.Text($"A new version v{update.Version} is available{(update.IsTesting ? " on testing." : ".")}",
                LunaStyle.AttentionForeground);
#if DEBUG
        if (Im.Tree.Header("Debug Utilities"u8))
            DrawDebugUtilities();
#endif
    }

    /// <summary> Draw additional debug utilities in the error window only when compiled in debug mode.</summary>
    protected virtual void DrawDebugUtilities()
    { }

    /// <inheritdoc/>
    public void Dispose()
    {
        _system.Dispose();
        _updateCheck.Dispose();
        _context.Dispose();
    }
}
