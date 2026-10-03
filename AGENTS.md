# AGENTS.md - Contexto Operativo para Agentes de IA

## Propósito del Proyecto
GYM UP es una API RESTful backend construida con **.NET 10** para la gestión integral de membresías de gimnasios, planificación de actividades y administración de horarios de profesores y salas.

## Arquitectura y Capas (N-Tier)
El flujo arquitectónico estricto es:
`Controller (GymPlatform.API)` -> `Service (GymPlatform.BusinessLogic)` -> `Repository (GymPlatform.DataAccess)` -> `DbContext (GymPlatform.DataAccess / EF Core SQLite)`

- **`src/GymPlatform.Shared`**: Contiene DTOs de entrada y salida (`<Entidad>CreateDTO`, `<Entidad>UpdateDTO`, `<Entidad>ResponseDTO`) y la jerarquía de excepciones tipadas (`GymException`, `ValidationException`, `NotFoundException`, `ConflictException`, `DuplicateResourceException`, `ScheduleConflictException`, `DependencyConflictException`).
- **`src/GymPlatform.DataAccess`**: Define las entidades (`Socio`, `PlanMembresia`, `Actividad`, `Horario`), el `GymDbContext` con `DeleteBehavior.Restrict`, repositorios con `AsNoTracking()` y el sembrado inicial en `DbInitializer`.
- **`src/GymPlatform.BusinessLogic`**: Contiene las interfaces y servicios de negocio. Toda la validación vive aquí; no se exponen entidades de base de datos ni se usa AutoMapper (se usan métodos de mapeo privados `MapToResponseDTO`).
- **`src/GymPlatform.API`**: Controladores RESTful con manejo explícito de excepciones vía bloques `try-catch`, OpenAPI y Scalar UI (`/scalar/v1`).
- **`tests/GymPlatform.BusinessLogic.Tests`**: Pruebas unitarias de servicios con `xUnit` y `Moq` (sin conexión a base de datos).
- **`tests/GymPlatform.API.Tests`**: Pruebas de integración de endpoints con `WebApplicationFactory` y SQLite en memoria.
- **`bruno/`**: Colección de requests HTTP para Bruno.

## Comandos CLI Clave
- **Compilar**: `dotnet build`
- **Ejecutar todos los tests**: `dotnet test`
- **Ejecutar API**: `dotnet run --project src/GymPlatform.API/GymPlatform.API.csproj`
- **Documentación interactiva**: Acceder a `/scalar/v1` en entorno Development.

## Reglas de Negocio Implementadas
1. **RN-01 (Socios - CU-01)**: Requiere mayoría de edad ($\ge 18$), formato estricto de email y validación de unicidad de DNI y Email (lanza `DuplicateResourceException` -> 409).
2. **RN-02 (Planes de Membresía - CU-02)**: Precios y duración en días mayores a cero. Nombre de plan único. Restricción de eliminación si tiene horarios asociados (`DependencyConflictException` -> 409).
3. **RN-03 (Horarios - CU-03)**: `HoraInicio` debe ser menor a `HoraFin`. Cupo mayor a cero. Validación de no solapamiento temporal de horarios para el mismo profesor o en la misma sala en un mismo día (`ScheduleConflictException` -> 409).
