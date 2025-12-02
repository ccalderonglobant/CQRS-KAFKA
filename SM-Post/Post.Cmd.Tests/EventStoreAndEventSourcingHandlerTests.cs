using CQRS.Core.Domain;
using CQRS.Core.Events;
using CQRS.Core.Exception;
using CQRS.Core.Producers;
using Moq;
using Post.Cmd.Infrastructure.Handlers;
using Post.Cmd.Infrastructure.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post.Cmd.Tests
{
    internal class InMemoryEventStoreRepositoryForNotFound : IEventStoreRepository
    {
        private readonly List<EventModel> _events = new();

        public Task SaveAsync(EventModel @event)
        {
            _events.Add(@event);
            return Task.CompletedTask;
        }

        public Task<List<EventModel>> FindByAggregateId(Guid aggregateId)
        {
            // Simulates a non-existent aggregate
            return Task.FromResult(new List<EventModel>());
        }
    }

    [TestFixture]
    public class EventStoreAndEventSourcingHandlerTests
    {
        [Test]
        public void GetEventsAsync_AggregateNotFound_ThrowsAggregateNotFoundException()
        {
            // Arrange
            var repo = new InMemoryEventStoreRepositoryForNotFound();

            var producerMock = new Mock<IEventProducer>();
            var eventStore = new EventStore(repo, producerMock.Object);

            var unknownId = Guid.NewGuid();

            // Act + Assert
            Assert.ThrowsAsync<AggregateNotFoundException>(async () =>
                await eventStore.GetEventsAsync(unknownId));
        }

        [Test]
        public void GetByIdAsync_UnknownAggregateId_ThrowsAggregateNotFoundException()
        {
            // Arrange
            var repo = new InMemoryEventStoreRepositoryForNotFound();
            var producerMock = new Mock<IEventProducer>();
            var eventStore = new EventStore(repo, producerMock.Object);

            var eventSourcingHandler = new EventSourcingHandler(eventStore);
            var unknownId = Guid.NewGuid();

            // Act + Assert
            Assert.ThrowsAsync<AggregateNotFoundException>(async () =>
                await eventSourcingHandler.GetByIdAsync(unknownId));
        }
    }
}
