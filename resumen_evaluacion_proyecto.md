# Resumen de Evaluación Integral del Proyecto: GYM UP API

**Destinatario:** Equipo de Desarrollo / Alumnos  
**Rol Evaluador:** Arquitecto de Software Senior, Analista Funcional y Auditor QA  
**Fecha:** Noviembre 2026  
**Tecnología:** .NET 10 / C# / EF Core SQLite / RESTful Web API  

---

## 1. Visión General del Proyecto y Madurez del SDLC

La solución presentada (**GYM UP**) implementa un backend RESTful con arquitectura en capas (**N-Tier**) enfocado en la administración de socios, catálogo de planes de membresía, actividades y planificación de horarios sin superposición de recursos.

Desde la perspectiva del **Ciclo de Vida de Desarrollo de Software (SDLC)**, el equipo evidencia un entendimiento notable del paradigma de separación de responsabilidades y modularidad en .NET:
- La solución se encuentra prolijamente segmentada en proyectos dedicados (`Shared`, `DataAccess`, `BusinessLogic`, `API`, `Migrations`, `Tests`).
- Se observa un esfuerzo concreto por formalizar el análisis previo mediante especificaciones de Casos de Uso (**CU-01**, **CU-02**, **CU-03**) y una Matriz de Trazabilidad documental.
- El proyecto compila limpiamente, cuenta con suites de pruebas (unitarias con `xUnit`/`Moq` e integración con `WebApplicationFactory`), e incluye una colección organizada de solicitudes en **Bruno** para pruebas dinámicas.

No obstante, la auditoría integral identificó **brechas críticas de seguridad y consistencia funcional** entre la especificación documental y la implementación técnica, principalmente la ausencia total del subsistema de autenticación y autorización JWT, divergencias en la nomenclatura de reglas de negocio, y la omisión de entidades core que vinculen a los socios con los planes y turnos del gimnasio.

---

## 2. Fortalezas Identificadas

### Arquitectura y Diseño de Software
* **Separación Estricta de Capas:** Se respeta el flujo de control unidireccional `Controller -> Service -> Repository -> DbContext`. Los controladores web no interactúan con entidades de base de datos ni con `DbContext`, delegando toda la orquestación en la capa de lógica de negocio.
* **Aislamiento de Dominio mediante DTOs:** No se exponen entidades de persistencia hacia el exterior. Se emplean records inmutables (`*CreateDTO`, `*UpdateDTO`, `*ResponseDTO`) y métodos de mapeo privados bien delimitados.
* **Jerarquía de Excepciones de Dominio:** Excelente diseño de excepciones tipadas (`NotFoundException`, `ValidationException`, `ConflictException`, `DuplicateResourceException`, `ScheduleConflictException`, `DependencyConflictException`) que desacoplan las reglas de negocio del protocolo HTTP.
* **Integridad Referencial en Persistencia:** Configuración adecuada de `DeleteBehavior.Restrict` en las claves foráneas de `Horario` hacia `Actividad` y `PlanMembresia` en `GymDbContext`, complementada con índices únicos a nivel de base de datos (`Dni`, `Email`, `Nombre`).

### Implementación y Buenas Prácticas RESTful
* **Semántica de Verbos y URIs:** Rutas en minúsculas, pluralizadas y basadas en sustantivos (`/api/socios`, `/api/planes`, `/api/horarios`). Uso estricto de restricciones de ruta GUID (`{id:guid}`).
* **Header `Location` en Creaciones:** Cumplimiento riguroso del estándar HTTP al retornar `201 Created` junto con el encabezado `Location` apuntando a la URI del recurso mediante `CreatedAtAction(nameof(GetById), ...)`.
* **Algoritmo de Solapamiento:** Correcta implementación de la condición de intervalos temporales abiertos (`inicio1 < fin2 && fin1 > inicio2`) para evaluar conflictos de agenda de profesores y salas en un mismo día.
* **Ecosistema de Pruebas y Herramientas:** Presencia de pruebas unitarias que aíslan dependencias vía Mocks y pruebas de integración con SQLite en memoria, además de documentación viva con **Scalar API Reference** (`/scalar/v1`).

---

## 3. Análisis de Brechas (Gap Analysis)

### A. Documentación vs. Casos de Uso (Fase de Diseño)
1. **Inconsistencia en la Matriz de Trazabilidad de Reglas de Negocio:**
   - En el archivo `# Anexos y Matriz de Trazabilidad -.txt`, se listan reglas de negocio numeradas de **RN-01 a RN-06** asociadas a los casos de uso:
     - CU-01: RN-01, RN-02
     - CU-02: RN-03, RN-04
     - CU-03: RN-05, RN-06
   - Sin embargo, en las especificaciones individuales (`ID del Caso de Uso CU-01.txt`, `CU-02.txt`, `CU-03.txt`), en el `README.md` y en el código fuente, la numeración se comprime a **RN-01, RN-02 y RN-03**. Esto genera desalineación documental entre la matriz de control y las especificaciones detalladas.
