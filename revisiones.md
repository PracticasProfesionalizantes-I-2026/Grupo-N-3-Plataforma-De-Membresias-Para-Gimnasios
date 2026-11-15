# Informe de Revisión Técnica Senior - GYM UP API

**Fecha de revisión:** 27 de octubre de 2026  
**Alcance revisado:** Solución integral **GYM UP** (`GymPlatform.slnx`), que comprende los proyectos de código fuente (`GymPlatform.Shared`, `GymPlatform.DataAccess`, `GymPlatform.BusinessLogic`, `GymPlatform.API`, `GymPlatform.Migrations`), proyectos de pruebas automatizadas (`GymPlatform.BusinessLogic.Tests`, `GymPlatform.API.Tests`), colecciones HTTP en `bruno/`, y documentación asociada (`README.md`, `AGENTS.md`, `Documentos/`, `prototipado.md`).  
**Rol del revisor:** Revisor Técnico Senior (Auditoría de solo lectura y diagnóstico de madurez).

---

## Resumen Ejecutivo

La solución backend **GYM UP** presenta una base arquitectónica sólida construida sobre **.NET 10**, con separación de capas N-Tier bien definida, modelado relacional consistente en Entity Framework Core con SQLite, inicialización de datos reproducible (`DbInitializer`), documentación OpenAPI interactiva vía Scalar y un conjunto de 34 pruebas automatizadas (22 unitarias y 12 de integración) que compilan y pasan exitosamente al 100%.

No obstante, la auditoría técnica identifica brechas significativas en validación perimetral, cobertura de pruebas HTTP en Bruno, gobierno de requerimientos (fechas y DoD ausentes) y una **dualidad arquitectónica** crítica: coexisten controladores RESTful estándar (`/api/socios`, `/api/planes`, `/api/horarios`, etc.) protegidos por JWT junto con nuevos controladores de Casos de Uso (`/api/use-cases/*`) que se encuentran completamente desprotegidos (sin `[Authorize]`), sin pruebas de integración y ausentes tanto en Bruno como en la documentación principal.

### Balance de Cumplimiento (12 Ejes Evaluados)
- **Cumple (✔):** 0 ejes (0%)
- **Parcial (⚠):** 10 ejes (83.3%)
- **No cumple (✘):** 2 ejes (16.7%)

---

## 1. Validación de datos (no aceptar datos sucios)

**Estado:** ⚠ Parcial

**Evidencia:**
- Contratos de entrada definidos como C# Records:
  - `src/GymPlatform.Shared/DTOs/SocioDTOs.cs` (L3-9)
  - `src/GymPlatform.Shared/DTOs/PlanMembresiaDTOs.cs` (L3-8)
  - `src/GymPlatform.Shared/DTOs/HorarioDTOs.cs` (L5-14)
  - `src/GymPlatform.Shared/DTOs/ActividadDTOs.cs` (L3-6)
  - `src/GymPlatform.Shared/UseCases/CU01_RegistrarSocioDTOs.cs` (L5-13)
- Validación ejecutada dentro de la capa de lógica de negocio:
  - `src/GymPlatform.BusinessLogic/Services/SocioService.cs` (L43-70: chequeos de `string.IsNullOrWhiteSpace`, regex y edad)
  - `src/GymPlatform.BusinessLogic/Services/PlanMembresiaService.cs` (L42-49: chequeos de nombre y precio > 0)
  - `src/GymPlatform.BusinessLogic/Services/HorarioService.cs` (L47-68: chequeos de orden de horas y cupo)
  - `src/GymPlatform.BusinessLogic/UseCases/CU01_RegistrarSocio/RegistrarSocioUseCase.cs` (L48-80: validación manual acumulada en lista)

**Qué falta:**
1. **Validación en capa perimetral (DTO/Schema/Filter):** Los DTOs no implementan validación declarativa mediante `System.ComponentModel.DataAnnotations` (ej. `[Required]`, `[EmailAddress]`, `[StringLength]`, `[Range]`) ni existe integración con `FluentValidation`.
2. **Filtrado previo a la lógica de negocio:** Las peticiones con tipos válidos pero datos semánticamente sucios o vacíos viajan intactas a través del Controller hasta instanciar servicios de dominio o casos de uso antes de ser rechazadas.
3. **Manejo uniforme de errores 400 vs 422:** No se utiliza el código HTTP 422 (*Unprocessable Entity*) para errores de validación semántica; se mapean genéricamente a HTTP 400 mediante captura de `ValidationException`.
4. **Saneo contra entradas maliciosas/XSS:** No hay saneo perimetral en campos de texto libre (`Nombre`, `Descripcion`, `Profesor`, `Sala`) más allá de llamadas elementales a `.Trim()` y `.ToLower()`.

