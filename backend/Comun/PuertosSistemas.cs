using System.Net;
using System.Net.Sockets;

namespace Backend.Comun
{
    /// <summary>
    /// Puertos de los sistemas generados: base + id del sistema. Las bases se configuran con
    /// PUERTO_BASE_BACKEND / PUERTO_BASE_FRONTEND (por defecto 6000 / 7000), lejos de los puertos
    /// de la fábrica (5032) y de los que usa Vite (5173+), para que no choquen.
    /// Es la única regla: la usan el generador, las herramientas y la API (SistemaDetalleResponse).
    /// </summary>
    public static class PuertosSistemas
    {
        public static int BaseBackend => Leer("PUERTO_BASE_BACKEND", 6000);

        public static int BaseFrontend => Leer("PUERTO_BASE_FRONTEND", 7000);

        public static int Backend(int systemId) => BaseBackend + systemId;

        public static int Frontend(int systemId) => BaseFrontend + systemId;

        /// <summary>true si otro programa ya está escuchando en ese puerto local.</summary>
        public static bool Ocupado(int puerto)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, puerto);
                listener.Start();
                listener.Stop();
                return false;
            }
            catch (SocketException)
            {
                return true;
            }
        }

        private static int Leer(string variable, int porDefecto) =>
            int.TryParse(Environment.GetEnvironmentVariable(variable), out var valor) && valor is > 1024 and < 60000
                ? valor
                : porDefecto;
    }
}
