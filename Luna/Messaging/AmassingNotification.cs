using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.EventArgs;

namespace Luna;

/// <summary> A base class for a single notification amassing multiple separate notifications. Use this when using one notification per case may spam too much. </summary>
/// <typeparam name="T"> The type of stored objects. Those are unique. </typeparam>
/// <param name="messageService"> The base message service. </param>
public abstract class AmassingNotification<T>(MessageService messageService) : INotificationAwareMessage
{
    /// <summary> The base message service. </summary>
    protected readonly MessageService MessageService = messageService;

    /// <summary> The actual amassing notification, if it is currently displayed. </summary>
    protected IActiveNotification? CurrentNotification;

    /// <summary> The list of gathered objects. Not a set by design. Each one of those is associated with a StoredNotification. </summary>
    protected readonly List<T> GatheredObjects = [];

    /// <summary> The list of stored notifications, each is associated with a gathered object. </summary>
    protected readonly List<StoredNotification> Messages = [];

    /// <inheritdoc cref="Messages"/>
    public IReadOnlyList<IStoredNotification> Notifications
        => Messages;

    /// <summary> The amount of gathered objects. </summary>
    public int Count
        => GatheredObjects.Count;

    /// <summary> Add a new object. This checks whether the object is already contained, and if it is not, adds a stored message for it and updates or displays the amassed notification. </summary>
    /// <param name="object"> The object to add a notification for. </param>
    protected virtual void AddObject(in T @object)
    {
        lock (GatheredObjects)
        {
            if (GatheredObjects.Contains(@object))
                return;

            GatheredObjects.Add(@object);
            var message = CreateStored(@object);
            Messages.Add(message);
            MessageService.AddMessage(message, true, false);
            if (CurrentNotification is null)
            {
                MessageService.AddMessage(this);
            }
            else
            {
                CurrentNotification.Title         = NotificationTitle;
                CurrentNotification.MinimizedText = CurrentNotification.Title;
                CurrentNotification.ExtendBy(TimeSpan.FromSeconds(30));
            }
        }
    }

    /// <inheritdoc/>
    public abstract NotificationType NotificationType { get; }

    /// <inheritdoc/>
    public abstract string NotificationMessage { get; }

    /// <inheritdoc/>
    public abstract string NotificationTitle { get; }

    /// <summary> Create the stored notification for an object. This is displayed in the messages tab and causes a log entry, but no notification. </summary>
    /// <param name="object"> The object to create a notification for. </param>
    /// <returns> The stored notification. </returns>
    protected abstract StoredNotification CreateStored(in T @object);

    /// <summary> The duration the amassed notification should be visible for. This is reset whenever a new object is amassed. </summary>
    public virtual TimeSpan NotificationDuration
        => TimeSpan.FromSeconds(30);

    /// <summary> The amassed notification does not cause messages. </summary>
    public string LogMessage
        => string.Empty;

    /// <summary> The amassed notification does not cause messages. </summary>
    public SeString ChatMessage
        => SeString.Empty;

    /// <summary> The amassed notification does not cause messages. </summary>
    public StringU8 StoredMessage
        => StringU8.Empty;

    /// <summary> The amassed notification does not cause messages. </summary>
    public StringU8 StoredTooltip
        => StringU8.Empty;

    /// <inheritdoc cref="OnNotificationActions"/>
    public virtual void NotificationActions(INotificationDrawArgs args)
    { }

    /// <inheritdoc/>
    public void OnNotificationActions(INotificationDrawArgs args)
    {
        // Update this since we can not be sure it has been invoked before drawing the notification otherwise.
        ImSharpPerFrame.OnUpdate();
        NotificationActions(args);
    }

    /// <summary> Set the displayed notification, subscribe to dismissal and set the minimized title. </summary>
    public void OnNotificationCreated(IActiveNotification notification)
    {
        CurrentNotification               =  notification;
        CurrentNotification.Dismiss       += OnNotificationDismissedInternal;
        CurrentNotification.MinimizedText =  CurrentNotification.Title;
    }


    /// <summary> Clear all gathered objects on dismissal and remove the displayed notification. </summary>
    private void OnNotificationDismissedInternal(INotificationDismissArgs args)
    {
        lock (GatheredObjects)
        {
            if (args.Notification != CurrentNotification)
                return;

            OnNotificationDismissed(args);
            GatheredObjects.Clear();
            CurrentNotification = null;
        }
    }

    /// <summary> Additional actions when the amassed notification is dismissed. </summary>
    /// <param name="args"> The dismissal arguments</param>
    /// <remarks> This is invoked before the current notification is set to null. </remarks>
    protected virtual void OnNotificationDismissed(INotificationDismissArgs args)
    { }

    /// <summary> A stored notification type that also provides its parent object. </summary>
    public interface IStoredNotification : IMessage
    {
        /// <summary> The object causing this notification. </summary>
        public T Object { get; }

        /// <summary> Remove this message from the messages tab. This will also remove it from its parent. </summary>
        public void Remove();
    }

    /// <summary> The base class for stored notifications. Those cause no actual notifications, only log and message entries, and should be created per object. </summary>
    /// <param name="parent"> The parent amassed notification. </param>
    /// <param name="object"> The object causing this notification. </param>
    protected abstract class StoredNotification(AmassingNotification<T> parent, T @object) : IStoredNotification
    {
        /// <inheritdoc/>
        public T Object { get; } = @object;

        /// <inheritdoc/>
        public NotificationType NotificationType
            => parent.NotificationType;

        /// <inheritdoc/>
        public string NotificationMessage
            => string.Empty;

        /// <inheritdoc/>
        public string NotificationTitle
            => string.Empty;

        /// <inheritdoc/>
        public TimeSpan NotificationDuration
            => TimeSpan.Zero;

        /// <inheritdoc/>
        public abstract string LogMessage { get; }

        /// <inheritdoc/>
        public SeString ChatMessage
            => SeString.Empty;

        /// <inheritdoc/>
        public abstract StringU8 StoredMessage { get; }

        /// <inheritdoc/>
        public abstract StringU8 StoredTooltip { get; }

        /// <inheritdoc/>
        public void OnNotificationActions(INotificationDrawArgs args)
        { }

        /// <inheritdoc/>
        public void Remove()
            => parent.MessageService.RemoveMessages((_, message) => message == this);

        /// <summary> Remove the associated object from the gathered objects when this message is dismissed, and update the notification if it is currently displayed. </summary>
        public virtual void OnRemoval()
        {
            lock (parent.GatheredObjects)
            {
                if (parent.Messages.Remove(this))
                    parent.GatheredObjects.Remove(Object);
                if (parent.CurrentNotification is { } notification)
                {
                    if (parent.GatheredObjects.Count is 0)
                        notification.DismissNow();
                    notification.Title         = parent.NotificationTitle;
                    notification.MinimizedText = notification.Title;
                }
            }
        }
    }
}
