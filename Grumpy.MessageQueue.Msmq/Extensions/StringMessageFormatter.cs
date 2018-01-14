using System.IO;
using System.Messaging;
using System.Text;

namespace Grumpy.MessageQueue.Msmq.Extensions
{
    internal class StringMessageFormatter : IMessageFormatter
    {
        public object Clone()
        {
            return new StringMessageFormatter();
        }

        public bool CanRead(Message message)
        {
            return true;
        }

        public object Read(Message message)
        {
            var streamReader = new StreamReader(message.BodyStream, Encoding.UTF8);

            return streamReader.ReadToEnd();
        }

        public void Write(Message message, object obj)
        {
            var buffer = Encoding.UTF8.GetBytes(obj.ToString());

            message.BodyStream = new MemoryStream(buffer);
        }
    }
}