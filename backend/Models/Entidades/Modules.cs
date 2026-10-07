using System;
using System.Collections.Generic;

namespace Backend.Models.Entidades;

public partial class Modules
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Version { get; set; }

    public virtual ICollection<SystemModules> SystemModules { get; set; } = new List<SystemModules>();

    public virtual ICollection<EntityModules> EntityModules { get; set; } = new List<EntityModules>();
}
