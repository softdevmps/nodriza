using System.Collections.Concurrent;

namespace Backend.Comun.Seguridad
{
    /// <summary>
    /// Protección contra fuerza bruta: después de 5 intentos fallidos seguidos, ese usuario queda
    /// bloqueado 15 minutos (aunque después ponga la contraseña correcta). Se cuenta por nombre de
    /// usuario, exista o no, para no revelar qué usuarios existen. En memoria: alcanza para una
    /// sola instancia de la fábrica; con varias instancias habría que moverlo a un store compartido.
    /// </summary>
    public static class BloqueoLogin
    {
        public const int MaxIntentos = 5;
        public static readonly TimeSpan Duracion = TimeSpan.FromMinutes(15);

        private sealed record Estado(int Fallos, DateTime? BloqueadoHasta);

        private static readonly ConcurrentDictionary<string, Estado> Estados = new(StringComparer.OrdinalIgnoreCase);

        private static string Clave(string? usuario) => (usuario ?? string.Empty).Trim();

        /// <summary>Minutos que faltan para desbloquear, o null si no está bloqueado.</summary>
        public static int? MinutosBloqueado(string? usuario)
        {
            if (!Estados.TryGetValue(Clave(usuario), out var estado) || estado.BloqueadoHasta is not { } hasta)
                return null;

            var resta = hasta - DateTime.UtcNow;
            return resta > TimeSpan.Zero ? (int)Math.Ceiling(resta.TotalMinutes) : null;
        }

        public static void RegistrarFallo(string? usuario)
        {
            Estados.AddOrUpdate(
                Clave(usuario),
                _ => new Estado(1, null),
                (_, actual) =>
                {
                    // Si el bloqueo anterior ya venció, se empieza a contar de nuevo.
                    var fallos = actual.BloqueadoHasta is { } h && h <= DateTime.UtcNow ? 1 : actual.Fallos + 1;
                    return new Estado(fallos, fallos >= MaxIntentos ? DateTime.UtcNow.Add(Duracion) : null);
                });
        }

        public static void RegistrarExito(string? usuario) => Estados.TryRemove(Clave(usuario), out _);
    }
}