**Recomendación:** Incorporar FluentValidation o DataAnnotations en los DTOs de `GymPlatform.Shared` con un `ActionFilter` automático o validación nativa de ASP.NET Core, de modo que el pipeline HTTP descarte cargas inválidas con formato estándar *ProblemDetails* (RFC 7807 / 9457) antes de invocar la lógica de negocio.

---

## 2. Aserciones

**Estado:** ⚠ Parcial

**Evidencia:**
- Aserciones en pruebas unitarias y de integración:
  - `tests/GymPlatform.BusinessLogic.Tests/SocioServiceTests.cs` (L36-40, L51-53)
  - `tests/GymPlatform.BusinessLogic.Tests/HorarioServiceTests.cs` (L45-49, L87-89)
  - `tests/GymPlatform.BusinessLogic.Tests/PlanMembresiaServiceTests.cs` (L31-33, L46-47)
  - `tests/GymPlatform.BusinessLogic.Tests/AuthServiceTests.cs` (L34-39, L49-51)
  - `tests/GymPlatform.API.Tests/EndpointsIntegrationTests.cs` (L39-42, L61-65, L93)
  - `tests/GymPlatform.API.Tests/AuthEndpointsIntegrationTests.cs` (L28-34, L46, L59, L78)
- Peticiones de Bruno:
  - `bruno/Socios/Registrar Socio.bru` (L1-22)
  - `bruno/Planes/Crear Plan.bru` (L1-21)
  - `bruno/Horarios/Registrar Horario.bru` (L1-26)
  - `bruno/Auth/Login Administrador.bru` (L1-19)

**Qué falta:**
1. **Aserciones en Bruno:** Ninguna de las 12 peticiones de Bruno contiene bloques `assert` o scripts `test { ... }` para verificar códigos de estado esperados (`res.status: eq 200/201`), encabezados de respuesta o esquemas de cuerpo.
2. **Verificación de contratos:** Bruno opera actualmente como un catálogo estático de disparadores HTTP manuales, sin capacidad de validación automatizada en pipelines ni ejecución por CLI (`bru run`).

**Recomendación:** Agregar aserciones estándar de Bruno (`assert { res.status: eq 200 }`, `assert { res.body.id: isDefined }`) en cada archivo `.bru` para habilitar pruebas funcionales automatizadas vía Bruno CLI.

---

## 3. Pruebas unitarias e integración

**Estado:** ⚠ Parcial

**Evidencia:**
- Tests Unitarios (22 tests en total):
  - `tests/GymPlatform.BusinessLogic.Tests/SocioServiceTests.cs` (6 tests: alta válida, menor de edad, email inválido, duplicado DNI, duplicado email, id no hallado).
  - `tests/GymPlatform.BusinessLogic.Tests/HorarioServiceTests.cs` (4 tests: alta válida, horarios invertidos, solapamiento profesor, solapamiento sala).
  - `tests/GymPlatform.BusinessLogic.Tests/PlanMembresiaServiceTests.cs` (4 tests: alta válida, precio no positivo, nombre duplicado, eliminación bloqueada por horarios).
  - `tests/GymPlatform.BusinessLogic.Tests/AuthServiceTests.cs` (8 tests: login exitoso, roles admin/entrenador/socio, password erróneo, usuario inexistente, usuario inactivo, hash BCrypt, claims JWT).
- Tests de Integración (12 tests en total):
  - `tests/GymPlatform.API.Tests/EndpointsIntegrationTests.cs` (6 tests: listado socios, alta socio 201, duplicado DNI 409, plan precio negativo 400, alta plan 201, horario solapado 409).
  - `tests/GymPlatform.API.Tests/AuthEndpointsIntegrationTests.cs` (6 tests: login 200 + token, login inválido 401, endpoint protegido sin token 401, rol prohibido 403, rol permitido 200, /api/auth/me 200).
- Ejecución CLI exitosa: `dotnet test` reporta 34/34 tests superados.

**Qué falta:**
1. **Medición y reporte de cobertura de código:** No existe paquete ni configuración de Coverlet (`coverlet.collector`) ni generación de reportes HTML/Cobertura/lcov en la solución.
2. **Cobertura de Casos de Uso y controladores específicos:** No existe ningún test unitario ni de integración para `RegistrarSocioUseCase`, `GestionarPlanesUseCase`, `RenovarMembresiaUseCase`, `GestionarClasesHorariosUseCase`, ni para los controladores en `src/GymPlatform.API/Controllers/UseCases/`.
3. **Desincronización en documentación:** El archivo `README.md` (L63) declara "21 Tests", omitiendo la existencia real de 34 tests.

