using CQRS.Core.Consumers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Post.Query.Infrastructure.Consumers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post.Query.Tests
{
    [TestFixture]
    public class ConsumerHostedServiceTests
    {
        [Test]
        public async Task StartAsync_ResolvesEventConsumer_AndCallsConsumeWithCorrectTopic()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<ConsumerHostedService>>();

            // Mocks for the scope.
            var rootProviderMock = new Mock<IServiceProvider>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();
            var scopedProviderMock = new Mock<IServiceProvider>();
            var eventConsumerMock = new Mock<IEventConsumer>();

            // When the CreateScope() extension is called, internally it:
            // - requests an IServiceScopeFactory from the root provider
            // - and then calls CreateScope() on that factory
            rootProviderMock
                .Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                .Returns(scopeFactoryMock.Object);

            scopeFactoryMock
                .Setup(sf => sf.CreateScope())
                .Returns(scopeMock.Object);

            scopeMock
                .SetupGet(s => s.ServiceProvider)
                .Returns(scopedProviderMock.Object);

            // Inside the scope, GetRequiredService<IEventConsumer>() requests an IEventConsumer.
            scopedProviderMock
                .Setup(sp => sp.GetService(typeof(IEventConsumer)))
                .Returns(eventConsumerMock.Object);

            var hostedService = new ConsumerHostedService(
                loggerMock.Object,
                rootProviderMock.Object);

            var cancellationToken = CancellationToken.None;

            // Act
            await hostedService.StartAsync(cancellationToken);

            // We wait a bit for the Task.Run to execute.
            await Task.Delay(50);

            // Assert
            // We verify that the scope was created.
            scopeFactoryMock.Verify(sf => sf.CreateScope(), Times.Once);

            // And that the consumer was called with the correct topic.
            eventConsumerMock.Verify(
                c => c.Consume("SocialMediaPostEvents"),
                Times.Once);
        }
    }
}
