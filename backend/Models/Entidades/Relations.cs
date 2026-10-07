using System;
using System.Collections.Generic;

namespace Backend.Models.Entidades;

public partial class Relations
{
    public int Id { get; set; }

    public int SystemId { get; set; }

    public int SourceEntityId { get; set; }

    public int TargetEntityId { get; set; }

    public string? RelationType { get; set; }

    public string? ForeignKey { get; set; }

    public string? InverseProperty { get; set; }

    public bool CascadeDelete { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Entities SourceEntity { get; set; } = null!;

    public virtual Systems System { get; set; } = null!;

    public virtual Entities TargetEntity { get; set; } = null!;
}