**Recomendación:** Integrar `coverlet.collector` en los proyectos de test, configurar un umbral mínimo de cobertura (ej. 80%) y agregar pruebas unitarias e integración sobre las clases ubicadas bajo `UseCases/`.

---

## 4. Funciones principales

**Estado:** ⚠ Parcial

**Evidencia:**
- Documentadas en:
  - `Documentos/ID del Caso de Uso CU-01.txt` (Registrar socio)
  - `Documentos/ID del Caso de Uso CU-02.txt` (Gestionar planes de membresía)
  - `Documentos/ID del Caso de Uso CU-03.txt` (Gestionar planes y horarios)
  - `README.md` (L80-166)
- Controladores y servicios tradicionales:
  - `src/GymPlatform.API/Controllers/SociosController.cs`
  - `src/GymPlatform.API/Controllers/PlanesController.cs`
  - `src/GymPlatform.API/Controllers/HorariosController.cs`
  - `src/GymPlatform.API/Controllers/ActividadesController.cs`
  - `src/GymPlatform.API/Controllers/AuthController.cs`
- Controladores y UseCases dedicados:
  - `src/GymPlatform.API/Controllers/UseCases/CU01_RegistrarSocioController.cs`
  - `src/GymPlatform.API/Controllers/UseCases/CU02_GestionarPlanesController.cs`
  - `src/GymPlatform.API/Controllers/UseCases/CU03_RenovarMembresiaController.cs`
  - `src/GymPlatform.API/Controllers/UseCases/CU04_GestionarClasesHorariosController.cs`

**Qué falta:**
1. **Consistencia arquitectónica (Dualidad de Endpoints):** Existen dos rutas alternativas para cada función (ej. `POST /api/socios` vs. `POST /api/use-cases/socios/registrar`). Esta bifurcación confunde al consumidor de la API sobre cuál es el contrato definitivo.
2. **Desfase en la definición de CU-03 y CU-04:** En los archivos de especificación (`Documentos/ID del Caso de Uso CU-03.txt`), CU-03 abarca planes y horarios. En el código fuente, CU-03 es `RenovarMembresia` (con pasarela externa) y CU-04 es `GestionarClasesHorarios`. No existe documentación consolidada de requisitos para la renovación de membresías ni para la pasarela de pagos.
3. **Mapeo incompleto de tests:** Los Casos de Uso CU-01 a CU-04 con sus servicios externos (`IPasarelaPagoService`, `INotificacionService`) carecen de pruebas que verifiquen transacciones y compensaciones ante fallos.

**Recomendación:** Consolidar una única estrategia de exposición de endpoints (o bien REST puro por recurso o bien endpoints RPC por caso de uso) y actualizar la documentación de casos de uso para que refleje CU-01, CU-02, CU-03 (Renovación) y CU-04 (Horarios) de manera unificada.

---

## 5. Actores principales

**Estado:** ⚠ Parcial

**Evidencia:**
- Identificación de Actores del Dominio:
  - `src/GymPlatform.Shared/Enums/RolUsuario.cs` (L1-16: `Administrador`, `Entrenador`, `Recepcionista`, `Socio`)
  - `src/GymPlatform.DataAccess/Entities/Usuario.cs`
  - `Actores cuadros (GYM UP).pdf` y especificaciones en `Documentos/`
- Sistemas Externos:
  - Pasarela de Pago: `src/GymPlatform.BusinessLogic/ExternalServices/IPasarelaPagoService.cs`
  - Notificaciones: `src/GymPlatform.BusinessLogic/ExternalServices/INotificacionService.cs`
- Políticas de Autorización en Controladores Tradicionales:
  - `SociosController.cs` (L12: `[Authorize]`, L23: `Roles = "Administrador,Recepcionista"`, L39: `Roles = "Administrador,Recepcionista,Socio"`)
  - `PlanesController.cs` (L12: `[Authorize]`, L25/39: `[AllowAnonymous]`, L57/80/109: `Roles = "Administrador"`)
  - `HorariosController.cs` (L12: `[Authorize]`, L25: `[AllowAnonymous]`, L40: `Roles = "Administrador,Entrenador"`)
  - `ActividadesController.cs` (L12: `[Authorize]`, L24: `[AllowAnonymous]`, L38: `Roles = "Administrador"`)
- Pruebas de 401 y 403:
  - `tests/GymPlatform.API.Tests/AuthEndpointsIntegrationTests.cs` (L50-79)

