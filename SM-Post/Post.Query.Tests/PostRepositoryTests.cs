using Microsoft.EntityFrameworkCore;
using Post.Query.Domain.Entities;
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
    public class PostRepositoryTests
    {
        private DatabaseContextFactory CreateInMemoryFactory(string dbName)
        {
            return new DatabaseContextFactory(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });
        }

        private async Task SeedDataAsync(DatabaseContextFactory factory)
        {
            using var context = factory.CreateDbContext();

            var post1 = new PostEntity
            {
                PostId = Guid.NewGuid(),
                Author = "cesar",
                DatePosted = DateTime.UtcNow,
                Message = "Post with comment",
                Likes = 5,
                Comments = new List<CommentEntity>
                {
                    new CommentEntity
                    {
                        CommentId = Guid.NewGuid(),
                        Username = "user1",
                        Comment = "Nice!",
                        CommentDate = DateTime.UtcNow,
                        Edited = false
                    }
                }
            };

            var post2 = new PostEntity
            {
                PostId = Guid.NewGuid(),
                Author = "other",
                DatePosted = DateTime.UtcNow,
                Message = "Post without comment",
                Likes = 1,
                Comments = new List<CommentEntity>()
            };

            var post3 = new PostEntity
            {
                PostId = Guid.NewGuid(),
                Author = "cesar",
                DatePosted = DateTime.UtcNow,
                Message = "Post with many likes",
                Likes = 10,
                Comments = new List<CommentEntity>()
            };

            await context.Posts.AddRangeAsync(post1, post2, post3);
            await context.SaveChangesAsync();
        }

        [Test]
        public async Task ListAllAsync_ReturnsAllPosts()
        {
            // Arrange
            var factory = CreateInMemoryFactory("ListAllDb");
            await SeedDataAsync(factory);

            var repo = new PostRepository(factory);

            // Act
            var posts = await repo.ListAllAsync();

            // Assert
            Assert.That(posts.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task ListByAuthorAsync_ReturnsOnlyPostsFromAuthor()
        {
            // Arrange
            var factory = CreateInMemoryFactory("ListByAuthorDb");
            await SeedDataAsync(factory);

            var repo = new PostRepository(factory);

            // Act
            var posts = await repo.ListByAuthorAsync("cesar");

            // Assert
            Assert.That(posts, Is.Not.Empty);
            Assert.That(posts.All(p => p.Author.Contains("cesar")), Is.True);
        }

        [Test]
        public async Task ListWithCommentsAsync_ReturnsOnlyPostsWithComments()
        {
            // Arrange
            var factory = CreateInMemoryFactory("ListWithCommentsDb");
            await SeedDataAsync(factory);

            var repo = new PostRepository(factory);

            // Act
            var posts = await repo.ListWithCommentsAsync();

            // Assert
            Assert.That(posts.Count, Is.EqualTo(1));
            Assert.That(posts.Single().Comments.Any(), Is.True);
        }

        [Test]
        public async Task ListWithLikesAsync_ReturnsOnlyPostsWithLikesGreaterOrEqual()
        {
            // Arrange
            var factory = CreateInMemoryFactory("ListWithLikesDb");
            await SeedDataAsync(factory);

            var repo = new PostRepository(factory);

            // Act
            var posts = await repo.ListWithLikesAsync(5);

            // Assert
            Assert.That(posts.All(p => p.Likes >= 5), Is.True);
            Assert.That(posts.Count, Is.EqualTo(2)); // likes 5 and 10
        }
    }
}
