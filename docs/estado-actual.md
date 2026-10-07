# Estado Actual - SystemBase

Este documento resume lo que ya implementamos hasta ahora en SystemBase y como se usa en el flujo actual de trabajo.

## 1. Objetivo de la fase actual
En esta fase construimos el pipeline minimo para:
- Definir sistemas por metadata (sistemas, entidades, campos, relaciones).
- Publicar un sistema y generar sus tablas en SQL Server.
- Operar datos en runtime con una UI generica.
- Ver sistemas publicados desde el sidebar, sin escribir URLs.

## 2. Stack actual
- Backend: .NET (API REST).
- Frontend: Vue 3 + Vuetify.
- DB: SQL Server.

## 3. Base de datos
### 3.1 Metadata (schema `sb`)
Contiene la definicion de sistemas y sus piezas:
- `sb.Systems`
- `sb.Entities`
- `sb.Fields`
- `sb.Relations`
- `sb.SystemModules`
- `sb.EntityModules`
- `sb.SystemMenus`
- `sb.SystemMenuRoles`
- `sb.SystemBuilds`
- otros (Modules, Permissions, RolePermissions, etc).

### 3.2 Tablas runtime (schema `sys_{slug}`)
Cuando publicas un sistema se crea un schema con el slug:
Ej: `sys_inventario`.

Dentro de ese schema se crean las tablas reales:
- `sys_inventario.Productos`
- `sys_inventario.Almacenes`
- `sys_inventario.Movimientos`

### 3.3 Seed inicial (recuperacion rapida)
Al iniciar el backend, se ejecuta un seed automatico que asegura lo minimo:
- Modulos base (`backend`, `frontend`).
- Rol **Admin**.
- Usuario **admin/admin** (password hasheado).
- Menus base (Home + Sistema con sus hijos).
- Asignacion de menus base al rol Admin.

Esto permite reconstruir la base de datos desde cero sin perder el acceso a la UI.

## 4. Backend (resumen funcional)
### 4.1 Sistemas
CRUD + publicar:
- `GET /api/v1/sistemas`
- `POST /api/v1/sistemas`
- `PUT /api/v1/sistemas/{id}`
- `DELETE /api/v1/sistemas/{id}`
- `POST /api/v1/sistemas/{id}/publicar`
- `POST /api/v1/sistemas/{id}/export`
- `POST /api/v1/sistemas/{id}/sql/execute` (solo DEV/admin, schema `sys_{slug}`, sincroniza metadata de entidades/campos por defecto)

### 4.2 Entidades y campos
- `GET /api/v1/sistemas/{systemId}/entidades`
- `POST /api/v1/sistemas/{systemId}/entidades`
- `GET /api/v1/sistemas/{systemId}/entidades/{id}`
- `GET /api/v1/sistemas/{systemId}/entidades/by-name/{name}`
- `GET /api/v1/sistemas/{systemId}/entidades/{entityId}/campos`
- `POST /api/v1/sistemas/{systemId}/entidades/{entityId}/campos`
- `DELETE /api/v1/sistemas/{systemId}/entidades/{id}?dropTable=true|false` (opcionalmente elimina tabla runtime `sys_{slug}.[TableName]`)

### 4.3 Relaciones
Se usan para FK y combos en runtime:
- `GET /api/v1/sistemas/{systemId}/relaciones`
- `POST /api/v1/sistemas/{systemId}/relaciones`
- `PUT /api/v1/sistemas/{systemId}/relaciones/{id}`

### 4.4 Datos runtime (CRUD generico)
- `GET /api/v1/sistemas/{systemId}/entidades/{entityId}/datos`
- `POST /api/v1/sistemas/{systemId}/entidades/{entityId}/datos`
- `PUT /api/v1/sistemas/{systemId}/entidades/{entityId}/datos/{id}`
- `DELETE /api/v1/sistemas/{systemId}/entidades/{entityId}/datos/{id}`

