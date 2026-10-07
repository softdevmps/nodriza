using System;
using System.Collections.Generic;

namespace Backend.Models.Entidades;

public partial class SystemMenus
{
    public int Id { get; set; }

    public int SystemId { get; set; }

    public int? ParentId { get; set; }

    public string? Title { get; set; }

    public string? Route { get; set; }

    public string? Icon { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<SystemMenus> InverseParent { get; set; } = new List<SystemMenus>();

    public virtual SystemMenus? Parent { get; set; }

    public virtual Systems System { get; set; } = null!;

    public virtual ICollection<Roles> Role { get; set; } = new List<Roles>();
}
