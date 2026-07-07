namespace Luna;

/// <summary> The type of parent a node has. </summary>
public enum ParentConditionType : byte
{
    /// <summary> This condition is the root condition. </summary>
    Root,

    /// <summary> This condition is a negated condition. </summary>
    Not,

    /// <summary> This condition is one in a group of conjunctive conditions. </summary>
    And,

    /// <summary> This condition is one in a group of disjunctive conditions. </summary>
    Or,

    /// <summary> This condition is a child of a custom condition. </summary>
    Custom,
};