## 5. Publicador (que hace al publicar)
Al publicar un sistema:
1. Valida nombres/slug.
2. Crea el schema `sys_{slug}` si no existe.
3. Crea tablas y columnas.
4. Aplica PK, identity y unique index.
5. Aplica relaciones FK usando `sb.Relations`.
6. Genera menu runtime en `sb.SystemMenus`.
7. Registra `sb.SystemBuilds` y marca el sistema como `published`.

## 5.1 Exportador (paquete de salida)
Permite generar un paquete reproducible con SQL + runtime completo:
- Endpoint: `POST /api/v1/sistemas/{id}/export`
- Parametro opcional: `?full=true` (incluye menus de administracion).
- Modos:
  - `?mode=zip` descarga el ZIP (default).
  - `?mode=workspace` genera carpeta en `systems/<slug>/`.
    - `?overwrite=true` para reemplazar si ya existe.
- Salida: descarga `zip` con:
  - `database.sql`: schema + metadata + runtime (sin datos).
  - `backend/`: API completa (igual a SystemBase).
  - `frontend/`: UI completa (igual a SystemBase).
  - `manifest.json`: definicion del sistema (entidades, campos, relaciones).
  - `README.md`: instrucciones y credenciales.
  - `env.example`: ejemplo visible de variables (copia de `.env.example`).
- Copia en disco: `backend/exports/<slug>_v<version>_<timestamp>/`
  - Override: variable `SYSTEMBASE_EXPORT_ROOT`
- Workspace: carpeta en `systems/<slug>/`
  - Override: variable `SYSTEMBASE_SYSTEMS_ROOT`
- Credenciales generadas en SQL:
  - usuario `admin` / password `admin`

## 5.2 Generador de backend (fase inicial)
Se agrego un generador para crear un backend CRUD por entidad:
- Endpoint: `POST /api/v1/sistemas/{id}/generar-backend`
  - Parametro: `?overwrite=true` para reemplazar si ya existe.
- Salida: `systems/<slug>/backend/`
  - Proyecto .NET 8 con JWT y Swagger.
  - Arquitectura igual a SystemBase: `Routes.cs`, `Controllers` -> `Gestores`, `Models/Entidades` + `Models/<Entidad>` con `Create/Update/Response`, `AuthController`/`AuthGestor`/`JwtService`/`AppController`.
  - CRUD por entidad en SQL directo (schema `sys_{slug}`).
  - Login JWT contra `dbo.Usuarios`.
  - Conexión por `.env` (DB + JWT).
  - Auto `dotnet restore` al finalizar (resultado devuelto a la UI).

### 5.3 Generador de frontend (fase inicial)
Se agrego un generador para crear un frontend runtime por sistema:
- Endpoint: `POST /api/v1/sistemas/{id}/generar-frontend`
  - Parametro: `?overwrite=true` para reemplazar si ya existe.
- Salida: `systems/<slug>/frontend/`
  - Copia la UI base y elimina vistas administrativas (solo runtime).
  - Router reducido a `Home` + `SistemaRuntime`.
  - `axios.js` apunta al backend del sistema (`http://localhost:5032+systemId/{apiBase}`).
- Se genera `src/comun/config/frontend-config.json` con la configuracion guardada en el diseñador.
- El frontend generado consume el backend del sistema (no el de SystemBase).

### 5.4 Configuracion visual de backend
En el diseniador de sistema se agrego una pestania "Backend" para configurar la generacion:
- Configuracion: `GET/PUT /api/v1/sistemas/{systemId}/backend-config`
- Config global:
  - `apiBase`, `requireAuth`, `schemaPrefix`, `persistence`, `defaultPageSize`, `maxPageSize`.
- Config por entidad:
  - `isEnabled`, `route`, `requireAuth`, `softDelete`, `softDeleteFieldId`.
  - `pagination`, `defaultPageSize`, `maxPageSize`.
  - `endpoints` (list/get/create/update/delete).
  - `filterFieldIds`, `defaultSortFieldId`, `defaultSortDirection`.
