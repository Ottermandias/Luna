using Dalamud.Interface.ImGuiNotification;

namespace Luna;

/// <summary> Some <see cref="Notification"/>-based extensions for the <see cref="MessageService"/>. </summary>
public static class MessageServiceExtensions
{
    /// <summary> Add a default notification message to the message service, notify and write to the log, see <see cref="Luna.Notification(string,NotificationType,uint)"/> </summary>
    /// <param name="service"> The service to add the message to. </param>
    /// <param name="content"> The content of the message. </param>
    /// <param name="type"> The severity type of the notification. </param>
    /// <param name="doStore"> Whether to add the notification to the manager's store. </param>
    public static void NotificationMessage(this MessageService service, string content, NotificationType type = NotificationType.None,
        bool doStore = true)
        => service.AddMessage(new Notification(content, type), doStore, true, true, false);

    /// <summary> Add an error notification message to the message service, notify and write to the log, see <see cref="Luna.Notification(Exception,string,string,NotificationType,uint)"/> </summary>
    /// <param name="service"> The service to add the message to. </param>
    /// <param name="ex"> The exception associated with the error. </param>
    /// <param name="content1"> The content of the message. </param>
    /// <param name="type"> The severity type of the notification. </param>
    /// <param name="doStore"> Whether to add the notification to the manager's store. </param>
    public static void NotificationMessage(this MessageService service, Exception ex, string content1,
        NotificationType type = NotificationType.None, bool doStore = true)
        => service.AddMessage(new Notification(ex, content1, content1.TrimEnd('.'), type), doStore, true, true, false);

    /// <summary> Add an error notification message to the message service, notify and write to the log, see <see cref="Luna.Notification(Exception,string,string,NotificationType,uint)"/> </summary>
    /// <param name="service"> The service to add the message to. </param>
    /// <param name="ex"> The exception associated with the error. </param>
    /// <param name="content1"> The content of the message. </param>
    /// <param name="content2"> The content of the message when written to the log. </param>
    /// <param name="type"> The severity type of the notification. </param>
    /// <param name="doStore"> Whether to add the notification to the manager's store. </param>
    public static void NotificationMessage(this MessageService service, Exception ex, string content1, string content2,
        NotificationType type = NotificationType.None, bool doStore = true)
        => service.AddMessage(new Notification(ex, content1, content2, type), doStore, true, true, false);
}