2. **Precondiciones No Funcionales (NFR) No Respaldadas:**
   - Los casos de uso CU-01, CU-02 y CU-03 declaran como precondición formal: *"El usuario operador debe tener una sesión activa con permisos de [Recepcionista / Administrador / Entrenador] y un Token JWT válido"*. No existe documento de arquitectura de seguridad, diseño de login, ni especificación de gestión de identidades que sustente este requerimiento.

### B. Diseño vs. Código Fuente (Fase de Implementación)
1. **Requerimiento Crítico Omitido: Seguridad y Autenticación JWT:**
   - La API carece de infraestructura de seguridad: no se encuentra registrado el middleware de autenticación (`app.UseAuthentication()`), no se incluye el paquete `Microsoft.AspNetCore.Authentication.JwtBearer`, y ningún endpoint posee el atributo `[Authorize]`. Todos los recursos (incluso mutaciones y eliminaciones destructivas) se encuentran expuestos de manera anónima y pública.
2. **Ausencia de Modelo de Negocio para Contratación y Reservas (Scope Under-delivery):**
   - El caso de uso CU-01 indica en su postcondición que el socio queda habilitado para *"contratar un plan de membresía"*, y el CU-03 especifica que el socio podrá *"reservar turnos según su plan contratado"*. Sin embargo, el modelo de datos no contempla entidades ni endpoints para `Membresia`, `SocioPlan` o `ReservaTurno`. El sistema permite crear socios, planes y horarios de forma aislada, pero no existe trazabilidad relacional entre un socio y los servicios que consume.
3. **Manejo Incompleto del Ciclo de Vida (Soft Delete vs Hard Delete):**
   - CU-02 establece explícitamente el requisito de *"desactivar planes"*. En las entidades `Socio` y `PlanMembresia` existe el campo booleano `Activo`, pero los endpoints `DELETE` ejecutan una eliminación física permanente (`_context.Remove`). No existen endpoints específicos ni soporte de negocio para bajas lógicas (Soft Delete), y los métodos `GET` no filtran registros inactivos.
4. **Scope Creep / Código Fuera de Casos de Uso:**
   - Se diseñó e implementó un módulo completo de **Actividades** (`Actividad`, `ActividadRepository`, `ActividadService`, `ActividadesController`, endpoints `/api/actividades`) con su propia regla de unicidad de nombres. No obstante, no existe un documento formal de Caso de Uso (ej. `CU-04 Gestionar Actividades`) en la carpeta `Documentos/`.
5. **Brecha de Integridad al Asignar Horarios:**
   - En CU-03, `HorarioService.CreateAsync` valida la existencia del `PlanMembresiaId`, pero omite verificar si el plan se encuentra activo (`plan.Activo == true`), permitiendo calendarizar clases vinculadas a planes discontinuados.

---

## 4. Conclusión de Arquitectura y Calidad de Código

### Evaluación de Clean Code y Principios SOLID
* **Buenas Prácticas:** El código es limpio, legible y sigue convenciones de nomenclatura idiomáticas de C# (.NET 10). La separación de capas es nítida y los DTOs evitan la fuga de modelos anémicos o entidades de base de datos a la capa HTTP.
* **Oportunidades de Refactorización:**
  1. **Boilerplate en Controladores (Violación DRY):** Los cuatro controladores repiten bloques `try-catch` idénticos con 4 a 5 cláusulas para mapear excepciones a respuestas HTTP. Se debe migrar a un middleware centralizado (`IExceptionHandler` de ASP.NET Core) que además estandarice las respuestas de error bajo el estándar **RFC 7807 (ProblemDetails)**.
  2. **Validación Declarativa en DTOs:** Toda la validación sintáctica (campos obligatorios, longitudes máximas, regex de email) está escrita de manera procedural en los servicios. Es altamente recomendable adoptar Data Annotations (`[Required]`, `[MaxLength]`, `[EmailAddress]`) o **FluentValidation** para aprovechar el filtro automático de modelo de ASP.NET Core.
  3. **Inconsistencia en Control de Dependencias:** Mientras que `PlanMembresiaService.DeleteAsync` chequea preventivamente dependencias y devuelve un `DependencyConflictException` (409), `ActividadService.DeleteAsync` no lo hace, provocando un error no capturado `DbUpdateException` (HTTP 500) cuando existen horarios asociados.
  4. **Paginación y Observabilidad:** Los métodos de consulta carecen de paginación (`page`, `pageSize`), lo que introduce riesgo de denegación de servicio por agotamiento de memoria ante grandes volúmenes de datos. Asimismo, no se implementó logging estructurado (`ILogger<T>`).

### Veredicto Final
El proyecto demuestra una base técnica sólida y una estructura arquitectónica muy profesional. Corrigiendo las brechas de seguridad (JWT/RBAC), unificando la documentación con la codificación y resolviendo los aspectos de deuda técnica detallados en el backlog adjunto, la solución alcanzará un estándar plenamente productivo.

