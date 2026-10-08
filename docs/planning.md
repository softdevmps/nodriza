# Planning — Nodriza (SystemBase)

> Estado al **2026-10-07**. Rama base: `main`. Documento vivo: actualizar el estado de cada tarea a medida que avanzamos.

## 1. Resumen

Nodriza es una **fábrica de sistemas**. Desde una UI web se diseña un sistema (entidades, campos y relaciones). Con un click por paso, la fábrica:

1. crea las tablas reales en SQL Server (**Publicar DB**),
2. permite cargar datos con una pantalla genérica (**runtime** `/s/<slug>`),
3. genera un **backend .NET** y un **frontend Vue** propios para ese sistema, en `systems/<slug>/`,
4. y lo **exporta** como ZIP (SQL + backend + frontend + manifest).

**Estado general (actualizado 2026-10-07):** el pipeline completo funciona y la **Fase 1 (seguridad e integridad de datos) está terminada**: todos los hallazgos de seguridad (S1–S13) y los bugs funcionales (F1–F16) están resueltos y verificados con las 177 pruebas de punta a punta, que pasan en verde.

Lo que queda es **mantenibilidad y base de calidad**:

- Los tests viven fuera del repo (`pruebas-e2e/`, local) y no hay CI.
- Hay archivos de más de 3.000 líneas y código duplicado (deuda D1–D5).
- Algunas limitaciones son conocidas y están documentadas (§4.4).

## 2. Qué tenemos hoy

### 2.1 Funcionalidad de la fábrica

| Área | Qué hace | Estado |
|---|---|---|
| Auth | Login JWT, registro de usuarios | ✅ Funciona · ⚠️ registro abierto (ver §4.1) |
| Usuarios | ABM de usuarios con rol | ✅ · ⚠️ sin control de rol |
| Roles | ABM de roles, menús por rol, sistemas por rol, permisos por entidad (view/create/edit/delete) | ✅ · ⚠️ sin control de rol |
| Menú | Menú lateral dinámico (menús fijos + menús de sistemas publicados), ABM de menús, vista autogenerada al crear un menú hijo | ✅ |
| Sistemas — diseño | CRUD de sistemas, entidades, campos (string, int, decimal, bool, datetime, guid) y relaciones | ✅ |
| Sistemas — publicar | Crea el schema `sys_<slug>`, las tablas, PK, identity, índices únicos, FKs, menús y permisos. Asigna los permisos al rol Admin | ✅ · ⚠️ solo agrega cosas (§4.2) |
| Sistemas — runtime | CRUD genérico sobre las tablas publicadas: búsqueda, filtros, paginación local, combos de FK con alta en línea, soft delete por convención (`IsActive`/`Activo`/`Active`), bloqueo de borrado si hay dependencias | ✅ |
| Consola SQL | Ejecutar scripts sobre `sys_<slug>` (solo DEV y admin) y sincronizar metadata desde las tablas reales | ✅ |
| Generador backend | Proyecto .NET 8 por sistema: CRUD por entidad con SQL directo, JWT, Swagger, configurable por entidad y campo (rutas, endpoints, auth, paginación, soft delete, filtros) | ✅ · ⚠️ defaults inseguros (§4.1) · modo EF Core no implementado |
| Generador frontend | Copia `frontend-runtime/` y le inyecta la URL del backend y la configuración visual (título, colores, densidad, locale, labels por campo) | ✅ |
| Herramientas | Iniciar/detener el backend y el frontend generados, ver sus logs, healthcheck, consola API tipo Postman | ✅ (solo DEV) |
| Exportar | ZIP o workspace con `database.sql`, backend, frontend, `manifest.json` y README | ✅ |

### 2.2 Arquitectura

- **Backend:** .NET 8, EF Core para la metadata y SQL directo parametrizado para las tablas de los sistemas. JWT. 45 rutas, todas definidas en `Routes.cs`.
- **Frontend:** Vue 3 + Vuetify 3 + Vite 7.
- **Base:** SQL Server 2022 en Docker, con una sola base y tres capas:
  - `dbo`: usuarios, roles y menús de la fábrica.
  - `sb`: metadata (sistemas, entidades, campos, relaciones, permisos, configs, builds).
  - `sys_<slug>`: tablas reales de cada sistema publicado.
