using System.ComponentModel.DataAnnotations;

namespace Backend.Comun
{
    /// <summary>
    /// Valida que la propiedad sea un nombre de tabla/columna seguro (ver NombresSql).
    /// Con [ApiController] un valor inválido responde 400 con el mensaje de la regla.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class IdentificadorSqlAttribute : ValidationAttribute
    {
        public bool Opcional { get; set; }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var texto = (value as string)?.Trim();
            if (string.IsNullOrEmpty(texto) && Opcional)
                return ValidationResult.Success;

            return NombresSql.EsIdentificadorValido(texto)
                ? ValidationResult.Success
                : new ValidationResult($"{validationContext.DisplayName} inválido: {NombresSql.ReglaIdentificador}", new[] { validationContext.MemberName ?? string.Empty });
        }
    }
}
