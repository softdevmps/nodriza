using Microsoft.EntityFrameworkCore;
using Backend.Comun.BaseDeDatos;
using Backend.Comun.BaseDeDatos.Tablas;
using Backend.Modulos.Sistemas.Campos.Modelos;


namespace Backend.Modulos.Sistemas.Campos
{
    public class CamposGestor
    {
        private readonly IDbContextFactory<SystemBaseContext> _contextos;

        public CamposGestor(IDbContextFactory<SystemBaseContext> contextos)
        {
            _contextos = contextos;
        }

        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "string",
            "int",
            "decimal",
            "bool",
            "datetime",
            "guid"
        };

        public List<CampoResponse> ObtenerPorEntidad(int systemId, int entityId)
        {
            using var context = _contextos.CreateDbContext();

            var exists = context.Entities.Any(e => e.Id == entityId && e.SystemId == systemId);
            if (!exists)
                return new List<CampoResponse>();

            return context.Fields
                .Where(f => f.EntityId == entityId)
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.Id)
                .Select(f => new CampoResponse
                {
                    Id = f.Id,
                    EntityId = f.EntityId,
                    Name = f.Name,
                    ColumnName = f.ColumnName,
                    DataType = f.DataType,
                    Required = f.Required,
                    MaxLength = f.MaxLength,
                    Precision = f.Precision,
                    Scale = f.Scale,
                    DefaultValue = f.DefaultValue,
                    IsPrimaryKey = f.IsPrimaryKey,
                    IsIdentity = f.IsIdentity,
                    IsUnique = f.IsUnique,
                    UiConfigJson = f.UiConfigJson,
                    SortOrder = f.SortOrder
                })
                .ToList();
        }

        public int? Crear(int systemId, int entityId, CampoCreateRequest request)
        {
            using var context = _contextos.CreateDbContext();

            var entity = context.Entities.FirstOrDefault(e => e.Id == entityId && e.SystemId == systemId);
            if (entity == null)
                return null;

            if (!AllowedTypes.Contains(request.DataType))
                return null;

            var name = request.Name.Trim();
            var columnName = request.ColumnName.Trim();

            var duplicated = context.Fields.Any(f =>
                f.EntityId == entityId &&
                (f.Name == name || f.ColumnName == columnName));

            if (duplicated)
                return null;

            if (request.IsPrimaryKey)
            {
                var hasPk = context.Fields.Any(f => f.EntityId == entityId && f.IsPrimaryKey);
                if (hasPk)
                    return null;
            }

            var field = new Fields
            {
                EntityId = entityId,
                Name = name,
                ColumnName = columnName,
                DataType = request.DataType.Trim().ToLowerInvariant(),
                Required = request.Required,
                MaxLength = request.MaxLength,
                Precision = request.Precision,
                Scale = request.Scale,
                DefaultValue = request.DefaultValue?.Trim(),
                IsPrimaryKey = request.IsPrimaryKey,
                IsIdentity = request.IsIdentity,
                IsUnique = request.IsUnique,
                UiConfigJson = request.UiConfigJson,
                SortOrder = request.SortOrder,
                CreatedAt = DateTime.UtcNow
            };

            context.Fields.Add(field);
            context.SaveChanges();

            return field.Id;
        }

        public bool Editar(int systemId, int entityId, int id, CampoUpdateRequest request)
        {
            using var context = _contextos.CreateDbContext();

            var entity = context.Entities.FirstOrDefault(e => e.Id == entityId && e.SystemId == systemId);
            if (entity == null)
                return false;

            if (!AllowedTypes.Contains(request.DataType))
                return false;

            var field = context.Fields.FirstOrDefault(f => f.Id == id && f.EntityId == entityId);
            if (field == null)
                return false;

            var name = request.Name.Trim();
            var columnName = request.ColumnName.Trim();

            var duplicated = context.Fields.Any(f =>
                f.EntityId == entityId &&
                f.Id != id &&
                (f.Name == name || f.ColumnName == columnName));

            if (duplicated)
                return false;

            if (request.IsPrimaryKey && !field.IsPrimaryKey)
            {
                var hasPk = context.Fields.Any(f => f.EntityId == entityId && f.IsPrimaryKey);
                if (hasPk)
                    return false;
            }

            // Las relaciones guardan la FK por nombre de columna: si se renombra, la siguen
            if (!string.Equals(field.ColumnName, columnName, StringComparison.Ordinal))
            {
                foreach (var relacion in context.Relations.Where(r => r.SourceEntityId == entityId && r.ForeignKey == field.ColumnName))
                    relacion.ForeignKey = columnName;
            }

            field.Name = name;
            field.ColumnName = columnName;
            field.DataType = request.DataType.Trim().ToLowerInvariant();
            field.Required = request.Required;
            field.MaxLength = request.MaxLength;
            field.Precision = request.Precision;
            field.Scale = request.Scale;
            field.DefaultValue = request.DefaultValue?.Trim();
            field.IsPrimaryKey = request.IsPrimaryKey;
            field.IsIdentity = request.IsIdentity;
            field.IsUnique = request.IsUnique;
            field.UiConfigJson = request.UiConfigJson;
            field.SortOrder = request.SortOrder;
            field.UpdatedAt = DateTime.UtcNow;

            context.SaveChanges();
            return true;
        }
    }
}
