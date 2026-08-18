namespace Luna;

/// <summary> Exception thrown when a <see cref="BasicWrapper{TEnum}"/> invokes a function that returns false. </summary>
public class AdapterInvocationException : Exception
{
    protected AdapterInvocationException(string message)
        : base(message)
    { }
}

internal class InvocationException<TEnum>(TEnum id)
    : AdapterInvocationException($"IPC Adapter method {typeof(TEnum).FullName}.{id} caused logic error.");
