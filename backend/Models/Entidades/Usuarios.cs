using System;
using System.Collections.Generic;

namespace Backend.Models.Entidades;

public partial class Usuarios
{
    public int Id { get; set; }

    public string? Username { get; set; }

    public string? Email { get; set; }

    public string? Nombre { get; set; }

    public string? Apellido { get; set; }

    public string? PasswordHash { get; set; }

    public int? RolId { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Roles? Rol { get; set; }
}
