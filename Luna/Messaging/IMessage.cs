using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.EventArgs;

namespace Luna;

/// <summary> A basic interface for persistent messaging. </summary>
public interface IMessage
{
    /// <summary> The type of the notification. </summary>
    public NotificationType NotificationType { get; }

    /// <summary> The message used in the notification popup. </summary>
    public string NotificationMessage { get; }

    /// <summary> The title displayed in the notification popup. </summary>
    public string NotificationTitle { get; }

    /// <summary> The duration the notification should stay visible. </summary>
    public TimeSpan NotificationDuration { get; }

    /// <summary> The message used in the log. </summary>
    public string LogMessage { get; }

    /// <summary> The log level to use for the log message. </summary>
    public Logger.LogLevel LogLevel
        => NotificationType switch
        {
            NotificationType.None    => Logger.LogLevel.Excessive,
            NotificationType.Success => Logger.LogLevel.Verbose,
            NotificationType.Warning => Logger.LogLevel.Warning,
            NotificationType.Error   => Logger.LogLevel.Error,
            NotificationType.Info    => Logger.LogLevel.Debug,
            _                        => Logger.LogLevel.Fatal,
        };

    /// <summary> The message printed to the game's chat. </summary>
    public SeString ChatMessage { get; }

    /// <summary> The message used in the notification log window. </summary>
    public StringU8 StoredMessage { get; }

    /// <summary> The tooltip shown in the notification log window when hovering over the message. </summary>
    public StringU8 StoredTooltip { get; }

    /// <summary> Will be subscribed to <see cref="IActiveNotification.DrawActions"/> when a notification is created. </summary>
    /// <param name="args"> The arguments passed by the event. </param>
    public void OnNotificationActions(INotificationDrawArgs args);
}
