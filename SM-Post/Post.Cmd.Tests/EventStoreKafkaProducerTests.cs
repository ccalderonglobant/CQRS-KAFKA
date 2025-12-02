using CQRS.Core.Domain;
using CQRS.Core.Events;
using CQRS.Core.Exception;
using CQRS.Core.Producers;
using Moq;
using Post.Cmd.Infrastructure.Stores;
using Post.Common.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post.Cmd.Tests
{
    [TestFixture]
    public class EventStoreKafkaProducerTests
    {
        [Test]
        public async Task SaveEventsAsync_NewAggregate_PersistsAndProducesEachEvent()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();

            var eventStoreRepoMock = new Mock<IEventStoreRepository>();
            var eventProducerMock = new Mock<IEventProducer>();

            // New aggregate
            eventStoreRepoMock
                .Setup(r => r.FindByAggregateId(aggregateId))
                .ReturnsAsync(new List<EventModel>());

            var eventStore = new EventStore(eventStoreRepoMock.Object, eventProducerMock.Object);

            var @event = new PostCreatedEvent
            {
                Id = aggregateId,
                Author = "cesar",
                Message = "hello kafka"
            };

            var events = new List<BaseEvent> { @event };

            // Act
            await eventStore.SaveEventsAsync(aggregateId, events, expectedVersion: -1);

            // Assert
            eventStoreRepoMock.Verify(
                r => r.SaveAsync(It.IsAny<EventModel>()),
                Times.Exactly(events.Count));

            // It must produce the event to Kafka using the configured topic.
            eventProducerMock.Verify(
                p => p.ProduceAsync(
                    "SocialMediaPostEvents",
                    It.IsAny<BaseEvent>()),
                Times.Exactly(events.Count));
        }

        [Test]
        public void SaveEventsAsync_WithWrongExpectedVersion_ThrowsConcurrencyException()
        {
            // Arrange
            var aggregateId = Guid.NewGuid();

            var previousEvent = new EventModel
            {
                AggregateIdentifier = aggregateId,
                Version = 3,
                EventData = new PostCreatedEvent { Id = aggregateId }
            };

            var eventStoreRepoMock = new Mock<IEventStoreRepository>();
            var eventProducerMock = new Mock<IEventProducer>();

            eventStoreRepoMock
                .Setup(r => r.FindByAggregateId(aggregateId))
                .ReturnsAsync(new List<EventModel> { previousEvent });

            var eventStore = new EventStore(eventStoreRepoMock.Object, eventProducerMock.Object);

            var newEvent = new PostCreatedEvent { Id = aggregateId };

            // expectedVersion != lastVersion → throws ConcurrencyException
            Assert.ThrowsAsync<ConcurrencyException>(
                async () => await eventStore.SaveEventsAsync(aggregateId, new[] { newEvent }, expectedVersion: 0));
        }
    }
}
