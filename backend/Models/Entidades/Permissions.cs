using System;
using System.Collections.Generic;

namespace Backend.Models.Entidades;

public partial class Permissions
{
    public int Id { get; set; }

    public int SystemId { get; set; }

    public string? Key { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Systems System { get; set; } = null!;

    public virtual ICollection<Roles> Role { get; set; } = new List<Roles>();
}
