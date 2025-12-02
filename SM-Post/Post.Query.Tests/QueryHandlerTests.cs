using Moq;
using Post.Query.Api.Queries;
using Post.Query.Domain.Entities;
using Post.Query.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post.Query.Tests
{
    [TestFixture]
    public class QueryHandlerTests
    {
        private Mock<IPostRepository> _postRepositoryMock = null!;
        private QueryHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _postRepositoryMock = new Mock<IPostRepository>();
            _handler = new QueryHandler(_postRepositoryMock.Object);
        }

        [Test]
        public async Task HandleAsync_FindAllPostsQuery_UsesListAllAsync()
        {
            // Arrange
            var posts = new List<PostEntity>
            {
                new PostEntity { PostId = Guid.NewGuid(), Author = "cesar", Message = "post 1" },
                new PostEntity { PostId = Guid.NewGuid(), Author = "other", Message = "post 2" },
            };

            _postRepositoryMock
                .Setup(r => r.ListAllAsync())
                .ReturnsAsync(posts);

            var query = new FindAllPostsQuery();

            // Act
            var result = await _handler.HandleAsync(query);

            // Assert
            _postRepositoryMock.Verify(r => r.ListAllAsync(), Times.Once);
            Assert.That(result, Is.SameAs(posts));
        }

        [Test]
        public async Task HandleAsync_FindPostByIdQuery_ReturnsSinglePostInList()
        {
            // Arrange
            var id = Guid.NewGuid();
            var post = new PostEntity { PostId = id, Author = "cesar", Message = "post" };

            _postRepositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(post);

            var query = new FindPostByIdQuery { Id = id };

            // Act
            var result = await _handler.HandleAsync(query);

            // Assert
            _postRepositoryMock.Verify(r => r.GetByIdAsync(id), Times.Once);
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0], Is.SameAs(post));
        }

        [Test]
        public async Task HandleAsync_FindPostsByAuthorQuery_UsesListByAuthorAsync()
        {
            // Arrange
            var author = "cesar";
            var posts = new List<PostEntity>
            {
                new PostEntity { PostId = Guid.NewGuid(), Author = author, Message = "post 1" }
            };

            _postRepositoryMock
                .Setup(r => r.ListByAuthorAsync(author))
                .ReturnsAsync(posts);

            var query = new FindPostsByAuthorQuery { Author = author };

            // Act
            var result = await _handler.HandleAsync(query);

            // Assert
            _postRepositoryMock.Verify(r => r.ListByAuthorAsync(author), Times.Once);
            Assert.That(result, Is.SameAs(posts));
        }

        [Test]
        public async Task HandleAsync_FindPostWithCommentsQuery_UsesListWithCommentsAsync()
        {
            // Arrange
            var posts = new List<PostEntity>
            {
                new PostEntity
                {
                    PostId = Guid.NewGuid(),
                    Author = "cesar",
                    Message = "with comments",
                    Comments = new List<CommentEntity>
                    {
                        new CommentEntity
                        {
                            CommentId = Guid.NewGuid(),
                            Username = "user1",
                            Comment = "Nice!"
                        }
                    }
                }
            };

            _postRepositoryMock
                .Setup(r => r.ListWithCommentsAsync())
                .ReturnsAsync(posts);

            var query = new FindPostWithCommentsQuery();

            // Act
            var result = await _handler.HandleAsync(query);

            // Assert
            _postRepositoryMock.Verify(r => r.ListWithCommentsAsync(), Times.Once);
            Assert.That(result, Is.SameAs(posts));
        }

        [Test]
        public async Task HandleAsync_FindPostsWithLikesQuery_UsesListWithLikesAsync()
        {
            // Arrange
            var minLikes = 5;
            var posts = new List<PostEntity>
            {
                new PostEntity { PostId = Guid.NewGuid(), Likes = 10, Author = "cesar" }
            };

            _postRepositoryMock
                .Setup(r => r.ListWithLikesAsync(minLikes))
                .ReturnsAsync(posts);

            var query = new FindPostsWithLikesQuery { NumberOfLikes = minLikes };

            // Act
            var result = await _handler.HandleAsync(query);

            // Assert
            _postRepositoryMock.Verify(r => r.ListWithLikesAsync(minLikes), Times.Once);
            Assert.That(result, Is.SameAs(posts));
        }
    }
}
