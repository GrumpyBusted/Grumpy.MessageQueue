using System.Collections.Generic;
using Grumpy.MessageQueue.Interfaces;

namespace Grumpy.MessageQueue.TestTools
{
    /// <inheritdoc />
    /// <summary>
    /// Test Implementation of Queue handler Factory
    /// </summary>
    public class TestQueueHandlerFactory : IQueueHandlerFactory
    {
        /// <summary>
        /// Add messages to this list before the instance is called, then the client will "feel" like it is receiving the messages
        /// </summary>
        public ICollection<object> Messages { get; }

        /// <inheritdoc />
        public TestQueueHandlerFactory()
        {
            Messages = new List<object>();
        }

        /// <inheritdoc />
        public IQueueHandler Create()
        {
            return new TestQueueHandler(Messages);
        }
    }
}