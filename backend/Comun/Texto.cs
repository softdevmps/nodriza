using System.Text;

namespace Backend.Comun
{
    /// <summary>
    /// Conversiones de nombres que se usan para rutas (kebab) y para nombres de clases generadas (PascalCase).
    /// Única implementación: si cambia, cambian a la vez las rutas de menú, las del backend generado y las del export.
    /// </summary>
    public static class Texto
    {
        /// <summary>"OrdenesDeCompra" o "Ordenes de compra" → "ordenes-de-compra". Vacío → "item".</summary>
        public static string ToKebab(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "item";

            var sb = new StringBuilder();
            var prevDash = false;

            foreach (var ch in value.Trim())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    if (char.IsUpper(ch) && sb.Length > 0 && !prevDash)
                        sb.Append('-');

                    sb.Append(char.ToLowerInvariant(ch));
                    prevDash = false;
                }
                else if (!prevDash && sb.Length > 0)
                {
                    sb.Append('-');
                    prevDash = true;
                }
            }

            var result = sb.ToString().Trim('-');
            return string.IsNullOrWhiteSpace(result) ? "item" : result;
        }

        /// <summary>"ordenes de_compra" → "OrdenesDeCompra" (cada palabra con mayúscula inicial). Vacío → "Item".</summary>
        public static string ToPascalCase(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Item";

            var sb = new StringBuilder();
            var palabra = new StringBuilder();

            void Volcar()
            {
                if (palabra.Length == 0)
                    return;
                var lower = palabra.ToString().ToLowerInvariant();
                sb.Append(char.ToUpperInvariant(lower[0]));
                if (lower.Length > 1)
                    sb.Append(lower[1..]);
                palabra.Clear();
            }

            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch))
                    palabra.Append(ch);
                else
                    Volcar();
            }
            Volcar();

            var result = sb.ToString();
            return string.IsNullOrWhiteSpace(result) ? "Item" : result;
        }
    }
}
