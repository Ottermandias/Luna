using Dalamud.Interface;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;

namespace Luna;

/// <summary> A helper class for sharing a single <see cref="ImSharpContext"/> across multiple instances of ImSharp or Luna through Dalamud IPC. </summary>
public sealed unsafe class ImSharpDalamudContext : IRequiredService, IDisposable
{
    private readonly string                  _contextTag;
    private readonly IUiBuilder              _uiBuilder;
    private readonly IDalamudPluginInterface _pluginInterface;

    /// <summary>
    ///   Creates an <see cref="ImSharpContext"/> and shares it through Dalamud's shared data store. <br/>
    ///   Sets up the logger for this instance of Luna. <br/>
    ///   Sets up <see cref="ImSharpPerFrame.OnUpdate"/> to be called on Dalamud's Draw.
    /// </summary>
    /// <param name="pluginInterface"> The plugin interface for the shared data store. </param>
    /// <param name="uiBuilder"> The uiBuilder to set up the <see cref="ImSharpPerFrame.OnUpdate"/> and fetch the <see cref="IUiBuilder.FontMono"/> when it is ready. </param>
    /// <param name="framework"> The framework to ensure the <see cref="IUiBuilder.FontMono"/> is fetched from the main thread. </param>
    /// <param name="logger"> The logger to set up for ImSharp. </param>
    /// <param name="services"> The service provider for the cache manager. </param>
    public ImSharpDalamudContext(IDalamudPluginInterface pluginInterface, IUiBuilder uiBuilder, IFramework framework, ILogger logger,
        IServiceProvider services)
    {
        _contextTag      = $"ImSharp.Context.V{ImSharpContext.CurrentVersion}";
        _pluginInterface = pluginInterface;
        _uiBuilder       = uiBuilder;

        ImSharpConfiguration.SetLogger(logger);

        // Set up the default cache manager.
        CacheManager.Instance.ServiceProvider =  services;
        _uiBuilder.DefaultFontChanged         += OnDefaultFontChanged;
        _uiBuilder.DefaultGlobalScaleChanged  += OnDefaultGlobalScaleChanged;
        _uiBuilder.DefaultStyleChanged        += OnDefaultStyleChanged;

        _uiBuilder.Draw += ImSharpPerFrame.OnUpdate;
        var created = false;
        var holder = pluginInterface.GetOrCreateData(_contextTag, IReadOnlyList<nint> () =>
        {
            created = true;
            return new ContextHolder(uiBuilder, framework);
        });
        var context = (ImSharpContext*)holder[0];
        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if (created)
            logger.LogDebug("Created new ImSharp Context at 0x{Context:X} with tag {Tag:l}.", (nint)context, _contextTag);
        else
            logger.LogDebug("Shared existing ImSharp Context at 0x{Context:X} with tag {Tag:l}.", (nint)context, _contextTag);
        ImSharpConfiguration.SetContext(context);
    }

    /// <summary> Clear all ImSharp configuration data and relinquish the context from Dalamud's data store.  </summary>
    public void Dispose()
    {
        _uiBuilder.DefaultFontChanged        -= OnDefaultFontChanged;
        _uiBuilder.DefaultGlobalScaleChanged -= OnDefaultGlobalScaleChanged;
        _uiBuilder.DefaultStyleChanged       -= OnDefaultStyleChanged;
        ImSharpConfiguration.SetContext(null);
        ImSharpConfiguration.SetLogger(null);
        _pluginInterface.RelinquishData(_contextTag);
        _uiBuilder.Draw -= ImSharpPerFrame.OnUpdate;
    }

