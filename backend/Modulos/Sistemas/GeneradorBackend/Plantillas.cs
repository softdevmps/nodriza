using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Backend.Modulos.Sistemas.GeneradorBackend
{
    /// <summary>
    /// Archivos fijos del backend generado (Program.cs, autenticación, JWT, .env, .csproj...).
    /// Viven como texto legible en Plantillas/*.plantilla (recurso embebido) y se completan con
    /// marcadores {{nombre}}. Lo que depende de cada entidad (rutas, modelos, gestor, controller)
    /// se arma en código en SistemasBackendGenerator.
    /// </summary>
    public static class Plantillas
    {
        public static string Leer(string nombre, params (string Marcador, object Valor)[] valores)
        {
            using var stream = typeof(Plantillas).Assembly.GetManifestResourceStream($"Plantillas/{nombre}.plantilla")
                ?? throw new InvalidOperationException($"No existe la plantilla {nombre}.plantilla");
            using var reader = new StreamReader(stream, new UTF8Encoding(false));
            var texto = reader.ReadToEnd();

            // Una sola pasada: un valor que contenga "{{algo}}" no se vuelve a reemplazar.
            var mapa = valores.ToDictionary(v => v.Marcador, v => Convert.ToString(v.Valor, CultureInfo.InvariantCulture) ?? string.Empty);
            return Regex.Replace(texto, @"\{\{(\w+)\}\}", m =>
                mapa.TryGetValue(m.Groups[1].Value, out var valor)
                    ? valor
                    : throw new InvalidOperationException($"Falta el valor de {{{{{m.Groups[1].Value}}}}} en {nombre}.plantilla"));
        }
    }
}
