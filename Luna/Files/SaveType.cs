namespace Luna.Files;

/// <summary> Different strategies for saving files. </summary>
public enum SaveType
{
    /// <summary> Queue the file to be saved as soon as possible in a queue that saves one file per frame. </summary>
    /// <remarks> Queueing the same file multiple times before the queue has reached it will not cause multiple saves. </remarks>
    Queue,

    /// <summary> Save the file after a given delay using the state of the file at the point of saving. </summary>
    /// <remarks> Additional triggers to save the same file will not increase the delay further, but may decrease it. It will be saved only once after the currently shortest delay has been reached. </remarks>
    Delay,

    /// <summary> Save the file immediately when entering the next framework update phase. </summary>
    /// <remarks> Additional triggers to save the same file before the framework update has been called will not cause multiple saves. </remarks>
    Immediate,

    /// <summary> Save the file immediately on the current thread and wait for the save to finish before continuing execution. </summary>
    ImmediateSync,

    /// <summary> Do not do anything. </summary>
    None,
}
