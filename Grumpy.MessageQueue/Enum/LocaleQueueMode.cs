namespace Grumpy.MessageQueue.Enum
{
    /// <summary>
    /// Create mode for Queue for Locale Queues
    /// </summary>
    public enum LocaleQueueMode
    {
        /// <summary>
        /// Durable Queue - Don't create
        /// </summary>
        Durable, 

        /// <summary>
        /// Durable Queue - Create it not exists
        /// </summary>
        DurableCreate, 

        /// <summary>
        /// None-Durable/Temporary Queue - Create and Delete when queue object are disposed
        /// </summary>
        TemporaryMaster, 

        /// <summary>
        /// None-Durable/Temporary Queue - Assume that the master will handle create and delete of the queue
        /// </summary>
        TemporarySlave 
    }
}