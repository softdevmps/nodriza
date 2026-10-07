using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Backend.Data;
using Backend.Utils;

// ===============================
// 🔹 Cargar variables de entorno
// ===============================
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ===============================
// 🔐 Connection String desde .env
// ===============================
var connectionString = SystemBaseContext.BuildConnectionString();


Console.WriteLine("==== CONNECTION STRING ====");
Console.WriteLine(connectionString);
Console.WriteLine("===========================");

// ===============================
// 🗄️ DbContext
// ===============================
builder.Services.AddDbContext<SystemBaseContext>(options =>
    options.UseSqlServer(connectionString));

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