**Qué falta:**
1. **Brecha de seguridad crítica en `Controllers/UseCases/`:** Los controladores `CU01_RegistrarSocioController`, `CU02_GestionarPlanesController`, `CU03_RenovarMembresiaController` y `CU04_GestionarClasesHorariosController` **carecen de la anotación `[Authorize]`**. Cualquier usuario anónimo puede invocar la creación de planes, la renovación de membresías y la creación de actividades y horarios sin token JWT.
2. **Cobertura en Bruno:** Las peticiones de Bruno en `bruno/Socios/`, `bruno/Planes/`, etc., tienen `auth: none` y fallan con 401 si no se inyecta manualmente un token. No hay peticiones configuradas para validar escenarios 401 y 403.

**Recomendación:** Añadir `[Authorize]` y los roles correspondientes a todos los controladores de la carpeta `Controllers/UseCases/` para alinearlos con la matriz de seguridad del sistema.

---

## 6. Requerimientos NO funcionales (RNF)

**Estado:** ⚠ Parcial

**Evidencia:**
- Atributos de Calidad Implementados en Código y Configuración:
  - **Seguridad y Criptografía:** Token JWT simétrico (HS256) con validación estricta de emisor, audiencia y vigencia en `src/GymPlatform.API/Program.cs` (L62-85). Hashing de contraseñas con sal mediante `BCrypt.Net` en `src/GymPlatform.BusinessLogic/Services/AuthService.cs` (L151).
  - **Integridad Referencial y Rendimiento de Lectura:** Restricción estricta de borrado (`DeleteBehavior.Restrict`) e índices únicos (`Dni`, `Email`, `Nombre`) en `src/GymPlatform.DataAccess/Context/GymDbContext.cs`. Consultas con `AsNoTracking()` en repositorios de lectura.
  - **Documentación Interactiva:** Scalar UI configurado en `/scalar/v1` en `Program.cs` (L105-110).
  - **Resiliencia ante Fallos Externos:** En `RegistrarSocioUseCase.cs` (L190-210), la falla en el envío del correo de notificación se degrada de forma no fatal, registrando la advertencia sin revertir la transacción del socio.
- Documentos de Requerimientos:
  - `Documentos/# Anexos y Matriz de Trazabilidad -.txt` (menciona códigos HTTP pero no métricas de RNF).

**Qué falta:**
1. **Lista formal de RNF cuantitativos:** No existe un catálogo de RNF con criterios medibles (ej. "Latencia p95 < 250ms con 50 req/s", "Disponibilidad 99.5%", "RTO/RPO de backup de BD", "Límite de carga de payload < 5MB").
2. **RNFs ausentes en la implementación:**
   - **CORS:** No está configurado `UseCors()`; la API rechazará llamadas directas desde navegadores web externos (frontend Angular/React).
   - **Rate Limiting:** No hay middleware de limitación de tasa contra ataques de fuerza bruta en `/api/auth/login`.
   - **Healthchecks:** No existe endpoint `/health` o `/ready` para monitoreo de la API y de la conexión SQLite.
   - **Manejo global de excepciones:** Falta `app.UseExceptionHandler()` o middleware unificado que formatee excepciones no controladas en formato RFC 7807, confiando únicamente en bloques `try-catch` dispersos por controlador.

**Recomendación:** Documentar formalmente la ficha de RNFs con metas cuantitativas e incorporar en `Program.cs` la política de CORS, el middleware de excepciones global (`UseExceptionHandler`), healthchecks nativos de ASP.NET Core (`MapHealthChecks`) y Rate Limiting.

---

## 7. Conocer los requerimientos para defender

**Estado:** ⚠ Parcial

**Evidencia:**
- Especificaciones funcionales existentes:
  - `Documentos/ID del Caso de Uso CU-01.txt`
  - `Documentos/ID del Caso de Uso CU-02.txt`
  - `Documentos/ID del Caso de Uso CU-03.txt`
  - `Documentos/# Anexos y Matriz de Trazabilidad -.txt`
- Matriz resumida en `README.md` (L160-166) que vincula CU-01, CU-02, CU-03 con RN-01, RN-02, RN-03, códigos HTTP y clases de test.

**Qué falta:**
1. **Definición de "Fuera de Alcance" (Out of Scope):** Los documentos no delimitan qué aspectos quedan excluidos de esta fase (ej. hardware biométrico de molinetes, reembolsos automáticos, pasarelas de pago reales distintas de la simulación, portal móvil para socios).
2. **Trazabilidad completa punta a punta:**
   - No hay trazabilidad documentada hacia las peticiones de Bruno.
   - No hay trazabilidad para CU-04 (Gestión de Clases y Horarios) ni para el módulo de Autenticación en los archivos de `Documentos/`.
   - La matriz del README solo cubre 3 casos de uso simplificados y omite los endpoints de `api/use-cases/*`.

