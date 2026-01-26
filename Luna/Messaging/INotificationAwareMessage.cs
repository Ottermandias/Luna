using Dalamud.Interface.ImGuiNotification;

namespace Luna;

/// <summary> A message that is aware of the <see cref="IActiveNotification"/>s created from it. </summary>
public interface INotificationAwareMessage : IMessage
{
    /// <summary> Will be called when a notification is created. </summary>
    /// <param name="notification"> The <see cref="IActiveNotification"/> that represents the newly-created notification. </param>
    public void OnNotificationCreated(IActiveNotification notification);
}
