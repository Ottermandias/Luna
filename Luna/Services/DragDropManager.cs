using Dalamud.Interface;
using Dalamud.Interface.DragDrop;
using Dalamud.Plugin;
using Microsoft.Extensions.Logging;

namespace Luna;

/// <summary> A global, shared manager for external file drag & drop actions. </summary>
/// <remarks>
///   Uses DataShares to only draw a single global, invisible window that can provide external drag & drop sources as well as window-wide drag & drop targets. <br/>
///   It is only subscribed to a draw event if there are any sources or targets to be drawn, and skips the drawing if no external file is being dragged.
/// </remarks>
public class DragDropManager : IDisposable, IUiService
{
    private const    int                     Version = 1;
    private readonly IUiBuilder              _uiBuilder;
    private readonly IDragDropManager        _dragDropManager;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ILogger                 _logger;

    /// <summary> A dictionary of external drag & drop sources, their validator functions and their optional tooltip builders. </summary>
    /// <remarks> This is shared via data share. </remarks>
    private readonly Dictionary<string, (Func<IDragDropManager, bool>, Func<IDragDropManager, bool>?)> _sources;

    /// <summary> A dictionary of window-wide drag & drop targets and the actions to invoke on the provided files and dictionaries when dropping. </summary>
    /// <remarks> This is shared via data share. </remarks>
    private readonly Dictionary<string, Action<IReadOnlyList<string>, IReadOnlyList<string>>> _targets;

    /// <summary> The shared last frame in which the global window was drawn, so that only one <see cref="DragDropManager"/> actually draws the window. </summary>
    /// <remarks> This is shared via data share. It is a single-element array to provide reference-semantics. </remarks>
    private readonly int[] _drawnFrame;

    /// <summary> A marker that gets set on initialization if this service created the data share. False if the share already existed. </summary>
    private bool _creator;

    /// <summary> The <see cref="IDragDropManager.IsDragging"/> property gets removed one frame before the drop actually happens, but we still need to draw the window in this frame. </summary>
    private bool _keepDragAlive;

    /// <summary> Accessor for the last frame. </summary>
    private int LastDrawnFrame
    {
        get => _drawnFrame[0];
        set => _drawnFrame[0] = value;
    }

    /// <summary> Keep track of our subscription status on the <see cref="UiBuilder.Draw"/> event. </summary>
    private bool _subscribed;

    /// <summary> Create a new instance of <see cref="DragDropManager"/> utilizing shared resources and dalamud services. </summary>
    public DragDropManager(IUiBuilder uiBuilder, IDragDropManager dragDropManager, IDalamudPluginInterface pi,
        IDalamudPluginInterface pluginInterface, ILogger logger)
    {
        _uiBuilder       = uiBuilder;
        _dragDropManager = dragDropManager;
        _pluginInterface = pluginInterface;
        _logger          = logger;

        (_sources, _targets, _drawnFrame) = _pluginInterface.GetOrCreateData($"Luna.DragDropManager.V{Version}",
            () =>
            {
                _creator = true;
                return Tuple.Create(
                    new Dictionary<string, (Func<IDragDropManager, bool> ValidityCheck, Func<IDragDropManager, bool>? TooltipBuilder)>(),
                    new Dictionary<string, Action<IReadOnlyList<string>, IReadOnlyList<string>>>(),
                    new int[1]);
            });
        if (_creator)
            _logger.LogDebug("Created new external drag & drop manager with version {Version}.", Version);
        else
            _logger.LogDebug("Shared existing external drag & drop manager with version {Version}.", Version);
    }

    /// <summary> Add a new external drag and drop source to the shared data. </summary>
    /// <param name="source"> The label for the source. Should not have more than 32 UTF8-characters. </param>
    /// <param name="validityCheck"> The validity check for supported file types etc., if left null, all drag & drop actions are valid. </param>
    /// <param name="tooltipBuilder"> A tooltip builder that draws a tooltip when this source is active. </param>
    /// <returns> True if the source was added, false if a source of that label already existed. </returns>
    /// <remarks> The source needs to be removed via <see cref="RemoveSource"/> when it is no longer used. </remarks>
    public bool AddSource(string source, Func<IDragDropManager, bool>? validityCheck = null,
        Func<IDragDropManager, bool>? tooltipBuilder = null)
    {
        if (!_sources.TryAdd(source, (validityCheck ?? AlwaysTrue, tooltipBuilder)))
        {
            _logger.LogWarning("Failed to add source {Source} to external drag & drop manager.", source);
            return false;
        }

        _logger.LogDebug("Added source {Source} to external drag & drop manager.", source);
        Subscribe();
        return true;
    }

    /// <summary> Remove an existing external drag and drop source via its label. </summary>
    /// <param name="source"> The label of the source to remove. </param>
    /// <returns> True if the source existed and was removed, false if no source of that label existed. </returns>
    public bool RemoveSource(string source)
    {
        if (!_sources.Remove(source))
            return false;

        _logger.LogDebug("Removed source {Source} from external drag & drop manager.", source);
        if (_sources.Count is 0 && _targets.Count is 0)
            Unsubscribe();

        return true;
    }

    /// <summary> Add a new window-wide drag and drop target to the shared data. </summary>
    /// <param name="target"> The label for the target. Should not have more than 32 UTF8-characters and should match an existing source. </param>
    /// <param name="action"> The method to invoke on a list of dragged files and a list of dragged directories when they are dropped onto the window. </param>
    /// <returns> True if the target was added, false if a target of that label already existed. </returns>
    /// <remarks> The target needs to be removed via <see cref="RemoveTarget"/> when it is no longer used. </remarks>
    public bool AddTarget(string target, Action<IReadOnlyList<string>, IReadOnlyList<string>> action)
    {
        if (!_targets.TryAdd(target, action))
        {
            _logger.LogWarning("Failed to add target {Target} to external drag & drop manager.", target);
            return false;
        }

        _logger.LogDebug("Added target {Target} to external drag & drop manager.", target);
        Subscribe();
        return true;
    }

