using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.Core.Messages
{
    public abstract class Message
    {
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }
    }
}
