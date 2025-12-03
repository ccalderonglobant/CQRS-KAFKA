using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post.Cmd.Domain.Aggregates
{
    public interface IPostAggregateFactory
    {
        PostAggregate Create(Guid id, string author, string message);
    }
}
