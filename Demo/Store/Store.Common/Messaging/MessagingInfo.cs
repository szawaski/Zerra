namespace Store.Common.Messaging
{
    /// <summary>
    /// Describes how a service's commands and events travel, through a message broker or directly. Registered as a bus service so query handlers can report it.
    /// </summary>
    public interface IMessagingInfo
    {
        string Description { get; }
    }

    public sealed class MessagingInfo : IMessagingInfo
    {
        public string Description { get; }

        public MessagingInfo(string description)
        {
            this.Description = description;
        }
    }
}
