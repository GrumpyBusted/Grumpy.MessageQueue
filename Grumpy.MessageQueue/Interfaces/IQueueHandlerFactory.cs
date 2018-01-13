namespace Grumpy.MessageQueue.Interfaces
{
    /// <summary>
    /// Queue Handler Factory
    /// </summary>
    public interface IQueueHandlerFactory
    {
        /// <summary>
        /// Create Queue Handler for receiving Messages from a Queue and triggering call back methods
        /// </summary>
        /// <returns>Queue Handler</returns>
        IQueueHandler Create();
    }
}