**Recomendación:** Elaborar un documento de especificación consolidado de requerimientos del sistema (SRS) que incluya alcance, fuera de alcance, y una matriz de trazabilidad completa que conecte: Requerimiento / CU → Endpoint → Test Unitario → Test de Integración → Petición Bruno.

---

## 8. Bruno — cobertura de todos los casos

**Estado:** ✘ No cumple

**Evidencia:**
- Directorio `bruno/`:
  - `bruno/bruno.json`
  - `bruno/environments/Local.bru` (`baseUrl: http://localhost:5000`)
  - `bruno/Socios/` (2 archivos: `Listar Socios.bru`, `Registrar Socio.bru`)
  - `bruno/Planes/` (2 archivos: `Listar Planes.bru`, `Crear Plan.bru`)
  - `bruno/Horarios/` (2 archivos: `Listar Horarios.bru`, `Registrar Horario.bru`)
  - `bruno/Actividades/` (2 archivos: `Listar Actividades.bru`, `Crear Actividad.bru`)
  - `bruno/Auth/` (4 archivos: `Login Administrador.bru`, `Login Entrenador.bru`, `Login Socio.bru`, `Obtener Perfil (Me).bru`)

**Qué falta:**
1. **Ausencia total de casos de prueba negativos:** No existe ningún archivo `.bru` para verificar:
   - **400 Bad Request:** Payload con datos vacíos o precio <= 0.
   - **401 Unauthorized:** Intento de acceso sin token.
   - **403 Forbidden:** Intento de un Socio de crear un Plan o Actividad.
   - **404 Not Found:** Búsqueda por identificador inexistente.
   - **409 Conflict:** Intento de registro con DNI/Email duplicado o solapamiento de horarios de profesor/sala.
   - **405 Method Not Allowed:** Verbo HTTP no soportado sobre la ruta.
   - **415 Unsupported Media Type:** Envío de payload con `Content-Type` inválido.
   - **500 Internal Server Error:** Simulación de fallo no recuperable.
2. **Encadenamiento dinámico no funcional:** En `Login Administrador.bru` no hay script `post-response` que guarde el JWT en la variable `{{token}}` del entorno (ej. `bru.setEnvVar("token", res.body.token)`).
3. **Incompatibilidad de Auth en colecciones existentes:** Las peticiones de escritura (`Crear Plan.bru`, `Registrar Horario.bru`, `Crear Actividad.bru`, `Registrar Socio.bru`) tienen configurado `auth: none`, por lo que devuelven HTTP 401 si se disparan tal como están guardadas.
4. **Endpoints de UseCases ausentes:** No existe ninguna petición en Bruno para los endpoints `/api/use-cases/*`.

**Recomendación:** Reestructurar la colección de Bruno organizándola por endpoint con subcarpetas para cada código HTTP (Camino Feliz 200/201, Error 400, No Auth 401, Prohibido 403, No Hallado 404, Conflicto 409), agregar scripts de extracción de token en los logins e incorporar aserciones en cada request.

---

## 9. Fechas límite

**Estado:** ✘ No cumple

**Evidencia:**
- Búsqueda en el repositorio: No existen archivos de cronograma, roadmaps ni especificaciones de hitos de entrega.
- Archivos revisados: `README.md`, `Documentos/*`, `prototipado.md`, `AGENTS.md`.

**Qué falta:**
1. **Cronograma e hitos formales:** No hay registro en el repositorio de fechas de inicio, revisiones intermedias, entregas de sprint o fecha límite final de entrega del proyecto.
2. **Definición de Terminado (Definition of Done - DoD):** No se encuentra explícitamente redactado qué condiciones debe cumplir un incremento de software para considerarse completado (ej. código compilando sin advertencias, cobertura > 80%, colección Bruno ejecutada con 100% de aserciones válidas, documentación OpenAPI revisada).

**Recomendación:** Incorporar en la documentación un archivo de gestión del proyecto (ej. `roadmap.md` o sección en `README.md`) con las fechas clave de entrega y la lista formal de criterios de aceptación / Definition of Done.

---

## 10. Casos improbables o no definidos

**Estado:** ⚠ Parcial

**Evidencia:**
- Manejo de Casos Borde en Servicios de Negocio:
  - Validación de edad < 18 en `SocioService.cs` (L59-70).
  - Validación de precio <= 0 y duración <= 0 en `PlanMembresiaService.cs` (L45-48).
  - Validación de hora inicio >= hora fin y cupo <= 0 en `HorarioService.cs` (L47-68).
  - Validación de longitud numérica de DNI (7 a 10 dígitos) en `RegistrarSocioUseCase.cs` (L57-60).
