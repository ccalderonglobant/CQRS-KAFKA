using CQRS.Core.Queries;
using Post.Query.Api.Queries;
using Post.Query.Domain.Entities;
using Post.Query.Infrastructure.Dispatchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post.Query.Tests
{
    [TestFixture]
    public class QueryDispatcherTests
    {
        [Test]
        public async Task SendAsync_WithRegisteredHandler_InvokesHandlerAndReturnsResult()
        {
            // Arrange
            var dispatcher = new QueryDispatcher();

            var query = new FindAllPostsQuery();
            var expected = new List<PostEntity>
            {
                new PostEntity { PostId = Guid.NewGuid(), Author = "cesar", Message = "post" }
            };

            dispatcher.RegisterHandler<FindAllPostsQuery>(q =>
            {
                // We can verify that the same query object arrives.
                Assert.That(q, Is.SameAs(query));
                return Task.FromResult(expected);
            });

            // Act
            var result = await dispatcher.SendAsync(query);

            // Assert
            Assert.That(result, Is.SameAs(expected));
        }

        [Test]
        public void RegisterHandler_TwiceForSameQueryType_ThrowsIndexOutOfRangeException()
        {
            var dispatcher = new QueryDispatcher();

            Func<FindAllPostsQuery, Task<List<PostEntity>>> handler =
                q => Task.FromResult(new List<PostEntity>());

            dispatcher.RegisterHandler(handler);

            Assert.Throws<IndexOutOfRangeException>(
                () => dispatcher.RegisterHandler(handler));
        }

        private class DummyQuery : BaseQuery { }

        [Test]
        public void SendAsync_WithUnregisteredQueryType_ThrowsArgumentNullException()
        {
            // Arrange
            var dispatcher = new QueryDispatcher();
            var query = new DummyQuery();

            // Act + Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(
                async () => await dispatcher.SendAsync(query));

            StringAssert.Contains("No query handler was registered", ex!.Message);
        }
    }
}
