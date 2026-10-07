using System;
using System.Collections.Generic;

namespace Backend.Models.Entidades;

public partial class EntityModules
{
    public int EntityId { get; set; }

    public int ModuleId { get; set; }

    public string? ConfigJson { get; set; }

    public bool IsEnabled { get; set; }

    public virtual Entities Entity { get; set; } = null!;

    public virtual Modules Module { get; set; } = null!;
}
