using System;
using System.Collections.Generic;

namespace Backend.Models.Entidades;

public partial class Menus
{
    public int Id { get; set; }

    public string? Titulo { get; set; }

    public string? Ruta { get; set; }

    public string? Icono { get; set; }

    public int Orden { get; set; }

    public int? PadreId { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<Menus> InversePadre { get; set; } = new List<Menus>();

    public virtual Menus? Padre { get; set; }

    public virtual ICollection<Roles> Rol { get; set; } = new List<Roles>();
}
