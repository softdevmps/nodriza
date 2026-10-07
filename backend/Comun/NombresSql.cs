using System.Text.RegularExpressions;

namespace Backend.Comun
{
    /// <summary>
    /// Reglas para los nombres que terminan en SQL (tablas, columnas) o en código generado
    /// (rutas). Se validan al guardarlos y otra vez antes de generar código, porque la
    /// consola SQL puede importar nombres de tablas creadas a mano.
    /// </summary>
    public static class NombresSql
    {
        // Letras (incluye acentos y ñ), dígitos y guion bajo; no empieza con dígito.
        // Excluye comillas, corchetes, espacios y cualquier símbolo que rompa SQL o C#.
        private static readonly Regex Identificador = new(@"^[\p{L}_][\p{L}\p{Nd}_]{0,127}$", RegexOptions.Compiled);

        // Segmentos de URL: a-z, dígitos, guion y guion bajo, separados por "/".
        private static readonly Regex Ruta = new(@"^[A-Za-z0-9_-]+(/[A-Za-z0-9_-]+)*$", RegexOptions.Compiled);

        public const string ReglaIdentificador =
            "Solo letras, números y guion bajo, sin espacios ni símbolos; no puede empezar con un número (máx. 128).";

        public const string ReglaRuta = "Solo letras sin acentos, números, guiones y '/' entre segmentos.";

        public static bool EsIdentificadorValido(string? nombre) =>
            !string.IsNullOrWhiteSpace(nombre) && Identificador.IsMatch(nombre);

        public static bool EsRutaValida(string? ruta) =>
            !string.IsNullOrWhiteSpace(ruta) && Ruta.IsMatch(ruta.Trim('/'));
    }
}