    /// <summary> Remove an existing window-wide drag and drop target via its label. </summary>
    /// <param name="target"> The label of the target to remove. </param>
    /// <returns> True if the target existed and was removed, false if no target of that label existed. </returns>
    public bool RemoveTarget(string target)
    {
        if (!_targets.Remove(target))
            return false;

        _logger.LogDebug("Removed source {Target} from external drag & drop manager.", target);
        if (_sources.Count is 0 && _targets.Count is 0)
            Unsubscribe();

        return true;
    }

    /// <summary> Draw debugging information about this service's state. </summary>
    public void DrawDebugInfo()
    {
        using var id    = Im.Id.Push("DragDropManager"u8);
        using var table = Im.Table.Begin("table"u8, 2, TableFlags.SizingFixedFit);
        if (!table)
            return;

        table.DrawColumn("Drag & Drop Manager Version"u8);
        table.DrawColumn($"{Version}");

        table.DrawColumn("Original Creator"u8);
        table.DrawColumn($"{_creator}");

        table.DrawColumn("Subscribed"u8);
        table.DrawColumn($"{_subscribed}");
        Im.Line.Same();
        if (Im.SmallButton(_subscribed ? "Unsubscribe"u8 : "Resubscribe"u8))
        {
            if (_subscribed)
                Unsubscribe();
            else
                Subscribe();
        }

        table.DrawColumn("Last Drawn Frame"u8);
        table.DrawColumn($"{LastDrawnFrame}");

        table.DrawColumn($"{_sources.Count} Sources");
        table.NextColumn();
        foreach (var source in _sources.Keys)
            Im.Text(source);

        table.DrawColumn($"{_sources.Count} Targets");
        table.NextColumn();
        foreach (var target in _targets.Keys)
            Im.Text(target);
    }

    /// <summary>
    ///   When required, draw an entirely independent ImGui window that is invisible and not interactable.
    ///   This window draws all external drag and drop sources, as well as all drag and drop targets across the entire main viewport.
    /// </summary>
    private void Draw()
    {
        
        // Keep the window alive one frame longer than IsDragging.
        if (!_dragDropManager.IsDragging)
        {
            if (!_keepDragAlive)
                return;

            _keepDragAlive = false;
        }
        else
        {
            _keepDragAlive = true;
        }

        // Keep track of the current frame in the shared state,
        // so that only one service draws the window with the sources and targets.
        var currentFrame = Im.State.FrameCount;
        if (LastDrawnFrame == currentFrame)
            return;

        LastDrawnFrame = currentFrame;

        // Make the window take up the entire main viewport with no padding, no visible elements and no interactivity.
        Im.Window.SetNextSize(Im.Viewport.Main.Size);
        Im.Viewport.Main.SetNextWindowPositionRelative(Vector2.Zero);
        using var style = ImStyleDouble.WindowPadding.Push(Vector2.Zero);
        using var window = Im.Window.Begin("###dragDropWindow"u8,
            WindowFlags.NoSavedSettings
          | WindowFlags.NoBackground
          | WindowFlags.NoCollapse
          | WindowFlags.NoDecoration
          | WindowFlags.NoMove
          | WindowFlags.NoDocking
          | WindowFlags.NoResize
          | WindowFlags.NoTitleBar);
        if (!window)
            return;

        // Draw all sources.
        foreach (var (source, (validity, tooltip)) in _sources)
        {
            if (tooltip is null)
                _dragDropManager.CreateImGuiSource(source, validity);
            else
                _dragDropManager.CreateImGuiSource(source, validity, tooltip);
        }

        // Draw all targets associated with a window-wide invisible button.
        if (_targets.Count is 0)
            return;

        // Magic number used in our version of imgui to make the drag and drop border on the window look nice.
        Im.Cursor.Position = new Vector2(5f, 4f);
        Im.InvisibleButton("##"u8, Im.ContentRegion.Available - new Vector2(5f, 4f));
        foreach (var (target, action) in _targets)
        {
            if (_dragDropManager.CreateImGuiTarget(target, out var files, out var directories))
            {
                _logger.LogDebug("Accepted payload for {Target}, invoking associated action.", target);
                try
                {
                    action(files, directories);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error invoking action for {Target}.", target);
                }
            }
        }
    }

    /// <summary> Subscribe to the Draw Event. Checks if external drag and drop is available at all. </summary>
    private void Subscribe()
    {
        if (_subscribed)
            return;

        if (!_dragDropManager.ServiceAvailable)
        {
            _logger.LogDebug("External drag & drop through Dalamud is unavailable.");
            return;
        }

        _uiBuilder.Draw += Draw;
        _subscribed     =  true;
        _logger.LogInformation("External drag & drop manager activated.");
    }

    /// <summary> Unsubscribe from the Draw Event. </summary>
    private void Unsubscribe()
    {
        if (!_subscribed)
            return;

        _logger.LogInformation("External drag & drop manager deactivated.");
        _uiBuilder.Draw -= Draw;
        _subscribed     =  false;
    }


    /// <inheritdoc/>
    public void Dispose()
    {
        _pluginInterface.RelinquishData($"Luna.DragDropManager.V{Version}");
        _uiBuilder.Draw -= Draw;
        _subscribed     =  false;
    }

    /// <summary> Always-true predicate to replace null-validity checks. </summary>
    private static bool AlwaysTrue(IDragDropManager _)
        => true;
}
