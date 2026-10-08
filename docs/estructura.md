# Estructura de carpetas

Guía para ubicarse en el código. La idea es una sola: **todo lo de un tema vive junto, en un módulo**. Si tenés que tocar Usuarios, vas a `backend/Modulos/Usuarios/` y a `frontend/src/modulos/usuarios/`. No hace falta saltar entre carpetas `Controllers`, `Models` y `Gestores`.

Los módulos se llaman igual en el backend y en el frontend:

| Módulo | Qué hace | Backend | Frontend |
|---|---|---|---|
| Auth | Login y registro | `Modulos/Auth` | `modulos/auth` |
| Usuarios | ABM de usuarios | `Modulos/Usuarios` | `modulos/usuarios` |
| Roles | Roles, menús y permisos por rol | `Modulos/Roles` | `modulos/roles` |
| Menu | Menú lateral y vistas autogeneradas | `Modulos/Menu` | `modulos/menu` |
| Sistemas | La fábrica: diseñar, publicar, generar y exportar sistemas | `Modulos/Sistemas` | `modulos/sistemas` |

## Backend (`backend/`)

```
backend/
├── Program.cs              Arranque: DB, CORS, JWT, Swagger, seed
├── Routes.cs               TODAS las URLs de la API en un solo lugar
├── sql/                    Scripts SQL para crear la base (los usa db-service/)
├── Comun/                  Lo que usan todos los módulos
│   ├── AppController.cs        Clase base de los controllers (lee el usuario del token)
│   ├── NombresSql.cs           Reglas de nombres seguros (tablas, columnas, rutas, schema del sistema)
│   ├── Texto.cs                ToKebab / ToPascalCase (rutas y nombres de clases generados)
│   ├── PuertosSistemas.cs      Puertos de los sistemas generados
│   ├── BaseDeDatos/
│   │   ├── SystemBaseContext.cs    DbContext de EF Core
│   │   ├── DbSeeder.cs             Crea el admin (ADMIN_PASSWORD), rol Admin y menús base al iniciar
│   │   └── Tablas/                 Una clase por tabla de la base (Usuarios, Roles, Systems, ...)
│   └── Seguridad/              JWT, política Admin, contraseñas y bloqueo por fuerza bruta
└── Modulos/
    ├── Auth/
    ├── Usuarios/
    ├── Roles/
    ├── Menu/
    ├── DevTools/           Reiniciar el backend de la fábrica (solo DEV)
    └── Sistemas/
        ├── SistemasController.cs   Alta, edición, borrado (archivado) y publicación de sistemas
        ├── SistemasGestor.cs
        ├── Entidades/          Entidades de un sistema (tablas que se van a crear)
        ├── Campos/             Campos de cada entidad (columnas)
        ├── Relaciones/         FKs entre entidades
        ├── Datos/              CRUD genérico sobre las tablas publicadas (/s/<slug>)
        ├── Publicacion/        "Publicar DB": schema sys_<slug>, migraciones seguras (MigracionEsquema), menús y permisos
        ├── ConsolaSql/         Consola SQL aislada (ConsolaSqlGestor) y sincronización de metadata (SincronizadorMetadata)
        ├── GeneradorBackend/   Genera systems/<slug>/backend; credenciales SQL propias por sistema (CredencialesSistema)
        ├── GeneradorFrontend/  Genera systems/<slug>/frontend (copia frontend-runtime/)
        ├── Exportacion/        ZIP / workspace con todo el sistema
        └── Herramientas/       Iniciar/detener backend y frontend generados y sus logs (ProcesosSistemas, LogsProcesos)
```

### Cómo es un módulo por dentro

Todos los módulos tienen la misma forma:

```
Modulos/Usuarios/
├── UsuariosController.cs   Recibe el request HTTP, valida y llama al gestor (que recibe por constructor). Sin lógica de negocio.
├── UsuariosGestor.cs       La lógica: consultas a la base, reglas, validaciones.
└── Modelos/                Lo que entra y sale por la API (XxxRequest, XxxResponse).
```

El flujo de un request es siempre **Routes.cs → Controller → Gestor → base de datos**.

Los gestores son clases normales registradas en `Comun/Inyeccion.cs`. El controller los recibe por constructor y cada gestor recibe una fábrica de contextos (`IDbContextFactory<SystemBaseContext>`): en cada método abre su conexión con `using var context = _contextos.CreateDbContext();`.

```csharp
public class UsuariosController : AppController
{
    private readonly UsuariosGestor _usuariosGestor;
    public UsuariosController(UsuariosGestor usuariosGestor) { _usuariosGestor = usuariosGestor; }
    // ... _usuariosGestor.ObtenerTodos()
}
```

### Namespaces

El namespace es igual a la carpeta: `Modulos/Sistemas/Campos/CamposGestor.cs` está en `Backend.Modulos.Sistemas.Campos`. Si movés un archivo, cambiale el namespace.

Hay una trampa: el namespace `Backend.Modulos.Usuarios` tiene el mismo nombre que la clase de tabla `Usuarios`. Pasa lo mismo con `Roles`. Por eso, dentro de `Modulos/`, las tablas se escriben con el alias `Tablas.Usuarios` (ver `UsuariosGestor.cs`).

### Tablas vs Entidades

- `Comun/BaseDeDatos/Tablas/` son las clases de EF Core que mapean las tablas de la fábrica. Se regeneran con `scaffold-all.sh`, así que no se editan a mano.
- `Modulos/Sistemas/Entidades/` es el módulo de la fábrica que maneja las *entidades que diseña el usuario* (la tabla `sb.Entities`).

