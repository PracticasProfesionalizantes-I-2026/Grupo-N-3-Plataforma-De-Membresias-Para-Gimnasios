# Backlog de Hallazgos y Mejoras Técnicas (Issues de GitHub)

Este checklist contiene las tareas correctivas y de mejora técnica resultantes de la auditoría integral (Documentación vs. Casos de Uso vs. Código Fuente). Las incidencias están priorizadas según su impacto en seguridad, completitud de negocio y calidad de software.

---

### 🔴 RIESGO ALTO (Crítico para la ejecución y completitud core)
*Incluye bugs bloqueantes, vulnerabilidades de seguridad graves y requerimientos principales o casos de uso core no implementados.*

- [ ] **[SEC-01] Implementar Autenticación y Autorización basada en Tokens JWT (Broken Access Control - OWASP A01)**
  - **Módulo / Archivos:** `src/GymPlatform.API/Program.cs`, `src/GymPlatform.API/Controllers/*.cs`
  - **Descripción:** Las precondiciones de CU-01, CU-02 y CU-03 exigen tokens JWT válidos y control de acceso basado en roles (`Recepcionista`, `Administrador`, `Entrenador`). Actualmente no existe autenticación configurada ni atributos `[Authorize]`, dejando todos los endpoints (incluyendo creación, modificación y eliminación) expuestos al acceso público y anónimo.
  - **Acción Correctiva:**
    1. Instalar el paquete `Microsoft.AspNetCore.Authentication.JwtBearer`.
    2. Configurar el esquema JWT y validar firma, emisor y audiencia en `Program.cs`.
    3. Habilitar `app.UseAuthentication()` previo a `app.UseAuthorization()`.
    4. Agregar el atributo `[Authorize(Roles = "...")]` en `SociosController` (Roles: `Recepcionista, Administrador`), `PlanesController` (Rol: `Administrador`), `HorariosController` (Roles: `Administrador, Entrenador`) y `ActividadesController`.
    5. Configurar el esquema `SecurityScheme` Bearer en `builder.Services.AddOpenApi()` para documentar la seguridad en Scalar UI.

- [ ] **[REQ-01] Implementar Entidades y Casos de Uso para Asociación de Socios a Planes y Turnos**
  - **Módulo / Archivos:** `src/GymPlatform.DataAccess/Entities/`, `src/GymPlatform.BusinessLogic/`
  - **Descripción:** En CU-01 (postcondición) se establece que el socio queda habilitado para contratar un plan, y en CU-03 que puede reservar turnos. Sin embargo, en el modelo de datos no existen las entidades intermedias `Membresia` (Socio-Plan) ni `ReservaHorario` (Socio-Horario). El sistema administra catálogos independientes sin permitir el flujo de negocio central del gimnasio.
  - **Acción Correctiva:**
    1. Modelar la entidad `Membresia` (`SocioId`, `PlanMembresiaId`, `FechaInicio`, `FechaVencimiento`, `Estado`).
    2. Modelar la entidad `Reserva` (`SocioId`, `HorarioId`, `FechaTurno`, `Asistencia`).
    3. Implementar la lógica de negocio y endpoints para contratación de planes y reserva de turnos validando cupo máximo disponible en el horario.

- [ ] **[BUG-01] Prevenir Error 500 no controlado por violación de clave foránea al eliminar Actividades con Horarios**
  - **Módulo / Archivos:** `src/GymPlatform.BusinessLogic/Services/ActividadService.cs`, línea 82
  - **Descripción:** `ActividadService.DeleteAsync` intenta borrar la entidad directamente sin validar si existen horarios asignados. Debido a `DeleteBehavior.Restrict` en `GymDbContext`, EF Core lanza `DbUpdateException`, la cual no es capturada en el controlador (`catch (GymException)`), resultando en un HTTP 500 en lugar de un error de conflicto de negocio (409).
  - **Acción Correctiva:**
    1. Agregar el método `Task<bool> HasAssignedHorariosAsync(Guid actividadId)` en `IActividadRepository` y su implementación en `ActividadRepository`.
    2. En `ActividadService.DeleteAsync`, verificar si existen horarios asociados y lanzar `DependencyConflictException("No se puede eliminar la actividad porque tiene horarios asignados.")` para que el controlador responda HTTP 409 Conflict de manera homogénea con `PlanMembresiaService`.

