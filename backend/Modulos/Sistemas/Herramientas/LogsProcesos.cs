using System.Collections.Concurrent;

namespace Backend.Modulos.Sistemas.Herramientas
{
    public sealed class LogEntrada
    {
        public long Id { get; init; }
        public DateTime Timestamp { get; init; }
        public string Level { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    /// <summary>Últimas N líneas de log de un proceso (circular, seguro entre hilos).</summary>
    public sealed class LogBuffer
    {
        private readonly int _capacity;
        private readonly List<LogEntrada> _entries = new();
        private readonly object _lock = new();
        private long _nextId = 1;

        public LogBuffer(int capacity)
        {
            _capacity = capacity;
        }

        public void Add(string level, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            lock (_lock)
            {
                _entries.Add(new LogEntrada
                {
                    Id = _nextId++,
                    Timestamp = DateTime.UtcNow,
                    Level = level,
                    Message = message
                });

                if (_entries.Count > _capacity)
                    _entries.RemoveRange(0, _entries.Count - _capacity);
            }
        }

        public IReadOnlyList<LogEntrada> Read(long after, int take, out long lastId)
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

    /// <summary>Logs en memoria de los procesos (backend/frontend) que la fábrica levanta por sistema.</summary>
    public static class LogsProcesos
    {
        private const int Capacidad = 500;
        private static readonly ConcurrentDictionary<(Componente, int), LogBuffer> Buffers = new();

        public static LogBuffer Get(Componente componente, int systemId) =>
            Buffers.GetOrAdd((componente, systemId), _ => new LogBuffer(Capacidad));

        public static LogBuffer Reset(Componente componente, int systemId)
        {
            var buffer = new LogBuffer(Capacidad);
            Buffers[(componente, systemId)] = buffer;
            return buffer;
        }

        public static void Add(Componente componente, int systemId, string level, string message) =>
            Get(componente, systemId).Add(level, message);
    }
}