## Frontend (`frontend/src/`)

```
src/
├── main.js · App.vue       Arranque de Vue
├── router/index.js         Todas las rutas de pantalla
├── comun/                  Lo que usa toda la app
│   ├── api/axios.js            Cliente HTTP (URL del backend + token)
│   ├── layout/                 MainLayout, header y sidebar
│   ├── store/menu.store.js     Estado del menú lateral
│   ├── config/                 frontend-config.json
│   ├── plugins/ · estilos/ · utils/
└── modulos/
    ├── auth/        Login.vue, Register.vue, auth.service.js
    ├── home/        Home.vue
    ├── usuarios/    Usuarios.vue, usuario.service.js, componentes/
    ├── roles/       Roles.vue, rol.service.js, componentes/
    ├── menu/        Menu.vue, AutoGeneratedView.vue, menu.service.js, componentes/, vistas-generadas/
    └── sistemas/    Sistemas.vue, SistemaEditor.vue, SistemaRuntime.vue, SistemaFrontendEmbed.vue,
                     un *.service.js por recurso, componentes/, editor/
```

### El diseñador de sistemas (`modulos/sistemas/editor/`)

`SistemaEditor.vue` solo arma el encabezado, las pestañas y los diálogos compartidos. Cada pestaña y cada diálogo propio es un componente en `editor/` (`TabDatos`, `TabBackend`, `TabHerramientas`, `TabFrontend`, `Dialogo*`). El estado y la lógica están en `editor/useSistemaEditor.js`, que `SistemaEditor.vue` crea y comparte con `provide('sistemaEditor', ...)`; cada componente toma lo que usa con `inject('sistemaEditor')`. Los estilos de todos están en `editor/sistema-editor.css`.

Para cambiar algo de una pestaña: el template está en su `Tab*.vue` y la función o el dato, en `useSistemaEditor.js` (exportado en el `return` del final).

La URL de la API se puede cambiar con `VITE_API_URL` (por defecto `http://localhost:5032/api/v1`).

### Cómo es un módulo por dentro

- **`.vue` en la raíz del módulo** = pantalla (tiene ruta en `router/index.js`).
- **`componentes/`** = piezas que usan esas pantallas (diálogos, tablas).
- **`*.service.js`** = llamadas al backend de ese módulo. Las pantallas nunca usan axios directo.

### Vistas autogeneradas

Cuando se crea un menú hijo desde la pantalla Menu, el backend (`Modulos/Menu/MenuViewGenerator.cs`) crea `modulos/menu/vistas-generadas/<Padre>/<Hijo>.vue`. `AutoGeneratedView.vue` la busca ahí. No muevas esa carpeta sin cambiar las dos cosas.

## Plantilla de frontends generados (`frontend-runtime/src/`)

Tiene la misma estructura que `frontend/`, pero solo con lo que necesita un sistema generado: `comun/`, `modulos/auth`, `modulos/home` y `modulos/runtime` (la pantalla de datos). El generador (`Modulos/Sistemas/GeneradorFrontend`) la copia a `systems/<slug>/frontend`. Después reescribe dos archivos: `src/comun/api/axios.js` y `src/comun/config/frontend-config.json`. Si los movés, actualizá el generador.

## Tests (`tests/Backend.Tests/`)

```
tests/Backend.Tests/
├── Infra/          EntornoPruebas (API en memoria + base temporal) y Constructores (atajos para armar datos)
├── Integracion/    Un archivo por área: Autenticacion, Autorizacion, Diseno, Publicacion, Datos
└── Unitarios/      Reglas puras, sin base de datos
```

Los tests de integración levantan la API real en memoria contra una base `nodriza_test_<id>` que se crea y se borra sola. Verifican el estado real de la base, no solo la respuesta HTTP. Se corren con `dotnet test` (ver README) y en cada PR por CI.

Antes de tocar algo con riesgo (publicación, permisos, datos) escribí o ajustá el test del área.

## Recetas

**Agregar un endpoint a un módulo existente** (ej: Usuarios)
1. Agregar la URL en `Routes.cs` → `Routes.v1.Usuarios`.
2. Si recibe o devuelve datos nuevos, crear el `Request`/`Response` en `Modulos/Usuarios/Modelos/`.
3. Agregar el método en `UsuariosGestor.cs`.
4. Agregar la acción en `UsuariosController.cs`.
5. En el frontend, agregar la llamada en `modulos/usuarios/usuario.service.js`.

**Agregar un test de integración**
1. Elegí el archivo del área en `tests/Backend.Tests/Integracion/` (o creá uno con `[Collection(ColeccionFabrica.Nombre)]`).
2. Armá los datos con `Constructores` (`CrearSistemaAsync`, `PublicarAsync`, `UsuarioComunAsync`) y operá con `_e.Admin`.
3. Verificá contra la base con `_e.EscalarAsync(...)`, además del status HTTP.

**Crear un módulo nuevo** (ej: Clientes)
- Backend: `Modulos/Clientes/` con `ClientesController.cs`, `ClientesGestor.cs` y `Modelos/`, las rutas en `Routes.cs` y `services.AddSingleton<ClientesGestor>()` en `Comun/Inyeccion.cs`.
- Frontend: `modulos/clientes/` con `Clientes.vue`, `cliente.service.js` y `componentes/`, y la ruta en `router/index.js`.
