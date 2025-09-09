using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace Luna;

/// <summary> Copied from / based on Serilog.Extensions.Logging. </summary>
internal sealed class EventIdPropertyCache(int maxCachedProperties = 1024)
{
    private readonly ConcurrentDictionary<EventKey, LogEventPropertyValue> _propertyCache = new();

    private int _count;

    public LogEventPropertyValue GetOrCreatePropertyValue(in EventId eventId)
    {
        var eventKey = new EventKey(eventId);

        LogEventPropertyValue? propertyValue;

        if (_count >= maxCachedProperties)
        {
            if (!_propertyCache.TryGetValue(eventKey, out propertyValue))
                propertyValue = CreatePropertyValue(in eventKey);
        }
        else
        {
            if (!_propertyCache.TryGetValue(eventKey, out propertyValue))
                // GetOrAdd is moved to a separate method to prevent closure allocation
                propertyValue = GetOrAddCore(in eventKey);
        }

        return propertyValue;
    }

    private static StructureValue CreatePropertyValue(in EventKey eventKey)
    {
        var properties = new List<LogEventProperty>(2);

        if (eventKey.Id != 0)
            properties.Add(new LogEventProperty("Id", new ScalarValue(eventKey.Id)));

        if (eventKey.Name != null)
            properties.Add(new LogEventProperty("Name", new ScalarValue(eventKey.Name)));

        return new StructureValue(properties);
    }

    private LogEventPropertyValue GetOrAddCore(in EventKey eventKey)
        => _propertyCache.GetOrAdd(
            eventKey,
            key =>
            {
                Interlocked.Increment(ref _count);

                return CreatePropertyValue(in key);
            });

    private readonly record struct EventKey(int Id, string? Name)
    {
        public EventKey(EventId eventId)
            : this(eventId.Id, eventId.Name)
        { }
    }
}