- [ ] **[BUG-02] Controlar Concurrencia y Condiciones de Carrera en Detección de Solapamientos (RN-03)**
  - **Módulo / Archivos:** `src/GymPlatform.BusinessLogic/Services/HorarioService.cs`, líneas 72 y 130
  - **Descripción:** La validación de superposición horaria sigue el patrón Check-Then-Act (`ValidarSolapamientoAsync` seguido de `CreateAsync`) sin aislamiento de transacciones en la base de datos. Peticiones simultáneas concurrentes pueden eludir la validación en memoria y generar solapamientos de sala o profesor en la base de datos.
  - **Acción Correctiva:**
    1. Implementar transacciones explícitas con nivel de aislamiento serializable o evaluar mecanismos de exclusión / bloqueo pesimista en base de datos.
    2. Agregar un índice condicional o restricción a nivel de motor de BD para impedir registros solapados en `(Profesor, DiaSemana)` y `(Sala, DiaSemana)`.

---

### 🟡 RIESGO MEDIO (Mantenibilidad y Flujos Secundarios)
*Incluye requerimientos secundarios faltantes, brechas de lógica diseño vs código, o code smells severos (ej. clases Dios, fuerte acoplamiento).*

- [ ] **[REQ-02] Implementar Flujo de Desactivación y Borrado Lógico (Soft Delete)**
  - **Módulo / Archivos:** `src/GymPlatform.BusinessLogic/Services/PlanMembresiaService.cs`, `src/GymPlatform.BusinessLogic/Services/SocioService.cs`
  - **Descripción:** CU-02 exige la capacidad de *"desactivar planes"*. Actualmente, tanto `Socio` como `PlanMembresia` tienen la propiedad `Activo`, pero los endpoints `DELETE` ejecutan borrado físico en base de datos (`_context.Remove`). Además, los endpoints `GET` retornan todos los registros sin discriminar si están activos o inactivos.
  - **Acción Correctiva:**
    1. Implementar endpoints específicos `PATCH /api/planes/{id}/desactivar` o ajustar el borrado para que cambie `Activo = false` preservando el historial referencial.
    2. En `HorarioService.CreateAsync`, agregar la validación de que el plan asociado esté activo (`if (!plan.Activo) throw new ValidationException("No se pueden asignar horarios a un plan inactivo");`).
    3. Añadir soporte para filtrar recursos activos en las consultas `GetAllAsync`.

- [ ] **[DOC-01] Alinear y Homogeneizar Matriz de Trazabilidad con Nomenclatura de Reglas de Negocio**
  - **Módulo / Archivos:** `Documentos/# Anexos y Matriz de Trazabilidad -.txt` vs `ID del Caso de Uso CU-0*.txt` y `README.md`
  - **Descripción:** El archivo de matriz enumera reglas de negocio de RN-01 a RN-06, mientras que las especificaciones de casos de uso individuales, el `README.md` y el código fuente se estructuran en base a RN-01, RN-02 y RN-03.
  - **Acción Correctiva:** Actualizar la tabla de la Matriz de Trazabilidad en `Documentos/# Anexos y Matriz de Trazabilidad -.txt` para reflejar con precisión la correspondencia oficial (CU-01 con RN-01, CU-02 con RN-02, CU-03 con RN-03), o desglosar formalmente RN-01 a RN-06 en todos los artefactos documentales.

- [ ] **[DOC-02] Formalizar Documento de Caso de Uso para la Entidad Actividades (Scope Creep)**
  - **Módulo / Archivos:** `Documentos/`
  - **Descripción:** Existe una suite completa de endpoints y lógica de negocio para gestionar actividades (`/api/actividades`, `ActividadService`), pero no existe un archivo formal de especificación `CU-04 Gestionar Actividades` en la carpeta `Documentos/`.
  - **Acción Correctiva:** Redactar y anexar el archivo `Documentos/ID del Caso de Uso CU-04.txt` con la estructura estándar (actores, precondiciones, flujo principal y excepciones) documentando la regla de unicidad de nombre de actividad.

- [ ] **[ARCH-01] Centralizar el Manejo de Excepciones Global mediante Middleware (RFC 7807 ProblemDetails)**
  - **Módulo / Archivos:** `src/GymPlatform.API/Controllers/*.cs`, `src/GymPlatform.API/Program.cs`
  - **Descripción:** Se detectó duplicación masiva de código (violación DRY) en más de 15 acciones de controladores con bloques `try-catch` repetitivos. Además, las respuestas de error utilizan un objeto anónimo `{ error = ex.Message }` en lugar del estándar moderno de la industria.
  - **Acción Correctiva:**
    1. Implementar un manejador global `IExceptionHandler` en ASP.NET Core que capture `GymException` y subtipos.
    2. Mapear cada excepción tipada al formato RFC 7807 / RFC 9457 (`ProblemDetails`).
    3. Eliminar los bloques `try-catch` repetitivos en los controladores, delegando la captura de excepciones en el pipeline global.

