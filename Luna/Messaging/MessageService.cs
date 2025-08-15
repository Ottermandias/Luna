using Dalamud.Plugin.Services;

namespace Luna;

/// <summary> A service that handles notifications for popups, chat and logging, and also can store and display notifications in a list. </summary>
/// <param name="log"> The logger to write to. </param>
/// <param name="chat"> The game's chat UI to write messages to. </param>
/// <param name="notificationManager"> The popup notification handler to use. </param>
/// <remarks> This can also handle messages with a tag to prevent message flooding. </remarks>
public class MessageService(Logger log, IChatGui chat, INotificationManager notificationManager)
    : IReadOnlyDictionary<DateTime, IMessage>
{
    /// <summary> The logger used by the service. </summary>
    public readonly Logger Log = log;

    /// <summary> The popup notification handler used by the service. </summary>
    public readonly INotificationManager NotificationManager = notificationManager;

    /// <summary> The game's chat UI used by the service. </summary>
    public readonly IChatGui Chat = chat;

    /// <summary> Contains sent messages that should be printed in the Notification log. </summary>
    private readonly SortedDictionary<DateTime, IMessage> _messages = [];

    /// <summary> How often tagged messages should be cleaned (in frames). </summary>
    public int LastTaggedMessageCleanCycle { get; init; } = 128;

    /// <summary> The maximum age of a tagged message before it can be sent again. </summary>
    private TimeSpan LastTaggedMessageMaxAge { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary> How often tagged messages have been cleaned yet. </summary>
    private int _taggedMessageCleanCounter;

    /// <summary> A dictionary of messages for specific tags and the time they got sent. </summary>
    private readonly ConcurrentDictionary<string, (DateTime LastMessage, IMessage Message)> _taggedMessages = [];

    /// <summary> Print a message with a tag only if it has not been sent within <seealso cref="LastTaggedMessageMaxAge"/>. </summary>
    /// <param name="tag"> The tag to compare messages by. </param>
    /// <param name="message"> The message. </param>
    public void AddTaggedMessage(string tag, IMessage message)
    {
        // First clean existing tagged messages.
        CleanTaggedMessages(true);

        // Don't warn twice for the same tag.
        if (_taggedMessages.TryGetValue(tag, out _))
            return;

        // Actually write the message to all receivers and update the dictionary.
        var time = AddMessage(message);
        _taggedMessages[tag] = (time, message);
    }

    /// <summary> Write a message to all set receivers. </summary>
    /// <param name="message"> The message. </param>
    /// <param name="doStore"> If this is true, the message will be stored for the notification log. </param>
    /// <param name="doNotify"> If this is true, the message will be printed to a notification popup. </param>
    /// <param name="doLog"> If this is true, the message will be logged in the application log. </param>
    /// <param name="doChat"> If this is true, the message will be printed to game chat. </param>
    /// <returns> The UTC time when the message was sent. </returns>
    public DateTime AddMessage(IMessage message, bool doStore = true, bool doNotify = true, bool doLog = true, bool doChat = false)
    {
        var time = DateTime.UtcNow;

        // Store the message if set up to do so and the message supports it.
        if (doStore)
        {
            var storedMessage = message.StoredMessage;
            if (storedMessage.Length > 0)
                lock (_messages)
                {
                    // Can not store messages on the exact same time stamp so add ticks until we can.
                    while (!_messages.TryAdd(time, message))
                        time = time.AddTicks(1);
                }
        }

        // Write the message to log if set up to do so and the message supports it.
        if (doLog)
        {
            var logMessage = message.LogMessage;
            if (logMessage.Length > 0)
                Log.Message(message.LogLevel, message.LogMessage);
        }

        // Create a notification popup if set up to do so and the message supports it.
        if (doNotify)
        {
            var notificationMessage = message.NotificationMessage;
            if (notificationMessage.Length > 0)
                NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification()
                {
                    Content         = message.NotificationMessage,
                    Title           = message.NotificationTitle,
                    Type            = message.NotificationType,
                    Minimized       = false,
                    InitialDuration = message.NotificationDuration,
                });
        }

        // Write the message to chat if set up to do so and the message supports it.
        if (doChat)
        {
            var chatMessage = message.ChatMessage;
            if (chatMessage.Payloads.Count > 0)
                Chat.Print(chatMessage);
        }

        return time;
    }

    /// <summary> Cleans up all tagged messages that happened long enough ago. </summary>
    /// <param name="force"> If this is false, it only cleans up sporadically. </param>
    public void CleanTaggedMessages(bool force)
    {
        if (!force && ++_taggedMessageCleanCounter >= LastTaggedMessageCleanCycle)
        {
            _taggedMessageCleanCounter = 0;
            return;
        }

        var expiredDate = DateTime.UtcNow - LastTaggedMessageMaxAge;
        foreach (var (key, value) in _taggedMessages)
        {
            if (value.Item1 <= expiredDate && _taggedMessages.TryRemove(key, out var pair))
                _messages.Remove(pair.LastMessage);
        }
    }

    /// <summary> Draw a table displaying all stored notifications. </summary>
    public void DrawNotificationLog()
    {
        using var id         = Im.Id.Push("NotificationLog"u8);
        var       buttonSize = new Vector2(Im.Style.FrameHeight);

        using var table = Im.Table.Begin("errors"u8, 5, TableFlags.RowBackground);
        table.SetupColumn("##del"u8,   TableColumnFlags.WidthFixed, buttonSize.X);
        table.SetupColumn("Time"u8,    TableColumnFlags.WidthFixed, Im.Font.CalculateSize("00:00:00.0000"u8).X);
        table.SetupColumn("##icon"u8,  TableColumnFlags.WidthFixed, buttonSize.X);
        table.SetupColumn("##multi"u8, TableColumnFlags.WidthFixed, buttonSize.X);
        table.SetupColumn("Message"u8, TableColumnFlags.WidthStretch);

        table.HeaderRow();

        lock (_messages)
        {
            var height     = Im.Style.FrameHeightWithSpacing;
            var deleteTime = DateTime.MinValue;
            using (var clipper = new Im.ListClipper(_messages.Count, height))
            {
                foreach (var (index, (date, message)) in clipper.Iterate(_messages.Index()))
                    PrintMessage(table, index, date, message, ref deleteTime);
            }

            if (deleteTime != DateTime.MinValue)
                _messages.Remove(deleteTime);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PrintMessage(in Im.TableDisposable table, int index, DateTime date, IMessage message, ref DateTime deleteTime)
    {
        using var id = Im.Id.Push(index);
        table.NextColumn();
        if (ImEx.Icon.Button(LunaStyle.DeleteIcon, "Remove this from the list."u8))
            deleteTime = date;

        table.DrawFrameColumn($"{date.ToLocalTime():HH:mm:ss.fff}");

        table.NextColumn();
        var (icon, color) = message.NotificationType.GetIcon();
        Im.Cursor.FrameAlign();
        ImEx.Icon.Draw(icon, color);

        var text           = message.StoredMessage;
        var firstLine      = text.Span;
        var newLine        = text.IndexOf((byte)'\n');
        var remainingLines = ""u8;
        var tooltip        = message.StoredTooltip;

        if (newLine >= 0)
        {
            firstLine      = firstLine[..newLine];
            remainingLines = text.Span[(newLine + 1)..];
        }

        table.NextColumn();
        var hovered = false;
        if (tooltip.Length > 0)
            hovered = LunaStyle.DrawAlignedHelpMarker();

        table.DrawFrameColumn(firstLine);
        if (!hovered && !Im.Item.Hovered(HoveredFlags.AllowWhenDisabled))
            return;

        using var tt = Im.Tooltip.Begin();
        if (remainingLines.Length > 0)
        {
            Im.Text(remainingLines);
            Im.Text(StringU8.Empty);
        }

        if (tooltip.Length > 0)
            Im.Text(tooltip);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<DateTime, IMessage>> GetEnumerator()
        => _messages.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => _messages.Count;

    /// <inheritdoc/>
    public bool ContainsKey(DateTime key)
        => _messages.ContainsKey(key);

    /// <inheritdoc/>
    public bool TryGetValue(DateTime key, [NotNullWhen(true)] out IMessage? value)
        => _messages.TryGetValue(key, out value);

    /// <inheritdoc/>
    public IMessage this[DateTime key]
        => _messages[key];

    /// <inheritdoc/>
    public IEnumerable<DateTime> Keys
        => _messages.Keys;

    /// <inheritdoc/>
    public IEnumerable<IMessage> Values
        => _messages.Values;
}
