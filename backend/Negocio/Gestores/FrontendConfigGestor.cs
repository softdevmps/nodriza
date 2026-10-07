using System.Text.Json;
using Backend.Data;
using Backend.Models.Entidades;
using Backend.Models.Sistemas;
using Microsoft.EntityFrameworkCore;

namespace Backend.Negocio.Gestores
{
    public static class FrontendConfigGestor
    {
        private const string FrontendModuleName = "frontend";
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        public static FrontendConfigResponse ObtenerPorSistema(int systemId)
        {
            using var context = new SystemBaseContext();
            var moduleId = EnsureFrontendModule(context);

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

            var response = new FrontendConfigResponse
            {
                System = systemConfig,
                Entities = new List<FrontendEntityConfig>()
            };

            foreach (var entity in entities)
            {
                var fields = entity.Fields
                    .OrderBy(f => f.SortOrder)
                    .ThenBy(f => f.Id)
                    .ToList();

                var defaults = BuildDefaultEntityConfig(entity, fields);
                FrontendEntityConfig? data = null;

                if (entityModules.TryGetValue(entity.Id, out var moduleRow) && !string.IsNullOrWhiteSpace(moduleRow.ConfigJson))
                {
                    data = TryDeserialize<FrontendEntityConfig>(moduleRow.ConfigJson);
                }

                var merged = ApplyEntityConfigData(defaults, data);
                response.Entities.Add(merged);
            }

            return response;
        }

        public static void GuardarPorSistema(int systemId, FrontendConfigRequest request)
        {
            using var context = new SystemBaseContext();
            var moduleId = EnsureFrontendModule(context);

            var systemModule = EnsureSystemModule(context, systemId, moduleId);
            systemModule.ConfigJson = JsonSerializer.Serialize(request.System, JsonOptions);
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

                var json = JsonSerializer.Serialize(item, JsonOptions);

                if (existing == null)
                {
                    context.EntityModules.Add(new EntityModules
                    {
                        EntityId = item.EntityId,
                        ModuleId = moduleId,
                        IsEnabled = true,
                        ConfigJson = json
                    });
                }
                else
                {
                    existing.IsEnabled = true;
                    existing.ConfigJson = json;
                }
            }

            context.SaveChanges();
        }

        public static void HabilitarFrontend(int systemId)
        {
            using var context = new SystemBaseContext();
            var moduleId = EnsureFrontendModule(context);

            var module = context.SystemModules
                .FirstOrDefault(sm => sm.SystemId == systemId && sm.ModuleId == moduleId);

            if (module == null)
            {
                module = new SystemModules
                {
                    SystemId = systemId,
                    ModuleId = moduleId,
                    IsEnabled = true
                };
                context.SystemModules.Add(module);
            }
            else
            {
                module.IsEnabled = true;
            }

            context.SaveChanges();
        }

