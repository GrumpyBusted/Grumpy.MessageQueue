namespace Grumpy.MessageQueue.Interfaces
{
    /// <inheritdoc />
    /// <summary>
    /// Message Queue on Locale Machine
    /// </summary>
    public interface ILocaleQueue : IQueue
    {
        /// <summary>
        /// Create Message Queue
        /// </summary>
        void Create();

        /// <summary>
        /// Delete Message Queue
        /// </summary>
        void Delete();

        /// <summary>
        /// Indicate id the Message Queue exists
        /// </summary>
        /// <returns>True = Exists</returns>
        bool Exists();
    }
}
