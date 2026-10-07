# Nodriza (SystemBase)

Fábrica de sistemas: se diseñan entidades, campos y relaciones desde la web y, con un click por paso, se publica la base de datos y se generan el backend y el frontend de cada sistema. Los sistemas generados quedan alojados en `systems/<slug>/` para seguir desarrollándolos a mano.

## Componentes

- `backend/`: API de la fábrica (.NET 8, EF Core + SQL directo, JWT).
- `frontend/`: UI de la fábrica (Vue 3 + Vuetify + Vite).
- `frontend-runtime/`: plantilla que copia el generador de frontend.
- `backend/sql/`: scripts SQL de la base de la fábrica (dbo + schema `sb`).
- `db-service/`: SQL Server en Docker.
- `systems/`: sistemas generados (`systems/<slug>/backend`, `systems/<slug>/frontend`, `ports.json`). Ignorado por git.
- `exports/`: ZIPs exportados. Ignorado por git.
- `docs/`: documentación funcional.

Backend y frontend están organizados por módulo (Auth, Usuarios, Roles, Menu, Sistemas). Ver [docs/estructura.md](docs/estructura.md) para saber dónde va cada cosa.

## Requisitos

- .NET SDK 8
- Node.js 20.19+ (Vite 7)
- Docker

## Levantar

### 1) Variables de entorno

```bash
cp backend/.env.example backend/.env
```

Completar `DB_*` y `JWT_*` (`JWT_SECRET` de 32+ caracteres). `DB_SERVER=localhost,1433`, `DB_USER=sa`.

### 2) Base de datos

```bash
cd db-service
docker compose --env-file ../backend/.env up -d
```

- `sqlserver`: SQL Server 2022 en `localhost:1433` (override con `DB_PORT`), datos en un volumen de Docker.
- `sqlserver-init`: crea la base, las tablas `dbo` y el schema `sb` (scripts idempotentes de `backend/sql/`).

Resetear desde cero: `docker compose --env-file ../backend/.env down -v` y volver a `up -d`.

### 3) Backend

```bash
cd backend
dotnet watch run
```

`http://localhost:5032` (Swagger en `/swagger`). Al iniciar, `DbSeeder` crea rol Admin, usuario `admin/admin`, módulos y menús base.

### 4) Frontend

```bash
cd frontend
npm install
npm run dev
```

`http://localhost:5173` (si está ocupado, Vite usa el siguiente puerto libre; el backend acepta cualquier origen localhost).

## Flujo

1. Crear sistema en `/sistemas`.
2. Diseñar entidades, campos y relaciones en `/sistemas/{id}`.
3. **Publicar DB**: crea el schema `sys_<slug>` con tablas, FKs, menús y permisos.
4. Operar datos desde el runtime genérico `/s/<slug>`.
5. **Generar backend**: proyecto .NET en `systems/<slug>/backend` (puerto `5032 + id`).
6. **Generar frontend**: copia de `frontend-runtime` en `systems/<slug>/frontend` (puerto `5173 + id`).
7. **Herramientas**: iniciar/detener backend y frontend generados, logs y consola API.
8. **Exportar**: ZIP o workspace con `database.sql`, backend, frontend y `manifest.json`.

Detalle completo en `docs/estado-actual.md`.

## Base de datos

Una sola base (`DB_NAME`) con tres capas:

- `dbo`: usuarios, roles, menús.
- `sb`: metadata de la fábrica (sistemas, entidades, campos, relaciones, permisos, configs).
- `sys_<slug>`: tablas reales de cada sistema publicado.

## Troubleshooting

### No se encontró proyecto MSBuild (`.csproj`) al iniciar un backend generado

Ejecutar **Generar Backend** del sistema y reintentar desde Herramientas.
