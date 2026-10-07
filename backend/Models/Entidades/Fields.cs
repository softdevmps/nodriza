using System;
using System.Collections.Generic;

namespace Backend.Models.Entidades;

public partial class Fields
{
    public int Id { get; set; }

    public int EntityId { get; set; }

    public string? Name { get; set; }

    public string? ColumnName { get; set; }

    public string? DataType { get; set; }

    public bool Required { get; set; }

    public int? MaxLength { get; set; }

    public int? Precision { get; set; }

    public int? Scale { get; set; }

    public string? DefaultValue { get; set; }

    public bool IsPrimaryKey { get; set; }

    public bool IsIdentity { get; set; }

    public bool IsUnique { get; set; }

    public string? UiConfigJson { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Entities Entity { get; set; } = null!;
}
