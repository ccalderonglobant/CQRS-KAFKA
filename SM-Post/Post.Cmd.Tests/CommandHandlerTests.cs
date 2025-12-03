using CQRS.Core.Domain;
using CQRS.Core.Handlers;
using Moq;
using Post.Cmd.Api.Commands;
using Post.Cmd.Domain.Aggregates;
using Post.Common.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post.Cmd.Tests
{
    [TestFixture]
    public class CommandHandlerTests
    {
        private Mock<IEventSourcingHandler<PostAggregate>> _eventSourcingHandlerMock = null!;
        private Mock<IPostAggregateFactory> _postAggregateFactoryMock = null!;
        private CommandHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _eventSourcingHandlerMock = new Mock<IEventSourcingHandler<PostAggregate>>();
            _postAggregateFactoryMock = new Mock<IPostAggregateFactory>();
            _handler = new CommandHandler(_eventSourcingHandlerMock.Object, _postAggregateFactoryMock.Object);
        }

        [Test]
        public async Task HandleAsync_NewPostCommand_ShouldCreateAggregateAndSave()
        {
            // Arrange
            var command = new NewPostCommand
            {
                Id = Guid.NewGuid(),
                Author = "cesar",
                Message = "hello world"
            };

            PostAggregate? savedAggregate = null;

            _eventSourcingHandlerMock
                .Setup(x => x.SaveAsync(It.IsAny<AggregateRoot>()))
                .Callback<AggregateRoot>(agg => savedAggregate = (PostAggregate)agg)
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleAsync(command);

            // Assert
            _eventSourcingHandlerMock.Verify(
                x => x.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);

            _eventSourcingHandlerMock.Verify(
                x => x.SaveAsync(It.IsAny<AggregateRoot>()),
                Times.Once);

            Assert.That(savedAggregate, Is.Not.Null);
            Assert.That(savedAggregate!.Id, Is.EqualTo(command.Id));

            var createdEvent = savedAggregate.GetUncommittedChanges()
                                             .OfType<PostCreatedEvent>()
                                             .Single();

            Assert.That(createdEvent.Author, Is.EqualTo(command.Author));
            Assert.That(createdEvent.Message, Is.EqualTo(command.Message));
        }

        [Test]
        public async Task HandleAsync_EditMessageCommand_WithAuthorUser_EditsAndSaves()
        {
            // Arrange
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "original");

            _eventSourcingHandlerMock
                .Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync(aggregate);

            AggregateRoot? saved = null;
            _eventSourcingHandlerMock
                .Setup(x => x.SaveAsync(It.IsAny<AggregateRoot>()))
                .Callback<AggregateRoot>(agg => saved = agg)
                .Returns(Task.CompletedTask);

            var command = new EditMessageCommand
            {
                Id = id,
                Message = "edited",
                Username = "cesar"
            };

            // Act
            await _handler.HandleAsync(command);

            // Assert
            _eventSourcingHandlerMock.Verify(x => x.GetByIdAsync(id), Times.Once);
            _eventSourcingHandlerMock.Verify(x => x.SaveAsync(It.IsAny<AggregateRoot>()), Times.Once);

            var savedAggregate = (PostAggregate)saved!;
            var @event = savedAggregate.GetUncommittedChanges()
                                       .OfType<MessageUpdatedEvent>()
                                       .Last();

            Assert.That(@event.Message, Is.EqualTo("edited"));
        }

        [Test]
        public void HandleAsync_EditMessageCommand_WithDifferentUser_ThrowsInvalidOperationException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "original");

            _eventSourcingHandlerMock
                .Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync(aggregate);

            var command = new EditMessageCommand
            {
                Id = id,
                Message = "edited",
                Username = "otherUser"
            };

            // Act + Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _handler.HandleAsync(command));

            _eventSourcingHandlerMock.Verify(x => x.SaveAsync(It.IsAny<AggregateRoot>()), Times.Never);
        }

        [Test]
        public async Task HandleAsync_LikePostCommand_ShouldLikeAndSave()
        {
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "message");

            _eventSourcingHandlerMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(aggregate);

            await _handler.HandleAsync(new LikePostCommand { Id = id });

            _eventSourcingHandlerMock.Verify(x => x.GetByIdAsync(id), Times.Once);
            _eventSourcingHandlerMock.Verify(x => x.SaveAsync(It.IsAny<AggregateRoot>()), Times.Once);

            var likedEvent = aggregate.GetUncommittedChanges()
                                      .OfType<PostLikedEvent>()
                                      .Last();

            Assert.That(likedEvent.Id, Is.EqualTo(id));
        }

        [Test]
        public async Task HandleAsync_AddCommentCommand_ShouldAddCommentAndSave()
        {
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "message");

            _eventSourcingHandlerMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(aggregate);

            var command = new AddCommentCommand
            {
                Id = id,
                Comment = "nice post",
                Username = "user1"
            };

            await _handler.HandleAsync(command);

            _eventSourcingHandlerMock.Verify(x => x.GetByIdAsync(id), Times.Once);
            _eventSourcingHandlerMock.Verify(x => x.SaveAsync(It.IsAny<AggregateRoot>()), Times.Once);

            var @event = aggregate.GetUncommittedChanges()
                                  .OfType<CommentAddedEvent>()
                                  .Last();

            Assert.That(@event.Comment, Is.EqualTo("nice post"));
            Assert.That(@event.Username, Is.EqualTo("user1"));
        }

        [Test]
        public async Task HandleAsync_DeletePostCommand_ShouldDeletePostAndSave()
        {
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "message");

            _eventSourcingHandlerMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(aggregate);

            var command = new DeletePostCommand
            {
                Id = id,
                Username = "cesar"
            };

            await _handler.HandleAsync(command);

            _eventSourcingHandlerMock.Verify(x => x.GetByIdAsync(id), Times.Once);
            _eventSourcingHandlerMock.Verify(x => x.SaveAsync(It.IsAny<AggregateRoot>()), Times.Once);

            var @event = aggregate.GetUncommittedChanges()
                                  .OfType<PostRemovedEvent>()
                                  .Last();

            Assert.That(@event.Id, Is.EqualTo(id));
        }
    }
}
