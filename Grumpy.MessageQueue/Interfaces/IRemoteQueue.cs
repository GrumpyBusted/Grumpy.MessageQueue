namespace Grumpy.MessageQueue.Interfaces
{
    /// <inheritdoc />
    /// <summary>
    /// Message Queue on Remote Server
    /// </summary>
    public interface IRemoteQueue : IQueue
    {
        /// <summary>
        /// Server Name
        /// </summary>
        string ServerName { get; }
    }
}