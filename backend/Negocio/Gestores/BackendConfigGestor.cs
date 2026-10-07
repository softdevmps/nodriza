using System.Text.Json;
using Backend.Data;
using Backend.Models.Entidades;
using Backend.Models.Sistemas;
using Microsoft.EntityFrameworkCore;

namespace Backend.Negocio.Gestores
{
    public static class BackendConfigGestor
    {
        private const string BackendModuleName = "backend";
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        private static readonly HashSet<string> SoftDeleteNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "isactive",
            "activo",
            "active"
        };

        public static BackendConfigResponse ObtenerPorSistema(int systemId)
        {
            using var context = new SystemBaseContext();
            var moduleId = EnsureBackendModule(context);

            var systemConfig = LoadSystemConfig(context, systemId, moduleId);

            var entityModuleRows = context.EntityModules
                .Where(em => em.ModuleId == moduleId)
                .ToList();

            var entityModules = entityModuleRows
                .GroupBy(em => em.EntityId)
                .ToDictionary(g => g.Key, g => g.First());

            var entities = context.Entities
                .Include(e => e.Fields)
                .Where(e => e.SystemId == systemId)
                .OrderBy(e => e.SortOrder)
                .ThenBy(e => e.Id)
                .ToList();

            var response = new BackendConfigResponse
            {
                System = systemConfig,
                Entities = new List<BackendEntityConfig>()
            };

            foreach (var entity in entities)
            {
                var fields = entity.Fields
                    .OrderBy(f => f.SortOrder)
                    .ThenBy(f => f.Id)
                    .ToList();

                var defaults = BuildDefaultEntityConfig(entity, fields, systemConfig);
                BackendEntityConfigData? data = null;

                if (entityModules.TryGetValue(entity.Id, out var moduleRow) && !string.IsNullOrWhiteSpace(moduleRow.ConfigJson))
                {
                    data = TryDeserialize<BackendEntityConfigData>(moduleRow.ConfigJson);
                }

                var merged = ApplyEntityConfigData(defaults, data);
                if (entityModules.TryGetValue(entity.Id, out var row))
                    merged.IsEnabled = row.IsEnabled;

                response.Entities.Add(merged);
            }

            return response;
        }

        public static void GuardarPorSistema(int systemId, BackendConfigRequest request)
        {
            using var context = new SystemBaseContext();
            var moduleId = EnsureBackendModule(context);

            var systemModule = EnsureSystemModule(context, systemId, moduleId);
            systemModule.ConfigJson = JsonSerializer.Serialize(ToSystemConfigData(request.System), JsonOptions);
            systemModule.IsEnabled = true;

            var entityIds = context.Entities
                .Where(e => e.SystemId == systemId)
                .Select(e => e.Id)
                .ToHashSet();

            foreach (var item in request.Entities)
            {
                if (!entityIds.Contains(item.EntityId))
                    continue;

                var existing = context.EntityModules
                    .FirstOrDefault(em => em.EntityId == item.EntityId && em.ModuleId == moduleId);

                var data = ToEntityConfigData(item);
                var json = JsonSerializer.Serialize(data, JsonOptions);

                if (existing == null)
                {
                    context.EntityModules.Add(new EntityModules
                    {
                        EntityId = item.EntityId,
                        ModuleId = moduleId,
                        IsEnabled = item.IsEnabled,
                        ConfigJson = json
                    });
                }
                else
                {
                    existing.IsEnabled = item.IsEnabled;
                    existing.ConfigJson = json;
                }
            }

            context.SaveChanges();
        }

        private static BackendSystemConfig LoadSystemConfig(SystemBaseContext context, int systemId, int moduleId)
        {
            var defaults = new BackendSystemConfig();
            var module = context.SystemModules
                .FirstOrDefault(sm => sm.SystemId == systemId && sm.ModuleId == moduleId);

            if (module == null || string.IsNullOrWhiteSpace(module.ConfigJson))
                return defaults;

            var data = TryDeserialize<BackendSystemConfigData>(module.ConfigJson);
            if (data == null)
                return defaults;

            return ApplySystemConfigData(defaults, data);
        }

