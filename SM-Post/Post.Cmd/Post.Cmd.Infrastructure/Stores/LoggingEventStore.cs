using CQRS.Core.Events;
using CQRS.Core.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post.Cmd.Infrastructure.Stores
{
    public class LoggingEventStore : IEventStore
    {
        private readonly IEventStore _inner;
        private readonly ILogger<LoggingEventStore> _logger;

        public LoggingEventStore(IEventStore inner, ILogger<LoggingEventStore> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task<List<BaseEvent>> GetEventsAsync(Guid aggregateId)
        {
            _logger.LogInformation("Loading events for aggregate {AggregateId}", aggregateId);

            try
            {
                var events = await _inner.GetEventsAsync(aggregateId);
                _logger.LogInformation("Loaded {EventCount} events for aggregate {AggregateId}", events.Count, aggregateId);
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while loading events for aggregate {AggregateId}", aggregateId);
                throw;
            }
        }

        public async Task SaveEventsAsync(Guid aggregateId, IEnumerable<BaseEvent> events, int expectedVersion)
        {
            _logger.LogInformation(
               "Saving {EventCount} events for aggregate {AggregateId} (expectedVersion={ExpectedVersion})",
               events is ICollection<BaseEvent> col ? col.Count : -1,
               aggregateId,
               expectedVersion);

            try
            {
                await _inner.SaveEventsAsync(aggregateId, events, expectedVersion);
                _logger.LogInformation("Successfully saved events for aggregate {AggregateId}", aggregateId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while saving events for aggregate {AggregateId}", aggregateId);
                throw;
            }
        }
    }
}
