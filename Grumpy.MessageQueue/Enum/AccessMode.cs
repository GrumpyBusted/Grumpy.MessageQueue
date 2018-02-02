namespace Grumpy.MessageQueue.Enum
{
    /// <summary>
    /// Queue Access Mode
    /// </summary>
    public enum AccessMode
    {
        /// <summary>
        /// Queue used for sending messages
        /// </summary>
        Send,
        
        /// <summary>
        /// Queue used for receiving messages
        /// </summary>
        Receive,
        
        /// <summary>
        /// Queue used for both receiving and sending messages
        /// </summary>
        SendAndReceive
    }
}