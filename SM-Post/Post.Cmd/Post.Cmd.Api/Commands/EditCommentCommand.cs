using CQRS.Core.Commands;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Post.Cmd.Api.Commands
{
    public class EditCommentCommand : BaseCommand
    {
        public Guid CommentId { get; set; }
        public string Comment { get; set; }
        public string Username { get; set; }

    }
}
