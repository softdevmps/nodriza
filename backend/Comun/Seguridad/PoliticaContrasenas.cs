namespace Backend.Comun.Seguridad
{
    /// <summary>Regla mínima de contraseñas: 8+ caracteres, con al menos una letra y un número.</summary>
    public static class PoliticaContrasenas
    {
        public const int LargoMinimo = 8;

        public const string Regla = "La contraseña debe tener al menos 8 caracteres, con letras y números.";

        /// <summary>Devuelve null si la contraseña cumple; si no, el mensaje para el usuario.</summary>
        public static string? Validar(string? password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < LargoMinimo)
                return Regla;
            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
                return Regla;
            return null;
        }
    }
}
