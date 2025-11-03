namespace Luna;

/// <summary>
///   A weighted list of unique buttons with priorities.
///   Buttons are ordered from lowest to highest priority.
///   Buttons with the same priority are generally ordered by insertion order, but no guarantees are made.
/// </summary>
public readonly struct ButtonList() : IReadOnlyList<BaseButton>
{
    /// <summary> The list sorted by the priority of its elements. </summary>
    private readonly SortedListAdapter<(BaseButton Button, int Priority)>
        _buttons = new([], (lhs, rhs) => lhs.Priority.CompareTo(rhs.Priority));

    /// <summary> Add a button with a given priority. If the same object already exists, its priority is updated. </summary>
    /// <param name="button"> The button to add. </param>
    /// <param name="priority"> The priority for the button. </param>
    public void AddButton(BaseButton button, int priority)
    {
        var idx = _buttons.IndexOf(p => ReferenceEquals(p.Button, button));
        if (idx < 0)
        {
            _buttons.Add((button, priority));
        }
        else if (_buttons[idx].Priority != priority)
        {
            _buttons.RemoveAt(idx);
            _buttons.Add((button, priority));
        }
    }

    /// <summary> Remove a button by reference equality. </summary>
    /// <param name="button"> The button to remove. </param>
    public void RemoveButton(BaseButton button)
    {
        var idx = _buttons.IndexOf(p => ReferenceEquals(p.Button, button));
        if (idx >= 0)
            _buttons.RemoveAt(idx);
    }

    /// <inheritdoc/>
    public IEnumerator<BaseButton> GetEnumerator()
        => _buttons.Select(p => p.Button).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => _buttons.Count;

    /// <inheritdoc/>
    public BaseButton this[int index]
        => _buttons[index].Button;
}

/// <summary>
///   A weighted list of unique buttons with priorities that are passed an argument.
///   Buttons are ordered from lowest to highest priority.
///   Buttons with the same priority are generally ordered by insertion order, but no guarantees are made.
/// </summary>
public readonly struct ButtonList<T>() : IReadOnlyList<BaseButton<T>>
{
    /// <summary> The list sorted by the priority of its elements. </summary>
    private readonly SortedListAdapter<(BaseButton<T> Button, int Priority)>
        _buttons = new([], (lhs, rhs) => lhs.Priority.CompareTo(rhs.Priority));

    /// <summary> Add a button with a given priority. If the same object already exists, its priority is updated. </summary>
    /// <param name="button"> The button to add. </param>
    /// <param name="priority"> The priority for the button. </param>
    public void AddButton(BaseButton<T> button, int priority)
    {
        var idx = _buttons.IndexOf(p => ReferenceEquals(p.Button, button));
        if (idx < 0)
            _buttons.Add((button, priority));
        else if (_buttons[idx].Priority == priority)
            return;

        _buttons.RemoveAt(idx);
        _buttons.Add((button, priority));
    }

    /// <summary> Remove a button by reference equality. </summary>
    /// <param name="button"> The button to remove. </param>
    public void RemoveButton(BaseButton<T> button)
    {
        var idx = _buttons.IndexOf(p => ReferenceEquals(p.Button, button));
        if (idx >= 0)
            _buttons.RemoveAt(idx);
    }

    /// <summary> Remove all buttons of a given type. </summary>
    /// <param name="buttonType"> The type of buttons to remove. </param>
    public void RemoveButtons(Type buttonType)
    {
        var idx = _buttons.IndexOf(p => p.Button.GetType() == buttonType);
        while (idx >= 0)
        {
            _buttons.RemoveAt(idx--);
            idx = _buttons.IndexOf(p => p.Button.GetType() == buttonType, idx);
        }
    }

    /// <inheritdoc/>
    public IEnumerator<BaseButton<T>> GetEnumerator()
        => _buttons.Select(p => p.Button).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => _buttons.Count;

    /// <inheritdoc/>
    public BaseButton<T> this[int index]
        => _buttons[index].Button;
}