        private static FrontendSystemConfig LoadSystemConfig(SystemBaseContext context, int systemId, int moduleId)
        {
            var defaults = BuildDefaultSystemConfig(context, systemId);
            var module = context.SystemModules
                .FirstOrDefault(sm => sm.SystemId == systemId && sm.ModuleId == moduleId);

            if (module == null || string.IsNullOrWhiteSpace(module.ConfigJson))
                return defaults;

            var data = TryDeserialize<FrontendSystemConfig>(module.ConfigJson);
            if (data == null)
                return defaults;

            NormalizeSystemConfig(data, defaults);
            return data;
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

        private static int EnsureFrontendModule(SystemBaseContext context)
        {
            var module = context.Modules.FirstOrDefault(m => m.Name == FrontendModuleName);
            if (module != null)
                return module.Id;

            module = new Modules
            {
                Name = FrontendModuleName,
                Description = "Frontend config"
            };
            context.Modules.Add(module);
            context.SaveChanges();
            return module.Id;
        }

        private static FrontendSystemConfig BuildDefaultSystemConfig(SystemBaseContext context, int systemId)
        {
            var defaults = new FrontendSystemConfig();
            var system = context.Systems
                .AsNoTracking()
                .FirstOrDefault(s => s.Id == systemId);

            if (system == null)
                return defaults;

            var systemName = system.Name?.Trim();
            if (!string.IsNullOrWhiteSpace(systemName))
                defaults.AppTitle = systemName;

            var systemTagline = system.Description?.Trim();
            if (!string.IsNullOrWhiteSpace(systemTagline))
                defaults.Tagline = systemTagline;

            return defaults;
        }

        private static void NormalizeSystemConfig(FrontendSystemConfig config, FrontendSystemConfig defaults)
        {
            config.AppTitle = ResolveSystemTitle(config.AppTitle, defaults.AppTitle);

            if (string.IsNullOrWhiteSpace(config.Tagline))
                config.Tagline = defaults.Tagline;
        }

        private static string ResolveSystemTitle(string? configuredTitle, string fallbackTitle)
        {
            var title = (configuredTitle ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(title))
                return fallbackTitle;

            var isGenericTitle = title.Equals("SystemBase", StringComparison.OrdinalIgnoreCase)
                || title.Equals("Sistema", StringComparison.OrdinalIgnoreCase);

            var hasSpecificFallback = !string.IsNullOrWhiteSpace(fallbackTitle)
                && !fallbackTitle.Equals("SystemBase", StringComparison.OrdinalIgnoreCase)
                && !fallbackTitle.Equals("Sistema", StringComparison.OrdinalIgnoreCase);

            return isGenericTitle && hasSpecificFallback ? fallbackTitle : title;
        }

        private static FrontendEntityConfig BuildDefaultEntityConfig(Entities entity, List<Fields> fields)
        {
            var config = new FrontendEntityConfig
            {
                EntityId = entity.Id,
                Name = entity.Name,
                DisplayName = entity.DisplayName,
                MenuLabel = entity.DisplayName ?? entity.Name,
                ShowInMenu = true,
                MenuIcon = "mdi-table",
                RouteSlug = null,
                ListStickyHeader = false,
                ListShowTotals = true,
                DefaultSortFieldId = null,
                DefaultSortDirection = "asc",
                FormLayout = "single",
                ConfirmSave = true,
                ConfirmDelete = true,
                EnableDuplicate = true,
                Messages = new FrontendEntityMessages(),
                Fields = new List<FrontendFieldConfig>()
            };

            foreach (var field in fields)
            {
                config.Fields.Add(new FrontendFieldConfig
                {
                    FieldId = field.Id,
                    Name = field.Name,
                    ColumnName = field.ColumnName,
                    DataType = field.DataType,
                    IsPrimaryKey = field.IsPrimaryKey,
                    IsIdentity = field.IsIdentity,
                    Required = field.Required,
                    MaxLength = field.MaxLength,
                    Label = field.Name,
                    ShowInList = true,
                    ShowInForm = !field.IsIdentity,
                    ShowInFilter = true,
                    Placeholder = null,
                    HelpText = null,
                    InputType = null,
                    Section = "General",
                    Format = null,
                    Min = null,
                    Max = null,
                    Pattern = null,
                    QuickToggle = false
                });
            }

            return config;
        }

        private static FrontendEntityConfig ApplyEntityConfigData(FrontendEntityConfig defaults, FrontendEntityConfig? data)
        {
            if (data == null)
                return defaults;

            defaults.DisplayName = string.IsNullOrWhiteSpace(data.DisplayName) ? defaults.DisplayName : data.DisplayName;
            defaults.MenuLabel = string.IsNullOrWhiteSpace(data.MenuLabel) ? defaults.MenuLabel : data.MenuLabel;
            defaults.ShowInMenu = data.ShowInMenu;
            defaults.MenuIcon = string.IsNullOrWhiteSpace(data.MenuIcon) ? defaults.MenuIcon : data.MenuIcon;
            defaults.RouteSlug = string.IsNullOrWhiteSpace(data.RouteSlug) ? defaults.RouteSlug : data.RouteSlug;
            defaults.ListStickyHeader = data.ListStickyHeader;
            defaults.ListShowTotals = data.ListShowTotals;
            defaults.DefaultSortFieldId = data.DefaultSortFieldId ?? defaults.DefaultSortFieldId;
            defaults.DefaultSortDirection = string.IsNullOrWhiteSpace(data.DefaultSortDirection)
                ? defaults.DefaultSortDirection
                : data.DefaultSortDirection;
            defaults.FormLayout = string.IsNullOrWhiteSpace(data.FormLayout) ? defaults.FormLayout : data.FormLayout;
            defaults.ConfirmSave = data.ConfirmSave;
            defaults.ConfirmDelete = data.ConfirmDelete;
            defaults.EnableDuplicate = data.EnableDuplicate;
            if (data.Messages != null)
            {
                defaults.Messages.Empty = string.IsNullOrWhiteSpace(data.Messages.Empty) ? defaults.Messages.Empty : data.Messages.Empty;
                defaults.Messages.Error = string.IsNullOrWhiteSpace(data.Messages.Error) ? defaults.Messages.Error : data.Messages.Error;
                defaults.Messages.SuccessCreate = string.IsNullOrWhiteSpace(data.Messages.SuccessCreate) ? defaults.Messages.SuccessCreate : data.Messages.SuccessCreate;
                defaults.Messages.SuccessUpdate = string.IsNullOrWhiteSpace(data.Messages.SuccessUpdate) ? defaults.Messages.SuccessUpdate : data.Messages.SuccessUpdate;
                defaults.Messages.SuccessDelete = string.IsNullOrWhiteSpace(data.Messages.SuccessDelete) ? defaults.Messages.SuccessDelete : data.Messages.SuccessDelete;
            }

            if (data.Fields != null && data.Fields.Count > 0)
            {
                var map = data.Fields.ToDictionary(f => f.FieldId, f => f);
                var ordered = new List<FrontendFieldConfig>();
                var used = new HashSet<int>();

                foreach (var custom in data.Fields)
                {
                    var field = defaults.Fields.FirstOrDefault(f => f.FieldId == custom.FieldId);
                    if (field == null)
                        continue;

                    field.DataType = string.IsNullOrWhiteSpace(custom.DataType) ? field.DataType : custom.DataType;
                    field.MaxLength = custom.MaxLength ?? field.MaxLength;
                    field.Label = string.IsNullOrWhiteSpace(custom.Label) ? field.Label : custom.Label;
                    field.ShowInList = custom.ShowInList;
                    field.ShowInForm = custom.ShowInForm;
                    field.ShowInFilter = custom.ShowInFilter;
                    field.Placeholder = string.IsNullOrWhiteSpace(custom.Placeholder) ? field.Placeholder : custom.Placeholder;
                    field.HelpText = string.IsNullOrWhiteSpace(custom.HelpText) ? field.HelpText : custom.HelpText;
                    field.InputType = string.IsNullOrWhiteSpace(custom.InputType) ? field.InputType : custom.InputType;
                    field.Section = string.IsNullOrWhiteSpace(custom.Section) ? field.Section : custom.Section;
                    field.Format = string.IsNullOrWhiteSpace(custom.Format) ? field.Format : custom.Format;
                    field.Min = custom.Min ?? field.Min;
                    field.Max = custom.Max ?? field.Max;
                    field.Pattern = string.IsNullOrWhiteSpace(custom.Pattern) ? field.Pattern : custom.Pattern;
                    field.QuickToggle = custom.QuickToggle;
                    ordered.Add(field);
                    used.Add(field.FieldId);
                }

                foreach (var field in defaults.Fields)
                {
                    if (!used.Contains(field.FieldId))
                        ordered.Add(field);
                }

                defaults.Fields = ordered;
            }

            return defaults;
        }

        private static T? TryDeserialize<T>(string json) where T : class
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }
    }
}
