using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Post.Common.Events;
using Post.Query.Infrastructure.DataAccess;
using Post.Query.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post.Query.Tests
{
    [TestFixture]
    public class EventHandlerIntegrationTests
    {
        private DatabaseContextFactory _factory = null!;
        private PostRepository _postRepository = null!;
        private CommentRepository _commentRepository = null!;
        private Infrastructure.Handlers.EventHandler _eventHandler = null!;

        [SetUp]
        public void SetUp()
        {
            var dbName = $"EventHandlerTestsDb_{Guid.NewGuid()}";

            _factory = new DatabaseContextFactory(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            _postRepository = new PostRepository(_factory);
            _commentRepository = new CommentRepository(_factory);

            // Use real EventHandler.
            _eventHandler = new Infrastructure.Handlers.EventHandler(_postRepository, _commentRepository);
        }

        [Test]
        public async Task On_PostCreatedEvent_PersistsPostInDatabase()
        {
            var postId = Guid.NewGuid();
            var @event = new PostCreatedEvent
            {
                Id = postId,
                Author = "cesar",
                Message = "hello world",
                DatePosted = DateTime.UtcNow
            };

            await _eventHandler.On(@event);

            var posts = await _postRepository.ListAllAsync();

            Assert.That(posts.Count, Is.EqualTo(1));

            var post = posts.Single();
            Assert.That(post.PostId, Is.EqualTo(postId));
            Assert.That(post.Author, Is.EqualTo("cesar"));
            Assert.That(post.Message, Is.EqualTo("hello world"));
            Assert.That(post.Likes, Is.EqualTo(0));
        }

        [Test]
        public async Task On_PostLikedEvent_IncrementsLikes()
        {
            var postId = Guid.NewGuid();

            await _eventHandler.On(new PostCreatedEvent
            {
                Id = postId,
                Author = "cesar",
                Message = "post to like",
                DatePosted = DateTime.UtcNow
            });

            await _eventHandler.On(new PostLikedEvent
            {
                Id = postId
            });

            var posts = await _postRepository.ListAllAsync();
            var post = posts.Single();

            Assert.That(post.Likes, Is.EqualTo(1));
        }

        [Test]
        public async Task On_MessageUpdatedEvent_UpdatesMessage()
        {
            var postId = Guid.NewGuid();

            await _eventHandler.On(new PostCreatedEvent
            {
                Id = postId,
                Author = "cesar",
                Message = "old message",
                DatePosted = DateTime.UtcNow
            });

            var updatedMessage = "new updated message";

            await _eventHandler.On(new MessageUpdatedEvent
            {
                Id = postId,
                Message = updatedMessage
            });

            var posts = await _postRepository.ListAllAsync();
            var post = posts.Single();

            Assert.That(post.Message, Is.EqualTo(updatedMessage));
        }

        [Test]
        public async Task On_CommentAddedEvent_AddsCommentToRepository()
        {
            var postId = Guid.NewGuid();

            await _eventHandler.On(new PostCreatedEvent
            {
                Id = postId,
                Author = "cesar",
                Message = "post with comment",
                DatePosted = DateTime.UtcNow
            });

            var commentId = Guid.NewGuid();

            await _eventHandler.On(new CommentAddedEvent
            {
                Id = postId,
                CommentId = commentId,
                Comment = "nice post!",
                Username = "user1",
                CommentDate = DateTime.UtcNow
            });

            var comment = await _commentRepository.GetByIdAsync(commentId);

            Assert.That(comment, Is.Not.Null);
            Assert.That(comment!.CommentId, Is.EqualTo(commentId));
            Assert.That(comment.Comment, Is.EqualTo("nice post!"));
            Assert.That(comment.Username, Is.EqualTo("user1"));
            Assert.That(comment.PostId, Is.EqualTo(postId));
            Assert.That(comment.Edited, Is.False);
        }

        [Test]
        public async Task On_CommentUpdatedEvent_UpdatesExistingComment()
        {
            var postId = Guid.NewGuid();
            var commentId = Guid.NewGuid();

            // We create a post.
            await _eventHandler.On(new PostCreatedEvent
            {
                Id = postId,
                Author = "cesar",
                Message = "post",
                DatePosted = DateTime.UtcNow
            });

            // We create a comment.
            await _eventHandler.On(new CommentAddedEvent
            {
                Id = postId,
                CommentId = commentId,
                Comment = "old comment",
                Username = "user1",
                CommentDate = DateTime.UtcNow
            });

            // We update the comment.
            var newText = "edited comment";
            var editDate = DateTime.UtcNow.AddMinutes(5);

            await _eventHandler.On(new CommentUpdatedEvent
            {
                Id = postId,
                CommentId = commentId,
                Comment = newText,
                Username = "user1",
                EditDate = editDate
            });

            var updatedComment = await _commentRepository.GetByIdAsync(commentId);

            Assert.That(updatedComment, Is.Not.Null);
            Assert.That(updatedComment!.CommentId, Is.EqualTo(commentId));
            Assert.That(updatedComment.Comment, Is.EqualTo(newText));
            Assert.That(updatedComment.Edited, Is.True);
            Assert.That(updatedComment.CommentDate, Is.EqualTo(editDate));
        }

        [Test]
        public async Task On_CommentRemovedEvent_DeletesComment()
        {
            var postId = Guid.NewGuid();
            var commentId = Guid.NewGuid();

            await _eventHandler.On(new PostCreatedEvent
            {
                Id = postId,
                Author = "cesar",
                Message = "post",
                DatePosted = DateTime.UtcNow
            });

            await _eventHandler.On(new CommentAddedEvent
            {
                Id = postId,
                CommentId = commentId,
                Comment = "comment to delete",
                Username = "user1",
                CommentDate = DateTime.UtcNow
            });

            // We ensure it exists before deleting it
            var existing = await _commentRepository.GetByIdAsync(commentId);
            Assert.That(existing, Is.Not.Null);

            await _eventHandler.On(new CommentRemovedEvent
            {
                Id = postId,
                CommentId = commentId
            });

            var deleted = await _commentRepository.GetByIdAsync(commentId);
            Assert.That(deleted, Is.Null);
        }

        [Test]
        public async Task On_PostRemovedEvent_RemovesPostFromDatabase()
        {
            var postId = Guid.NewGuid();

            await _eventHandler.On(new PostCreatedEvent
            {
                Id = postId,
                Author = "cesar",
                Message = "post to delete",
                DatePosted = DateTime.UtcNow
            });

            await _eventHandler.On(new PostRemovedEvent
            {
                Id = postId
            });

            var posts = await _postRepository.ListAllAsync();

            Assert.That(posts, Is.Empty);
        }

        [Test]
        public async Task On_MessageUpdatedEvent_WithNonExistingPost_DoesNothing()
        {
            var nonExistingId = Guid.NewGuid();

            await _eventHandler.On(new MessageUpdatedEvent
            {
                Id = nonExistingId,
                Message = "won't be stored"
            });

            var posts = await _postRepository.ListAllAsync();
            Assert.That(posts, Is.Empty);
        }

        [Test]
        public async Task On_CommentUpdatedEvent_WithNonExistingComment_DoesNothing()
        {
            var nonExistingCommentId = Guid.NewGuid();

            await _eventHandler.On(new CommentUpdatedEvent
            {
                Id = Guid.NewGuid(),
                CommentId = nonExistingCommentId,
                Comment = "ghost comment",
                Username = "user",
                EditDate = DateTime.UtcNow
            });

            var comment = await _commentRepository.GetByIdAsync(nonExistingCommentId);
            Assert.That(comment, Is.Null);
        }
    }
}
