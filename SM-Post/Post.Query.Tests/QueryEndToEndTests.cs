using Microsoft.EntityFrameworkCore;
using Post.Common.Events;
using Post.Query.Api.Queries;
using Post.Query.Infrastructure.DataAccess;
using Post.Query.Infrastructure.Dispatchers;
using Post.Query.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post.Query.Tests
{
    [TestFixture]
    public class QueryEndToEndTests
    {
        private DatabaseContextFactory _factory = null!;
        private PostRepository _postRepository = null!;
        private CommentRepository _commentRepository = null!;
        private Infrastructure.Handlers.EventHandler _eventHandler = null!;
        private QueryHandler _queryHandler = null!;
        private QueryDispatcher _dispatcher = null!;

        [SetUp]
        public void SetUp()
        {
            // A different in-memory DB per test to avoid data contamination
            var dbName = $"QueryEndToEndDb_{Guid.NewGuid()}";

            _factory = new DatabaseContextFactory(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            _postRepository = new PostRepository(_factory);
            _commentRepository = new CommentRepository(_factory);

            _eventHandler = new Infrastructure.Handlers.EventHandler(_postRepository, _commentRepository);
            _queryHandler = new QueryHandler(_postRepository);

            _dispatcher = new QueryDispatcher();
            _dispatcher.RegisterHandler<FindAllPostsQuery>(_queryHandler.HandleAsync);
            _dispatcher.RegisterHandler<FindPostByIdQuery>(_queryHandler.HandleAsync);
            _dispatcher.RegisterHandler<FindPostsByAuthorQuery>(_queryHandler.HandleAsync);
            _dispatcher.RegisterHandler<FindPostWithCommentsQuery>(_queryHandler.HandleAsync);
            _dispatcher.RegisterHandler<FindPostsWithLikesQuery>(_queryHandler.HandleAsync);
        }

        private async Task<(Guid postWithCommentId, Guid postWithManyLikesId)> SeedDataAsync()
        {
            // Post 1: autor = cesar, 1 like, with comment
            var post1Id = Guid.NewGuid();

            await _eventHandler.On(new PostCreatedEvent
            {
                Id = post1Id,
                Author = "cesar",
                Message = "post de cesar con comentario",
                DatePosted = DateTime.UtcNow
            });

            await _eventHandler.On(new PostLikedEvent
            {
                Id = post1Id
            });

            await _eventHandler.On(new CommentAddedEvent
            {
                Id = post1Id,
                CommentId = Guid.NewGuid(),
                Comment = "nice post!",
                Username = "user1",
                CommentDate = DateTime.UtcNow
            });

            // Post 2: autor = other, 5 likes, without comments
            var post2Id = Guid.NewGuid();

            await _eventHandler.On(new PostCreatedEvent
            {
                Id = post2Id,
                Author = "other",
                Message = "post de otro autor con muchos likes",
                DatePosted = DateTime.UtcNow
            });

            for (int i = 0; i < 5; i++)
            {
                await _eventHandler.On(new PostLikedEvent
                {
                    Id = post2Id
                });
            }

            return (post1Id, post2Id);
        }

        [Test]
        public async Task FindAllPostsQuery_ReturnsAllSeededPosts()
        {
            // Arrange
            await SeedDataAsync();

            // Act
            var result = await _dispatcher.SendAsync(new FindAllPostsQuery());

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task FindPostByIdQuery_ReturnsSingleCorrectPost()
        {
            // Arrange
            var (postWithCommentId, _) = await SeedDataAsync();

            var query = new FindPostByIdQuery
            {
                Id = postWithCommentId
            };

            // Act
            var result = await _dispatcher.SendAsync(query);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));

            var post = result.Single();
            Assert.That(post.PostId, Is.EqualTo(postWithCommentId));
            Assert.That(post.Author, Is.EqualTo("cesar"));
        }

        [Test]
        public async Task FindPostsByAuthorQuery_ReturnsOnlyThatAuthor()
        {
            // Arrange
            await SeedDataAsync();

            var query = new FindPostsByAuthorQuery
            {
                Author = "cesar"
            };

            // Act
            var result = await _dispatcher.SendAsync(query);

            // Assert
            Assert.That(result, Is.Not.Empty);
            Assert.That(result.All(p => p.Author.Contains("cesar")), Is.True);
        }

        [Test]
        public async Task FindPostWithCommentsQuery_ReturnsOnlyPostsWithComments()
        {
            // Arrange
            await SeedDataAsync();

            var query = new FindPostWithCommentsQuery();

            // Act
            var result = await _dispatcher.SendAsync(query);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));

            var post = result.Single();
            Assert.That(post.Comments, Is.Not.Null);
            Assert.That(post.Comments.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task FindPostsWithLikesQuery_ReturnsPostsWithAtLeastMinLikes()
        {
            // Arrange
            var (_, postWithManyLikesId) = await SeedDataAsync();

            var query = new FindPostsWithLikesQuery
            {
                NumberOfLikes = 5
            };

            // Act
            var result = await _dispatcher.SendAsync(query);

            // Assert
            Assert.That(result, Is.Not.Empty);
            Assert.That(result.All(p => p.Likes >= 5), Is.True);

            // And specifically it should include the post with many likes.
            Assert.That(result.Any(p => p.PostId == postWithManyLikesId), Is.True);
        }
    }
}