- Config por campo (expose/readOnly/required/maxLength/unique/defaultValue/displayAs).
- Por defecto todas las entidades se generan si no hay config.

### 5.5 Herramientas dev (backend)
En la pestaña **Herramientas** del diseñador:
- Botones **Iniciar / Detener / Reiniciar** backend del sistema (solo DEV).
- Healthcheck del backend via SystemBase:
  - `GET /api/v1/sistemas/{id}/backend/ping`
- Logs del proceso `dotnet watch run` (solo si el backend se inicia desde SystemBase):
  - `GET /api/v1/sistemas/{id}/backend/logs`
- Consola API tipo mini‑Postman con ejemplos request/response.
- Todo en modo manual (sin polling automatico).

## 6. Frontend (pantallas actuales)
### 6.1 Sistemas
Ruta: `/sistemas`
- Lista, crea, publica y elimina sistemas.
- Al publicar refresca el sidebar automaticamente.

### 6.2 Disenador de sistema
Ruta: `/sistemas/{id}`
- ABM de entidades y campos.
- Panel de relaciones (FK).
- Consola SQL de bootstrap en pestaña **Datos** (solo DEV/admin).
- Accion de eliminar entidad con opcion de borrar tambien la tabla runtime.
- Boton "Datos" para abrir runtime.
- Boton **Publicar DB** ahora vive en la pestaña **Datos**.
- Pestañas: Datos / Backend / Herramientas / Frontend.
- UI de Herramientas incluye:
  - Consola de logs del backend.
  - Lista de APIs con filtros (por metodo y texto) + colores por metodo.
  - Consola API mejorada (Request/Response).
- UI de Frontend incluye:
  - Config global (titulo app, busqueda, filtros, paginacion).
  - Config por entidad (labels y visibilidad de campos).
  - Boton **Generar frontend**.

### 6.3 Runtime
Ruta: `/s/{slug}/{entidad}` (o `/s/{slug}`).
- Muestra entidades y registros.
- CRUD generico.
- Si hay FK definidos, se muestran combos en el formulario.
- Incluye busqueda, filtro por campo y paginacion local.
- En combos FK se puede crear registro en linea (boton +).
- Respeta permisos por entidad (view/create/edit/delete).

## 7. Sidebar dinamico
El sidebar ahora mezcla:
- Menus estaticos (`dbo.Menus`).
- Menus de sistemas (`sb.SystemMenus`).

Endpoint:
- `GET /api/v1/menu/sidebar`

Comportamiento:
- Agrupa por sistema.
- Muestra solo sistemas publicados y activos.
- Entra a `/s/{slug}` desde el item "Entidades".
- Cuando existe frontend generado, aparece un item **Frontend** bajo el sistema.
- El item **Frontend** abre una vista embebida (iframe) dentro de SystemBase y
  ofrece boton para abrir el frontend en una pestania aparte.

## 8. Relaciones y combos FK
Cuando una relacion existe:
- `SourceEntityId` = entidad donde esta el FK.
- `TargetEntityId` = entidad referenciada.
- `ForeignKey` = nombre del campo en la entidad origen.

En runtime:
- Si un campo coincide con un FK, se muestra como combo.
- El combo lista datos de la entidad destino.
- El campo de display se elige asi:
  - `Nombre` o `Name` si existe.
  - primer campo string.
  - sino el PK.

Ejemplo recomendado (inventario):
- Movimientos -> Productos: `ManyToOne`, FK = `ProductoId`, Cascade = No.
- Movimientos -> Almacenes: `ManyToOne`, FK = `AlmacenId`, Cascade = No.

## 9. Validaciones e integridad
En runtime se validan:
- Required (backend).
- MaxLength (backend).
- Unicos (`IsUnique`) con verificacion antes de insertar/editar.
- Precision/Scale para decimales.

