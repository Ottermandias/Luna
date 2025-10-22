using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.EventArgs;

namespace Luna;

/// <summary> A basic notification that does not print to chat. </summary>
public class Notification : IMessage
{
    /// <inheritdoc/>
    public NotificationType NotificationType { get; }

    /// <inheritdoc/>
    public string NotificationMessage { get; }

    /// <inheritdoc/>
    public string NotificationTitle { get; init; }

    /// <inheritdoc/>
    public TimeSpan NotificationDuration { get; init; }

    /// <inheritdoc/>
    public string LogMessage { get; }

    /// <inheritdoc/>
    public SeString ChatMessage
        => SeString.Empty;

    /// <inheritdoc/>
    public StringU8 StoredMessage { get; }

    /// <inheritdoc/>
    public StringU8 StoredTooltip { get; }

    /// <inheritdoc/>
    public void OnNotificationActions(INotificationDrawArgs args)
    {}

    /// <summary> Create a new notification with the given message. </summary>
    /// <param name="content"> The message to display in the notification and write to log.  </param>
    /// <param name="type"> The severity of the message. </param>
    /// <param name="duration"> The duration the notification is visible in milliseconds. </param>
    public Notification(string content, NotificationType type = NotificationType.Warning, uint duration = 5000)
    {
        NotificationType     = type;
        NotificationMessage  = content;
        NotificationDuration = TimeSpan.FromMilliseconds(duration);
        NotificationTitle    = type.ToString();
        LogMessage           = NotificationMessage;
        StoredMessage         = new StringU8(NotificationMessage);
        StoredTooltip         = StringU8.Empty;
    }

    /// <summary> Create a new error notification with the given message. </summary>
    /// <param name="ex"> The exception associated with the error, used to construct part of the message. </param>
    /// <param name="content1"> The message to display in the notification window. </param>
    /// <param name="content2"> The message to display in the log before the exception's message and stack trace. </param>
    /// <param name="type"> The severity of the message. </param>
    /// <param name="duration"> The duration the notification is visible in milliseconds. </param>
    public Notification(Exception ex, string content1, string content2, NotificationType type = NotificationType.Error, uint duration = 5000)
    {
        NotificationType     = type;
        NotificationMessage  = content1;
        NotificationDuration = TimeSpan.FromMilliseconds(duration);
        NotificationTitle    = type.ToString();
        LogMessage           = $"{content2}:\n{ex}";
        StoredMessage         = new StringU8(NotificationMessage);
        StoredTooltip         = new StringU8($"{ex}");
    }
}
