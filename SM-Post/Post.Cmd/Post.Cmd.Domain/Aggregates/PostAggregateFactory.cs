using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post.Cmd.Domain.Aggregates
{
    public class PostAggregateFactory : IPostAggregateFactory
    {
        public PostAggregate Create(Guid id, string author, string message)
        {
            return new PostAggregate(id, author, message);
        }
    }
}
