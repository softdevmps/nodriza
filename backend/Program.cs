using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Backend.Comun;
using Backend.Comun.BaseDeDatos;
using Backend.Comun.Seguridad;
// ===============================
// 🔹 Cargar variables de entorno
// ===============================
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ===============================
// 🔐 Connection String desde .env
// ===============================
var connectionString = SystemBaseContext.BuildConnectionString();
// No se imprime: contiene la contraseña de la base.

// No anunciar la tecnología del servidor (cabecera "Server: Kestrel").
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// ===============================
// 🗄️ DbContext
// ===============================
// Fábrica de contextos: los gestores abren uno por operación. También registra SystemBaseContext
// por request (lo usan la política Admin, la validación del token y el seed).
builder.Services.AddDbContextFactory<SystemBaseContext>(options =>
    options.UseSqlServer(connectionString));

// Gestores de cada módulo (ver Comun/Inyeccion.cs)
builder.Services.AddGestores();

// ===============================
// 🌍 CORS (FRONTEND VUE)
// ===============================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // Cualquier puerto local: vite usa 5174, 5175... si 5173 esta ocupado
        policy
            .SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Content-Disposition");
    });
});

// ===============================
// 🔐 JWT Authentication
// ===============================
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
            ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    Environment.GetEnvironmentVariable("JWT_SECRET")!)
            )
        };

        // Un token sigue siendo válido hasta que vence: además se exige que el usuario siga activo,
        // así desactivar a alguien le corta el acceso en el momento.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                if (!int.TryParse(context.Principal?.FindFirst("usuarioId")?.Value, out var usuarioId))
                {
                    context.Fail("Token sin usuario.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<SystemBaseContext>();
                var activo = await db.Usuarios.AsNoTracking().AnyAsync(u => u.Id == usuarioId && u.Activo);
                if (!activo)
                    context.Fail("Usuario inactivo.");
            }
        };
    });

// ===============================
// 🛡️ Autorización: política Admin por rol (ver Comun/Seguridad/PoliticaAdmin.cs)
// ===============================
builder.Services.AddScoped<IAuthorizationHandler, RequiereAdminHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Politicas.Admin, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new RequiereAdmin()));
});

// ===============================
// 🌐 Controllers + Swagger
// ===============================
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ===============================
// 🌱 Seed inicial (roles/menus/modulos/admin)
// ===============================
DbSeeder.Seed(app.Services, app.Logger);

// ===============================
// 🔧 Middleware
// ===============================

// Errores no controlados: se registran en el log y el cliente recibe un mensaje genérico,
// nunca el stack trace ni el mensaje de SQL Server (también en Development).
app.UseExceptionHandler(errores => errores.Run(async context =>
{
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    app.Logger.LogError(feature?.Error, "Error no controlado en {Metodo} {Ruta}", context.Request.Method, context.Request.Path);
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new
    {
        message = "Ocurrió un error inesperado. El detalle quedó registrado en el servidor.",
        traceId = context.TraceIdentifier
    });
}));

// Cabeceras de seguridad básicas.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    if (context.Request.Path.StartsWithSegments("/api"))
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ⚠️ ORDEN IMPORTANTE
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// ===============================
// 🎯 Controllers
// ===============================
app.MapControllers();

app.Run();

// Expone Program para los tests de integración (WebApplicationFactory<Program>).
public partial class Program { }
