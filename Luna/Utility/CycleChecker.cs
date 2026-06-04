namespace Luna;

/// <summary> A utility to check linear parented graphs for cycles. </summary>
public static class CycleChecker
{
    /// <summary> An object that may or may not have a single, unique parent. </summary>
    /// <typeparam name="TParent"> The type of the object. </typeparam>
    public interface IHasParent<TParent> where TParent : class
    {
        /// <summary> The parent, if any is set. </summary>
        public TParent? Parent { get; }

        /// <summary> Whether this being an ancestor of <see cref="potentialAncestor"/> causes a cycle. </summary>
        /// <param name="potentialAncestor"> A potential ancestor. </param>
        /// <returns> True if this being a descendant of <see cref="potentialAncestor"/> causes a cycle. </returns>
        public bool CausesCycle(TParent potentialAncestor)
            => ReferenceEquals(this, potentialAncestor);
    }

    /// <summary> Check whether it is possible to set <paramref cref="potentialChild"/>'s parent to <paramref cref="potentialParent"/> without creating a cycle. </summary>
    /// <typeparam name="TChild"> The type of the child to check. </typeparam>
    /// <typeparam name="TParent"> The type of the potential parent. </typeparam>
    /// <param name="potentialChild"> The potential child of which we want to set the parent. </param>
    /// <param name="potentialParent"> The potential parent or null for no parent.</param>
    /// <returns> False if <paramref name="potentialParent"/> or any of its ancestors is <paramref name="potentialChild"/> itself, true otherwise. </returns>
    public static bool Check<TChild, TParent>(TChild potentialChild, TParent? potentialParent)
        where TChild : class, IHasParent<TParent>, TParent
        where TParent : class
    {
        var currentAncestor = potentialParent;
        while (currentAncestor is not null)
        {
            if (potentialChild.CausesCycle(currentAncestor))
                return false;

            currentAncestor = currentAncestor is IHasParent<TParent> p ? p.Parent : null;
        }

        return true;
    }
}
