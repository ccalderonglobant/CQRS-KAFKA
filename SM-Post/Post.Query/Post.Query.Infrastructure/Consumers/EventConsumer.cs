using Confluent.Kafka;
using CQRS.Core.Consumers;
using CQRS.Core.Events;
using Microsoft.Extensions.Options;
using Post.Query.Infrastructure.Converters;
using Post.Query.Infrastructure.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Post.Query.Infrastructure.Consumers
{
    public class EventConsumer : KafkaConsumerBase
    {
        private readonly IEventHandler _eventHandler;

        public EventConsumer(IOptions<ConsumerConfig> config, IEventHandler eventHandler) : base(config)
        {
            _eventHandler = eventHandler;
        }

        public void ProcessEvent(string message)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new EventJsonConverter() }
            };

            var @event = JsonSerializer.Deserialize<BaseEvent>(message, options);
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event), "Could not deserialize event message!");
            }

            var handleMethod = _eventHandler
                .GetType()
                .GetMethod("On", new Type[] { @event.GetType() });

            if (handleMethod == null)
            {
                throw new ArgumentNullException(nameof(handleMethod), "Could not find event handler method!");
            }

            handleMethod.Invoke(_eventHandler, new object[] { @event });
        }

        protected override void ProcessMessage(string message)
        {
            ProcessEvent(message);
        }
    }
}
