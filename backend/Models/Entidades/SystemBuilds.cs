using System;
using System.Collections.Generic;

namespace Backend.Models.Entidades;

public partial class SystemBuilds
{
    public int Id { get; set; }

    public int SystemId { get; set; }

    public string? Version { get; set; }

    public string Status { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public string? Log { get; set; }

    public virtual Systems System { get; set; } = null!;
}
