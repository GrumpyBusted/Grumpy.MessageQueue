using FluentAssertions;
using Grumpy.MessageQueue.Interfaces;
using NSubstitute;
using Xunit;

namespace Grumpy.MessageQueue.UnitTests
{
    public class QueueHandlerFactoryTests
    {
        [Fact]
        public void QueueHandlerFactoryCanCreateInstance()
        {
            CreateCut().Create().Should().NotBeNull();
        }

        private static IQueueHandlerFactory CreateCut()
        {
            return new QueueHandlerFactory(Substitute.For<IQueueFactory>());
        }
    }
}