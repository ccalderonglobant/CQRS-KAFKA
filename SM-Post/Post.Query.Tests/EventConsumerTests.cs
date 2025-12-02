using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Moq;
using Post.Common.Events;
using Post.Query.Infrastructure.Consumers;
using Post.Query.Infrastructure.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Post.Query.Tests
{
    [TestFixture]
    public class EventConsumerTests
    {
        [Test]
        public void ProcessEvent_WithPostCreatedEvent_CallsEventHandlerOnPostCreated()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var @event = new PostCreatedEvent
            {
                Id = postId,
                Author = "cesar",
                Message = "hello from kafka",
                DatePosted = DateTime.UtcNow
            };

            // We serialize the "normal" event, without any special converter.
            // The converter is used during deserialization (ProcessEvent).
            var json = JsonSerializer.Serialize(@event);

            // Empty config; it's not used in ProcessEvent, but the class requires it.
            var consumerConfig = new ConsumerConfig();
            IOptions<ConsumerConfig> options = Options.Create(consumerConfig);

            var eventHandlerMock = new Mock<IEventHandler>();

            var consumer = new EventConsumer(options, eventHandlerMock.Object);

            // Act
            consumer.ProcessEvent(json);

            // Assert: We verify that On(PostCreatedEvent) was called with the correct data.
            eventHandlerMock.Verify(
                h => h.On(It.Is<PostCreatedEvent>(e =>
                    e.Id == postId &&
                    e.Author == "cesar" &&
                    e.Message == "hello from kafka")),
                Times.Once);
        }

        [Test]
        public void ProcessEvent_WithUnsupportedType_ThrowsJsonException()
        {
            // Arrange: We construct a JSON that has an unsupported Type.
            var payload = new
            {
                Id = Guid.NewGuid(),
                Type = "UnknownEvent",
                Version = 1
            };

            var json = JsonSerializer.Serialize(payload);

            var consumerConfig = new ConsumerConfig();
            IOptions<ConsumerConfig> options = Options.Create(consumerConfig);

            var eventHandlerMock = new Mock<IEventHandler>();
            var consumer = new EventConsumer(options, eventHandlerMock.Object);

            // Act + Assert: The EventJsonConverter should throw a JsonException.
            Assert.Throws<System.Text.Json.JsonException>(() =>
                consumer.ProcessEvent(json));

            // And it should not call any On(...) method.
            eventHandlerMock.VerifyNoOtherCalls();
        }
    }
}