- Comportamientos pasivos del framework:
  - Entity Framework Core parametriza queries por defecto, protegiendo contra inyección SQL directa en repositorios.

**Qué falta:**
1. **Límites máximos de tamaño y desbordamiento:** No hay restricciones de tamaño máximo para payloads entrantes (`[RequestSizeLimit]`) ni longitudes máximas validadas a nivel DTO antes de llegar a la base de datos (un payload con una cadena gigante podría saturar memoria antes de fallar por truncamiento).
2. **Fechas anómalas:** `SocioService.cs` solo valida que el socio sea mayor a 18 años, pero no restringe fechas en el futuro absurdas ni fechas de nacimiento previas a 1900.
3. **Casos no definidos en HTTP (405, 415, JSON malformado):** No existe middleware ni política configurada que estandarice el cuerpo de respuesta ante `405 Method Not Allowed`, `415 Unsupported Media Type` o cuerpos JSON con sintaxis rota; se entrega la respuesta por defecto de ASP.NET Core sin envoltorio común del gimnasio.
4. **Protección contra scripts maliciosos (XSS):** No se aplican filtros de codificación de caracteres o saneo de etiquetas HTML/JavaScript en campos que posteriormente podrían exponerse en interfaces visuales.

**Recomendación:** Implementar validaciones de rango máximo y formato en DTOs, configurar límites de tamaño de solicitud en Kestrel/Controllers, e integrar un formateador estandarizado de respuestas para todos los códigos de error HTTP de la plataforma.

---

## 11. Ubicación en el código (mapa)

**Estado:** ⚠ Parcial

**Evidencia:**
- Estructura documentada en `README.md` (L25-49) y `AGENTS.md` (L5-18).
- Flujo de petición documentado con diagrama Mermaid y fórmula en `README.md` (L7-23):
  $$\text{HTTP Request} \longrightarrow \mathbf{Controller} \longrightarrow \mathbf{Service} \longrightarrow \mathbf{Repository} \longrightarrow \mathbf{DbContext} \ (\text{SQLite})$$
- Proyectos en la solución:
  - `src/GymPlatform.Shared`
  - `src/GymPlatform.DataAccess`
  - `src/GymPlatform.BusinessLogic`
  - `src/GymPlatform.API`
  - `src/GymPlatform.Migrations`
  - `tests/GymPlatform.BusinessLogic.Tests`
  - `tests/GymPlatform.API.Tests`

**Qué falta:**
1. **Mapa de componentes para Casos de Uso:** No está documentado en el README dónde reside la lógica de los nuevos casos de uso (`BusinessLogic/UseCases/CU01_...` a `CU04_...`), sus DTOs asociados (`Shared/UseCases/`), sus controladores (`API/Controllers/UseCases/`), ni los servicios externos simulados (`BusinessLogic/ExternalServices/`).
2. **Explicación del doble patrón de acceso:** Falta documentar la coexistencia y justificación del patrón clásico `Controller -> Service -> Repository` frente al patrón `Controller -> UseCase -> UnitOfWork / ExternalServices`.

**Recomendación:** Actualizar el diagrama y el árbol de carpetas del `README.md` para mapear explícitamente la carpeta `UseCases/`, los servicios externos simulados (`ExternalServices`) y el patrón transaccional `UnitOfWork`.

---

## 12. Interfaces → endpoint

**Estado:** ⚠ Parcial

**Evidencia:**
- Endpoints tradicionales documentados parcialmente en `README.md` (L80-157).
- Especificaciones de Swagger/OpenAPI generadas por Scalar en `/scalar/v1`.
- Tabla preliminar de reglas en `README.md` (L160-166) con 4 columnas, sin detalle de interfaces ni métodos completos.

**Qué falta:**
- En el repositorio no existía la matriz integral consolidada que vincule cada interfaz/caso de uso con su método HTTP, ruta exacta, requerimiento de autenticación, contrato de request, contrato de respuesta exitosa y códigos de error contemplados.

A continuación, se documenta la tabla consolidada completa para que el equipo cuente con el registro exacto:

### Tabla de Interfaces → Endpoint del Sistema GYM UP

