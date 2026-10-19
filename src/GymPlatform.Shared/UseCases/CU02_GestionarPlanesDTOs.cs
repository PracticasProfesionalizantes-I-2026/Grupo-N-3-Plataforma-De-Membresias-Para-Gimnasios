namespace GymPlatform.Shared.UseCases;

public record PlanConEstadisticasDTO(
    Guid Id,
    string Nombre,
    string Descripcion,
    decimal Precio,
    int DuracionDias,
    bool Activo,
    int MembresiasActivas,
    int HorariosAsignados
);
