namespace Grumpy.MessageQueue.Msmq.Interfaces
{
    /// <summary>
    /// Message Queue Transaction Factory
    /// </summary>
    public interface IMessageQueueTransactionFactory 
    {
        /// <summary>
        /// Create Transaction
        /// </summary>
        IMessageQueueTransaction Create();
    }
}