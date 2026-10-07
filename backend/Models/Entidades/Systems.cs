using System;
using System.Collections.Generic;

namespace Backend.Models.Entidades;

public partial class Systems
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Slug { get; set; }

    public string? Description { get; set; }

    public string? Namespace { get; set; }

    public string? Version { get; set; }

    public string? Status { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? PublishedAt { get; set; }

    public virtual ICollection<Entities> Entities { get; set; } = new List<Entities>();

    public virtual ICollection<Permissions> Permissions { get; set; } = new List<Permissions>();

    public virtual ICollection<Relations> Relations { get; set; } = new List<Relations>();

    public virtual ICollection<SystemBuilds> SystemBuilds { get; set; } = new List<SystemBuilds>();

    public virtual ICollection<SystemMenus> SystemMenus { get; set; } = new List<SystemMenus>();

    public virtual ICollection<SystemModules> SystemModules { get; set; } = new List<SystemModules>();
}
