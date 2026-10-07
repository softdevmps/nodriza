using System.Collections.Concurrent;

namespace Backend.Utils
{
    public sealed class BackendLogEntry
    {
        public long Id { get; init; }
        public DateTime Timestamp { get; init; }
        public string Level { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    public sealed class BackendLogBuffer
    {
        private readonly int _capacity;
        private readonly List<BackendLogEntry> _entries = new();
        private readonly object _lock = new();
        private long _nextId = 1;

        public BackendLogBuffer(int capacity)
        {
            _capacity = capacity;
        }

        public void Add(string level, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            lock (_lock)
            {
                var entry = new BackendLogEntry
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

        public IReadOnlyList<BackendLogEntry> Read(long after, int take, out long lastId)
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

    public static class BackendProcessLogStore
    {
        private const int DefaultCapacity = 500;
        private static readonly ConcurrentDictionary<int, BackendLogBuffer> Buffers = new();

        public static BackendLogBuffer Get(int systemId)
        {
            return Buffers.GetOrAdd(systemId, _ => new BackendLogBuffer(DefaultCapacity));
        }

        public static BackendLogBuffer Reset(int systemId)
        {
            var buffer = new BackendLogBuffer(DefaultCapacity);
            Buffers[systemId] = buffer;
            return buffer;
        }

        public static void Add(int systemId, string level, string message)
        {
            Get(systemId).Add(level, message);
        }
    }
}
