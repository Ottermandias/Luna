using ImSharp.ImNodes;

namespace Luna;

/// <summary> A structured cache of condition data to draw. </summary>
/// <typeparam name="TContext"> The context type for the conditions. </typeparam>
/// <param name="context"> A provided context for the drawn conditions, used for updating too. </param>
public abstract partial class ConditionDrawerCache<TContext>(TContext context)
    : BasicCache where TContext : IConditionContext<TContext>
{
    /// <summary> The condition context. </summary>
    public readonly TContext Context = context;

    /// <summary> A persistent ID counter that resets when the cache data is updated. </summary>
    public ImGuiId IdCounter { get; protected set; }

    /// <summary> The precomputed size of all default nodes (Root, And, Or, Not, True, False). </summary>
    public Vector2 ConnectorNodeSize { get; protected set; }

    /// <summary> The precomputed size of the Root node. </summary>
    public Vector2 RootNodeSize { get; protected set; }

    /// <summary> The precomputed size of the Not node. </summary>
    public Vector2 NotNodeSize { get; protected set; }

    /// <summary> The precomputed size of the True and False nodes. </summary>
    public Vector2 ConstantNodeSize { get; protected set; }

    /// <summary> The spacing between nodes. </summary>
    public Vector2 NodeSpacing { get; protected set; }

    /// <summary> The size of the drawn buttons. </summary>
    public Vector2 ButtonSize { get; protected set; }

    /// <summary> The colors for the default node types. </summary>
    public Colors NodeColors = new();

    /// <summary> Create a new custom condition. </summary>
    /// <returns> The newly created condition, which should be a custom literal condition. </returns>
    protected virtual ICondition<TContext>? CreateNewCondition()
        => null;

    /// <summary> Draw this cache as a node editor. </summary>
    /// <returns> Whether any condition was changed during drawing, in which case the caller might want to reduce the conditions again and save them. </returns>
    public bool Draw()
    {
        var oldModifier = ImNodes.Io.SetLinkDetachWithModifier(Im.Io, ModFlags.None);
        var oldPan      = ImNodes.Io.AltMouseButton;
        ImNodes.Io.AltMouseButton = MouseButton.Right;
        using var style = ImNodesStyleDouble.NodePadding.Push(Vector2.Zero)
            .Push(ImNodesStyleSingle.GridSpacing, Im.Style.FrameHeight);
        using var colors = ImNodes.Color.Empty();
        var       ret    = false;
        using (var editor = ImNodes.NodeEditor())
        {
            editor.DisableHoverInteraction(true);
            editor.DisableSelection(true);
            foreach (var node in Nodes)
            {
                colors.Push(ImNodesColor.TitleBar, node.TitleColor(this))
                    .Push(ImNodesColor.NodeOutline, node.BorderColor(this));
                using var n = editor.Node(node.Id);
                n.Draggable         =  false;
                n.GridSpacePosition =  node.Position;
                ret                 |= node.DrawContent(this, n);
                colors.Pop(2);
            }

            foreach (var link in Links)
                editor.Link(link.Id, link.Start, link.End);

            if (Im.Window.Appearing)
                ImNodes.EditorContext.Panning = new Vector2(ImNodes.Style.GridSpacing, (Im.Window.Size.Y - ConnectorNodeSize.Y) / 2);
        }

        if (ret)
            Dirty |= IManagedCache.DirtyFlags.Custom;

        ImNodes.Io.SetLinkDetachWithModifier(Im.Io, oldModifier);
        ImNodes.Io.AltMouseButton = oldPan;
        return ret;
    }

    /// <summary> Draw the button that adds the first condition to an empty root. </summary>
    /// <param name="root"> The root node. </param>
    /// <param name="drawerCache"> The parent cache. </param>
    /// <param name="node"> The node object. </param>
    /// <param name="buttonSize"> The size of the button. </param>
    /// <returns> True if a new condition was created and set. </returns>
    protected virtual bool DrawAddRootConditionButton(RootNode root, ConditionDrawerCache<TContext> drawerCache, ImSharp.ImNodes.Node node, Vector2 buttonSize)
    {
        if (!ImEx.Icon.ButtonCorners(LunaStyle.AddObjectIcon, Corners.Bottom, "Set a condition for this object."u8, false, buttonSize)
         || drawerCache.CreateNewCondition() is not { } condition)
            return false;

        root.Setter(condition);
        return true;
    }

    /// <summary> The list of nodes to be drawn. </summary>
    protected readonly List<Node> Nodes = [];

    /// <summary> The list of links between nodes to be drawn. </summary>
    protected readonly List<Link> Links = [];

    /// <summary> The list of the widest node per depth level. </summary>
    protected readonly List<float> DepthWidth = [];

    /// <inheritdoc/>
    public override void Update()
    {
        if (!AnyDirty)
            return;

        Dirty = IManagedCache.DirtyFlags.Clean;
        // Reset the ID counter.
        IdCounter = 0;
        Nodes.Clear();
        Links.Clear();
        DepthWidth.Clear();

        // Compute the default sizes.
        ButtonSize = new Vector2(Im.Style.FrameHeight);
        var nodeHeight = 2 * ButtonSize.Y;
        RootNodeSize      = new Vector2(3 * ButtonSize.X,                         nodeHeight);
        NotNodeSize       = new Vector2(2 * ButtonSize.X,                         nodeHeight);
        ConnectorNodeSize = new Vector2(4 * ButtonSize.X,                         nodeHeight);
        ConstantNodeSize  = new Vector2(Im.Font.CalculateButtonSize("False"u8).X, nodeHeight);
        NodeSpacing       = ButtonSize with { X = nodeHeight };
        OnUpdate();

        // Add the root node.
        var currentRoot = Context.GetRoot();
        var rootNode    = new RootNode(IdCounter++, Context.SetRoot, IdCounter++, currentRoot is null);
        var size        = rootNode.GetOwnSize(this);
        AddNode(rootNode, size.X, currentRoot is not null);

        // Add all other nodes iteratively, centering them within their children.
        // Nodes add their own links.
        var conditionNode = HandleCondition(Context.SetRoot, currentRoot, ParentConditionType.Root, 1);
        rootNode.SubtreeHeight = Math.Max(conditionNode?.SubtreeHeight ?? 0, size.Y);

        // If we have any actual conditions, add a link from them to the root.
        if (conditionNode is { } c)
            Links.Add(new Link(IdCounter++, c.Output, rootNode.Input));

        ComputePositions();
    }

    /// <summary>
    ///   Using the previously computed heights of each subtree and widths of each depth layer, compute the grid-based node positions.
    ///   The root node is fixed at (FrameHeight, - FrameHeight), i.e. one grid width off to the right and the pin placed on the zero line.
    /// </summary>
    protected virtual void ComputePositions()
    {
        var currentPosition = new Vector2(Im.Style.FrameHeight, -Im.Style.FrameHeight);
        var currentDepth    = 0;
        Nodes[0].Position = currentPosition;

        for (var i = 1; i < Nodes.Count; ++i)
        {
            var node = Nodes[i];
            // Greater depth can only be 1 greater at most.
            if (node.Depth > currentDepth)
            {
                currentPosition.X += DepthWidth[currentDepth] + NodeSpacing.X;
                currentPosition.Y -= 0.5f * (Nodes[i - 1].SubtreeHeight - node.SubtreeHeight);
                currentDepth      =  node.Depth;
            }
            else if (node.Depth == currentDepth)
            {
                currentPosition.Y += 0.5f * (node.SubtreeHeight + Nodes[i - 1].SubtreeHeight) + NodeSpacing.Y;
            }
            else
            {
                // Adapt the X position backwards as required.
                currentDepth = node.Depth;
                Node sibling = null!;
                for (var j = i - 1; j >= 0; --j)
                {
                    if (Nodes[j].Depth == currentDepth)
                    {
                        sibling = Nodes[j];
                        break;
                    }
                }

                currentPosition.X = sibling.Position.X;
                currentPosition.Y = sibling.Position.Y + 0.5f * (sibling.SubtreeHeight + node.SubtreeHeight) + NodeSpacing.Y;
            }

            node.Position = currentPosition;
        }
    }

    /// <summary> Invoked after the default sizes are set, but before any nodes are created. </summary>
    protected virtual void OnUpdate()
    { }

    /// <summary> Handle each type of supported condition. </summary>
    /// <param name="setter"> The setter to invoke to assign this condition itself something else. </param>
    /// <param name="condition"> The condition itself. </param>
    /// <param name="parent"> The type of parent node. </param>
    /// <param name="depth"> The depth of the condition in the tree. </param>
    /// <returns> The ID of this conditions output node (which is guaranteed to exist unless the condition is null, since only the root has none). </returns>
    protected virtual OutputNode? HandleCondition(Action<ICondition<TContext>?> setter,
        ICondition<TContext>? condition, ParentConditionType parent, byte depth)
        => condition switch
        {
            null                       => null,
            AndCondition<TContext> and => HandleListCondition(setter, and, parent, depth),
            OrCondition<TContext> or   => HandleListCondition(setter, or,  parent, depth),
            NotCondition<TContext> not => HandleNotCondition(setter, not, parent, depth),
            TrueCondition<TContext>    => HandleConstantCondition(setter, true,  parent, depth),
            FalseCondition<TContext>   => HandleConstantCondition(setter, false, parent, depth),
            _                          => HandleCustomCondition(setter, condition, parent, depth),
        };

    /// <summary> A basic link. </summary>
    /// <param name="id"> The ID of the link itself. </param>
    /// <param name="start"> The ID of the start pin, which should be the output pin of a node. </param>
    /// <param name="end"> The ID of the end pin, which should be the input pin of a node. </param>
    protected readonly struct Link(LinkId id, AttributeId start, AttributeId end)
    {
        public readonly LinkId      Id    = id;
        public readonly AttributeId Start = start;
        public readonly AttributeId End   = end;
    }

    /// <summary> Default colors for the pre-defined nodes. </summary>
    public struct Colors()
    {
        public Rgba32 RootTitle     = Rgba32.Black.WithAlpha(102);
        public Rgba32 AndTitle      = Rgba32.Yellow.WithAlpha(102);
        public Rgba32 OrTitle       = Rgba32.Green.WithAlpha(51);
        public Rgba32 NotTitle      = Rgba32.Red.WithAlpha(51);
        public Rgba32 ConstantTitle = Rgba32.Cyan.WithAlpha(51);
        public Rgba32 CustomTitle   = Rgba32.Blue.WithAlpha(51);

        public Rgba32 RootBorder     = Rgba32.Black;
        public Rgba32 AndBorder      = new(153, 153, 51);
        public Rgba32 OrBorder       = new(51, 153, 51);
        public Rgba32 NotBorder      = new(153, 51, 51);
        public Rgba32 ConstantBorder = new(51, 153, 153);
        public Rgba32 CustomBorder   = new(51, 71, 220);
    }
}
