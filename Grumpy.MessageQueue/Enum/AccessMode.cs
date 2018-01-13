namespace Grumpy.MessageQueue.Enum
{
    /// <summary>
    /// Queue Access Mode
    /// </summary>
    public enum AccessMode
    {
        /// <summary>
        /// Queue not connected
        /// </summary>
        None,
        
        /// <summary>
        /// Queue used for sending messages
        /// </summary>
        Send,
        
        /// <summary>
        /// Queue used for receiving messages
        /// </summary>
        Receive
    }
}