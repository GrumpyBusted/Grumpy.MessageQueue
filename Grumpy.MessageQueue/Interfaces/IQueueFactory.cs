using Grumpy.MessageQueue.Enum;

namespace Grumpy.MessageQueue.Interfaces
{
    /// <summary>
    /// Message Queue Factory
    /// </summary>
    public interface IQueueFactory
    {
        /// <summary>
        /// Create an instance of a Locale Message Queue
        /// </summary>
        /// <param name="name">Queue Name</param>
        /// <param name="privateQueue">Should Queue be used as a private queue</param>
        /// <param name="localeQueueMode">Durable or not and if to Create</param>
        /// <param name="transactional">Transactional Queue</param>
        /// <param name="accessMode">Queue Access Mode</param>
        /// <returns>The Queue</returns>
        ILocaleQueue CreateLocale(string name, bool privateQueue, LocaleQueueMode localeQueueMode,  bool transactional, AccessMode accessMode);

        /// <summary>
        /// Create an instance of a Remote Message Queue
        /// </summary>
        /// <param name="serverName">Server Name</param>
        /// <param name="name">Queue Name</param>
        /// <param name="privateQueue">Should Queue be used as a private queue</param>
        /// <param name="remoteQueueMode">Durable or not Durable</param>
        /// <param name="transactional">Transactional Queue</param>
        /// <param name="accessMode">Queue Access Mode</param>
        /// <returns>The Queue</returns>
        IRemoteQueue CreateRemote(string serverName, string name, bool privateQueue, RemoteQueueMode remoteQueueMode, bool transactional, AccessMode accessMode);
    }
}