Soft delete:
- Si la entidad tiene un campo booleano llamado `IsActive` o `Activo`, al eliminar se marca en `false` en lugar de borrar.
- En listados se filtran los registros con `IsActive/Activo = true`.

Bloqueo de borrado:
- Si no hay soft delete y existen relaciones sin cascade, se bloquea el delete.

## 10. Roles y menus de sistemas
Se agrego un selector de sistemas por rol:
- UI en Roles permite asignar sistemas publicados.
- Internamente se asignan todos los `sb.SystemMenus` del sistema al rol.
- El sidebar filtra por rol.

Endpoints:
- `GET /api/v1/roles/{id}/system-menus`
- `PUT /api/v1/roles/{id}/system-menus`

## 11. Permisos por entidad
Se agregaron permisos por entidad y accion:
- Acciones: `view`, `create`, `edit`, `delete`.
- Se generan automaticamente al publicar.
- Se asignan por rol desde Roles.
- El backend valida permisos en CRUD runtime.
- El listado de entidades runtime se filtra por permiso `view`.
Nota: si el sistema ya estaba publicado antes de este cambio, es necesario publicar nuevamente para generar los permisos.

Endpoints:
- `GET /api/v1/roles/{id}/permissions/{systemId}`
- `PUT /api/v1/roles/{id}/permissions/{systemId}`

## 12. Mejoras de UX y estabilidad
- Guard del layout valida rutas segun menu y soporta rutas hijas (ej: `/sistemas/:id`).
- Silenciado el warning de `ResizeObserver` en consola (Vuetify).
- Header y sidebar fijos; el scroll solo aplica al contenido principal.
- Refinamientos visuales en:
  - Header del diseñador (badge + chip de slug).
  - Tabs más limpios.
  - Seccion Puertos con cards.

## 13. Flujo de prueba recomendado
1. Crear sistema en `/sistemas`.
2. Crear entidades y campos en `/sistemas/{id}`.
3. Crear relaciones.
4. Publicar DB.
5. (Opcional) Configurar backend y generar backend.
6. (Opcional) Configurar frontend y generar frontend.
7. Ir a `/s/{slug}` o al menu **Frontend** y cargar registros.
8. (Opcional) Exportar desde `/sistemas` (descarga ZIP).

## 14. Limitaciones actuales
- No hay migraciones automáticas (solo publish incremental).
- Soft delete depende de nombres convencionales (`IsActive` / `Activo`).

## 15. Archivos clave
- Backend:
  - `backend/Modulos/Sistemas/Publicacion/SistemasPublicador.cs`
  - `backend/Modulos/Sistemas/Exportacion/SistemasExportador.cs`
  - `backend/Modulos/Sistemas/GeneradorBackend/SistemasBackendGenerator.cs`
  - `backend/Modulos/Sistemas/Exportacion/Modelos/ExportResult.cs`
  - `backend/Modulos/Sistemas/GeneradorBackend/Modelos/BackendGenerateResult.cs`
  - `backend/Modulos/Sistemas/Datos/DatosGestor.cs`
  - `backend/Modulos/Sistemas/Relaciones/RelacionesGestor.cs`
  - `backend/Modulos/Sistemas/Publicacion/PermisosGestor.cs`
  - `backend/Modulos/Menu/MenuGestor.cs`
  - `backend/Comun/BaseDeDatos/SystemBaseContext.cs`
- Frontend:
  - `frontend/src/modulos/sistemas/Sistemas.vue`
  - `frontend/src/modulos/sistemas/SistemaEditor.vue`
  - `frontend/src/modulos/sistemas/SistemaRuntime.vue`
  - `frontend/src/modulos/sistemas/componentes/RegistroDialog.vue`
  - `frontend/src/modulos/sistemas/componentes/RelacionDialog.vue`
  - `frontend/src/modulos/roles/componentes/RolPermisosDialog.vue`
  - `frontend/src/comun/layout/MainLayout.vue`
  - `frontend/src/main.js`
  - `frontend/src/comun/store/menu.store.js`