| Interfaz / Caso de uso | Endpoint | Método | Auth | Request (Body / Params) | Response OK | Errores Posibles |
| :--- | :--- | :---: | :--- | :--- | :--- | :--- |
| **IAuthService** (Login) | `/api/auth/login` | POST | Anónimo | `LoginRequestDTO` (nombreUsuario, password) | `200 OK` (`LoginResponseDTO`: token, exp, usuario, rol) | `400 Bad Request`, `401 Unauthorized` |
| **IAuthService** (Perfil) | `/api/auth/me` | GET | Bearer JWT | Ninguno (toma Claims del token) | `200 OK` (claims: id, usuario, rol, nombre) | `401 Unauthorized` |
| **ISocioService** (Listar) | `/api/socios` | GET | Bearer (Admin, Recep) | Ninguno | `200 OK` (`IEnumerable<SocioResponseDTO>`) | `401 Unauthorized`, `403 Forbidden`, `500 Internal` |
| **ISocioService** (Obtener) | `/api/socios/{id}` | GET | Bearer (Admin, Recep, Socio) | Route: `id` (Guid) | `200 OK` (`SocioResponseDTO`) | `401`, `403`, `404 Not Found`, `500` |
| **ISocioService / CU-01** (Crear) | `/api/socios` | POST | Bearer (Admin, Recep) | `SocioCreateDTO` (nombre, dni, email, tel, fnac) | `201 Created` (`SocioResponseDTO`) | `400 Bad Request`, `401`, `403`, `409 Conflict` (DNI/Email), `500` |
| **ISocioService** (Actualizar) | `/api/socios/{id}` | PUT | Bearer (Admin, Recep) | Route: `id`, Body: `SocioUpdateDTO` | `200 OK` (`SocioResponseDTO`) | `400`, `401`, `403`, `404`, `409`, `500` |
| **ISocioService** (Eliminar) | `/api/socios/{id}` | DELETE | Bearer (Admin, Recep) | Route: `id` (Guid) | `204 NoContent` | `401`, `403`, `404 Not Found`, `500` |
| **IPlanMembresiaService** (Listar) | `/api/planes` | GET | Anónimo | Ninguno | `200 OK` (`IEnumerable<PlanMembresiaResponseDTO>`) | `500 Internal Server Error` |
| **IPlanMembresiaService** (Obtener) | `/api/planes/{id}` | GET | Anónimo | Route: `id` (Guid) | `200 OK` (`PlanMembresiaResponseDTO`) | `404 Not Found`, `500` |
| **IPlanMembresiaService / CU-02** (Crear) | `/api/planes` | POST | Bearer (Admin) | `PlanMembresiaCreateDTO` (nombre, desc, precio, duracion) | `201 Created` (`PlanMembresiaResponseDTO`) | `400 Bad Request`, `401`, `403`, `409 Conflict` (Nombre), `500` |
| **IPlanMembresiaService** (Actualizar) | `/api/planes/{id}` | PUT | Bearer (Admin) | Route: `id`, Body: `PlanMembresiaUpdateDTO` | `200 OK` (`PlanMembresiaResponseDTO`) | `400`, `401`, `403`, `404`, `409`, `500` |
| **IPlanMembresiaService** (Eliminar) | `/api/planes/{id}` | DELETE | Bearer (Admin) | Route: `id` (Guid) | `204 NoContent` | `401`, `403`, `404`, `409 Conflict` (Horarios vinculados), `500` |
| **IActividadService** (Listar) | `/api/actividades` | GET | Anónimo | Ninguno | `200 OK` (`IEnumerable<ActividadResponseDTO>`) | `500 Internal Server Error` |
| **IActividadService** (Obtener) | `/api/actividades/{id}` | GET | Anónimo | Route: `id` (Guid) | `200 OK` (`ActividadResponseDTO`) | `404 Not Found`, `500` |
| **IActividadService** (Crear) | `/api/actividades` | POST | Bearer (Admin) | `ActividadCreateDTO` (nombre, descripcion) | `201 Created` (`ActividadResponseDTO`) | `400 Bad Request`, `401`, `403`, `409 Conflict` (Nombre), `500` |
| **IHorarioService** (Listar) | `/api/horarios` | GET | Anónimo | Ninguno | `200 OK` (`IEnumerable<HorarioResponseDTO>`) | `500 Internal Server Error` |
| **IHorarioService** (Obtener) | `/api/horarios/{id}` | GET | Anónimo | Route: `id` (Guid) | `200 OK` (`HorarioResponseDTO`) | `404 Not Found`, `500` |
| **IHorarioService / CU-03** (Crear) | `/api/horarios` | POST | Bearer (Admin, Entrenador) | `HorarioCreateDTO` (actividadId, planId, profesor, sala, dia, inicio, fin, cupo) | `201 Created` (`HorarioResponseDTO`) | `400 Bad Request`, `401`, `403`, `404 Not Found`, `409 Conflict` (RN-03 Solapamiento), `500` |
| **IRegistrarSocioUseCase** (CU-01 Avanzado) | `/api/use-cases/socios/registrar` | POST | **Ninguna (Brecha ⚠)** | `RegistrarSocioRequestDTO` (datos socio + plan opcional + metodoPago) | `201 Created` (`SocioRegistradoResultDTO`) | `400 Bad Request`, `409 Conflict`, `500 Internal` |
| **IGestionarPlanesUseCase** (Planes Activos) | `/api/use-cases/planes/activos` | GET | **Ninguna (Brecha ⚠)** | Ninguno | `200 OK` (`IEnumerable<PlanMembresiaResponseDTO>`) | `500 Internal Server Error` |
| **IGestionarPlanesUseCase** (Crear Plan) | `/api/use-cases/planes/crear` | POST | **Ninguna (Brecha ⚠)** | `PlanMembresiaCreateDTO` | `201 Created` (`PlanMembresiaResponseDTO`) | `400 Bad Request`, `409 Conflict`, `500` |
| **IRenovarMembresiaUseCase** (Consultar) | `/api/use-cases/membresias/socio/{socioId}` | GET | **Ninguna (Brecha ⚠)** | Route: `socioId` (Guid) | `200 OK` (`IEnumerable<MembresiaResumenDTO>`) | `404 Not Found`, `500` |
| **IRenovarMembresiaUseCase** (Renovar) | `/api/use-cases/membresias/renovar` | POST | **Ninguna (Brecha ⚠)** | `RenovarMembresiaRequestDTO` (socioId, planId, metodoPago, datosTarjeta) | `200 OK` (`RenovacionMembresiaResultDTO`) | `400 Bad Request`, `404`, `422 Unprocessable` (Tarjeta rechazada), `503 Service Unavailable`, `500` |
| **IGestionarClasesHorariosUseCase** (Crear Horario) | `/api/use-cases/clases/horarios` | POST | **Ninguna (Brecha ⚠)** | `HorarioCreateDTO` | `201 Created` (`HorarioResponseDTO`) | `400 Bad Request`, `404`, `409 Conflict` (Solapamiento), `500` |

