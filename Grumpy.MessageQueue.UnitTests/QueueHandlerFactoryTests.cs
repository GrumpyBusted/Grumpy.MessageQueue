using FluentAssertions;
using Grumpy.MessageQueue.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
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
            return new QueueHandlerFactory(NullLogger.Instance, Substitute.For<IQueueFactory>());
        }
    }
}