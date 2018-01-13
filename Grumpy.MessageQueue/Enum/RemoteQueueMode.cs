namespace Grumpy.MessageQueue.Enum
{
    /// <summary>
    /// Create mode for Queue for Remote Queues
    /// </summary>
    public enum RemoteQueueMode
    {
        /// <summary>
        /// Durable Queue
        /// </summary>
        Durable, 

        /// <summary>
        /// None-Durable/Temporary Queue
        /// </summary>
        Temporary
    }
}