- **Organización del código:** por módulo (Auth, Usuarios, Roles, Menu, Sistemas), con los mismos nombres en backend y frontend. Ver [estructura.md](estructura.md).
- **Puertos:**

  | Servicio | Puerto |
  |---|---|
  | API de la fábrica | 5032 |
  | UI de la fábrica | 5173 (o el siguiente libre) |
  | Backend generado | `5032 + systemId` |
  | Frontend generado | `5173 + systemId` |

### 2.3 Números

| | |
|---|---|
| Archivos C# del backend | 106 |
| Archivos más grandes | `SistemaEditor.vue` 3.752 líneas · `SistemasBackendGenerator.cs` 1.750 · `SistemaRuntime.vue` (runtime) 1.749 · `SistemasController.cs` 1.341 · `SistemasExportador.cs` ~1.200 |
| Tests | 96 en el repo (`tests/Backend.Tests`, corren en CI) + 177 E2E locales (`pruebas-e2e/`, no versionadas) |
| CI | GitHub Actions en cada PR |
| Warnings de compilación | 171 (nullability) |
| `npm audit` | 0 vulnerabilidades (antes 11) |

### 2.4 Hecho hasta ahora (esta etapa)

- [x] Entorno local levantado y verificado de punta a punta (Docker + API + UI + login).
- [x] Reorganización de carpetas por módulo, en backend, frontend y frontend-runtime, con guía para juniors (`docs/estructura.md`). Rama `refactor/estructura-por-modulos`, **PR pendiente de merge**.
- [x] **Fase 1 completa** en la rama `fix/autorizacion-admin-y-decimales` (sobre `refactor/estructura-por-modulos`):
  autorización por rol, decimales, inyección en el generador, secretos en el export, integridad de datos,
  migraciones seguras, archivado, autenticación, credenciales propias por sistema, consola SQL aislada,
  dependencias y puertos. Resultado de las pruebas: **177/177 en verde**.
- [x] Herramienta local de pruebas de punta a punta (`pruebas-e2e/`, no versionada). Corre en un entorno aislado y deja un reporte por severidad. Primera corrida (2026-10-07): 172 pruebas, 115 ok, 57 fallas (11 críticas). Los hallazgos nuevos se sumaron en §4 (S8–S13, F9–F16).

## 3. Cómo levantar