        private static SystemModules EnsureSystemModule(SystemBaseContext context, int systemId, int moduleId)
        {
            var module = context.SystemModules
                .FirstOrDefault(sm => sm.SystemId == systemId && sm.ModuleId == moduleId);

            if (module != null)
                return module;

            module = new SystemModules
            {
                SystemId = systemId,
                ModuleId = moduleId,
                IsEnabled = true
            };

            context.SystemModules.Add(module);
            context.SaveChanges();
            return module;
        }

        private static BackendEntityConfig BuildDefaultEntityConfig(Entities entity, List<Fields> fields, BackendSystemConfig systemConfig)
        {
            var pk = fields.FirstOrDefault(f => f.IsPrimaryKey) ?? fields.FirstOrDefault();
            var softDeleteField = fields.FirstOrDefault(f => SoftDeleteNames.Contains(f.ColumnName));

            var config = new BackendEntityConfig
            {
                EntityId = entity.Id,
                Name = entity.Name,
                DisplayName = entity.DisplayName,
                IsEnabled = true,
                Route = ToKebab(entity.Name),
                RequireAuth = null,
                SoftDelete = softDeleteField != null,
                SoftDeleteFieldId = softDeleteField?.Id,
                Pagination = false,
                DefaultPageSize = null,
                MaxPageSize = null,
                Endpoints = new BackendEndpointsConfig(),
                DefaultSortFieldId = pk?.Id,
                DefaultSortDirection = "asc",
                FilterFieldIds = new List<int>(),
                Fields = new List<BackendFieldConfig>()
            };

            foreach (var field in fields)
            {
                config.Fields.Add(new BackendFieldConfig
                {
                    FieldId = field.Id,
                    Name = field.Name,
                    ColumnName = field.ColumnName,
                    DataType = field.DataType,
                    IsPrimaryKey = field.IsPrimaryKey,
                    IsIdentity = field.IsIdentity,
                    Expose = true,
                    ReadOnly = field.IsIdentity || field.IsPrimaryKey,
                    Required = field.Required,
                    MaxLength = field.MaxLength,
                    Unique = field.IsUnique,
                    DefaultValue = field.DefaultValue,
                    DisplayAs = null
                });
            }

            return config;
        }

        private static BackendEntityConfig ApplyEntityConfigData(BackendEntityConfig defaults, BackendEntityConfigData? data)
        {
            if (data == null)
                return defaults;

            defaults.Route = string.IsNullOrWhiteSpace(data.Route) ? defaults.Route : data.Route!;
            defaults.RequireAuth = data.RequireAuth ?? defaults.RequireAuth;
            defaults.SoftDelete = data.SoftDelete ?? defaults.SoftDelete;
            defaults.SoftDeleteFieldId = data.SoftDeleteFieldId ?? defaults.SoftDeleteFieldId;
            defaults.Pagination = data.Pagination ?? defaults.Pagination;
            defaults.DefaultPageSize = data.DefaultPageSize ?? defaults.DefaultPageSize;
            defaults.MaxPageSize = data.MaxPageSize ?? defaults.MaxPageSize;
            defaults.DefaultSortFieldId = data.DefaultSortFieldId ?? defaults.DefaultSortFieldId;
            defaults.DefaultSortDirection = string.IsNullOrWhiteSpace(data.DefaultSortDirection)
                ? defaults.DefaultSortDirection
                : data.DefaultSortDirection;
            defaults.FilterFieldIds = data.FilterFieldIds ?? defaults.FilterFieldIds;

            if (data.Endpoints != null)
            {
                defaults.Endpoints = data.Endpoints;
            }

            if (data.Fields != null)
            {
                foreach (var field in defaults.Fields)
                {
                    if (!data.Fields.TryGetValue(field.FieldId, out var fieldData))
                        continue;

                    field.Expose = fieldData.Expose ?? field.Expose;
                    field.ReadOnly = fieldData.ReadOnly ?? field.ReadOnly;
                    field.Required = fieldData.Required ?? field.Required;
                    field.MaxLength = fieldData.MaxLength ?? field.MaxLength;
                    field.Unique = fieldData.Unique ?? field.Unique;
                    field.DefaultValue = fieldData.DefaultValue ?? field.DefaultValue;
                    field.DisplayAs = fieldData.DisplayAs ?? field.DisplayAs;
                }
            }

            return defaults;
        }

