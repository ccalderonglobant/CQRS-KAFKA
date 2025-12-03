using Confluent.Kafka;
using CQRS.Core.Consumers;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post.Query.Infrastructure.Consumers
{
    public abstract class KafkaConsumerBase : IEventConsumer
    {
        private readonly ConsumerConfig _config;

        protected KafkaConsumerBase(IOptions<ConsumerConfig> config)
        {
            _config = config.Value;
        }

        public void Consume(string topic)
        {

            using var consumer = new ConsumerBuilder<string, string>(_config)
               .SetKeyDeserializer(Deserializers.Utf8)
               .SetValueDeserializer(Deserializers.Utf8)
               .Build();

            consumer.Subscribe(topic);

            while (true)
            {
                var consumeResult = consumer.Consume();
                if (consumeResult?.Message == null)
                {
                    continue;
                }

                // Template Method: delegate to subclass
                ProcessMessage(consumeResult.Message.Value);

                consumer.Commit(consumeResult);
            }
        }

        // Subclasses must implement how to process each raw message
        protected abstract void ProcessMessage(string message);
    }
}
