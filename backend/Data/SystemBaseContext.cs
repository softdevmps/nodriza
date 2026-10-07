using System;
using System.Collections.Generic;
using Backend.Models.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public partial class SystemBaseContext : DbContext
{
    public SystemBaseContext()
    {
    }

    public SystemBaseContext(DbContextOptions<SystemBaseContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Entities> Entities { get; set; }

    public virtual DbSet<Fields> Fields { get; set; }

    public virtual DbSet<Menus> Menus { get; set; }

    public virtual DbSet<Modules> Modules { get; set; }

    public virtual DbSet<Permissions> Permissions { get; set; }

    public virtual DbSet<Relations> Relations { get; set; }

    public virtual DbSet<Roles> Roles { get; set; }

    public virtual DbSet<SystemMenuRoles> SystemMenuRoles { get; set; }

    public virtual DbSet<SystemMenus> SystemMenus { get; set; }

    public virtual DbSet<SystemBuilds> SystemBuilds { get; set; }

    public virtual DbSet<SystemModules> SystemModules { get; set; }

    public virtual DbSet<Systems> Systems { get; set; }

    public virtual DbSet<Usuarios> Usuarios { get; set; }

    public virtual DbSet<EntityModules> EntityModules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entities>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Entities__3214EC07F9A26605");

            entity.ToTable("Entities", "sb");

            entity.HasIndex(e => new { e.SystemId, e.Name }, "UX_sb_Entities_System_Name").IsUnique();

            entity.HasIndex(e => new { e.SystemId, e.TableName }, "UX_sb_Entities_System_TableName").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DisplayName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.SortOrder).HasDefaultValue(1);
            entity.Property(e => e.TableName).HasMaxLength(128);

            entity.HasOne(d => d.System).WithMany(p => p.Entities)
                .HasForeignKey(d => d.SystemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sb_Entities_System");
        });

        modelBuilder.Entity<Fields>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Fields__3214EC072453D8D7");

            entity.ToTable("Fields", "sb");

            entity.HasIndex(e => new { e.EntityId, e.ColumnName }, "UX_sb_Fields_Entity_ColumnName").IsUnique();

            entity.HasIndex(e => new { e.EntityId, e.Name }, "UX_sb_Fields_Entity_Name").IsUnique();

            entity.Property(e => e.ColumnName).HasMaxLength(128);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.DataType).HasMaxLength(50);
            entity.Property(e => e.DefaultValue).HasMaxLength(200);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.SortOrder).HasDefaultValue(1);
            entity.Property(e => e.Required).HasDefaultValue(false);
            entity.Property(e => e.MaxLength);
            entity.Property(e => e.Precision);
            entity.Property(e => e.Scale);
            entity.Property(e => e.IsPrimaryKey).HasDefaultValue(false);
            entity.Property(e => e.IsIdentity).HasDefaultValue(false);
            entity.Property(e => e.IsUnique).HasDefaultValue(false);
            entity.Property(e => e.UiConfigJson);

            entity.HasOne(d => d.Entity).WithMany(p => p.Fields)
                .HasForeignKey(d => d.EntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sb_Fields_Entity");
        });

        modelBuilder.Entity<Menus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Menus__3214EC07F7F592C7");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Icono)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Ruta)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Titulo)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Padre).WithMany(p => p.InversePadre)
                .HasForeignKey(d => d.PadreId)
                .HasConstraintName("FK_Menus_Padre");
        });

        modelBuilder.Entity<Modules>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Modules__3214EC07A63BA0A5");

            entity.ToTable("Modules", "sb");

            entity.HasIndex(e => e.Name, "UQ__Modules__737584F67205A572").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Version).HasMaxLength(20);
        });

        modelBuilder.Entity<Permissions>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Permissi__3214EC0742A89361");

            entity.ToTable("Permissions", "sb");

            entity.HasIndex(e => new { e.SystemId, e.Key }, "UX_sb_Permissions_System_Key").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.Key).HasMaxLength(150);

            entity.HasOne(d => d.System).WithMany(p => p.Permissions)
                .HasForeignKey(d => d.SystemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sb_Permissions_System");
        });

        modelBuilder.Entity<Relations>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Relation__3214EC0789B316C1");

            entity.ToTable("Relations", "sb");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ForeignKey).HasMaxLength(128);
            entity.Property(e => e.InverseProperty).HasMaxLength(128);
            entity.Property(e => e.RelationType).HasMaxLength(30);
            entity.Property(e => e.CascadeDelete).HasDefaultValue(false);

            entity.HasOne(d => d.SourceEntity).WithMany(p => p.RelationsSourceEntity)
                .HasForeignKey(d => d.SourceEntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sb_Relations_SourceEntity");

            entity.HasOne(d => d.System).WithMany(p => p.Relations)
                .HasForeignKey(d => d.SystemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sb_Relations_System");

            entity.HasOne(d => d.TargetEntity).WithMany(p => p.RelationsTargetEntity)
                .HasForeignKey(d => d.TargetEntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sb_Relations_TargetEntity");
        });

        modelBuilder.Entity<Roles>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC073595AE93");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasMany(d => d.Menu).WithMany(p => p.Rol)
                .UsingEntity<Dictionary<string, object>>(
                    "RolMenu",
                    r => r.HasOne<Menus>().WithMany()
                        .HasForeignKey("MenuId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__RolMenu__MenuId__6E01572D"),
                    l => l.HasOne<Roles>().WithMany()
                        .HasForeignKey("RolId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__RolMenu__RolId__6D0D32F4"),
                    j =>
                    {
                        j.HasKey("RolId", "MenuId").HasName("PK__RolMenu__E5BAEFD27D7A5CDD");
                    });

            entity.HasMany(d => d.Permission).WithMany(p => p.Role)
                .UsingEntity<Dictionary<string, object>>(
                    "RolePermissions",
                    r => r.HasOne<Permissions>().WithMany()
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__RolePermi__Permi__71D1E811"),
                    l => l.HasOne<Roles>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__RolePermi__RoleI__70DDC3D8"),
                    j =>
                    {
                        j.HasKey("RoleId", "PermissionId").HasName("PK__RolePerm__6400A1A8E80EA170");
                        j.ToTable("RolePermissions", "sb");
                    });
        });

        modelBuilder.Entity<SystemMenuRoles>(entity =>
        {
            entity.HasKey(e => new { e.SystemMenuId, e.RoleId }).HasName("PK__SystemMe__2E59CDF941182F30");

            entity.ToTable("SystemMenuRoles", "sb");
        });

        modelBuilder.Entity<SystemMenus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SystemMe__3214EC07DC344DEC");

            entity.ToTable("SystemMenus", "sb");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Route).HasMaxLength(200);
            entity.Property(e => e.SortOrder).HasDefaultValue(1);
            entity.Property(e => e.Title).HasMaxLength(100);

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK_sb_SystemMenus_Parent");

            entity.HasOne(d => d.System).WithMany(p => p.SystemMenus)
                .HasForeignKey(d => d.SystemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sb_SystemMenus_System");

            entity.HasMany(d => d.Role).WithMany(p => p.SystemMenu)
                .UsingEntity<SystemMenuRoles>(
                    r => r.HasOne<Roles>().WithMany().HasForeignKey(e => e.RoleId).HasConstraintName("FK_sb_SystemMenuRoles_Role"),
                    l => l.HasOne<SystemMenus>().WithMany().HasForeignKey(e => e.SystemMenuId).HasConstraintName("FK_sb_SystemMenuRoles_SystemMenu"),
                    j =>
                    {
                        j.HasKey(e => new { e.SystemMenuId, e.RoleId }).HasName("PK__SystemMe__2E59CDF941182F30");
                        j.ToTable("SystemMenuRoles", "sb");
                    });
        });

        modelBuilder.Entity<SystemBuilds>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SystemBu__3214EC07");

            entity.ToTable("SystemBuilds", "sb");

            entity.Property(e => e.Version).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.StartedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FinishedAt);
            entity.Property(e => e.Log);

            entity.HasOne(d => d.System).WithMany(p => p.SystemBuilds)
                .HasForeignKey(d => d.SystemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sb_SystemBuilds_System");
        });

        modelBuilder.Entity<EntityModules>(entity =>
        {
            entity.HasKey(e => new { e.EntityId, e.ModuleId }).HasName("PK__EntityMo__7B0D5DC9");

            entity.ToTable("EntityModules", "sb");

            entity.Property(e => e.ConfigJson);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);

            entity.HasOne(d => d.Entity).WithMany(p => p.EntityModules)
                .HasForeignKey(d => d.EntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sb_EntityModules_Entity");

            entity.HasOne(d => d.Module).WithMany(p => p.EntityModules)
                .HasForeignKey(d => d.ModuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sb_EntityModules_Module");
        });

        modelBuilder.Entity<SystemModules>(entity =>
        {
            entity.HasKey(e => new { e.SystemId, e.ModuleId }).HasName("PK__SystemMo__F123B1F069F4A87C");

            entity.ToTable("SystemModules", "sb");

            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.ConfigJson);

            entity.HasOne(d => d.Module).WithMany(p => p.SystemModules)
                .HasForeignKey(d => d.ModuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sb_SystemModules_Module");

            entity.HasOne(d => d.System).WithMany(p => p.SystemModules)
                .HasForeignKey(d => d.SystemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sb_SystemModules_System");
        });

        modelBuilder.Entity<Systems>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Systems__3214EC07889BECB1");

            entity.ToTable("Systems", "sb");

            entity.HasIndex(e => e.Slug, "UQ__Systems__BC7B5FB6F600BA1D").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.PublishedAt);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Namespace).HasMaxLength(200);
            entity.Property(e => e.Slug).HasMaxLength(80);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("draft");
            entity.Property(e => e.Version).HasMaxLength(20);
        });

        modelBuilder.Entity<Usuarios>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuarios__3214EC07677D505A");

            entity.HasIndex(e => e.Username, "UQ__Usuarios__536C85E43F09255D").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Usuarios__A9D1053486D218DA").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Apellido)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RolId)
                .HasConstraintName("FK_Usuarios_Roles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
