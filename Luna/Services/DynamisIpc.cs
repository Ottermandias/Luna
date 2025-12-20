using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace Luna;

/// <summary> A wrapper class for IPC with Dynamis, a developer utility plugin. </summary>
/// <remarks> This can be used to better display debug information. </remarks>
public class DynamisIpc : IDisposable, IService
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly Logger                  _log;

    private readonly ICallGateSubscriber<uint, uint, ulong, Version, object?> _initialized;
    private readonly ICallGateSubscriber<object?>                             _disposed;

    private ICallGateSubscriber<nint, string?, object?>?                           _inspectObject;
    private ICallGateSubscriber<nint, uint, string, uint, uint, string?, object?>? _inspectRegion;
    private Action<nint, Func<string?>?, string?, ulong, Vector2>?                 _drawPointerAction;
    private ICallGateSubscriber<nint, object?>?                                    _imGuiDrawPointerTooltipDetails;
    private ICallGateSubscriber<nint, Func<string?>?, object?>?                    _imGuiOpenPointerContextMenu;
    private ICallGateSubscriber<nint, (string, Type?, uint, uint)>?                _getClass;
    private ICallGateSubscriber<nint, string?, Type?, (bool, uint)>?               _isInstanceOf;
    private ICallGateSubscriber<object?>?                                          _preloadDataYaml;

    /// <summary> Whether this service is currently subscribed to Dynamis' IPC methods. </summary>
    public bool IsSubscribed
        => VersionMajor > 0;

    /// <summary> Get the available features from Dynamis' IPC. </summary>
    public ulong Features { get; private set; }

    /// <summary> Get the current major version of the Dynamis plugin. </summary>
    public uint VersionMajor { get; private set; }

    /// <summary> Get the current minor version of the Dynamis plugin. </summary>
    public uint VersionMinor { get; private set; }

    /// <summary> The exception thrown when setting up the IPC subscription, if any. </summary>
    public Exception? Error { get; private set; }

    public DynamisIpc(IDalamudPluginInterface pi, Logger log)
    {
        _pluginInterface = pi;
        _log             = log;

        try
        {
            // Get and subscribe to IPC initialization and disposal events of Dynamis.
            _initialized = _pluginInterface.GetIpcSubscriber<uint, uint, ulong, Version, object?>("Dynamis.ApiInitialized");
            _initialized.Subscribe(OnInitialized);
            _disposed = _pluginInterface.GetIpcSubscriber<object?>("Dynamis.ApiDisposing");
            _disposed.Subscribe(OnDisposed);

            UpdateVersion();
            Error = null;
        }
        catch (Exception ex)
        {
            _initialized = null!;
            _disposed    = null!;
            Error        = ex;
            _log.Error($"Error subscribing to Dynamis IPC Events:\n{ex}");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Error        = null;
        VersionMajor = 0;
        OnDisposed();
        _initialized.Unsubscribe(OnInitialized);
        _disposed.Unsubscribe(OnDisposed);
    }

    /// <summary> Open a Dynamis window inspecting the object at the given address. </summary>
    /// <param name="address"> The address. </param>
    /// <param name="name"> The optional name of the window. </param>
    public void InspectObject(nint address, string? name = null)
        => _inspectObject?.InvokeAction(address, name);

    /// <summary> Open a dynamis window inspecting a given memory and data region. </summary>
    /// <param name="address"> The address of the region. </param>
    /// <param name="size"> The size of the region. </param>
    /// <param name="typeName"> The type name to display. </param>
    /// <param name="typeTemplateId"> The type's template ID. </param>
    /// <param name="classKindId"> The class kind ID. </param>
    /// <param name="name"> The optional name of the window. </param>
    public void InspectRegion(nint address, uint size, string typeName, uint typeTemplateId, uint classKindId, string? name = null)
        => _inspectRegion?.InvokeAction(address, size, typeName, typeTemplateId, classKindId, name);

    /// <summary> Open the dynamis context menu for a pointer. </summary>
    /// <param name="address"> The address of the pointer. </param>
    /// <param name="name"> A function to obtain a name for the object. </param>
    public void OpenContextMenu(nint address, Func<string?>? name = null)
        => _imGuiOpenPointerContextMenu?.InvokeAction(address, name);

    /// <summary> Draw details for the given address as a tooltip. </summary>
    /// <param name="address"> The address. </param>
    public void DrawTooltipDetails(nint address)
        => _imGuiDrawPointerTooltipDetails?.InvokeAction(address);

    /// <summary> Get information for the class of an object at the given address, if available. </summary>
    /// <param name="address"> The address to query. </param>
    /// <returns> The name of the class, the best matching managed type, if any, an approximate size and the displacement of the address in the class. </returns>
    public (string Name, Type? BestManagedType, uint EstimatedSize, uint Displacement) GetClass(nint address)
        => _getClass?.InvokeFunc(address) ?? ("Unavailable", null, 0, 0);

    /// <summary> Check whether a given object is an instance of a given class. </summary>
    /// <param name="address"> The address of the object. </param>
    /// <param name="className"> The name of the class, if known. </param>
    /// <param name="classType"> The managed type of the class, if known. </param>
    /// <returns></returns>
    public (bool IsInstance, uint Displacement) IsInstanceOf(nint address, string? className, Type? classType)
        => _isInstanceOf?.InvokeFunc(address, className, classType) ?? (false, 0);

    /// <summary> Draw a pointer formatted in hex with Dynamis utility if available, and as a copy-on-click selectable otherwise, using <see cref="Im.Font.Mono"/>. </summary>
    /// <param name="address"> The address to draw. </param>
    /// <param name="name"> A function to obtain the name of the object. </param>
    /// <param name="customText"> Custom text to display instead of the pointer. </param>
    /// <param name="flags"> Flags to control the behavior of the dynamis integration. </param>
    /// <param name="selectableFlags"> Flags to control the behavior of the drawn selectable. </param>
    /// <param name="size"> The size of the drawn selectable. </param>
    [OverloadResolutionPriority(100)]
    public void DrawPointer(nint address, Func<string?>? name = null, string? customText = null,
        DrawPointerFlags flags = DrawPointerFlags.None, SelectableFlags selectableFlags = SelectableFlags.None,
        Vector2 size = default)
    {
        if (_drawPointerAction is not null)
            _drawPointerAction.Invoke(address, name, customText, unchecked((uint)selectableFlags | ((ulong)flags << 32)), size);
        else
            DrawPointerBase(address, customText, flags, selectableFlags, size);
    }

    /// <inheritdoc cref="DrawPointer(nint,Func{string?}?,string?,DrawPointerFlags,SelectableFlags,Vector2)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [OverloadResolutionPriority(100)]
    public unsafe void DrawPointer(void* address, Func<string?>? name = null, string? customText = null,
        DrawPointerFlags flags = DrawPointerFlags.None, SelectableFlags selectableFlags = SelectableFlags.None,
        Vector2 size = default)
        => DrawPointer((nint)address);

    /// <summary> Draw debug information about the state of the Dynamis IPC service. </summary>
    public void DrawDebugInfo()
    {
        using var table = Im.Table.Begin("##Dynamis"u8, 2, TableFlags.SizingFixedFit | TableFlags.RowBackground);
        if (!table)
            return;

        table.DrawColumn("Available"u8);
        table.DrawColumn($"{IsSubscribed}");
        if (IsSubscribed)
        {
            table.DrawColumn("Version"u8);
            table.DrawColumn($"{VersionMajor}.{VersionMinor}");
            table.DrawColumn("Features"u8);
            table.DrawColumn($"{Features:X4}");
            table.DrawColumn("Detach"u8);
            table.NextColumn();
            if (Im.SmallButton("Try##Detach"u8))
                OnDisposed();

            table.DrawColumn("Reattach"u8);
            table.NextColumn();
            if (Im.SmallButton("Try##Reattach"u8))
                UpdateVersion();
        }
        else
        {
            table.DrawColumn("Error"u8);
            table.DrawColumn($"{Error?.Message}");
            table.DrawColumn("Attach"u8);
            table.NextColumn();
            if (Im.SmallButton("Try##Attach"u8))
                UpdateVersion();
        }
    }

    /// <summary> Attach to Dynamis. </summary>
    private void OnInitialized(uint major, uint minor, ulong flags, Version _)
    {
        // First clear all old data.
        OnDisposed();

        // We expect the major version to be 1 at the moment.
        if (major is not 1)
        {
            _log.Debug($"Could not attach to Dynamis {VersionMajor}.{VersionMinor}, only 1.X is supported.");
            return;
        }

        // We need at least minor version 3.
        if (minor < 3)
        {
            _log.Debug($"Could not attach to Dynamis {VersionMajor}.{VersionMinor}, only 1.3 or higher is supported.");
            return;
        }

        VersionMajor = major;
        VersionMinor = minor;
        Features     = flags;

        // Get the IPC subscribers for the functions we need to use.
        try
        {
            _inspectObject = _pluginInterface.GetIpcSubscriber<nint, string?, object?>("Dynamis.InspectObject.V2");
            _inspectRegion = _pluginInterface.GetIpcSubscriber<nint, uint, string, uint, uint, string?, object?>("Dynamis.InspectRegion.V2");
            _imGuiDrawPointerTooltipDetails = _pluginInterface.GetIpcSubscriber<nint, object?>("Dynamis.ImGuiDrawPointerTooltipDetails.V1");
            _imGuiOpenPointerContextMenu =
                _pluginInterface.GetIpcSubscriber<nint, Func<string?>?, object?>("Dynamis.ImGuiOpenPointerContextMenu.V1");
            _getClass        = _pluginInterface.GetIpcSubscriber<nint, (string, Type?, uint, uint)>("Dynamis.GetClass.V1");
            _isInstanceOf    = _pluginInterface.GetIpcSubscriber<nint, string?, Type?, (bool, uint)>("Dynamis.IsInstanceOf.V1");
            _preloadDataYaml = _pluginInterface.GetIpcSubscriber<object?>("Dynamis.PreloadDataYaml.V1");
            _drawPointerAction = _pluginInterface
                .GetIpcSubscriber<Action<nint, Func<string?>?, string?, ulong, Vector2>>("Dynamis.GetImGuiDrawPointerDelegate.V3").InvokeFunc();

            // Preload the data.yml file on attaching so that the first interaction with dynamis does not cause a long hitch.
            _log.Verbose("Preloading Dynamis data.yml...");
            _preloadDataYaml.InvokeAction();
            _log.Debug($"Attached to Dynamis {VersionMajor}.{VersionMinor}.");
        }
        catch (Exception ex)
        {
            Error = ex;
            _log.Error($"Error subscribing to Dynamis IPC:\n{ex}");
            OnDisposed();
        }
    }

    /// <summary> Clear all current IPC data. </summary>
    private void OnDisposed()
    {
        // Only log disposal if we were previously attached.
        if (VersionMajor > 0)
            _log.Debug($"Detaching from Dynamis {VersionMajor}.{VersionMinor}.");

        Error        = null;
        VersionMajor = 0;
        VersionMinor = 0;
        Features     = 0;

        _inspectObject                  = null;
        _inspectRegion                  = null;
        _getClass                       = null;
        _isInstanceOf                   = null;
        _preloadDataYaml                = null;
        _drawPointerAction              = null;
        _imGuiDrawPointerTooltipDetails = null;
        _imGuiOpenPointerContextMenu    = null;
    }

    /// <summary> Get the current API version from Dynamis and handle it accordingly, attach if possible. </summary>
    private void UpdateVersion()
    {
        try
        {
            if (_pluginInterface.GetIpcSubscriber<(uint Major, uint Minor, ulong Flags)>("Dynamis.GetApiVersion") is { } subscriber)
            {
                var (major, minor, flags) = subscriber.InvokeFunc();
                OnInitialized(major, minor, flags, null!);
            }
            else
            {
                OnDisposed();
            }
        }
        catch (Exception ex)
        {
            Error = ex;
            _log.Verbose($"Error subscribing to Dynamis IPC:\n{ex}");
            OnDisposed();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void DrawPointerBase(nint address, string? customText, DrawPointerFlags flags, SelectableFlags selectableFlags,
        Vector2 size)
    {
        using var font = Im.Font.PushMono();

        using (Im.Font.Push(Im.Font.Mono, customText is null ? address != nint.Zero : flags.HasFlag(DrawPointerFlags.MonoFont)))
        {
            using var style = ImStyleSingle.Alpha.Push(0.5f * Im.Style.Alpha,
                    customText is null ? address == nint.Zero : flags.HasFlag(DrawPointerFlags.Semitransparent))
                .Push(ImStyleDouble.SelectableTextAlign, new Vector2(1.0f, 0.5f),
                    size != default || flags.HasFlag(DrawPointerFlags.RightAligned));
            var clicked = customText is null
                ? Im.Selectable(address == nint.Zero ? "nullptr" : $"0x{address:X}", flags.HasFlag(DrawPointerFlags.Selected), selectableFlags)
                : Im.Selectable(customText,                                          flags.HasFlag(DrawPointerFlags.Selected), selectableFlags);
            if (clicked)
            {
                try
                {
                    Im.Clipboard.Set($"0x{address:X}");
                }
                catch
                {
                    // ignored
                }
            }
        }

        Im.Tooltip.OnHover("Click to copy to clipboard."u8);
    }

    // See https://github.com/Exter-N/Dynamis/blob/main/Dynamis/UI/ImGuiComponents.cs
    [Flags]
    public enum DrawPointerFlags : uint
    {
        None = 0,

        /// <summary> Draws the ImGui selectable as selected. </summary>
        Selected = 1,

        /// <summary>
        ///   Draws the supplied custom text in a monospace font.
        ///   Applied to the default text if the pointer is not null.
        /// </summary>
        MonoFont = 2,

        /// <summary>
        ///   Draws the supplied custom text with halved opacity.
        ///   Applied to the default text if the pointer is null.
        /// </summary>
        Semitransparent = 4,

        /// <summary>
        ///   Aligns the text to the right horizontally and centers it vertically.
        ///   Always applied when passed an explicit size.
        /// </summary>
        RightAligned = 8,
    }
}

/// <summary> Helper-extensions to deal with null-services. </summary>
public static class DynamisIpcExtensions
{
    extension(DynamisIpc? dynamis)
    {
        /// <inheritdoc cref="DynamisIpc.DrawPointer(nint,Func{string?}?,string?,DynamisIpc.DrawPointerFlags,SelectableFlags,Vector2)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawPointerChecked(nint address)
        {
            if (dynamis is null)
                DynamisIpc.DrawPointerBase(address, null, DynamisIpc.DrawPointerFlags.None, SelectableFlags.None, default);
            else
                dynamis.DrawPointer(address);
        }

        /// <inheritdoc cref="DynamisIpc.DrawPointer(nint,Func{string?}?,string?,DynamisIpc.DrawPointerFlags,SelectableFlags,Vector2)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void DrawPointerChecked(void* address)
        {
            if (dynamis is null)
                DynamisIpc.DrawPointerBase((nint)address, null, DynamisIpc.DrawPointerFlags.None, SelectableFlags.None, default);
            else
                dynamis.DrawPointer(address);
        }
    }
}
