using Backend.Data;
using Backend.Models.Entidades;
using Backend.Models.Sistemas;

namespace Backend.Negocio.Gestores
{
    public static class RelacionesGestor
    {
        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "OneToMany",
            "ManyToOne",
            "OneToOne",
            "ManyToMany"
        };

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

        public static int? Crear(int systemId, RelacionCreateRequest request)
        {
            if (!AllowedTypes.Contains(request.RelationType))
                return null;

            using var context = new SystemBaseContext();

            var systemExists = context.Systems.Any(s => s.Id == systemId);
            if (!systemExists)
                return null;

            var source = context.Entities.FirstOrDefault(e => e.Id == request.SourceEntityId && e.SystemId == systemId);
            var target = context.Entities.FirstOrDefault(e => e.Id == request.TargetEntityId && e.SystemId == systemId);
            if (source == null || target == null)
                return null;

            var relation = new Relations
            {
                SystemId = systemId,
                SourceEntityId = request.SourceEntityId,
                TargetEntityId = request.TargetEntityId,
                RelationType = request.RelationType,
                ForeignKey = request.ForeignKey?.Trim(),
                InverseProperty = request.InverseProperty?.Trim(),
                CascadeDelete = request.CascadeDelete,
                CreatedAt = DateTime.UtcNow
            };

            context.Relations.Add(relation);
            context.SaveChanges();

            return relation.Id;
        }

        public static bool Editar(int systemId, int id, RelacionUpdateRequest request)
        {
            if (!AllowedTypes.Contains(request.RelationType))
                return false;

            using var context = new SystemBaseContext();

            var relation = context.Relations.FirstOrDefault(r => r.Id == id && r.SystemId == systemId);
            if (relation == null)
                return false;

            relation.RelationType = request.RelationType;
            relation.ForeignKey = request.ForeignKey?.Trim();
            relation.InverseProperty = request.InverseProperty?.Trim();
            relation.CascadeDelete = request.CascadeDelete;

            context.SaveChanges();
            return true;
        }
    }
}