- [ ] **[ARCH-02] Optimizar Consultas LINQ con `.ToLower()` para evitar Table Scans en SQLite**
  - **Módulo / Archivos:** `src/GymPlatform.DataAccess/Repositories/HorarioRepository.cs`, líneas 40 y 48; `SocioRepository.cs`; `PlanMembresiaRepository.cs`
  - **Descripción:** El uso de `.Where(h => h.Profesor.ToLower() == profesor.ToLower())` en LINQ a EF Core se traduce en la función SQL `LOWER()`, lo que invalida el uso de índices B-Tree en SQLite y fuerza escaneos completos de tabla.
  - **Acción Correctiva:**
    1. Configurar la collation `NOCASE` en `GymDbContext` para las columnas de búsqueda (`Profesor`, `Sala`, `Email`, `Nombre`).
    2. Crear índices compuestos en `(Profesor, DiaSemana)` y `(Sala, DiaSemana)` para optimizar las consultas de validación de solapamiento.

---

### 🟢 RIESGO BAJO (Deuda técnica y Mejoras Menores)
*Incluye casos borde, validaciones faltantes, code smells menores y convenciones de formato/nomenclatura.*

- [ ] **[VAL-01] Incorporar Validación Declarativa de Esquema en DTOs (Data Annotations o FluentValidation)**
  - **Módulo / Archivos:** `src/GymPlatform.Shared/DTOs/*.cs`
  - **Descripción:** Los DTOs carecen de validaciones declarativas (`[Required]`, `[MaxLength]`, `[Range]`, `[EmailAddress]`). La validación sintáctica básica se realiza manualmente en los servicios. Si un cliente envía cadenas de longitud superior a la admitida por la base de datos (ej. Nombre > 100 caracteres), EF Core arrojará una excepción de persistencia en lugar de un error 400.
  - **Acción Correctiva:**
    1. Agregar data annotations o crear validadores `FluentValidation` en los DTOs para validar longitudes máximas y rangos.
    2. Agregar validación de formato numérico/caracteres válidos para el campo `Dni`.
    3. Establecer un límite superior razonable para `CupoMaximo` en `HorarioCreateDTO` (ej. 1 a 500).

- [ ] **[VAL-02] Robustecer Validación de Caso Borde en Fecha de Nacimiento**
  - **Módulo / Archivos:** `src/GymPlatform.BusinessLogic/Services/SocioService.cs`, líneas 53-62
  - **Descripción:** El servicio valida la mayoría de edad ($\ge 18$), pero no verifica un límite inferior razonable para fechas en el pasado distante (ej. nacimientos en el año 1800) ni contempla un mensaje de error específico si se ingresa una fecha futura.
  - **Acción Correctiva:** Incorporar validación que rechace fechas futuras explícitamente y limite la edad máxima razonable (ej. hasta 120 años).

- [ ] **[REST-01] Implementar Paginación y Filtrado en Colecciones GET**
  - **Módulo / Archivos:** `src/GymPlatform.API/Controllers/*.cs`, `src/GymPlatform.BusinessLogic/Services/*.cs`
  - **Descripción:** Los endpoints `GET /api/socios`, `/api/planes` y `/api/horarios` devuelven la totalidad de los registros de la base de datos en un solo payload, lo que representa un riesgo de latencia y uso excesivo de memoria conforme crezca el volumen de datos.
  - **Acción Correctiva:** Incorporar parámetros opcionales de consulta (`page`, `pageSize`) con valores por defecto (ej. `page=1`, `pageSize=20`) y retornar metadatos de paginación.

- [ ] **[OBS-01] Integrar Logging Estructurado (`ILogger<T>`)**
  - **Módulo / Archivos:** `src/GymPlatform.API/Controllers/*.cs`, `src/GymPlatform.BusinessLogic/Services/*.cs`
  - **Descripción:** Ni los controladores ni los servicios inyectan `ILogger`. Las operaciones críticas de creación, actualización, conflictos de horarios o excepciones internas no dejan traza observable en consola o archivo.
  - **Acción Correctiva:** Inyectar `ILogger<T>` en los servicios y middleware de excepciones para registrar eventos de auditoría operativa y errores inesperados.

- [ ] **[DOC-03] Alinear Especificación de Códigos HTTP de Error en CU-01**
  - **Módulo / Archivos:** `Documentos/ID del Caso de Uso CU-01.txt`, Matriz vs Flujo Alternativo
  - **Descripción:** En la tabla de la Matriz de Trazabilidad se indica para CU-01 únicamente *"201 Created / 400 Bad Request"*, omitiendo el código `409 Conflict` que sí está correctamente especificado en el Flujo Alternativo 3a ("Socio ya existente / Duplicado").
  - **Acción Correctiva:** Actualizar la Matriz de Trazabilidad para incluir explícitamente el código `409 Conflict` en CU-01.

