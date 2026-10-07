using System.Collections.Concurrent;

namespace Backend.Utils
{
    public sealed class FrontendLogEntry
    {
        public long Id { get; init; }
        public DateTime Timestamp { get; init; }
        public string Level { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    public sealed class FrontendLogBuffer
    {
        private readonly int _capacity;
        private readonly List<FrontendLogEntry> _entries = new();
        private readonly object _lock = new();
        private long _nextId = 1;

        public FrontendLogBuffer(int capacity)
        {
            _capacity = capacity;
        }

        public void Add(string level, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            lock (_lock)
            {
                var entry = new FrontendLogEntry
                {
                    Id = _nextId++,
                    Timestamp = DateTime.UtcNow,
                    Level = level,
                    Message = message
                };

                _entries.Add(entry);
                if (_entries.Count > _capacity)
                {
                    var removeCount = _entries.Count - _capacity;
                    _entries.RemoveRange(0, removeCount);
                }
            }
        }

        public IReadOnlyList<FrontendLogEntry> Read(long after, int take, out long lastId)
        {
            lock (_lock)
            {
                var items = _entries
                    .Where(entry => entry.Id > after)
                    .Take(take)
                    .ToList();

                lastId = items.Count > 0 ? items[^1].Id : after;
                return items;
            }
        }
    }

    public static class FrontendProcessLogStore
    {
        private const int DefaultCapacity = 500;
        private static readonly ConcurrentDictionary<int, FrontendLogBuffer> Buffers = new();

        public static FrontendLogBuffer Get(int systemId)
        {
            return Buffers.GetOrAdd(systemId, _ => new FrontendLogBuffer(DefaultCapacity));
        }

        public static FrontendLogBuffer Reset(int systemId)
        {
            var buffer = new FrontendLogBuffer(DefaultCapacity);
            Buffers[systemId] = buffer;
            return buffer;
        }

        public static void Add(int systemId, string level, string message)
        {
            Get(systemId).Add(level, message);
        }
    }
}