        private static BackendSystemConfig ApplySystemConfigData(BackendSystemConfig defaults, BackendSystemConfigData data)
        {
            defaults.ApiBase = string.IsNullOrWhiteSpace(data.ApiBase) ? defaults.ApiBase : data.ApiBase!;
            defaults.RequireAuth = data.RequireAuth ?? defaults.RequireAuth;
            defaults.SchemaPrefix = string.IsNullOrWhiteSpace(data.SchemaPrefix) ? defaults.SchemaPrefix : data.SchemaPrefix!;
            defaults.Persistence = string.IsNullOrWhiteSpace(data.Persistence) ? defaults.Persistence : data.Persistence!;
            defaults.DefaultPageSize = data.DefaultPageSize ?? defaults.DefaultPageSize;
            defaults.MaxPageSize = data.MaxPageSize ?? defaults.MaxPageSize;
            defaults.AudioStorageProvider = string.IsNullOrWhiteSpace(data.AudioStorageProvider) ? defaults.AudioStorageProvider : data.AudioStorageProvider!;
            defaults.AudioTranscodeEnabled = data.AudioTranscodeEnabled ?? defaults.AudioTranscodeEnabled;
            defaults.AudioTranscodeFormat = string.IsNullOrWhiteSpace(data.AudioTranscodeFormat)
                ? defaults.AudioTranscodeFormat
                : data.AudioTranscodeFormat!;
            defaults.AudioTranscodeBitrate = string.IsNullOrWhiteSpace(data.AudioTranscodeBitrate)
                ? defaults.AudioTranscodeBitrate
                : data.AudioTranscodeBitrate!;
            defaults.AudioTranscodeDeleteOriginal = data.AudioTranscodeDeleteOriginal ?? defaults.AudioTranscodeDeleteOriginal;
            defaults.AudioRetentionSoftDays = data.AudioRetentionSoftDays ?? defaults.AudioRetentionSoftDays;
            defaults.AudioRetentionPurgeDays = data.AudioRetentionPurgeDays ?? defaults.AudioRetentionPurgeDays;
            defaults.AudioRetentionRunMinutes = data.AudioRetentionRunMinutes ?? defaults.AudioRetentionRunMinutes;
            return defaults;
        }

        private static BackendSystemConfigData ToSystemConfigData(BackendSystemConfig config)
        {
            return new BackendSystemConfigData
            {
                ApiBase = config.ApiBase,
                RequireAuth = config.RequireAuth,
                SchemaPrefix = config.SchemaPrefix,
                Persistence = config.Persistence,
                DefaultPageSize = config.DefaultPageSize,
                MaxPageSize = config.MaxPageSize,
                AudioStorageProvider = config.AudioStorageProvider,
                AudioTranscodeEnabled = config.AudioTranscodeEnabled,
                AudioTranscodeFormat = config.AudioTranscodeFormat,
                AudioTranscodeBitrate = config.AudioTranscodeBitrate,
                AudioTranscodeDeleteOriginal = config.AudioTranscodeDeleteOriginal,
                AudioRetentionSoftDays = config.AudioRetentionSoftDays,
                AudioRetentionPurgeDays = config.AudioRetentionPurgeDays,
                AudioRetentionRunMinutes = config.AudioRetentionRunMinutes
            };
        }