---

## Resumen Final de la Revisión

- **Total de puntos Cumple (✔):** 0 / 12
- **Total de puntos Parciales (⚠):** 10 / 12 (Validación, Aserciones, Pruebas, Funciones, Actores, RNF, Trazabilidad, Casos Bordes, Mapa de Código, Interfaces)
- **Total de puntos No cumple (✘):** 2 / 12 (Bruno sin casos de prueba/aserciones, Fechas límite y DoD ausentes)

---

## Bloqueantes Priorizados (Hallazgos Críticos)

1. **[Seguridad / Autorización - ✘ Crítico] Desprotección total de la capa de UseCases (`Controllers/UseCases`):**
   Los controladores `CU01_RegistrarSocioController`, `CU02_GestionarPlanesController`, `CU03_RenovarMembresiaController` y `CU04_GestionarClasesHorariosController` no cuentan con el atributo `[Authorize]`. Cualquier cliente no autenticado puede ejecutar altas de socios, cobros transaccionales, creaciones de planes y asignaciones horarias.
2. **[Automatización / QA - ✘ Crítico] Colección Bruno sin cobertura de errores ni aserciones:**
   La colección actual en `bruno/` no cuenta con scripts de prueba ni aserciones, y no contempla caminos de error (400, 401, 403, 404, 409). Las peticiones tienen `auth: none`, imposibilitando su ejecución autónoma en pipelines de integración continua.
3. **[Arquitectura de Entrada - ⚠ Alto] Ausencia de validación en la capa perimetral:**
   Los DTOs carecen de esquemas declarativos (DataAnnotations o FluentValidation). La validación ocurre de forma tardía en la lógica de negocio, consumiendo recursos y dificultando el formato uniforme de errores RFC 7807 (*ProblemDetails*).
4. **[Gobierno del Proyecto - ✘ Medio] Falta de Cronograma, Hitos y Definition of Done (DoD):**
   No existe documentación que determine los hitos de entrega, fechas límite de sprint ni las condiciones que debe cumplir un entregable para considerarse terminado.
5. **[Consistencia de Dominio - ⚠ Medio] Bifurcación y divergencia de endpoints y casos de uso:**
   Coexisten dos conjuntos de controladores (tradicionales vs. casos de uso) y existe una divergencia de nombres entre las especificaciones iniciales (`CU-03 Gestionar planes y horarios` en `Documentos/`) y la implementación de código (`CU-03 RenovarMembresia` y `CU-04 GestionarClasesHorarios`).

---

**Fecha de finalización de revisión:** 27 de octubre de 2026  
**Firma:** Antigravity Senior Technical Reviewer
