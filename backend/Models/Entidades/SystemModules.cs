using System;
using System.Collections.Generic;

namespace Backend.Models.Entidades;

public partial class SystemModules
{
    public int SystemId { get; set; }

    public int ModuleId { get; set; }

    public bool IsEnabled { get; set; }

    public string? ConfigJson { get; set; }

    public virtual Modules Module { get; set; } = null!;

    public virtual Systems System { get; set; } = null!;
}
