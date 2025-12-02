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
    public class PostAggregateTests
    {
        [Test]
        public void Ctor_WithValidData_RaisesPostCreatedEvent()
        {
            // Arrange
            var id = Guid.NewGuid();
            var author = "cesar";
            var message = "hello world";

            // Act
            var aggregate = new PostAggregate(id, author, message);
            var changes = aggregate.GetUncommittedChanges().ToList();

            // Assert
            Assert.That(aggregate.Id, Is.EqualTo(id));
            Assert.That(changes, Has.Count.EqualTo(1));

            var @event = changes.OfType<PostCreatedEvent>().Single();
            Assert.That(@event.Id, Is.EqualTo(id));
            Assert.That(@event.Author, Is.EqualTo(author));
            Assert.That(@event.Message, Is.EqualTo(message));
        }

        [Test]
        public void EditMessage_WhenPostIsInactive_Throws()
        {
            // Arrange
            var aggregate = new PostAggregate(); // _active = false

            // Act + Assert
            Assert.Throws<InvalidOperationException>(
                () => aggregate.EditMessage("new message", "cesar"));
        }

        [Test]
        public void EditMessage_WhenUserIsNotAuthor_ThrowsInvalidOperationException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "original message");

            // Act + Assert
            Assert.Throws<InvalidOperationException>(() =>
                aggregate.EditMessage("new message", "otherUser"));
        }

        [Test]
        public void EditMessage_WhenPostIsActive_RaisesMessageUpdatedEvent()
        {
            // Arrange
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "message original");
            var newMessage = "message edited";

            // Act
            aggregate.EditMessage(newMessage, "cesar");
            var changes = aggregate.GetUncommittedChanges().ToList();

            // Assert
            var @event = changes.OfType<MessageUpdatedEvent>().Last();
            Assert.That(@event.Id, Is.EqualTo(id));
            Assert.That(@event.Message, Is.EqualTo(newMessage));
        }

        [Test]
        public void EditMessage_EmptyMessage_ThrowsInvalidOperationException()
        {
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "original message");

            Assert.Throws<InvalidOperationException>(() =>
                aggregate.EditMessage(string.Empty, "cesar"));
        }

        [Test]
        public void LikePost_WhenInactive_Throws()
        {
            var aggregate = new PostAggregate();

            Assert.Throws<InvalidOperationException>(() => aggregate.LikePost());
        }

        [Test]
        public void LikePost_WhenActive_RaisesPostLikedEvent()
        {
            // Arrange
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "message");

            // Act
            aggregate.LikePost();
            var changes = aggregate.GetUncommittedChanges().ToList();

            // Assert
            var @event = changes.OfType<PostLikedEvent>().Last();
            Assert.That(@event.Id, Is.EqualTo(id));
        }

        [Test]
        public void AddComment_WhenInactive_Throws()
        {
            var aggregate = new PostAggregate();

            Assert.Throws<InvalidOperationException>(
                () => aggregate.AddComment("nice post", "user1"));
        }

        [Test]
        public void AddComment_WhenActive_RaisesCommentAddedEvent()
        {
            // Arrange
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "message");

            // Act
            aggregate.AddComment("nice post", "user1");
            var changes = aggregate.GetUncommittedChanges().ToList();

            // Assert
            var @event = changes.OfType<CommentAddedEvent>().Last();
            Assert.That(@event.Id, Is.EqualTo(id));
            Assert.That(@event.Comment, Is.EqualTo("nice post"));
            Assert.That(@event.Username, Is.EqualTo("user1"));
            Assert.That(@event.CommentId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void EditComment_WhenUserIsNotOwner_Throws()
        {
            // Arrange
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "message");

            aggregate.AddComment("nice post", "owner");
            var commentEvent = aggregate.GetUncommittedChanges()
                                        .OfType<CommentAddedEvent>()
                                        .Last();

            // Act + Assert
            Assert.Throws<InvalidOperationException>(() =>
                aggregate.EditComment(commentEvent.CommentId, "edited", "otherUser"));
        }

        [Test]
        public void DeletePost_SetsPostAsInactiveAndRaisesPostRemovedEvent()
        {
            // Arrange
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "message");

            // Act
            aggregate.DeletePost("cesar");
            var changes = aggregate.GetUncommittedChanges().ToList();

            // Assert
            var @event = changes.OfType<PostRemovedEvent>().Last();
            Assert.That(@event.Id, Is.EqualTo(id));
        }

        [Test]
        public void AddComment_EmptyComment_ThrowsInvalidOperationException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "message");

            // Act + Assert
            Assert.Throws<InvalidOperationException>(() =>
                aggregate.AddComment(string.Empty, "user1"));
        }

        [Test]
        public void EditComment_CommentNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "message");
            var nonExistingCommentId = Guid.NewGuid();

            // Act + Assert
            Assert.Throws<KeyNotFoundException>(() =>
                aggregate.EditComment(nonExistingCommentId, "edited", "user1"));
        }

        [Test]
        public void RemoveComment_CommentNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "message");
            var nonExistingCommentId = Guid.NewGuid();

            // Act + Assert
            Assert.Throws<KeyNotFoundException>(() =>
                aggregate.RemoveComment(nonExistingCommentId, "user1"));
        }

        [Test]
        public void DeletePost_InactivePost_ThrowsInvalidOperationException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "message");

            // We mark the post as inactive.
            aggregate.DeletePost("cesar");
            var changes = aggregate.GetUncommittedChanges(); // To close changes

            // Act + Assert
            Assert.Throws<InvalidOperationException>(() =>
                aggregate.DeletePost("cesar"));
        }

        [Test]
        public void DeletePost_DifferentUser_ThrowsInvalidOperationException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "message");

            // Act + Assert
            Assert.Throws<InvalidOperationException>(() =>
                aggregate.DeletePost("otherUser"));
        }

        [Test]
        public void RemoveComment_WhenUserIsNotOwner_ThrowsInvalidOperationException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var aggregate = new PostAggregate(id, "cesar", "message");

            aggregate.AddComment("nice post", "owner");
            var addedEvent = aggregate.GetUncommittedChanges()
                                      .OfType<CommentAddedEvent>()
                                      .Last();
            var commentId = addedEvent.CommentId;

            // Act + Assert
            Assert.Throws<InvalidOperationException>(() =>
                aggregate.RemoveComment(commentId, "otherUser"));
        }
    }
}