        private static BackendEntityConfigData ToEntityConfigData(BackendEntityConfig config)
        {
            var data = new BackendEntityConfigData
            {
                Route = config.Route,
                RequireAuth = config.RequireAuth,
                SoftDelete = config.SoftDelete,
                SoftDeleteFieldId = config.SoftDeleteFieldId,
                Pagination = config.Pagination,
                DefaultPageSize = config.DefaultPageSize,
                MaxPageSize = config.MaxPageSize,
                DefaultSortFieldId = config.DefaultSortFieldId,
                DefaultSortDirection = config.DefaultSortDirection,
                FilterFieldIds = config.FilterFieldIds,
                Endpoints = config.Endpoints,
                Fields = new Dictionary<int, BackendFieldConfigData>()
            };

            foreach (var field in config.Fields)
            {
                data.Fields[field.FieldId] = new BackendFieldConfigData
                {
                    Expose = field.Expose,
                    ReadOnly = field.ReadOnly,
                    Required = field.Required,
                    MaxLength = field.MaxLength,
                    Unique = field.Unique,
                    DefaultValue = field.DefaultValue,
                    DisplayAs = field.DisplayAs
                };
            }

            return data;
        }

        private static T? TryDeserialize<T>(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch
            {
                return default;
            }
        }

        private static int EnsureBackendModule(SystemBaseContext context)
        {
            var module = context.Modules.FirstOrDefault(m => m.Name == BackendModuleName);
            if (module != null)
                return module.Id;

            module = new Modules
            {
                Name = BackendModuleName,
                Version = "1.0",
                Description = "Configuracion del generador de backend"
            };

            context.Modules.Add(module);
            context.SaveChanges();

            return module.Id;
        }

        private static string ToKebab(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "item";

            var sb = new System.Text.StringBuilder();
            var prevDash = false;

            foreach (var ch in value.Trim())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    if (char.IsUpper(ch) && sb.Length > 0 && !prevDash)
                        sb.Append('-');

                    sb.Append(char.ToLowerInvariant(ch));
                    prevDash = false;
                }
                else
                {
                    if (!prevDash && sb.Length > 0)
                    {
                        sb.Append('-');
                        prevDash = true;
                    }
                }
            }

            var result = sb.ToString().Trim('-');
            return string.IsNullOrWhiteSpace(result) ? "item" : result;
        }

        private class BackendSystemConfigData
        {
            public string? ApiBase { get; set; }
            public bool? RequireAuth { get; set; }
            public string? SchemaPrefix { get; set; }
            public string? Persistence { get; set; }
            public int? DefaultPageSize { get; set; }
            public int? MaxPageSize { get; set; }
            public string? AudioStorageProvider { get; set; }
            public bool? AudioTranscodeEnabled { get; set; }
            public string? AudioTranscodeFormat { get; set; }
            public string? AudioTranscodeBitrate { get; set; }
            public bool? AudioTranscodeDeleteOriginal { get; set; }
            public int? AudioRetentionSoftDays { get; set; }
            public int? AudioRetentionPurgeDays { get; set; }
            public int? AudioRetentionRunMinutes { get; set; }
        }

        private class BackendEntityConfigData
        {
            public string? Route { get; set; }
            public bool? RequireAuth { get; set; }
            public bool? SoftDelete { get; set; }
            public int? SoftDeleteFieldId { get; set; }
            public bool? Pagination { get; set; }
            public int? DefaultPageSize { get; set; }
            public int? MaxPageSize { get; set; }
            public BackendEndpointsConfig? Endpoints { get; set; }
            public int? DefaultSortFieldId { get; set; }
            public string? DefaultSortDirection { get; set; }
            public List<int>? FilterFieldIds { get; set; }
            public Dictionary<int, BackendFieldConfigData>? Fields { get; set; }
        }

        private class BackendFieldConfigData
        {
            public bool? Expose { get; set; }
            public bool? ReadOnly { get; set; }
            public bool? Required { get; set; }
            public int? MaxLength { get; set; }
            public bool? Unique { get; set; }
            public string? DefaultValue { get; set; }
            public string? DisplayAs { get; set; }
        }
    }
}