Ver el [README](../README.md#levantar). Resumen:

```bash
cd db-service && docker compose --env-file ../backend/.env up -d
cd backend  && dotnet watch run      # http://localhost:5032/swagger
cd frontend && npm install && npm run dev   # entrar con admin/admin
```

## 4. Hallazgos

> Todos los hallazgos marcados con ✅ están resueltos y cubiertos por `pruebas-e2e`. Lo que queda abierto está en §4.3 (deuda) y §4.4 (limitaciones conocidas).

### 4.1 Seguridad

| # | Problema | Impacto | Dónde |
|---|---|---|---|
| ✅ S1 | **Escalada de privilegios.** `POST /auth/registrar` es anónimo. Con el token de ese usuario nuevo se puede llamar a `PUT /usuarios/{id}` y ponerse `RolId` = Admin, o cambiarle la contraseña al usuario `admin`. | **Crítico.** Cualquiera con acceso a la API toma control de la fábrica. | `Modulos/Usuarios/UsuariosController.cs`, `Modulos/Auth` |
| ✅ S2 | Los endpoints de administración (usuarios, roles, menús, sistemas, generar, exportar) solo exigen estar logueado; ninguno chequea el rol. El frontend oculta los menús, pero eso no protege la API. | Alto | Todos los controllers de `Modulos/` salvo la consola SQL y DevTools |
| ✅ S3 | "Es admin" se decide por `Username == "admin"` **o** por el rol Admin. | Medio | `SistemasController.IsAdminUser`, `DevToolsController` |
| ✅ S4 | Se imprime el connection string con la contraseña de la base en la consola al arrancar. | Medio | `Program.cs` |
| ✅ S5 | Backend generado con valores inseguros: `JWT_SECRET=secret`, `DB_PASSWORD=Password123!` como fallback, CORS abierto a cualquier origen y registro anónimo. | Alto en los sistemas generados | `GeneradorBackend/SistemasBackendGenerator.cs` |
| ✅ S6 | La consola SQL valida con expresiones regulares; se puede eludir con SQL dinámico. | Bajo (solo DEV y admin, pero depende de S1/S2) | `SistemasController.ValidateSqlScript` |
| ✅ S7 | 11 vulnerabilidades en dependencias npm. | Bajo/medio | `frontend/package.json` |
| ✅ S8 | **Inyección de código en el generador.** El `TableName` (y el `ColumnName`) se escriben sin escapar dentro de strings C# del backend generado. Al hacer "Iniciar backend", ese código corre en el servidor. | **Crítico** (ejecución remota de código, combinado con S2) | `GeneradorBackend/SistemasBackendGenerator.cs`, `EntidadesGestor`, `CamposGestor` |
| ✅ S9 | **El ZIP exportado incluye `backend/.env` con la contraseña de la base y el secreto JWT** cuando el sistema ya tenía backend generado (se comprime el workspace tal cual). | **Crítico** | `SistemasController.Exportar` |
| ✅ S10 | Los sistemas generados copian el secreto JWT, el emisor y la cuenta `sa` de la fábrica: un token de la fábrica es válido en todos los sistemas generados, y cada sistema tiene acceso total al servidor SQL. Además, el backend generado no trae `.gitignore`. | Alto | Generador backend |
| ✅ S11 | Un usuario desactivado sigue operando con su token hasta que vence. | Alto | Validación JWT (no consulta el estado del usuario) |
| ✅ S12 | Los errores devuelven al cliente el mensaje crudo de SQL Server (al publicar, con PK o FK inválida, con username duplicado) y, en Development, el stack trace completo. | Medio | `DatosGestor`, `SistemasPublicador`, `UsuariosGestor` |
| ✅ S13 | La contraseña inicial del admin viaja en `frontend-config.json` (se publica en el navegador) y en el README del export. La semilla deja activo `admin/admin`. | Medio/alto | `FrontendSystemConfig`, `SistemasExportador`, `DbSeeder` |

### 4.2 Bugs y limitaciones funcionales

| # | Problema | Dónde |
|---|---|---|
| ✅ F1 | **Publicar solo agrega.** Si cambiás el tipo de un campo, lo renombrás o lo borrás, la tabla real no cambia. Las columnas nuevas en tablas existentes se crean siempre `NULL`, sin respetar `Required`. | `Publicacion/SistemasPublicador.cs` |
| ✅ F2 | Borrar un sistema no borra su schema `sys_<slug>` ni la carpeta `systems/<slug>`. | `SistemasGestor.Eliminar` |
| ✅ F3 | Las relaciones `ManyToMany` y `OneToMany` se aceptan en el diseño, pero el publicador las trata a todas como una FK simple. No crea tabla intermedia. | `SistemasPublicador.AplicarRelaciones` |
| ✅ F4 | Puertos fijos `5173 + id`: si la UI de la fábrica no consigue el 5173, choca con el frontend de algún sistema generado. | `SistemasController`, `SistemaEditor.vue` |
| ✅ F5 | El runtime trae todos los registros y pagina en el navegador. Con tablas grandes se va a poner lento. | `Datos/DatosGestor.Listar` |
| ✅ F6 | Quedaron restos de otro proyecto: variables `AUDIO_*` y ffmpeg en el `.env` generado y en `BackendSystemConfig`. | Generador backend |
| ✅ F7 | `Persistence = "ef"` aparece como opción, pero devuelve "no implementado". | Generador backend |
| ✅ F8 | ~~El flujo de generar y levantar un backend/frontend todavía no se probó de punta a punta.~~ Probado con `pruebas-e2e`: compila, levanta y hace CRUD contra la base. | — |
| ✅ F9 | **Los decimales enviados como texto se interpretan con la cultura del servidor.** En un servidor en español, `"10.50"` se guarda como **1050.00**. El formulario del runtime envía siempre texto, así que todo importe cargado desde la UI se altera en silencio. | `Datos/DatosGestor.ConvertValue` |
| ✅ F10 | En la **primera** publicación el rol Admin no recibe los permisos del sistema y no puede cargar datos hasta que se publica de nuevo. `AsignarPermisosAdmin` consulta la base antes de que se guarden los permisos recién creados. | `Publicacion/SistemasPublicador.cs` |
| ✅ F11 | Alta o edición de datos en una entidad con un campo único → **500**. `DatosGestor` hace `using` sobre la conexión de EF y la consulta siguiente usa una conexión ya destruida. | `Datos/DatosGestor.cs` |
| ✅ F12 | Las fechas en UTC (`…Z`) se guardan corridas según la zona horaria del servidor (13:45Z → 10:45). Además pierden precisión (`.1234567` → `.1233333`), porque se envían como `DATETIME` y no como `DATETIME2`. | `Datos/DatosGestor.cs` |
| ✅ F13 | Un sistema nuevo con el mismo slug que uno borrado arranca con los datos del anterior (consecuencia de F2). | `SistemasGestor.Eliminar` |
| ✅ F14 | Borrar una entidad deja su menú en el sidebar. | `EntidadesGestor.Eliminar` |
| ✅ F15 | PK duplicada, FK inexistente, username duplicado o un rol inexistente responden **500** en vez de 400. `POST /usuarios` además responde 200 aunque la creación falle. | `DatosGestor`, `UsuariosController` |
| ✅ F16 | Exportar un sistema que solo tiene backend generado produce un ZIP sin `frontend/`. | `SistemasController.Exportar` |

### 4.3 Deuda técnica (pendiente)

| # | Problema |
|---|---|
| ~~D1~~ | (resuelto, ver arriba) Archivos enormes que mezclan responsabilidades. `SistemasController` mezcla CRUD, consola SQL, export y manejo de procesos. `SistemaEditor.vue` tiene 4 pestañas completas en un solo archivo. |
| ✅ D1 | ~~Archivos enormes~~: `SistemasController` y `SistemaEditor.vue` divididos (Fase 4.1/4.2).
| ✅ D2 | Helpers duplicados (`ToSafeSqlName`, `ToKebab`, `MapSqlType`, `ToPascalCase`) en 4 o 5 archivos, y con pequeñas diferencias entre sí. |
| ✅ D3 | ~~El generador escribe el C# desde strings dentro de C#~~: los archivos fijos ahora son plantillas legibles en `GeneradorBackend/Plantillas/` (Fase 4.4). Lo que depende de cada entidad sigue armándose en código. |
| ◐ D4 | Layout, Vuetify, estilos y utilidades ya no se duplican: viven en `frontend-runtime/` y la fábrica los importa con `@runtime` (Fase 4.5). Siguen separados, porque divergieron a propósito: login/registro (el del runtime soporta auth central) y la pantalla de datos (`SistemaRuntime.vue`: 961 vs 1749 líneas, hablan con APIs distintas). Unificarlos es reescribir una de las dos y se decide como producto. |
| ✅ D5 | ~~Gestores `static` con `new SystemBaseContext()`~~: ahora se inyectan (Fase 4.3). |
| D6 | ~~No hay tests ni CI~~ (resuelto en Fase 2). Falta linter. Hay 171 warnings de nullability. |

### 4.4 Limitaciones conocidas (decididas o pendientes de producto)

| # | Limitación | Por qué / cómo se maneja |
|---|---|---|
| L1 | Renombrar un campo o tabla en el diseño no renombra la columna real: se crea una nueva y la vieja queda (con sus datos) como opcional. | No se pierden datos. Un renombrado automático necesita registrar el nombre anterior: va a Fase 3. |
| L2 | Las migraciones no achican, no convierten tipos con pérdida ni borran columnas: esos cambios se rechazan con un mensaje claro. | Decisión: nunca truncar ni perder datos en silencio. |
| L3 | El runtime de la fábrica todavía pagina en el navegador. El backend ya soporta `take`/`skip`. | Pasar la UI a paginación del servidor (Fase 3). |
| L4 | El bloqueo por fuerza bruta vive en memoria. | Alcanza con una instancia; con varias habría que moverlo a un store compartido. |
| L5 | Los sistemas generados comparten la tabla de usuarios `dbo.Usuarios` con la fábrica (solo lectura + alta, con permisos mínimos). | Diseño actual: usuarios centralizados. |
| L6 | ManyToMany y OneToMany no están disponibles. | Decisión: se quitaron hasta implementarlas completas (tabla intermedia + UI + generadores). |

## 5. Plan por fases

Esfuerzo: **S** = horas · **M** = 1–2 días · **L** = 3+ días.

### Fase 0 — Cerrar lo pendiente

| # | Tarea | Esf. | Criterio de terminado |
|---|---|---|---|
| 0.1 | Mergear `refactor/estructura-por-modulos` y después `fix/autorizacion-admin-y-decimales` | S | Están en `main` |
| ✅ 0.2 | Prueba de punta a punta (hecha con `pruebas-e2e`: publicar, datos, generar y levantar backend y frontend, exportar) | S | 177/177 en verde |

### ✅ Fase 1 — Seguridad e integridad de datos (completa)

Criterio general: las suites correspondientes de `pruebas-e2e` pasan en verde.

| # | Tarea | Esf. | Criterio de terminado |
|---|---|---|---|
| ✅ 1.0 | **Decimales y enteros con cultura invariante** al convertir datos del runtime (F9) | S | `"10.50"` se guarda como 10.50 |
| ✅ 1.1 | Política de autorización "Admin" basada en el **rol**, aplicada a usuarios, roles, menús y todo `Sistemas/*` de diseño, generación y exportación | M | Un usuario sin rol Admin recibe 403 en esos endpoints |
| ✅ 1.2 | Registro: decidir si se cierra o si queda abierto pero sin rol y sin acceso (ver §6) | S | No existe forma de auto-asignarse un rol |
| ✅ 1.3 | Quitar el chequeo `Username == "admin"` | S | Ser admin depende solo del rol |
| ✅ 1.4 | No loguear el connection string | S | No aparecen secretos en la consola |
| ✅ 1.5 | Backend generado: sin secretos por defecto (si falta `JWT_SECRET`, falla al arrancar), CORS configurable, registro configurable | M | Un backend generado no arranca con secretos débiles |
| ✅ 1.6 | `npm audit fix` en `frontend` y `frontend-runtime` | S | 0 vulnerabilidades altas |
| ✅ 1.7 | Validar `TableName`/`ColumnName` al crearlos y escapar todo lo que el generador escribe en código C# (S8) | S | El generador rechaza o escapa nombres con comillas |
| ✅ 1.8 | Export: no incluir `.env`, `bin/`, `obj/` ni `node_modules/` del workspace (S9) | S | Ningún archivo del ZIP contiene secretos |
| ✅ 1.9 | Corregir F10 (permisos del Admin en la primera publicación) y F11 (conexión destruida en `DatosGestor`) | S | El admin carga datos apenas publica; los campos únicos no dan 500 |
| ✅ 1.10 | Invalidar el acceso de usuarios desactivados aunque tengan un token vigente (S11) | S | El token de un usuario desactivado recibe 401 |
| ✅ 1.11 | Secreto JWT y usuario SQL propios por sistema generado, y `.gitignore` en lo generado (S10) | M | Un token de la fábrica no sirve en un sistema generado |

### ✅ Fase 2 — Base de calidad (completa)

| # | Tarea | Esf. | Criterio de terminado |
|---|---|---|---|
| ✅ 2.1 | Proyecto de tests del backend (`tests/Backend.Tests`: xUnit + WebApplicationFactory + base temporal) con lo crítico: auth/autorización, publicar, datos, diseño | M | `dotnet test` en verde: 96 tests, ~8 s |
| ✅ 2.2 | CI en GitHub Actions (`.github/workflows/ci.yml`): build y tests del backend con SQL Server efímero, build y `npm audit` de los dos frontends | S | Cada PR muestra el check |
| ✅ 2.3 | Unificar helpers en `Comun/` (`NombresSql`, `Texto`) y borrar las copias | S | Una sola implementación de cada uno (11 copias eliminadas) |

### Fase 3 — Correctitud del pipeline

| # | Tarea | Esf. | Criterio de terminado |
|---|---|---|---|
| ✅ 3.1 | Publicar con **migraciones** (hecho: cambios seguros automáticos; inseguros se rechazan. Falta: renombrar y preview, ver L1/L2): detectar diferencias entre la metadata y la tabla real (agregar, cambiar tipo o nulabilidad, renombrar, borrar), mostrar un preview del SQL y aplicarlo con confirmación | L | Cambiar un campo y republicar actualiza la tabla |
| ✅ 3.2 | Borrar un sistema (hecho: se **archiva**, decisión del 2026-10-07) también borra el schema y la carpeta (opcional, con confirmación) | S | No quedan restos |
| ✅ 3.3 | Relaciones (hecho: se quitaron los tipos no soportados): implementar ManyToMany (tabla intermedia) o sacar del diseño los tipos que no se soportan | M | El diseño refleja lo que realmente se crea |
| 3.4 | Paginación del lado del servidor en la UI del runtime (el backend ya la soporta, ver L3) | M | Listar trae solo la página pedida |
| ✅ 3.5 | Puertos configurables (base en `.env`) y detección de puerto ocupado | S | No hay choques |
| ✅ 3.6 | Limpiar los restos `AUDIO_*` y la opción EF Core (implementarla o quitarla) | S | La config solo muestra lo que existe |
| 3.7 | El login de los sistemas generados muestra "Registrarse" siempre, pero su backend trae el registro cerrado (`REGISTRO_PUBLICO=false`): agregar `/auth/opciones` al backend generado y ocultar el botón como en la fábrica (encontrado en 4.5) | S | El botón solo aparece si el registro está abierto |

### Fase 4 — Mantenibilidad

| # | Tarea | Esf. | Criterio de terminado |
|---|---|---|---|
| ✅ 4.1 | Dividir `SistemasController` (hecho: 1.452 → 148 líneas; ConsolaSql/, Exportacion/, Herramientas/ y controllers de generación) en `SistemasController`, `ConsolaSqlController`, `ExportacionController` y `HerramientasController`, con las mismas rutas | M | Ningún controller pasa de 300 líneas |
| ✅ 4.2 | Dividir `SistemaEditor.vue` en un componente por pestaña (hecho: 3.571 → 122 líneas + 7 componentes ≤ 460; lógica en `useSistemaEditor.js`, que todavía se puede partir por pestaña) | M | Ningún `.vue` pasa de 800 líneas |
| ✅ 4.3 | Gestores inyectables (hecho: 17 gestores registrados en `Comun/Inyeccion.cs`, contexto por operación con `IDbContextFactory`) | L | Los gestores se pueden testear con dependencias falsas |
| ✅ 4.4 | Generador de backend con archivos de plantilla (hecho: 17 archivos `.plantilla` embebidos con marcadores `{{nombre}}`, sin dependencias nuevas; la salida generada es idéntica byte a byte) | L | Las plantillas son archivos `.cs` legibles |
| ✅ 4.5 | Reducir la duplicación entre `frontend` y `frontend-runtime` (hecho: header, sidebar, Vuetify, CSS y `slug.js` en un solo lugar con el alias `@runtime`; la UI de la fábrica da 0 px de diferencia en las capturas) | M | Un cambio de layout se hace en un solo lugar |

### Fase 5 — Funcionalidad nueva (a definir)

Ideas para discutir. Ninguna está comprometida todavía:

- Más tipos de campo: texto largo, fecha sin hora, enum/lista, archivo/imagen.
- Validaciones configurables por campo (regex, mínimo/máximo).
- Auditoría: quién creó o modificó cada registro y cuándo.
- Versionado de sistemas e historial de publicaciones (`sb.SystemBuilds` ya existe).
- Importar y exportar datos (CSV/Excel) en el runtime.
- Plantillas de sistemas listas para clonar (Inventario, CRM, Turnos…).
- Deploy de sistemas generados: Dockerfile por sistema.

## 6. Decisiones

Tomadas el 2026-10-07:

1. **Registro público:** cerrado por defecto (`REGISTRO_PUBLICO=false`); el alta la hace un admin.
2. **Admin inicial:** contraseña desde `ADMIN_PASSWORD`; si falta, se genera una aleatoria y se muestra una sola vez.
3. **Borrar un sistema:** se archiva (schema `sys_<slug>_eliminado_<fecha>` y `systems/_eliminados/`).
4. **ManyToMany:** se quitó hasta implementarla completa.
5. **EF Core en los generados:** se quitó la opción (no estaba implementada).

Abiertas:

- **Prioridad de la Fase 5:** ¿qué funcionalidad nueva aporta más valor primero?
- **Renombrados en el diseño (L1):** ¿se implementan como migración, con preview?

## 7. Pruebas de punta a punta

`pruebas-e2e/` (local, no versionada) prueba la fábrica completa en un entorno aislado: base `nodriza_e2e`, API en `:5299` y carpetas propias. No toca la base de desarrollo, `systems/` ni `exports/`. Ver `pruebas-e2e/README.md`.

```bash
cd pruebas-e2e && node correr.mjs            # ~3 min
node correr.mjs --solo 03                     # una suite
```

Cada arreglo de la Fase 1 se da por terminado cuando su prueba pasa en verde.

## 8. Próximo paso recomendado

1. Mergear en orden: `refactor/estructura-por-modulos` → `fix/autorizacion-admin-y-decimales` → `chore/fase-2-calidad` (0.1).
2. **Fase 4:** ✅ completa.
3. **Fase 3 pendiente:** paginación del servidor en la UI del runtime (3.4) y renombrados en el diseño (L1).