    /// <summary> Draw debug information about the currently configured <see cref="ImSharpContext"/>. </summary>
    /// <param name="dynamisIpc"> If this is set, draw pointers using Dynamis. </param>
    public void DrawDebugInfo(DynamisIpc? dynamisIpc = null)
    {
        using var id    = Im.Id.Push("ImSharpContext"u8);
        using var table = Im.Table.Begin("table"u8, 2, TableFlags.SizingFixedFit);
        if (!table)
            return;

        table.DrawColumn("Library Version"u8);
        table.DrawColumn($"{ImSharpContext.CurrentVersion}");

        table.DrawColumn("Tag"u8);
        table.DrawColumn(_contextTag);

        table.DrawColumn("ImSharp Context"u8);
        table.NextColumn();
        dynamisIpc.DrawPointerChecked(ImSharpConfiguration.Context);
        if (ImSharpConfiguration.Context is null)
            return;

        table.DrawColumn("Version"u8);
        table.DrawColumn($"{ImSharpConfiguration.Context->Version}");

        table.DrawColumn("Text Buffer"u8);
        table.NextColumn();
        dynamisIpc.DrawPointerChecked(ImSharpConfiguration.Context->TextBuffer);
        table.DrawColumn("Text Buffer Size"u8);
        table.DrawColumn($"{ImSharpConfiguration.Context->TextBufferSize}");

        table.DrawColumn("Label Buffer"u8);
        table.NextColumn();
        dynamisIpc.DrawPointerChecked(ImSharpConfiguration.Context->LabelBuffer);
        table.DrawColumn("Label Buffer Size"u8);
        table.DrawColumn($"{ImSharpConfiguration.Context->LabelBufferSize}");

        table.DrawColumn("Hint Buffer"u8);
        table.NextColumn();
        dynamisIpc.DrawPointerChecked(ImSharpConfiguration.Context->HintBuffer);
        table.DrawColumn("Hint Buffer Size"u8);
        table.DrawColumn($"{ImSharpConfiguration.Context->HintBufferSize}");

        table.DrawColumn("Input Buffer"u8);
        table.NextColumn();
        dynamisIpc.DrawPointerChecked(ImSharpConfiguration.Context->InputBuffer);
        table.DrawColumn("Input Buffer Size"u8);
        table.DrawColumn($"{ImSharpConfiguration.Context->InputBufferSize}");

        table.DrawColumn("ImGui Context"u8);
        table.NextColumn();
        dynamisIpc.DrawPointerChecked(ImSharpConfiguration.Context->ImGuiContext);

        table.DrawColumn("MonoFont"u8);
        table.NextColumn();
        dynamisIpc.DrawPointerChecked(ImSharpConfiguration.Context->MonoFont);

        table.DrawColumn("DefaultFont"u8);
        table.NextColumn();
        dynamisIpc.DrawPointerChecked(ImSharpConfiguration.Context->DefaultFont);
    }

    /// <summary> A utility class that can be shared through Dalamud's data store. </summary>
    /// <remarks>
    ///   We need to hack here a little bit because the data store can only share known types and only reference-types.
    ///   So this is IDisposable so that Dalamud tears down the context when all consumers relinquish their claim.
    ///   And it is a list of generic pointers so we can access the single element we need which is otherwise of unknown type, and cast it.
    /// </remarks>
    private sealed class ContextHolder : IDisposable, IReadOnlyList<nint>
    {
        private nint _context = (nint)ImSharpContext.SetupDefault();

        public ContextHolder(IUiBuilder uiBuilder, IFramework framework)
        {
            uiBuilder.WaitForUi().ContinueWith(_ => framework.RunOnFrameworkThread(() =>
            {
                ((ImSharpContext*)_context)->MonoFont    = uiBuilder.FontMono.Handle;
                ((ImSharpContext*)_context)->DefaultFont = uiBuilder.FontDefault.Handle;
            }).Wait());
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (_context == nint.Zero)
                return;

            ImSharpContext.TearDownDefault((ImSharpContext*)_context);
            ImSharpConfiguration.Logger.LogDebug("Teared down ImSharp context at 0x{Context:X}.", _context);
            _context = nint.Zero;
        }

        ~ContextHolder()
            => Dispose();

        public IEnumerator<nint> GetEnumerator()
        {
            if (_context != nint.Zero)
                yield return _context;
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public int Count
            => _context == nint.Zero ? 0 : 1;

        public IntPtr this[int index]
            => index is 0 && _context != nint.Zero ? _context : throw new IndexOutOfRangeException();
    }

    private static void OnDefaultStyleChanged()
        => CacheManager.Instance.SetFontDirty();

    private static void OnDefaultGlobalScaleChanged()
        => CacheManager.Instance.SetFontDirty();

    private static void OnDefaultFontChanged()
    {
        CacheManager.Instance.SetStyleDirty();
        CacheManager.Instance.SetColorsDirty();
    }
}
