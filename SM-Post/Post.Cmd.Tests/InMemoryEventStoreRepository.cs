using CQRS.Core.Domain;
using CQRS.Core.Events;
using CQRS.Core.Producers;
using Moq;
using Post.Cmd.Api.Commands;
using Post.Cmd.Domain.Aggregates;
using Post.Cmd.Infrastructure.Handlers;
using Post.Cmd.Infrastructure.Stores;
using Post.Common.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post.Cmd.Tests
{
    internal class InMemoryEventStoreRepository : IEventStoreRepository
    {
        private readonly List<EventModel> _events = new();

        public Task SaveAsync(EventModel @event)
        {
            // We don't use the Mongo Id in the tests, so it's not critical..
            _events.Add(@event);
            return Task.CompletedTask;
        }

        public Task<List<EventModel>> FindByAggregateId(Guid aggregateId)
        {
            var list = _events
                .Where(e => e.AggregateIdentifier == aggregateId)
                .ToList();

            return Task.FromResult(list);
        }

        public IReadOnlyList<EventModel> Events => _events;
    }

    [TestFixture]
    public class CommandEndToEndTests
    {
        [Test]
        public async Task NewPostCommand_FlowsThrough_CommandHandler_EventStore_Producer_And_Rehydration()
        {
            // Arrange
            var repo = new InMemoryEventStoreRepository();

            var producerMock = new Mock<IEventProducer>();
            producerMock
                .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<BaseEvent>()))
                .Returns(Task.CompletedTask);

            // A real EventStore, but using an in-memory repository and a mocked producer.
            var eventStore = new EventStore(repo, producerMock.Object);

            // Real EventSourcingHandler.
            var eventSourcingHandler = new EventSourcingHandler(eventStore);

            // Real CommandHandler.
            var commandHandler = new CommandHandler(eventSourcingHandler);

            var postId = Guid.NewGuid();
            var author = "cesar";
            var message = "hello world from command side";

            var command = new NewPostCommand
            {
                Id = postId,
                Author = author,
                Message = message
            };

            // Act
            await commandHandler.HandleAsync(command);

            // Assert 1: An event was saved in the in-memory event store.
            Assert.That(repo.Events.Count, Is.EqualTo(1));

            var stored = repo.Events.Single();
            Assert.That(stored.AggregateIdentifier, Is.EqualTo(postId));
            Assert.That(stored.AggregateType, Is.EqualTo(nameof(PostAggregate)));
            Assert.That(stored.EventData, Is.TypeOf<PostCreatedEvent>());

            var created = (PostCreatedEvent)stored.EventData;
            Assert.That(created.Id, Is.EqualTo(postId));
            Assert.That(created.Author, Is.EqualTo(author));
            Assert.That(created.Message, Is.EqualTo(message));

            // Assert 2: The producer (Kafka) was called with the correct topic and the correct event.
            producerMock.Verify(
            p => p.ProduceAsync(
                "SocialMediaPostEvents",
                It.Is<BaseEvent>(e =>
                    e is PostCreatedEvent &&
                    ((PostCreatedEvent)e).Id == postId &&
                    ((PostCreatedEvent)e).Author == author &&
                    ((PostCreatedEvent)e).Message == message)),
            Times.Once);

            // Assert 3: We can rehydrate the aggregate from the events.
            var rehydrated = await eventSourcingHandler.GetByIdAsync(postId);

            Assert.That(rehydrated, Is.Not.Null);
            Assert.That(rehydrated.Id, Is.EqualTo(postId));
            Assert.That(rehydrated.Version, Is.EqualTo(0)); // first event → version 0
        }

        [Test]
        public async Task AddCommentCommand_AppendsCommentEvent_And_ProducesSecondMessage()
        {
            // Arrange: Same wiring as the previous test
            var repo = new InMemoryEventStoreRepository();

            var producerMock = new Mock<IEventProducer>();
            producerMock
                .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<BaseEvent>()))
                .Returns(Task.CompletedTask);

            var eventStore = new EventStore(repo, producerMock.Object);
            var eventSourcingHandler = new EventSourcingHandler(eventStore);
            var commandHandler = new CommandHandler(eventSourcingHandler);

            var postId = Guid.NewGuid();

            var newPost = new NewPostCommand
            {
                Id = postId,
                Author = "cesar",
                Message = "post with comments"
            };

            var addComment = new AddCommentCommand
            {
                Id = postId,
                Comment = "nice post!",
                Username = "user1"
            };

            // Act: First, we create the post.
            await commandHandler.HandleAsync(newPost);

            // Then we add a comment.
            await commandHandler.HandleAsync(addComment);

            // Assert 1: Now there are 2 events in the event store.
            Assert.That(repo.Events.Count, Is.EqualTo(2));

            var first = repo.Events[0];
            var second = repo.Events[1];

            Assert.That(first.EventData, Is.TypeOf<PostCreatedEvent>());
            Assert.That(second.EventData, Is.TypeOf<CommentAddedEvent>());

            var commentEvent = (CommentAddedEvent)second.EventData;
            Assert.That(commentEvent.Id, Is.EqualTo(postId));
            Assert.That(commentEvent.Comment, Is.EqualTo("nice post!"));
            Assert.That(commentEvent.Username, Is.EqualTo("user1"));
            Assert.That(commentEvent.CommentId, Is.Not.EqualTo(Guid.Empty));

            // Assert 2: The producer was called twice (PostCreated + CommentAdded).
            producerMock.Verify(
                p => p.ProduceAsync(
                    "SocialMediaPostEvents",
                    It.IsAny<BaseEvent>()),
                Times.Exactly(2));

            // And specifically once with the correct CommentAddedEvent.
            producerMock.Verify(
             p => p.ProduceAsync(
                 "SocialMediaPostEvents",
                 It.Is<BaseEvent>(e =>
                     e is CommentAddedEvent &&
                     ((CommentAddedEvent)e).Id == postId &&
                     ((CommentAddedEvent)e).Comment == "nice post!" &&
                     ((CommentAddedEvent)e).Username == "user1")),
             Times.Once);

            // Assert 3: The aggregate version should be 1 (two events: versions 0 and 1).
            var aggregate = await eventSourcingHandler.GetByIdAsync(postId);
            Assert.That(aggregate.Version, Is.EqualTo(1));
        }
    }
}
