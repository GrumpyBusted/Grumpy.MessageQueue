using System.Collections.Generic;
using System.Linq;

namespace Grumpy.MessageQueue.Msmq.Extensions
{
    internal static class MessageQueueListExtensions
    {
        public static IEnumerable<string> SelectNames(this IEnumerable<System.Messaging.MessageQueue> list, string prefix)
        {
            return list.Where(q => q.QueueName.StartsWith(prefix)).Select(q => q.QueueName.Substring(prefix.Length));
        }
    }
}