using Backend.Modulos.Sistemas.Publicacion;
using Microsoft.EntityFrameworkCore;
using Backend.Comun.BaseDeDatos;
using Backend.Comun.BaseDeDatos.Tablas;
using Backend.Modulos.Sistemas.Relaciones.Modelos;


namespace Backend.Modulos.Sistemas.Relaciones
{
    public static class RelacionesGestor
    {
        public static List<RelacionResponse> ObtenerPorSistema(int systemId)
        {
            using var context = new SystemBaseContext();

            return context.Relations
                .Where(r => r.SystemId == systemId)
                .OrderBy(r => r.Id)
                .Select(r => new RelacionResponse
                {
                    Id = r.Id,
                    SystemId = r.SystemId,
                    SourceEntityId = r.SourceEntityId,
                    TargetEntityId = r.TargetEntityId,
                    RelationType = r.RelationType,
                    ForeignKey = r.ForeignKey,
                    InverseProperty = r.InverseProperty,
                    CascadeDelete = r.CascadeDelete
                })
                .ToList();
        }

        public static (int? Id, string? Error) Crear(int systemId, RelacionCreateRequest request)
        {
            using var context = new SystemBaseContext();

            if (!context.Systems.Any(s => s.Id == systemId))
                return (null, "Sistema no encontrado.");

            var source = context.Entities.Include(e => e.Fields).FirstOrDefault(e => e.Id == request.SourceEntityId && e.SystemId == systemId);
            var target = context.Entities.Include(e => e.Fields).FirstOrDefault(e => e.Id == request.TargetEntityId && e.SystemId == systemId);
            if (source == null || target == null)
                return (null, "Las dos entidades tienen que pertenecer a este sistema.");

            var error = Validar(request.RelationType, request.ForeignKey, source, target);
            if (error != null)
                return (null, error);

            var relation = new Relations
            {
                SystemId = systemId,
                SourceEntityId = request.SourceEntityId,
                TargetEntityId = request.TargetEntityId,
                RelationType = NormalizarTipo(request.RelationType),
                ForeignKey = request.ForeignKey.Trim(),
                InverseProperty = request.InverseProperty?.Trim(),
                CascadeDelete = request.CascadeDelete,
                CreatedAt = DateTime.UtcNow
            };

            context.Relations.Add(relation);
            context.SaveChanges();

            return (relation.Id, null);
        }

        public static (bool Ok, bool NotFound, string? Error) Editar(int systemId, int id, RelacionUpdateRequest request)
        {
            using var context = new SystemBaseContext();

            var relation = context.Relations.FirstOrDefault(r => r.Id == id && r.SystemId == systemId);
            if (relation == null)
                return (false, true, null);

            var source = context.Entities.Include(e => e.Fields).First(e => e.Id == relation.SourceEntityId);
            var target = context.Entities.Include(e => e.Fields).First(e => e.Id == relation.TargetEntityId);
            var error = Validar(request.RelationType, request.ForeignKey, source, target);
            if (error != null)
                return (false, false, error);

            relation.RelationType = NormalizarTipo(request.RelationType);
            relation.ForeignKey = request.ForeignKey.Trim();
            relation.InverseProperty = request.InverseProperty?.Trim();
            relation.CascadeDelete = request.CascadeDelete;

            context.SaveChanges();
            return (true, false, null);
        }

        /// <summary>
        /// Solo tipos que se publican de verdad (ManyToOne, OneToOne); la FK tiene que ser un campo
        /// de la entidad origen del mismo tipo que la clave primaria de la entidad destino.
        /// </summary>
        private static string? Validar(string tipo, string foreignKey, Entities source, Entities target)
        {
            if (!SistemasPublicador.RelacionesSoportadas.Contains(tipo))
                return $"Tipo de relación no soportado: {tipo}. Por ahora solo ManyToOne y OneToOne.";

            var fk = source.Fields.FirstOrDefault(f => string.Equals(f.ColumnName, foreignKey?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (fk == null)
                return $"La FK \"{foreignKey}\" no es un campo de {source.Name}.";

            var pk = target.Fields.FirstOrDefault(f => f.IsPrimaryKey);
            if (pk == null)
                return $"{target.Name} no tiene clave primaria.";

            if (!string.Equals(fk.DataType, pk.DataType, StringComparison.OrdinalIgnoreCase))
                return $"La FK {fk.ColumnName} ({fk.DataType}) tiene que ser del mismo tipo que la clave de {target.Name} ({pk.DataType}).";

            return null;
        }

        private static string NormalizarTipo(string tipo) =>
            SistemasPublicador.RelacionesSoportadas.First(t => string.Equals(t, tipo, StringComparison.OrdinalIgnoreCase));
}
}
