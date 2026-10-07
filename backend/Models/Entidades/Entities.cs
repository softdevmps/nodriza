using System;
using System.Collections.Generic;

namespace Backend.Models.Entidades;

public partial class Entities
{
    public int Id { get; set; }

    public int SystemId { get; set; }

    public string? Name { get; set; }

    public string? DisplayName { get; set; }

    public string? TableName { get; set; }

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Fields> Fields { get; set; } = new List<Fields>();

    public virtual ICollection<Relations> RelationsSourceEntity { get; set; } = new List<Relations>();

    public virtual ICollection<Relations> RelationsTargetEntity { get; set; } = new List<Relations>();

    public virtual ICollection<EntityModules> EntityModules { get; set; } = new List<EntityModules>();

    public virtual Systems System { get; set; } = null!;
}
